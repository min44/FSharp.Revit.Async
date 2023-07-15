module FSharp.Revit.Async.RevitTask

open FutureExternalEvent


let Initialize = Initialize
let Reset      = Reset
let RevitCommandAwait cmd = cmd |> RunAsync |> Async.AwaitTask