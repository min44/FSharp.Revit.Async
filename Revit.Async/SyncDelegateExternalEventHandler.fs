namespace Revit.Async

open System
open System.Threading.Tasks
open Autodesk.Revit.UI


type GenericExternalEventHandler<'TResult>() =
    let guid = Guid.NewGuid()
    let mutable _resultHandler: DefaultResultHandler<'TResult> option = None 
    let mutable _funcDelegate: (UIApplication -> 'TResult) option = None
    
    member x.ResultHandler
        with get() = _resultHandler.Value
        and set v = _resultHandler <- Some v
        
    member x.FuncDelegate
        with get() = _funcDelegate.Value
        and set v = _funcDelegate <- Some v
        
    member x.Prepare(funcDelegate) =
        let tcs = new TaskCompletionSource<'TResult>()
        x.FuncDelegate <- funcDelegate
        x.ResultHandler <- DefaultResultHandler(tcs)
        tcs.Task
        
    member x.Id = guid
    member x.GetName() = $"GenericExternalEventHandler-{x.Id}"
    member x.Execute(app) = x.ResultHandler.Wait(fun () -> x.FuncDelegate app) |> ignore
    
    interface IExternalEventHandler with
        member x.Execute(app) = x.Execute(app)
        member x.GetName() = x.GetName()