using System.Diagnostics.CodeAnalysis;
using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators;

public interface IOperator : IConfigurationNode;

/// <summary>A reusable operator configuration supporting the operation identified by TOperation.</summary>
public interface IOperator<TOperation, TCandidate> : IOperator
    where TOperation : IOperation<TOperation>
{
    IOperatorContract<TCandidate>? CandidateContract => this as IOperatorContract<TCandidate>;

    /// <summary>Builds this operator's executor for the run types, resolving children through the scope.</summary>
    /// <remarks>
    /// Resolution calls this for every build. Validate the configuration first and take shared execution data, such as
    /// counters or caches, from <see cref="ResolutionScope.GetScopedState{TState}"/>, passing this configuration.
    /// The result implements the operation's executor interface for the run types.
    /// </remarks>
    IExecutor CreateExecutor<TSearchSpace, TProblem, TSearchState>(ResolutionScope<TCandidate, TSearchSpace, TProblem, TSearchState> scope)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TSearchState : class, ISearchState;
}

public interface IOperatorExecutor : IExecutor;

/// <summary>
/// Declares that a value recommends an operator for a specific operator role.
/// </summary>
/// <remarks>
/// A successful call must return <see langword="true"/> and a new, nonnull operator. Observation sources are matched
/// by reference, so a shared instance would couple configurations that accepted the same recommendation. Returning
/// <see langword="false"/> with a null recommendation declines to recommend an operator for the source's current state.
/// </remarks>
public interface IRecommends<TOperator>
    where TOperator : class, IOperator
{
    bool TryCreateRecommendedOperator([NotNullWhen(true)] out TOperator? recommendation);
}
