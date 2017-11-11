module Nikeza.Server.Integration

open System
open System.IO
open FsUnit
open NUnit.Framework
open TestAPI
open Command
open Store
open Sql
open Read
open Model
open Literals
open Platforms

[<TearDownAttribute>]
let teardown() = cleanDataStore()

[<Test>]
let ``Get profile image from StackOverflow`` () =

    StackOverflow |> getThumbnail stackoverflowUserId
                  |> should equal someStackoverflowImage

[<Test>]
let ``Get profile image from YouTube`` () =

    YouTube |> getThumbnail youtubeUserId
            |> should equal someYoutubeImage

[<Test>]
let ``Get profile image from WordPress`` () =

    WordPress |> getThumbnail wordpressUserId
              |> should equal someWordpressImage

[<Test>]
let ``Get profile image from Medium`` () =

    Medium |> getThumbnail mediumUserId
           |> should equal someMediumImage

[<Test>]
let ``Load links from Medium`` () =

    // Setup
    let profileId = Register someProfile |> execute
    let source =  { someSource with AccessId=  mediumUserId
                                    ProfileId= profileId
                                    Platform=  Medium |> PlatformToString }
    // Test
    AddSource { source with ProfileId= unbox profileId } |> execute |> ignore
    let links = source.ProfileId |> Store.linksFrom source.Platform |> List.toSeq

    // Verify
    links |> Seq.length |> should (be greaterThan) 0

[<Test>]
let ``Load links from WordPress`` () =

    // Setup
    let profileId = Register someProfile |> execute
    let source =  { someSource with AccessId=  wordpressUserId
                                    ProfileId= profileId
                                    Platform=  WordPress |> PlatformToString }
    // Test
    AddSource { source with ProfileId= unbox profileId } |> execute |> ignore
    let links = source.ProfileId |> Store.linksFrom source.Platform |> List.toSeq

    // Verify
    links |> Seq.length |> should (be greaterThan) 0

[<Test>]
let ``Load links from StackOverflow`` () =

    // Setup
    let profileId = Register someProfile |> execute
    let source =  { someSource with AccessId= stackoverflowUserId
                                    ProfileId= profileId
                                    Platform=  StackOverflow |> PlatformToString }
    // Test
    AddSource { source with ProfileId= unbox profileId } |> execute |> ignore
    let links = source.ProfileId |> Store.linksFrom source.Platform |> List.toSeq

    // Verify
    links |> Seq.length |> should (be greaterThan) 0

[<Test>]
let ``Load links from YouTube`` () =

    // Setup
    let profileId = Register someProfile |> execute
    let source =  { someSource with AccessId=  File.ReadAllText(ChannelIdFile)
                                    ProfileId= profileId
                                    Platform=  YouTube |> PlatformToString }
    // Test
    AddSource { source with ProfileId= unbox profileId } |> execute |> ignore
    let links = source.ProfileId |> Store.linksFrom source.Platform |> List.toSeq

    // Verify
    links |> Seq.length |> should (be greaterThan) 0

[<Test>]
let ``Read YouTube APIKey file`` () =
    let text = File.ReadAllText(KeyFile_YouTube)
    text.Length |> should (be greaterThan) 0

[<Test>]
let ``Read YouTube AccessId file`` () =
    let text = File.ReadAllText(ChannelIdFile)
    text.Length |> should (be greaterThan) 0

[<Test>]
let ``Subscriber observes provider link`` () =

    // Setup
    let profileId =    Register someProfile    |> execute
    let subscriberId = Register someSubscriber |> execute

    Follow { FollowRequest.ProfileId= profileId
             FollowRequest.SubscriberId= subscriberId 
           } |> execute |> ignore

    let linkId = AddLink  { someLink with ProfileId = profileId } |> execute

    // Test
    let link = { SubscriberId= subscriberId; LinkIds=[Int32.Parse(linkId)] }
    let linkObservedIds = ObserveLinks link |> execute

    // Verify
    linkObservedIds |> should equal linkId

[<Test>]
let ``No recent links after subscriber observes new link`` () =

    // Setup
    let profileId =    Register someProfile    |> execute
    let subscriberId = Register someSubscriber |> execute

    Follow { FollowRequest.ProfileId= profileId
             FollowRequest.SubscriberId= subscriberId 
           } |> execute |> ignore

    let linkId = AddLink  { someLink with ProfileId = profileId } |> execute
    let link = { SubscriberId= subscriberId; LinkIds=[Int32.Parse(linkId)] }

    ObserveLinks link |> execute |> ignore

    // Test
    let recentLinks = subscriberId |> getRecent

    // Verify
    recentLinks |> List.isEmpty |> should equal true

