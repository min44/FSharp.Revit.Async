module private BimGen.Revit.Async.Extensions

open System.Threading
open System.Threading.Tasks
open System.Runtime.CompilerServices
open Logger
open ResultHandlers

[<Extension>]
type TaskCompletionSourceExtension =
    [<Extension>]
    static member Await (tcs: TaskCompletionSource<'TResult>, unlockKey: Task<'TSource>, onComplete) =
            let func (task: Task<'TSource>) =
                try
                    match task with
                    | t when t.IsCompleted -> onComplete task.Result tcs
                    | t when t.IsFaulted ->
                        if t.Exception |> (isNull >> not) then
                            tcs.TrySetException(t.Exception) |> ignore
                    | t when t.IsCanceled -> tcs.TrySetCanceled() |> ignore
                    | _ -> Logger.Debug("Error: Await top unknown Exception") 
                finally ()
                
            unlockKey.ContinueWith(
                func,
                CancellationToken.None,
                TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default) |> ignore
            tcs

    [<Extension>]
    static member AwaitDown (tcs: TaskCompletionSource<'TResult>, creatingTask, enqueue) =
        async {
            try
                let! result = creatingTask |> Async.AwaitTask
                match creatingTask with
                | t when t.IsCompleted -> tcs.TrySetResult(result) |> ignore
                | t when t.IsCanceled -> tcs.TrySetCanceled() |> ignore
                | t when t.IsFaulted ->
                    if t.Exception |> (isNull >> not) then Logger.Debug(t.Exception)
                    tcs.TrySetException(t.Exception) |> ignore
                | _ -> Logger.Debug("Error: Await down unknown Exception")
            finally enqueue() }


[<Extension>]
type ExternalEventResultHandlerExtension =
    [<Extension>]
    static member Wait (resultHandler: DefaultResultHandler<'TResult>, func) =
        try resultHandler.SetResult(func());
        with ex -> resultHandler.ThrowException(ex)