using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Execution;

namespace HEAL.HeuristicLib.Operators;

/// <summary>Identifies an operation, such as mutation or evaluation, and supplies what resolution and wrappers need of it.</summary>
/// <remarks>
/// An operation implements these members explicitly. It recognizes the executor interface its operators implement for
/// the run types, wraps such an executor so that each call passes through an <see cref="IInvocationWrapper"/>, and
/// states whether its calls report candidate work. Resolution and wrappers reach them through <see cref="OperationInfo"/>.
/// </remarks>
public interface IOperation<TSelf> where TSelf : IOperation<TSelf>
{
    /// <summary>Returns whether the executor implements this operation's executor interface for the run types.</summary>
    static abstract bool IsExecutor<TCandidate, TSearchState>(IExecutor executor)
        where TSearchState : class, ISearchState;

    /// <summary>Returns an executor whose named method passes each call, as an invocation, to the wrapper.</summary>
    /// <remarks>
    /// The executor passes <see cref="IsExecutor{TCandidate, TSearchState}"/>. Derive the
    /// returned executor from <see cref="WrappingExecutor{TExecutor, TWrapper}"/>, which forwards its controls.
    /// </remarks>
    static abstract IExecutor Wrap<TCandidate, TSearchState, TWrapper>(IExecutor executor, TWrapper wrapper)
        where TSearchState : class, ISearchState
        where TWrapper : struct, IInvocationWrapper;

    /// <summary>Whether the operation's invocations report candidate work.</summary>
    /// <remarks>
    /// The default is false. An operation with candidate work returns true and implements
    /// <see cref="IInvocation{TResult}.CountCandidateWork"/> on its invocations.
    /// </remarks>
    static virtual bool SupportsCandidateWork => false;
}

/// <summary>An operation whose calls split into items that each yield one result, such as mutated parents or evaluated candidates.</summary>
/// <remarks>
/// Routing is what an item operation adds: <see cref="Route{TCandidate, TSearchState}"/> runs
/// each item of a call on another executor, which lets <see cref="ChoosePerItem{TOperation, TCandidate}"/> choose a
/// child per item.
/// </remarks>
public interface IItemOperation<TSelf> : IOperation<TSelf> where TSelf : IItemOperation<TSelf>
{
    /// <summary>Returns an executor that runs each item of a call on the child the router assigns it and returns the results in item order.</summary>
    /// <remarks>
    /// Every child passes <see cref="IOperation{TSelf}.IsExecutor{TCandidate, TSearchState}"/>.
    /// Each call assigns all its items with the call's random number generator before any child runs; the items
    /// assigned to one child run as one call on it, in item order and with the call's other arguments, and a child
    /// assigned every item receives the original call. Derive the returned executor from
    /// <see cref="RoutingExecutor{TExecutor}"/>.
    /// </remarks>
    static abstract IExecutor Route<TCandidate, TSearchState>(WeightedRouter<IExecutor> children)
        where TSearchState : class, ISearchState;
}