[<Test>]
let ``Follow Provider`` () =

    // Setup
    let profileId =    Register someProfile    |> execute
    let subscriberId = Register someSubscriber |> execute

    // Test
    Follow { FollowRequest.ProfileId= profileId
             FollowRequest.SubscriberId= subscriberId 
           } |> execute |> ignore

    // Verify
    let sql = @"SELECT SubscriberId, ProfileId
                FROM   Subscription
                WHERE  SubscriberId = @SubscriberId
                AND    ProfileId =    @ProfileId"

    let (connection,command) = createCommand sql connectionString

    try
        connection.Open()
        command.Parameters.AddWithValue("@SubscriberId", subscriberId) |> ignore
        command.Parameters.AddWithValue("@ProfileId",   profileId)   |> ignore

        use reader = command |> prepareReader
        let entryAdded = reader.GetInt32(0) = Int32.Parse (subscriberId) && 
                         reader.GetInt32(1) = Int32.Parse (profileId)

        entryAdded |> should equal true

    // Teardown
    finally dispose connection command


[<Test>]
let ``Unsubscribe from Provider`` () =

    // Setup
    let profileId =    Register someProfile    |> execute 
    let subscriberId = Register someSubscriber |> execute

    execute ( Follow { FollowRequest.ProfileId= profileId; FollowRequest.SubscriberId= subscriberId }) |> ignore

    // Test
    execute ( Unsubscribe { UnsubscribeRequest.SubscriberId= subscriberId; UnsubscribeRequest.ProfileId= profileId }) |> ignore

    // Verify
    let sql = @"SELECT SubscriberId, ProfileId
                FROM   Subscription
                WHERE  SubscriberId = @SubscriberId
                AND    ProfileId =   @ProfileId"

    let (connection,command) = createCommand sql connectionString

    try
        connection.Open()
        command.Parameters.AddWithValue("@SubscriberId", subscriberId) |> ignore
        command.Parameters.AddWithValue("@ProfileId",   profileId)   |> ignore

        use reader = command.ExecuteReader()
        reader.Read() |> should equal false

    // Teardown
    finally dispose connection command

[<Test>]
let ``Add featured link`` () =

    //Setup
    Register someProfile |> execute |> ignore

    let lastId = AddLink  someLink |> execute
    let data = { LinkId=Int32.Parse(lastId); IsFeatured=true }

    // Test
    FeatureLink data |> execute |> ignore

    // Verify
    let sql = @"SELECT Id, IsFeatured
                FROM   Link
                WHERE  Id  = @id
                AND    IsFeatured = @isFeatured"

    let (connection,command) = createCommand sql connectionString

    try
        connection.Open()
        command.Parameters.AddWithValue("@id",         data.LinkId)     |> ignore
        command.Parameters.AddWithValue("@isFeatured", data.IsFeatured) |> ignore

        use reader = command |> prepareReader
        let isFeatured = reader.GetBoolean(1)
        isFeatured |> should equal true

    // Teardown
    finally dispose connection command

[<Test>]
let ``Adding link results in new topics added to database`` () =

    //Setup
    let profileId = Register someProfile |> execute

    // Test
    AddLink { someLink with Topics= [someProviderTopic]; ProfileId= profileId } |> execute |> ignore

    // Verify
    match getTopic someTopic.Name with
    | Some topic -> topic.Name |> should equal someTopic.Name
    | None       -> Assert.Fail()

[<Test>]
let ``Remove link`` () =

    //Setup
    Register someProfile |> execute |> ignore
    let linkId = AddLink someLink |> execute
    
    // Test
    RemoveLink { LinkId = Int32.Parse(linkId) } |> execute |> ignore

    // Verify
    let sql = @"SELECT Id, IsFeatured
                FROM   Link
                WHERE  Id = @id"

    let (connection,command) = createCommand sql connectionString

    try
        connection.Open()
        command.Parameters.AddWithValue("@id", linkId) |> ignore

        use reader = command.ExecuteReader()
        reader.Read() |> should equal false

    // Teardown
    finally dispose connection command

[<Test>]
let ``Unfeature Link`` () =
    
    //Setup
    Register someProfile |> execute |> ignore

    let linkId = AddLink  someLink |> execute
    let data = { LinkId=Int32.Parse(linkId); IsFeatured=false }

    // Test
    FeatureLink data |> execute |> ignore

    // Verify
    let sql = @"SELECT Id, IsFeatured
                FROM   Link
                WHERE  Id  = @id
                AND    IsFeatured = @isFeatured"

    let (connection,command) = createCommand sql connectionString
    try
        connection.Open()
        command.Parameters.AddWithValue("@id",         data.LinkId)     |> ignore
        command.Parameters.AddWithValue("@isFeatured", data.IsFeatured) |> ignore

        use reader = command |> prepareReader
        let isFeatured = reader.GetBoolean(1)
        isFeatured |> should equal false

    // Teardown
    finally dispose connection command

