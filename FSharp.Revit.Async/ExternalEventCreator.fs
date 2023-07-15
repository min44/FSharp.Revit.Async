module private FSharp.Revit.Async.FutureExternalEvent

open System
open System.Threading
open System.Threading.Tasks
open System.Collections.Concurrent
open Autodesk.Revit.UI
open Autodesk.Revit.UI.Events
open Operators
open AsyncLocker
open ExternalEventHandler


let Locker = AsyncLocker()
let UnlockKeys = ConcurrentQueue<UnlockKey>()

let AppIdling _ _ =
    let res, unlockKey = UnlockKeys.TryDequeue()
    while res do unlockKey.Dispose()

let mutable IdlingEventHandler = None
let mutable ExternalEventCreatorHandler = ExternalEventHandler()
let mutable ExternalEventCreatorExternalEvent = ExternalEvent.Create ExternalEventCreatorHandler

let Reset (app: UIApplication) =
    if IdlingEventHandler.IsSome then
        app.Idling.RemoveHandler(IdlingEventHandler.Value)
        ExternalEventCreatorHandler       <- ExternalEventHandler<ExternalEvent>()
        ExternalEventCreatorExternalEvent <- ExternalEvent.Create ExternalEventCreatorHandler

let Initialize (app: UIApplication) =
    if IdlingEventHandler.IsNone then
        let handler = EventHandler<IdlingEventArgs>(AppIdling)
        IdlingEventHandler <- Some handler
        app.Idling.AddHandler(IdlingEventHandler.Value)

let OnComplete handler (tcs: TaskCompletionSource<ExternalEvent>) (task: Task<UnlockKey>) = 
    try
    let extenalEventTask =
        let func _ = ExternalEvent.Create handler
        let task   = ExternalEventCreatorHandler.Prepare func
        ExternalEventCreatorExternalEvent.Raise() |> ignore
        task
    let externalEvent =
        extenalEventTask
        |> Async.AwaitTask
        |> Async.RunSynchronously
    match extenalEventTask with
    | t when t.IsCompleted -> tcs.TrySetResult externalEvent  |> ignore
    | t when t.IsCanceled  -> tcs.TrySetCanceled ()           |> ignore
    | t when t.IsFaulted   -> tcs.TrySetException t.Exception |> ignore
    | t                    -> tcs.TrySetException t.Exception |> ignore
    finally UnlockKeys.Enqueue task.Result

let OnGetLock handler tcs (task: Task<UnlockKey>) =
    try
    match task with
    | t when t.IsCompleted -> OnComplete handler tcs task
    | t when t.IsCanceled  -> tcs.TrySetCanceled ()           |> ignore
    | t when t.IsFaulted   -> tcs.TrySetException t.Exception |> ignore
    | t                    -> tcs.TrySetException t.Exception |> ignore
    with ex                -> tcs.TrySetException ex          |> ignore
    
let CreateExternalEventTask handler =
    let tcs = TaskCompletionSource<ExternalEvent>()
    Locker
        .LockAsync()
        .ContinueWith(
            OnGetLock handler tcs,
            CancellationToken.None,
            TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default)
    |> ignore
    tcs.Task

let RunAsync cmd =
    task {
    let handler = ExternalEventHandler()
    let task = handler.Prepare cmd
    use! externalEvent = CreateExternalEventTask handler
    externalEvent.Raise() |> ignore
    return! task }