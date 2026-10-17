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
    static abstract OperationExecutorAdapter<TSelf, TCandidate, TSearchSpace, TProblem, TRunSearchState>
        CreateExecutorAdapter<TCandidate, TSearchSpace, TProblem, TRunSearchState>()
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TRunSearchState : class, ISearchState;
}

