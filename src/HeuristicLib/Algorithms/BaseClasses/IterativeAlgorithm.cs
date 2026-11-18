using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Algorithms;

/// <remarks>
/// An iterative algorithm builds an <see cref="IterativeAlgorithmExecutor{TCandidate, TSearchSpace, TProblem, TSearchState}"/>
/// in its <c>CreateExecutor</c> hook and passes it the resolved <see cref="Interceptor"/>; the executor base applies it
/// after every step. The hook's return type requires that executor, so no iterative algorithm skips its interceptor.
/// </remarks>
public abstract record IterativeAlgorithm<TSelf, TCandidate, TSearchState>
    : Algorithm<TSelf, TCandidate, TSearchState>, IIterativeAlgorithm<TCandidate, TSearchState>
    where TSelf : IterativeAlgorithm<TSelf, TCandidate, TSearchState>
    where TSearchState : class, ISearchState
{
    public IOperator<Interception<TSearchState>, TCandidate>? Interceptor { get; init; }

    public static Parameter<IterativeAlgorithm<TSelf, TCandidate, TSearchState>, IOperator<Interception<TSearchState>, TCandidate>?> InterceptorParameter { get; } =
        new(nameof(Interceptor), static algorithm => algorithm.Interceptor, static (algorithm, value) => algorithm with { Interceptor = value });

    public static IReadOnlyList<IParameter> Parameters { get; } = [InterceptorParameter];

    public override IReadOnlyList<IParameter> GetParameters() => [.. base.GetParameters(), .. Parameters];

    /// <inheritdoc cref="IAlgorithm{TCandidate, TSearchState}.CreateExecutor{TSearchSpace, TProblem}"/>
    protected abstract override IterativeAlgorithmExecutor<TCandidate, TSearchSpace, TProblem, TSearchState> CreateExecutor<TSearchSpace, TProblem>(
        ResolutionScope<TCandidate, TSearchSpace, TProblem, TSearchState> scope);
}

/// <remarks>
/// Derive from this base only when the algorithm reads its search space or problem itself; naming them costs two type
/// arguments on every mention of the configuration. An algorithm that just hands them to its operators belongs on
/// <see cref="IterativeAlgorithm{TSelf, TCandidate, TSearchState}"/>.
/// <para>
/// A run this algorithm was not written for is reported when the execution graph is built. The bound algorithm base
/// converts the executor to the run's types, so this base declares the interceptor itself rather than deriving from
/// the agnostic iterative base.
/// </para>
/// </remarks>
public abstract record IterativeAlgorithm<TSelf, TCandidate, TSearchSpace, TProblem, TSearchState>
    : Algorithm<TSelf, TCandidate, TSearchSpace, TProblem, TSearchState>, IIterativeAlgorithm<TCandidate, TSearchState>
    where TSelf : IterativeAlgorithm<TSelf, TCandidate, TSearchSpace, TProblem, TSearchState>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
    where TSearchState : class, ISearchState
{
    public IOperator<Interception<TSearchState>, TCandidate>? Interceptor { get; init; }

    public static Parameter<IterativeAlgorithm<TSelf, TCandidate, TSearchSpace, TProblem, TSearchState>, IOperator<Interception<TSearchState>, TCandidate>?> InterceptorParameter { get; } =
        new(nameof(Interceptor), static algorithm => algorithm.Interceptor, static (algorithm, value) => algorithm with { Interceptor = value });

    public static IReadOnlyList<IParameter> Parameters { get; } = [InterceptorParameter];

    public override IReadOnlyList<IParameter> GetParameters() => [.. base.GetParameters(), .. Parameters];

    /// <summary>Builds this algorithm's executor for the types it is written for, resolving the interceptor and other children through the scope.</summary>
    /// <remarks>
    /// Resolution calls this once per configuration and problem in a scope, on the first request; later requests for that
    /// pair receive the same executor. Validate the configuration first and look up run results through the scope.
    /// Keep private mutable data in executor fields; explicit sharing retains the executor across child scopes.
    /// </remarks>
    protected abstract override IterativeAlgorithmExecutor<TCandidate, TSearchSpace, TProblem, TSearchState> CreateExecutor(ResolutionScope<TCandidate, TSearchSpace, TProblem, TSearchState> scope);
}

public abstract class IterativeAlgorithmExecutor<TCandidate, TSearchSpace, TProblem, TSearchState>
    : AlgorithmExecutor<TCandidate, TSearchSpace, TProblem, TSearchState>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
    where TSearchState : class, ISearchState
{
    private readonly IInterceptionExecutor<TSearchState>? interceptor;

    protected IterativeAlgorithmExecutor(IInterceptionExecutor<TSearchState>? interceptor, TProblem problem)
        : base(problem)
    {
        this.interceptor = interceptor;
    }

    protected abstract TSearchState ExecuteStep(TSearchState? previousState, IRandomNumberGenerator random);

    protected virtual bool TryExecuteStep(TSearchState? previousState, IRandomNumberGenerator random, [NotNullWhen(true)] out TSearchState? nextState)
    {
        nextState = ExecuteStep(previousState, random);
        return true;
    }

    protected virtual bool HasCompleted(int yieldedStateCount, TSearchState? previousState) => false;

    /// <summary>Decides whether the run stops after yielding the state just produced.</summary>
    /// <remarks><paramref name="random"/> is the iteration's generator, which the step and the interceptor also received.</remarks>
    protected virtual bool IsTerminalState(TSearchState state, int yieldedStateCount, TSearchState? previousState, IRandomNumberGenerator random) => false;

    public sealed override async IAsyncEnumerable<TSearchState> RunStreamingAsync(IRandomNumberGenerator random, TSearchState? initialState = null, [EnumeratorCancellation] CancellationToken ct = default)
    {
        var previousState = initialState;

        foreach (var yieldedStateCount in Enumerable.InfiniteSequence(0, 1))
        {
            if (HasCompleted(yieldedStateCount, previousState))
            {
                yield break;
            }

            ct.ThrowIfCancellationRequested();
            var iterationRandom = random.Fork(yieldedStateCount);
            if (!TryExecuteStep(previousState, iterationRandom, out var newState))
            {
                yield break;
            }

            if (interceptor is not null)
            {
                newState = interceptor.Transform(newState, previousState, iterationRandom);
            }

            var isTerminalState = IsTerminalState(newState, yieldedStateCount + 1, previousState, iterationRandom);
            yield return newState;

            if (isTerminalState)
            {
                yield break;
            }

            await Task.Yield();
            previousState = newState;
        }
    }
}