[<Test>]
let ``Register Profile`` () =

    // Setup
    let data = { someProfile with FirstName="Scott"; LastName="Nimrod" }

    // Test
    Register data |> execute |> ignore

    // Verify
    let sql = @"SELECT FirstName, LastName
                FROM   Profile
                WHERE  FirstName = @FirstName
                AND    LastName  = @LastName"

    let (connection,command) = createCommand sql connectionString
    try
        connection.Open()
        command.Parameters.AddWithValue("@FirstName", data.FirstName) |> ignore
        command.Parameters.AddWithValue("@LastName",  data.LastName)  |> ignore
        
        use reader = command |> prepareReader
        let isRegistered = (data.FirstName, data.LastName) = (reader.GetString(0), reader.GetString(1))
        isRegistered |> should equal true

    // Teardown
    finally dispose connection command

[<Test>]
let ``Update profile`` () =
    
    // Setup
    let modifiedName = "MODIFIED_NAME"
    let data = { someProfile with FirstName="Scott"; LastName="Nimrod" }
    let lastId = Register data |> execute
    // Test
    UpdateProfile { ProfileId =  unbox lastId
                    FirstName =  data.FirstName
                    LastName =   modifiedName
                    Bio =        data.Bio
                    Email =      data.Email
                    ImageUrl=    data.ImageUrl
                    Sources =    data.Sources } |> execute |> ignore
    // Verify
    let sql = @"SELECT LastName FROM [dbo].[Profile] WHERE  Id = @Id"
    let (readConnection,readCommand) = createCommand sql connectionString
    try readConnection.Open()
        readCommand.Parameters.AddWithValue("@Id", lastId) |> ignore
        
        use reader = readCommand |> prepareReader
        reader.GetString(0) = modifiedName |> should equal true

    // Teardown
    finally dispose readConnection readCommand

[<Test>]
let ``Get links of provider`` () =

    //Setup
    let profileId = Register someProfile |> execute
    AddLink { someLink with ProfileId= unbox profileId } |> execute |> ignore
    
    // Test
    let links = profileId |> getLinks

    // Verify
    let linkFound = links |> Seq.head
    linkFound.ProfileId  |> should equal profileId

[<Test>]
let ``Get followers`` () =

    // Setup
    let profileId =    Register someProfile    |> execute
    let subscriberId = Register someSubscriber |> execute
    
    Follow { FollowRequest.ProfileId=   profileId
             FollowRequest.SubscriberId= subscriberId } |> execute |> ignore

    // Test
    let follower = profileId |> getFollowers |> List.head
    
    // Verify
    follower.ProfileId |> should equal subscriberId

[<Test>]
let ``Get subscriptions`` () =

    // Setup
    let profileId =    Register someProfile   |> execute
    let subscriberId = Register someSubscriber |> execute

    Follow { FollowRequest.ProfileId=   profileId
             FollowRequest.SubscriberId= subscriberId } |> execute |> ignore

    // Test
    let subscription = subscriberId |> getSubscriptions |> List.head
    
    // Verify
    subscription.ProfileId |> should equal profileId

[<Test>]
let ``Get profiles`` () =

    // Setup
    Register { someProfile with FirstName= "profile1" } |> execute |> ignore
    Register { someProfile with FirstName= "profile2" } |> execute |> ignore

    // Test
    let profiles = getAllProfiles()
    
    // Verify
    profiles |> List.length |> should equal 2

[<Test>]
let ``Get profile`` () =

    Register someProfile 
    |> execute
    |> getProfile
    |> function | Some _ -> ()
                | None   -> Assert.Fail()

[<Test>]
let ``Get platforms`` () =

    getPlatforms() 
    |> List.isEmpty 
    |> should equal false

[<Test>]
let ``Adding data source results in links saved`` () =

    // Setup
    let profileId = Register someProfile |> execute
    let source =  { someSource with AccessId= File.ReadAllText(ChannelIdFile) }
    AddSource { source with ProfileId= unbox profileId } |> execute |> ignore

    // Test
    let links = Store.linksFrom source.Platform profileId

    // Verify
    links |> List.isEmpty |> should equal false

