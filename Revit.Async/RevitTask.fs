module Revit.Async.RevitTask

open Autodesk.Revit.UI
open FutureExternalEvent
   
let RevitCommandAwait (inputFunction: UIApplication -> 'TResult) =
    RunAsync<'TResult> inputFunction
    |> Async.AwaitTask

let RevitCommandRun (inputFunction: UIApplication -> 'TResult) =
    RunAsync<'TResult> inputFunction
    |> Async.AwaitTask
    |> Async.RunSynchronously

let InitializeRevitAsync app = Initialize app