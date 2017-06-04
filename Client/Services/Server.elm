module Services.Server exposing (..)

import Controls.Login as Login exposing (Model)
import Domain.Core exposing (..)


tryLogin : Login.Model -> Login.Model
tryLogin credentials =
    -- TODO - Replace tbelow his with integration code...
    let
        successful =
            String.toLower credentials.username == "test" && String.toLower credentials.password == "test"
    in
        if successful then
            { username = credentials.username, password = credentials.password, loggedIn = True }
        else
            { username = credentials.username, password = credentials.password, loggedIn = False }


topicUrl : Id -> Topic -> Url
topicUrl id topic =
    Url "http://google.com"


latestPosts : Id -> ContentType -> List Post
latestPosts id contentType =
    []


contributorUrl : Id -> Url
contributorUrl id =
    Url "../Domain/Contributor.elm"


recentContributors : List Profile
recentContributors =
    []


getContributor : Id -> Maybe Profile
getContributor id =
    Nothing


articles : Id -> List Article
articles profileId =
    []


videos : Id -> List Video
videos profileId =
    []


podcasts : Id -> List Podcast
podcasts profileId =
    []