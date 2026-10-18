namespace HEAL.HeuristicLib.Execution;

/// <summary>Takes a function that invokes a child operator and returns a function adding behavior such as retry, timing or counting.</summary>
/// <remarks>
/// An operation adapter calls Wrap once when constructing an executor. The returned function is reused for
/// its invocations; TArguments contains the operation's inputs, usually as a value tuple. The supplied function
/// calls an already resolved child. Keep child-bound functions in the constructed executor, not in this wrapper.
/// The optional candidate-work function supplies the operation's counting rule. Invoke it only when needed;
/// null means candidate counting is unsupported. It does not determine whether the call can be split into items.
/// Implementations wrap invocations during executor construction and are not persisted configuration strategies.
/// </remarks>
public interface IInvocationWrapper
{
    Func<TArguments, TResult> Wrap<TArguments, TResult>(Func<TArguments, TResult> invokeChild,
        Func<TArguments, TResult, int>? countCandidateWork);
}
