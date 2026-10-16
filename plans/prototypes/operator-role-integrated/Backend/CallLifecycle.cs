using System.Runtime.ExceptionServices;

namespace HEAL.HeuristicLib.Execution;

public interface ICallHooks<TToken>
{
    TToken Enter();
    void Success(TToken token, int candidateWork);
    void Fault(TToken token, Exception failure);
    void Finally(TToken token);
}

// One invocation-local token and failure ledger; never shared between simultaneous or nested calls.
public struct CallFrame<THooks, TToken> where THooks : struct, ICallHooks<TToken>
{
    private THooks hooks;
    private readonly TToken token;
    private Exception? childFailure;
    private Exception? callbackFailure;

    public CallFrame(THooks hooks)
    {
        this.hooks = hooks;
        token = this.hooks.Enter();
    }

    public void Success(int candidateWork)
    {
        try { hooks.Success(token, candidateWork); }
        catch (Exception failure) { callbackFailure = failure; }
    }

    public void Fault(Exception failure)
    {
        childFailure = failure;
        try { hooks.Fault(token, failure); }
        catch (Exception callback) { callbackFailure = callback; }
    }

    public void Finally()
    {
        Exception? cleanupFailure = null;
        try { hooks.Finally(token); }
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
