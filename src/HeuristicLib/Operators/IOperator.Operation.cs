using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators;

/// <summary>A reusable operator configuration supporting the operation identified by TOperation.</summary>
public interface IOperator<TOperation, TCandidate> : IOperator
    where TOperation : IOperationDescriptor<TOperation>
{
    IOperatorContract<TCandidate>? CandidateContract => this as IOperatorContract<TCandidate>;

    /// <summary>Creates the factory that shares this operator's execution data across repeated resolutions.</summary>
    /// <remarks>Validate configuration and create counters or caches here, before any child executor is resolved.</remarks>
    OperatorExecutorFactory CreateExecutorFactory();
}

/// <summary>Identifies an operation, such as mutation or evaluation, and supplies the adapter for its executor interface.</summary>
public interface IOperationDescriptor<TSelf> where TSelf : IOperationDescriptor<TSelf>
{
    /// <summary>Creates the adapter that wraps this operation's executors for the run types.</summary>
    /// <remarks>Adapters keep no state; resolution creates one per operation and run types and reuses it.</remarks>
    static abstract OperationExecutorAdapter CreateExecutorAdapter<TCandidate, TSearchSpace, TProblem, TSearchState>()
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TSearchState : class, ISearchState;
}

/// <summary>Marks an operation whose calls split into items that each yield one result, such as mutated candidates.</summary>
/// <remarks>
/// The operation's invocations implement <see cref="IItemInvocation{TItemResult}"/>, which lets
/// <see cref="ChoosePerCandidate{TOperation, TCandidate}"/> run each item on a different child.
/// </remarks>
public interface IItemOperation<TSelf> : IOperationDescriptor<TSelf> where TSelf : IItemOperation<TSelf>;
