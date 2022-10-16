module private BimGen.Revit.Async.ResultHandlers

open System.Threading.Tasks

type DefaultResultHandler<'TResult>(taskCompletionSource: TaskCompletionSource<'TResult>) =
    member x.TaskCompletionSource = taskCompletionSource
    member x.SetResult(result) = x.TaskCompletionSource.TrySetResult(result);
    member x.Cancel() = x.TaskCompletionSource.TrySetCanceled();
    member x.ThrowException(ex: exn) = x.TaskCompletionSource.TrySetException(ex)
