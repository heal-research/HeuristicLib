using System.Diagnostics.CodeAnalysis;
using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators;

public interface IOperator : IConfigurationNode;

/// <summary>The construction hook of an operator over <typeparamref name="TCandidate"/>.</summary>
/// <remarks>
/// The hook names no operation, so a configuration has one however many operations it performs, and the compiler rejects a
/// second. A slot names the operation too, through <see cref="IOperator{TOperation, TCandidate}"/>.
/// </remarks>
public interface IOperator<TCandidate> : IOperator
{
    /// <summary>Builds this operator's executor for the run types, resolving children through the scope.</summary>
    /// <remarks>
    /// Resolution calls this once per configuration/problem binding in a scope; later requests for that binding receive the same
    /// executor. Validate configuration first. Obtain shared resources through explicit handles when sharing is intended.
    /// Keep private mutable data in executor fields. The result implements the operation's executor interface for the run
    /// types. A configuration performing several operations returns an executor that serves every one of them.
    /// </remarks>
    IExecutor CreateExecutor<TSearchSpace, TProblem, TSearchState>(ResolutionScope<TCandidate, TSearchSpace, TProblem, TSearchState> scope)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TSearchState : class, ISearchState;
}

/// <summary>A reusable operator configuration supporting the operation identified by TOperation.</summary>
public interface IOperator<TOperation, TCandidate> : IOperator<TCandidate>
    where TOperation : IOperation<TOperation>
{
    IOperatorContract<TCandidate>? CandidateContract => this as IOperatorContract<TCandidate>;
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
