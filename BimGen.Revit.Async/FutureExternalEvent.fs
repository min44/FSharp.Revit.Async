module private BimGen.Revit.Async.FutureExternalEvent

open System
open System.Collections.Concurrent
open System.Threading.Tasks
open Autodesk.Revit.UI
open Autodesk.Revit.UI.Events
open Operators
open AsyncLocker
open ExternalEventHandler
open Extensions

let mutable HasInitialized = false

let Locker = AsyncLocker()

let UnlockKeys = ConcurrentQueue<UnlockKey>()

let ExternalEventCreatorHandler = ExternalEventHandler<ExternalEvent>()

let ExternalEventCreatorExternalEvent = ExternalEvent.Create(ExternalEventCreatorHandler)

let RaiseAccepted(externalEvent: ExternalEvent) =
    let request = externalEvent.Raise()
    request = ExternalEventRequest.Accepted 
    
let AppIdling _ _ =
    let res, unlockKey = UnlockKeys.TryDequeue()
    while res do unlockKey.Dispose()

let Initialize (app: UIApplication) =
    if not HasInitialized then
        app.Idling.AddHandler(EventHandler<IdlingEventArgs>(AppIdling))
        HasInitialized <- true

let CreateExternalEventTask (handler: ExternalEventHandler<'TResult>) =
    let onComplete unlockKey (tcs: TaskCompletionSource<ExternalEvent>) =
        let enqueue() = UnlockKeys.Enqueue unlockKey
        let eventCreatingFunction _ = ExternalEvent.Create handler
        let creatingTask =
            let task = ExternalEventCreatorHandler.Prepare eventCreatingFunction
            let raiseResult = ExternalEventCreatorExternalEvent.Raise()
            if raiseResult = ExternalEventRequest.Accepted then task
            else task
        tcs.AwaitDown(creatingTask, enqueue) |> Async.RunSynchronously
    let unlockKey = Locker.LockAsync()
    let tcs = TaskCompletionSource<ExternalEvent>().Await(unlockKey, onComplete)
    tcs.Task

let RunAsync<'TResult> inputFunction =
    task {
        let handler = ExternalEventHandler<'TResult>()
        let task = handler.Prepare inputFunction
        use! externalEvent = CreateExternalEventTask handler
        if externalEvent |> RaiseAccepted then return! task
        else return! task }