[<Test>]
let ``Add data source`` () =

    // Setup
    let profileId = Register someProfile |> execute
    let source =  { someSource with AccessId= File.ReadAllText(ChannelIdFile) }

    // Test
    let sourceId = AddSource { source with ProfileId= unbox profileId } |> execute

    // Verify
    let sql = @"SELECT Id FROM [dbo].[Source] WHERE Id = @id"
    let (connection,command) = createCommand sql connectionString

    try connection.Open()
        command.Parameters.AddWithValue("@id", sourceId) |> ignore

        use reader = command.ExecuteReader()
        reader.Read() |> should equal true

    // Teardown
    finally dispose connection command

[<Test>]
let ``Adding data source results in links with topics found`` () =

    //Setup
    let profileId = Register someProfile |> execute

    // Test
    let sourceId = AddSource { someSource with ProfileId= unbox profileId } |> execute

    // Verify
    getSource sourceId |> function
    | Some source -> source.Links |> Seq.forall(fun l -> l.Topics |> List.isEmpty |> not) |> should equal false
    | None        -> Assert.Fail()
    
[<Test>]
let ``Get sources`` () =

    //Setup
    let profileId = execute <| Register someProfile
    AddSource { someSource with ProfileId = unbox profileId } |> execute |> ignore

    // Test
    let sources = profileId |> getSources
    
    // Verify
    sources |> List.isEmpty |> should equal false

[<Test>]
let ``Remove source`` () =

    //Setup
    let profileId = execute <| Register someProfile
    let sourceId =  AddSource { someSource with ProfileId= unbox profileId } |> execute
    
    // Test
    RemoveSource { Id = Int32.Parse(sourceId) } |> execute |> ignore

    // Verify
    let sql = @"SELECT Id FROM [dbo].[Source] WHERE  Id  = @id"
    let (connection,command) = createCommand sql connectionString

    try connection.Open()
        command.Parameters.AddWithValue("@id", sourceId) |> ignore

        use reader = command.ExecuteReader()
        reader.Read() |> should equal false

    // Teardown
    finally dispose connection command


[<Test>]
let ``Add featured topic`` () =

    //Setup
    let profileId = Register someProfile |> execute

    let link = { someLink with Topics= [someProviderTopic]; ProfileId= profileId }
    AddLink link |> execute |> ignore

    let topic = getTopic link.Topics.Head.Name
    let request = { Name=topic.Value.Name; ProfileId=profileId; TopicId= topic.Value.Id; IsFeatured=true }

    // Test
    let featuredTopicId = FeatureTopic request |> execute

    // Verify
    Int32.Parse(featuredTopicId) |> should (be greaterThan) 0

[<Test>]
let ``Remove featured topic`` () =

    //Setup
    let profileId = Register someProfile |> execute

    let link = { someLink with Topics= [someProviderTopic]; ProfileId= profileId }
    AddLink link |> execute |> ignore

    let topic = getTopic link.Topics.Head.Name
    let request = { Name= topic.Value.Name; ProfileId=profileId; TopicId= topic.Value.Id; IsFeatured=true }
    FeatureTopic request |> execute |> ignore

    // Test
    UnfeatureTopic request |> execute |> ignore

    // Verify
    let featuredTopics = getFeaturedTopics profileId
    featuredTopics |> List.isEmpty |> should equal true

[<Test>]
let ``5 or less provider topics become featured topics`` () =

    //Setup
    let profileId = Register someProfile |> execute
    let link =    { someLink with Topics= [someProviderTopic]; ProfileId=profileId }

    // Test
    AddLink link |> execute |> ignore

    // Verify
    let featuredTopics = getFeaturedTopics profileId
    featuredTopics |> List.isEmpty |> should equal false

[<Test>]
let ``Fetching provider includes their featured topics`` () =

    //Setup
    let profileId = Register someProfile |> execute
    let link =    { someLink with Topics= [someProviderTopic]; ProfileId=profileId |> string }

    // Test
    AddLink link |> execute |> ignore

    // Verify
    let provider = getProviders().[0]
    provider.Topics |> List.isEmpty |> should equal false


[<Test>]
let ``Logging into portal retrieves portfolio`` () =

    //Setup
    let profileId = Register someProfile |> execute
    let link =    { someLink with Topics= [someProviderTopic]; ProfileId= profileId }
    AddLink link |> execute |> ignore

    // Test
    match login someProfile.Email with
          | Some provider -> if provider.Portfolio |> isEmpty
                                then Assert.Fail()
                                else ()
          | None          -> Assert.Fail()

[<EntryPoint>]
let main argv =
    cleanDataStore()                      
    ``Add data source`` ()
    0