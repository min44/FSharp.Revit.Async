module private FSharp.Revit.Async.ExternalEventHandler

open System
open System.Threading.Tasks
open Autodesk.Revit.UI


type ExternalEventHandler<'TResult>() =
    let mutable _cmd: (UIApplication -> 'TResult) option = None
    member val Tcs = new TaskCompletionSource<'TResult>() with get, set
    member val Id  = Guid.NewGuid()
    member x.Prepare cmd =
        _cmd <- Some cmd
        let tcs = new TaskCompletionSource<'TResult>()
        x.Tcs <- tcs
        tcs.Task
    member x.Execute uiApp =
        try _cmd.Value uiApp |> x.Tcs.TrySetResult    |> ignore
        with e -> e          |> x.Tcs.TrySetException |> ignore
    member x.GetName() = $"ExternalEventHandler-{x.Id}"
    interface IExternalEventHandler with
        member x.Execute app = x.Execute app
        member x.GetName() = x.GetName()