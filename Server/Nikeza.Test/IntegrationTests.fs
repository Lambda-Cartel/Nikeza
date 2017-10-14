module Integration

open System
open FsUnit
open NUnit.Framework
open Nikeza.TestAPI
open Nikeza.Server.Command
open Nikeza.Server.Store
open Nikeza.Server.Sql
open Nikeza.Server.Read
open Nikeza.Server.Model

[<TearDownAttribute>]
let teardown() = cleanDataStore()

[<Test>]
let ``Follow Provider`` () =

    // Setup
    Register someProvider |> execute |> ignore
    let providerId =   getLastId "Profile" |> string
    
    Register someSubscriber |> execute |> ignore
    let subscriberId = getLastId "Profile" |> string

    // Test
    Follow { FollowRequest.ProviderId= providerId
             FollowRequest.SubscriberId= subscriberId 
           } |> execute |> ignore

    // Verify
    let sql = @"SELECT SubscriberId, ProviderId
                FROM   Subscription
                WHERE  SubscriberId = @SubscriberId
                AND    ProviderId =   @ProviderId"

    let (connection,command) = createCommand sql connectionString

    try
        connection.Open()
        command.Parameters.AddWithValue("@SubscriberId", subscriberId) |> ignore
        command.Parameters.AddWithValue("@ProviderId",   providerId)   |> ignore

        use reader = command |> prepareReader
        let entryAdded = reader.GetInt32(0) = Int32.Parse (subscriberId) && 
                         reader.GetInt32(1) = Int32.Parse (providerId)

        entryAdded |> should equal true

    // Teardown
    finally dispose connection command


[<Test>]
let ``Unsubscribe from Provider`` () =

    // Setup
    let providerId =   execute (Register someProvider)
    let subscriberId = execute (Register someSubscriber)

    execute ( Follow { FollowRequest.ProviderId= providerId; FollowRequest.SubscriberId= subscriberId }) |> ignore

    // Test
    execute ( Unsubscribe { UnsubscribeRequest.SubscriberId= subscriberId; UnsubscribeRequest.ProviderId= providerId }) |> ignore

    // Verify
    let sql = @"SELECT SubscriberId, ProviderId
                FROM   Subscription
                WHERE  SubscriberId = @SubscriberId
                AND    ProviderId =   @ProviderId"

    let (connection,command) = createCommand sql connectionString

    try
        connection.Open()
        command.Parameters.AddWithValue("@SubscriberId", subscriberId) |> ignore
        command.Parameters.AddWithValue("@ProviderId",   providerId)   |> ignore

        use reader = command.ExecuteReader()
        reader.Read() |> should equal false

    // Teardown
    finally dispose connection command

[<Test>]
let ``Add featured link`` () =

    //Setup
    Register someProvider |> execute |> ignore
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
let ``Remove link`` () =

    //Setup
    Register someProvider |> execute |> ignore
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
    Register someProvider |> execute |> ignore
    AddLink  someLink     |> execute |> ignore

    let data = { LinkId=getLastId "Link"; IsFeatured=false }

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
    let data = { someProvider with FirstName="Scott"; LastName="Nimrod" }

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
    let data = { someProvider with FirstName="Scott"; LastName="Nimrod" }
    let lastId = Register data |> execute
    // Test
    UpdateProfile { ProfileId =  unbox lastId
                    FirstName =  data.FirstName
                    LastName =   modifiedName
                    Bio =        data.Bio
                    Email =      data.Email
                    ImageUrl=    data.ImageUrl } |> execute |> ignore
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
    let providerId = Register someProvider |> execute
    AddLink  { someLink with ProviderId= unbox providerId } |> execute |> ignore
    
    // Test
    let links = providerId |> getLinks

    // Verify
    let linkFound = links |> Seq.head
    linkFound.ProviderId  |> should equal providerId

[<Test>]
let ``Get followers`` () =

    // Setup
    let providerId =   Register someProvider   |> execute
    let subscriberId = Register someSubscriber |> execute
    

    Follow { FollowRequest.ProviderId=   providerId
             FollowRequest.SubscriberId= subscriberId } |> execute |> ignore

    // Test
    let follower = providerId |> getFollowers |> List.head
    
    // Verify
    follower.ProfileId |> should equal subscriberId

[<Test>]
let ``Get subscriptions`` () =

    // Setup
    let providerId =   Register someProvider   |> execute
    let subscriberId = Register someSubscriber |> execute

    Follow { FollowRequest.ProviderId=   providerId
             FollowRequest.SubscriberId= subscriberId } |> execute |> ignore

    // Test
    let subscription = subscriberId |> getSubscriptions |> List.head
    
    // Verify
    subscription.ProfileId |> should equal providerId

[<Test>]
let ``Get providers`` () =

    // Setup
    Register { someProvider with FirstName= "Provider1" } |> execute |> ignore
    Register { someProvider with FirstName= "Provider2" } |> execute |> ignore

    // Test
    let providers = getProviders()
    
    // Verify
    providers |> List.length |> should equal 2

[<Test>]
let ``Get provider`` () =

    // Setup
    Register someProvider 
    |> execute
    |> getProvider
    |> function | Some p -> ()
                | None   -> Assert.Fail()

[<Test>]
let ``Get platforms`` () =

    getPlatforms() |> List.isEmpty |> should equal false

[<Test>]
let ``Add source`` () =

    //Setup
    let providerId = execute <| Register someProvider

    // Test
    let sourceId = AddSource { someSource with ProfileId= unbox providerId } |> execute

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
let ``Get sources`` () =

    //Setup
    let providerId = execute <| Register someProvider
    AddSource { someSource with ProfileId = unbox providerId } |> execute |> ignore

    // Test
    let sources = providerId |> getSources
    
    // Verify
    sources |> List.isEmpty |> should equal false

[<Test>]
let ``Remove source`` () =

    //Setup
    let providerId = execute <| Register someProvider
    
    let sourceId = AddSource { someSource with ProfileId= unbox providerId } |> execute
    
    // Test
    RemoveSource { SourceId = Int32.Parse(sourceId) } |> execute |> ignore

    // Verify
    let sql = @"SELECT Id FROM [dbo].[Source] WHERE  Id  = @id"
    let (connection,command) = createCommand sql connectionString

    try connection.Open()
        command.Parameters.AddWithValue("@id", sourceId) |> ignore

        use reader = command.ExecuteReader()
        reader.Read() |> should equal false

    // Teardown
    finally dispose connection command

[<EntryPoint>]
let main argv =
    cleanDataStore()                      
    ``Add source`` ()
    0