using System.Runtime.ExceptionServices;

namespace HEAL.HeuristicLib.Execution;

/// <summary>Defines callbacks for starting an operation call, reporting its outcome, and finishing cleanup.</summary>
/// <remarks>
/// The starting callback returns data, such as a timestamp, that later callbacks receive for the same call.
/// If starting throws, neither the operation nor the remaining callbacks run. After starting succeeds, cleanup runs exactly once.
/// A success callback failure is not reported as an operation failure. Multiple failures aggregate in operation, callback, cleanup order.
/// </remarks>
public interface IOperationCallCallbacks<TCallData>
{
    TCallData OnStarting();
    void OnSucceeded(TCallData callData, int candidateWork);
    void OnFailed(TCallData callData, Exception failure);
    void OnFinished(TCallData callData);
}

/// <summary>
/// Runs callbacks for one operation call. Keeps the data returned at the start and preserves failures from the
/// operation, callbacks and cleanup.
/// </summary>
/// <remarks>
/// Construct before entering the adapter's try/finally. Report operation failure and rethrow it in the inner catch;
/// call NotifySucceeded outside that catch and Finish in the outer finally. Do not share or copy an active notifier.
/// </remarks>
public struct OperationCallNotifier<TCallbacks, TCallData> where TCallbacks : struct, IOperationCallCallbacks<TCallData>
{
    private TCallbacks callbacks;
    private readonly TCallData callData;
    private Exception? childFailure;
    private Exception? callbackFailure;

    public OperationCallNotifier(TCallbacks callbacks)
    {
        this.callbacks = callbacks;
        callData = this.callbacks.OnStarting();
    }

    public void NotifySucceeded(int candidateWork)
    {
        try
        { callbacks.OnSucceeded(callData, candidateWork); }
        catch (Exception failure) { callbackFailure = failure; }
    }

    public void NotifyFailed(Exception failure)
    {
        childFailure = failure;
        try
        { callbacks.OnFailed(callData, failure); }
        catch (Exception callback) { callbackFailure = callback; }
    }

    public void Finish()
    {
        Exception? cleanupFailure = null;
        try
        { callbacks.OnFinished(callData); }
        catch (Exception failure) { cleanupFailure = failure; }
        if (callbackFailure is null && cleanupFailure is null)
            return; // The adapter's original throw preserves a lone child failure and its stack.
        var failures = new List<Exception>(3);
        if (childFailure is not null)
            failures.Add(childFailure);
        if (callbackFailure is not null)
            failures.Add(callbackFailure);
        if (cleanupFailure is not null)
            failures.Add(cleanupFailure);
        if (failures.Count == 1)
            ExceptionDispatchInfo.Capture(failures[0]).Throw();
        throw new AggregateException(failures);
    }
}
