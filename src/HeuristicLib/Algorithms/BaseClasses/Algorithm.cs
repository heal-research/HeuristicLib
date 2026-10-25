using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Algorithms;

/// <remarks>
/// Derive from this base when the algorithm only hands its search space and problem to its operators. Derive from
/// <see cref="Algorithm{TSelf, TCandidate, TSearchSpace, TProblem, TSearchState}"/> when it reads them itself.
/// </remarks>
public abstract record Algorithm<TSelf, TCandidate, TSearchState>
    : IAlgorithm<TCandidate, TSearchState>
    where TSelf : Algorithm<TSelf, TCandidate, TSearchState>
    where TSearchState : class, ISearchState
{
    internal TSelf Self => (TSelf)this;

    IAlgorithmExecutor<TCandidate, TSearchSpace, TProblem, TSearchState> IAlgorithm<TCandidate, TSearchState>.CreateExecutor<TSearchSpace, TProblem>(
        ResolutionScope<TCandidate, TSearchSpace, TProblem, TSearchState> scope) => CreateExecutor(scope);

    /// <summary>
    /// Agnostic in the search space and problem, so this base answers on the search state alone, which
    /// <see cref="IAlgorithmExecutor{TCandidate, TSearchSpace, TProblem, TSearchState}"/> is invariant in.
    /// </summary>
    public virtual bool Fits(ExecutionSignature execution) => execution.SearchState == typeof(TSearchState);

    /// <inheritdoc cref="IAlgorithm{TCandidate, TSearchState}.CreateExecutor{TSearchSpace, TProblem}"/>
    protected abstract IAlgorithmExecutor<TCandidate, TSearchSpace, TProblem, TSearchState> CreateExecutor<TSearchSpace, TProblem>(
        ResolutionScope<TCandidate, TSearchSpace, TProblem, TSearchState> scope)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>;
}

/// <remarks>
/// Derive from this base only when the algorithm reads its search space or problem itself; naming them costs two type
/// arguments on every mention of the configuration. An algorithm that just hands them to its operators belongs on
/// <see cref="Algorithm{TSelf, TCandidate, TSearchState}"/>.
/// <para>
/// A run this algorithm was not written for is reported when the execution graph is built.
/// </para>
/// </remarks>
public abstract record Algorithm<TSelf, TCandidate, TSearchSpace, TProblem, TSearchState>
    : Algorithm<TSelf, TCandidate, TSearchState>
    where TSelf : Algorithm<TSelf, TCandidate, TSearchSpace, TProblem, TSearchState>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
    where TSearchState : class, ISearchState
{
    public override bool Fits(ExecutionSignature execution) =>
        base.Fits(execution)
        && execution.SearchSpace.IsAssignableTo(typeof(TSearchSpace))
        && execution.Problem.IsAssignableTo(typeof(TProblem));

    // The run's types convert to the declared ones, so the executor built for those serves the run.
    protected sealed override IAlgorithmExecutor<TCandidate, TRunSearchSpace, TRunProblem, TSearchState> CreateExecutor<TRunSearchSpace, TRunProblem>(
        ResolutionScope<TCandidate, TRunSearchSpace, TRunProblem, TSearchState> scope)
    {
        if (!typeof(TRunSearchSpace).IsAssignableTo(typeof(TSearchSpace)) || !typeof(TRunProblem).IsAssignableTo(typeof(TProblem)))
        {
            throw ExecutionSignature.Mismatch(
                this,
                ExecutionSignature.Describe(typeof(TSearchSpace), typeof(TProblem), typeof(TSearchState)),
                ExecutionSignature.Describe(typeof(TRunSearchSpace), typeof(TRunProblem)));
        }

        return (IAlgorithmExecutor<TCandidate, TRunSearchSpace, TRunProblem, TSearchState>)CreateExecutor(scope.For<TCandidate, TSearchSpace, TProblem, TSearchState>());
    }

    /// <summary>Builds this algorithm's executor for the types it is written for, resolving children through the scope.</summary>
    /// <remarks>
    /// Resolution calls this for every build. Validate the configuration first and take shared execution data from
    /// <see cref="ResolutionScope.GetScopedState{TState}"/>, passing this configuration.
    /// </remarks>
    protected abstract IAlgorithmExecutor<TCandidate, TSearchSpace, TProblem, TSearchState> CreateExecutor(ResolutionScope<TCandidate, TSearchSpace, TProblem, TSearchState> scope);
}

public abstract class AlgorithmExecutor<TCandidate, TSearchSpace, TProblem, TSearchState>
    : IAlgorithmExecutor<TCandidate, TSearchSpace, TProblem, TSearchState>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
    where TSearchState : class, ISearchState
{
    public abstract IAsyncEnumerable<TSearchState> RunStreamingAsync(TProblem problem, IRandomNumberGenerator random, TSearchState? initialState = null, CancellationToken ct = default);
}
