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

    IAlgorithmExecutor<TSearchState> IAlgorithm<TCandidate, TSearchState>.CreateExecutor<TSearchSpace, TProblem>(
        ResolutionScope<TCandidate, TSearchSpace, TProblem, TSearchState> scope) => CreateExecutor(scope);

    /// <summary>
    /// Agnostic in the search space and problem, so this base answers on the search state alone, which
    /// <see cref="IAlgorithmExecutor{TSearchState}"/> is invariant in.
    /// </summary>
    public virtual bool Supports(ExecutionSignature execution) => execution.SearchState == typeof(TSearchState);

    /// <inheritdoc cref="IConfigurationNode.ChildExecution"/>
    public virtual ExecutionSignature ChildExecution(ExecutionSignature execution) => execution;

    /// <inheritdoc cref="IConfigurationNode.GetParameters"/>
    public virtual IReadOnlyList<IParameter> GetParameters() => [];

    /// <inheritdoc cref="IAlgorithm{TCandidate, TSearchState}.CreateExecutor{TSearchSpace, TProblem}"/>
    protected abstract IAlgorithmExecutor<TSearchState> CreateExecutor<TSearchSpace, TProblem>(
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
    public override bool Supports(ExecutionSignature execution) =>
        base.Supports(execution)
        && execution.SearchSpace.IsAssignableTo(typeof(TSearchSpace))
        && execution.Problem.IsAssignableTo(typeof(TProblem));

    /// <inheritdoc cref="IConfigurationNode.ChildExecution"/>
    public override ExecutionSignature ChildExecution(ExecutionSignature execution) =>
        ExecutionSignature.For<TSearchSpace, TProblem, TSearchState>();

    // Supports has already accepted the run's types, so the run's problem is a TProblem.
    protected sealed override IAlgorithmExecutor<TSearchState> CreateExecutor<TRunSearchSpace, TRunProblem>(
        ResolutionScope<TCandidate, TRunSearchSpace, TRunProblem, TSearchState> scope) =>
        scope.Problem is TProblem problem
            ? CreateExecutor(new ResolutionScope<TCandidate, TSearchSpace, TProblem, TSearchState>(scope, problem))
            : throw ExecutionSignature.Mismatch(
                this,
                ExecutionSignature.Describe(typeof(TSearchSpace), typeof(TProblem), typeof(TSearchState)),
                ExecutionSignature.Describe(typeof(TRunSearchSpace), typeof(TRunProblem), typeof(TSearchState)));

    /// <summary>Builds this algorithm's executor for the types it is written for, resolving children through the scope.</summary>
    /// <remarks>
    /// Resolution calls this once per configuration and problem in a scope, on the first request; later requests for that
    /// pair receive the same executor. Validate the configuration first and look up run results through the scope.
    /// Keep private mutable data in executor fields; explicit sharing retains the executor across child scopes.
    /// </remarks>
    protected abstract IAlgorithmExecutor<TSearchState> CreateExecutor(ResolutionScope<TCandidate, TSearchSpace, TProblem, TSearchState> scope);
}

public abstract class AlgorithmExecutor<TSearchState> : IAlgorithmExecutor<TSearchState>
    where TSearchState : class, ISearchState
{
    public abstract IAsyncEnumerable<TSearchState> RunStreamingAsync(IRandomNumberGenerator random, TSearchState? initialState = null, CancellationToken ct = default);
}
