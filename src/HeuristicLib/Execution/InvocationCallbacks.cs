using System.Runtime.ExceptionServices;

namespace HEAL.HeuristicLib.Execution;

/// <summary>Defines callbacks for starting an operation invocation, reporting its outcome, and finishing cleanup.</summary>
/// <remarks>
/// The starting callback returns data, such as a timestamp, that later callbacks receive for the same call.
/// If starting throws, neither the operation nor the remaining callbacks run. After starting succeeds, cleanup runs exactly once.
/// A success callback failure is not reported as an operation failure. Multiple failures aggregate in operation, callback, cleanup order.
/// </remarks>
public interface IInvocationCallbacks<TInvocationData>
{
    TInvocationData OnStarting();
    void OnSucceeded(TInvocationData invocationData);
    void OnFailed(TInvocationData invocationData, Exception failure);
    void OnFinished(TInvocationData invocationData);
}

/// <summary>
/// Notifies callbacks when an invocation starts, succeeds or fails, and finishes. Keeps callback data and preserves
/// exceptions from the invocation, callbacks and cleanup.
/// </summary>
/// <remarks>
/// Construct before entering the wrapper's try/finally. Report operation failure and rethrow it in the inner catch;
/// call NotifySucceeded outside that catch and Finish in the outer finally. Do not share or copy an active notifier.
/// </remarks>
public struct InvocationNotifier<TCallbacks, TInvocationData> where TCallbacks : struct, IInvocationCallbacks<TInvocationData>
{
    private TCallbacks callbacks;
    private readonly TInvocationData invocationData;
    private Exception? childFailure;
    private Exception? callbackFailure;

    public InvocationNotifier(TCallbacks callbacks)
    {
        this.callbacks = callbacks;
        invocationData = this.callbacks.OnStarting();
    }

    public void NotifySucceeded()
    {
        try
        { callbacks.OnSucceeded(invocationData); }
        catch (Exception failure) { callbackFailure = failure; }
    }

    public void NotifyFailed(Exception failure)
    {
        childFailure = failure;
        try
        { callbacks.OnFailed(invocationData, failure); }
        catch (Exception callback) { callbackFailure = callback; }
    }

    public void Finish()
    {
        Exception? cleanupFailure = null;
        try
        { callbacks.OnFinished(invocationData); }
        catch (Exception failure) { cleanupFailure = failure; }
        if (callbackFailure is null && cleanupFailure is null)
            return; // The wrapper's original throw preserves a lone child failure and its stack.
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

/// <summary>Runs start, success, failure and cleanup callbacks around a child operation invocation.</summary>
/// <remarks>Callbacks are optional convenience behavior above call wrapping. Candidate counting is separate.</remarks>
public sealed class CallbackInvocationWrapper<TCallbacks, TInvocationData>(TCallbacks callbacks)
    : IInvocationWrapper where TCallbacks : struct, IInvocationCallbacks<TInvocationData>
{
    public Func<TArguments, TResult> Wrap<TArguments, TResult>(Func<TArguments, TResult> invokeChild,
        Func<TArguments, TResult, int>? countCandidateWork) => arguments =>
    {
        var notifier = new InvocationNotifier<TCallbacks, TInvocationData>(callbacks);
        try
        {
            TResult result;
            try
            { result = invokeChild(arguments); }
            catch (Exception failure)
            {
                notifier.NotifyFailed(failure);
                throw;
            }
            notifier.NotifySucceeded();
            return result;
        }
        finally { notifier.Finish(); }
    };
}
