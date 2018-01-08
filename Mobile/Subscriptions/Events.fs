module Nikeza.Mobile.Subscriptions.Events

open Nikeza.DataTransfer
open Nikeza.Common

type NotificationEvent =
    | LinksDiscovered        of Provider

    | SubscriberAdded        of ProfileId
    | SubscriberAddFailed    of ProfileId
    
    | SubscriberRemoved      of ProfileId
    | SubscriberRemoveFailed of ProfileId

type QueryEvent =
    | GetRecentSucceeded  of Provider list
    | GetLatestFailed     of string
                              
    | GetMembersSucceeded of Provider list
    | GetMembersFailed    of string

    | GetSubscriptionsSucceeded of Provider list
    | GetSubscriptionsFailed    of string