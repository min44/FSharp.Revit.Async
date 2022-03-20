namespace BimGen.StructuredCabling.Revit.Async

open System.Threading.Tasks
open System.Runtime.CompilerServices


type DefaultResultHandler<'TResult>(taskCompletionSource: TaskCompletionSource<'TResult>) =
    member x.TaskCompletionSource = taskCompletionSource
    member x.SetResult(result) = x.TaskCompletionSource.TrySetResult(result);
    member x.Cancel() = x.TaskCompletionSource.TrySetCanceled();
    member x.ThrowException(ex: exn) = x.TaskCompletionSource.TrySetException(ex)


[<Extension>]
type ExternalEventResultHandlerExtensions =
    [<Extension>]
    static member Wait (resultHandler: DefaultResultHandler<'TResult>, func) =
        try resultHandler.SetResult(func());
        with ex -> resultHandler.ThrowException(ex)
