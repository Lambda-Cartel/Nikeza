﻿module Tests.Portal

open FsUnit
open NUnit.Framework
open Nikeza.Mobile.TestAPI
open Nikeza.Mobile.UILogic
open Nikeza.Mobile.UILogic.Portal
open Nikeza.Mobile.UILogic.Pages
open Nikeza.Mobile.AppLogic
open System.Linq

[<Test>]
let ``Initialize viewmodel`` () =

    // Setup
    let portal = ViewModel(Portal.dependencies someUser)
    
    // Test
    portal.Init()

    // Verify
    portal.Subscriptions.Any() |> should equal true

[<Test>]
let ``Provide placeholders if less than 3 links`` () =

    // Setup
    let portal = ViewModel(Portal.dependencies someUser)
    
    // Test
    portal.Init()

    // Verify
    portal.Subscriptions.Count |> should equal 3


[<Test>]
let ``Navigate: Portal to Members`` () =

    // Setup
    let mutable pageRequested = false
    let sideEffect = function PageRequested.Members _ -> pageRequested <- true | _ -> ()

    let dependencies' =  Portal.dependencies someUser
    let sideEffects =  { dependencies'.SideEffects with ForPageRequested=[sideEffect] }
    let dependencies = { dependencies'             with SideEffects= sideEffects }

    let portal = ViewModel(dependencies)

    // Test
    portal.ViewMembers.Execute()
    
    // Verify
    pageRequested |> should equal true

[<Test>]
let ``Navigate: Portal to Latest`` () =

    // Setup
    let mutable pageRequested = false
    let sideEffect = function PageRequested.Latest _ -> pageRequested <- true | _ -> ()

    let dependencies' =  Portal.dependencies someUser
    let sideEffects =  { dependencies'.SideEffects with ForPageRequested=[sideEffect] }
    let dependencies = { dependencies'             with SideEffects= sideEffects }

    let portal = ViewModel(dependencies)

    // Test
    portal.ViewLatest.Execute()
    
    // Verify
    pageRequested |> should equal true

[<Test>]
let ``Navigate: Portal to Followers`` () =

    // Setup
    let mutable pageRequested = false
    let sideEffect = function PageRequested.Followers _ -> pageRequested <- true | _ -> ()

    let dependencies' =  Portal.dependencies someUser
    let sideEffects =  { dependencies'.SideEffects with ForPageRequested=[sideEffect] }
    let dependencies = { dependencies'             with SideEffects= sideEffects }

    let portal = ViewModel(dependencies)

    // Test
    portal.ViewFollowers.Execute()
    
    // Verify
    pageRequested |> should equal true

[<Test>]
let ``Navigate: Portal to Subscriptions`` () =

    // Setup
    let mutable pageRequested = false
    let sideEffect = function PageRequested.Subscriptions _ -> pageRequested <- true | _ -> ()

    let dependencies' =  Portal.dependencies someUser
    let sideEffects =  { dependencies'.SideEffects with ForPageRequested=[sideEffect] }
    let dependencies = { dependencies'             with SideEffects= sideEffects }

    let portal = ViewModel(dependencies)

    // Test
    portal.ViewSubscriptions.Execute()
    
    // Verify
    pageRequested |> should equal true