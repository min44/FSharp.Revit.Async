module private FSharp.Revit.Async.AsyncLocker

open System
open System.Threading
open System.Threading.Tasks
open System.Runtime.CompilerServices


[<IsReadOnly; Struct>]
type UnlockKey(locker: AsyncLocker) =
    member x.Dispose() = locker.Release()
    
    interface IDisposable with
        member x.Dispose() = x.Dispose()

and AsyncLocker() =
    member val Semaphore = new SemaphoreSlim(1, 1)
    member x.LockAsync(): Task<UnlockKey> =
        let waitTask = x.Semaphore.WaitAsync()
        let func _   = new UnlockKey(x)
        waitTask.ContinueWith(
            func,
            CancellationToken.None,
            TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default)
    member x.Release() = x.Semaphore.Release() |> ignore