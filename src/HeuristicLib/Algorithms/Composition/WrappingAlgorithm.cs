using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Algorithms;

/// <remarks>A wrapping algorithm resolves its child through the scope and builds its executor around the result.</remarks>
public abstract record WrappingAlgorithm<TCandidate, TSearchState>(IAlgorithm<TCandidate, TSearchState> ChildAlgorithm)
    : IAlgorithm<TCandidate, TSearchState>
    where TSearchState : class, ISearchState
{
    public virtual bool Fits(ExecutionSignature execution) => execution.SearchState == typeof(TSearchState) && execution.Fits(ChildAlgorithm);

    IAlgorithmExecutor<TCandidate, TSearchSpace, TProblem, TSearchState> IAlgorithm<TCandidate, TSearchState>.CreateExecutor<TSearchSpace, TProblem>(
        ResolutionScope<TCandidate, TSearchSpace, TProblem, TSearchState> scope) => CreateExecutor(scope);

    /// <inheritdoc cref="IAlgorithm{TCandidate, TSearchState}.CreateExecutor{TSearchSpace, TProblem}"/>
    protected abstract IAlgorithmExecutor<TCandidate, TSearchSpace, TProblem, TSearchState> CreateExecutor<TSearchSpace, TProblem>(
        ResolutionScope<TCandidate, TSearchSpace, TProblem, TSearchState> scope)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>;
}

public abstract class WrappingAlgorithmExecutor<TCandidate, TSearchSpace, TProblem, TSearchState>(
    IAlgorithmExecutor<TCandidate, TSearchSpace, TProblem, TSearchState> childAlgorithm)
    : AlgorithmExecutor<TCandidate, TSearchSpace, TProblem, TSearchState>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
    where TSearchState : class, ISearchState
{
    protected IAlgorithmExecutor<TCandidate, TSearchSpace, TProblem, TSearchState> ChildAlgorithm { get; } = childAlgorithm;
}
