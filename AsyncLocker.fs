namespace BimGen.StructuredCabling.Revit.Async

open System
open System.Runtime.CompilerServices
open System.Threading
open System.Threading.Tasks


[<IsReadOnly; Struct>]
type UnlockKey(locker: AsyncLocker) =
    member x.Locker = locker
    member x.Dispose() = x.Locker.Release()
    
    interface IDisposable with
        member x.Dispose() = x.Dispose()

and AsyncLocker() =
    let semaphoreSlim = new SemaphoreSlim(1, 1)
    member x.Semaphore = semaphoreSlim
    member x.LockAsync() : Task<UnlockKey> =
        let waitTask = x.Semaphore.WaitAsync()
        waitTask.ContinueWith(
            (fun task -> new UnlockKey(x)),
            CancellationToken.None,
            TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default)
    member x.Release() = x.Semaphore.Release() |> ignore