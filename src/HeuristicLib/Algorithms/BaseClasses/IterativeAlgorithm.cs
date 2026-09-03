using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Algorithms;

public abstract record IterativeAlgorithm<TSelf, TCandidate, TSearchState>
    : Algorithm<TSelf, TCandidate, TSearchState>, IIterativeAlgorithm<TCandidate>
    where TSelf : IterativeAlgorithm<TSelf, TCandidate, TSearchState>
    where TSearchState : class, ISearchState
{
    public IInterceptor<TCandidate>? Interceptor { get; init; }

    public override IAlgorithmInstance<TCandidate, TRunSearchSpace, TRunProblem, TSearchState> CreateExecutionInstance<TRunSearchSpace, TRunProblem>(ExecutionInstanceRegistry instanceRegistry)
    {
        var resolver = instanceRegistry.For<TCandidate, TRunSearchSpace, TRunProblem, TSearchState>();
        return CreateExecutionInstance(instanceRegistry, resolver.ResolveOptional(Interceptor));
    }

    protected abstract IterativeAlgorithmInstance<TCandidate, TRunSearchSpace, TRunProblem, TSearchState> CreateExecutionInstance<TRunSearchSpace, TRunProblem>(
        ExecutionInstanceRegistry instanceRegistry,
        IInterceptorInstance<TCandidate, TRunSearchSpace, TRunProblem, TSearchState>? resolvedInterceptor)
        where TRunSearchSpace : class, ISearchSpace<TCandidate>
        where TRunProblem : class, IProblem<TCandidate, TRunSearchSpace>;
}

/// <remarks>
/// Derive from this base only when the algorithm reads its search space or problem itself; naming them costs two type
/// arguments on every mention of the configuration. An algorithm that just hands them to its operators belongs on
/// <see cref="IterativeAlgorithm{TSelf, TCandidate, TSearchState}"/>.
/// <para>
/// A run this algorithm was not written for is reported when the execution graph is built.
/// </para>
/// </remarks>
public abstract record IterativeAlgorithm<TSelf, TCandidate, TSearchSpace, TProblem, TSearchState>
    : IterativeAlgorithm<TSelf, TCandidate, TSearchState>
    where TSelf : IterativeAlgorithm<TSelf, TCandidate, TSearchSpace, TProblem, TSearchState>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
    where TSearchState : class, ISearchState
{
    protected abstract IterativeAlgorithmInstance<TCandidate, TSearchSpace, TProblem, TSearchState> CreateExecutionInstance(
        ExecutionInstanceRegistry instanceRegistry,
        IInterceptorInstance<TCandidate, TSearchSpace, TProblem, TSearchState>? resolvedInterceptor);

    /// <remarks>
    /// Resolves the interceptor at this algorithm's own search space and problem, which is what the creation method
    /// below takes.
    /// </remarks>
    public sealed override IAlgorithmInstance<TCandidate, TRunSearchSpace, TRunProblem, TSearchState> CreateExecutionInstance<TRunSearchSpace, TRunProblem>(ExecutionInstanceRegistry instanceRegistry)
    {
        var resolver = instanceRegistry.For<TCandidate, TSearchSpace, TProblem, TSearchState>();
        var bound = CreateExecutionInstance(instanceRegistry, resolver.ResolveOptional(Interceptor));

        if (bound is not IAlgorithmInstance<TCandidate, TRunSearchSpace, TRunProblem, TSearchState> instance)
        {
            throw new InvalidOperationException(
                $"{GetType().Name} is written for {typeof(TSearchSpace).Name} and {typeof(TProblem).Name}, and cannot run over {typeof(TRunSearchSpace).Name} with {typeof(TRunProblem).Name}.");
        }

        return instance;
    }

    protected sealed override IterativeAlgorithmInstance<TCandidate, TRunSearchSpace, TRunProblem, TSearchState> CreateExecutionInstance<TRunSearchSpace, TRunProblem>(
        ExecutionInstanceRegistry instanceRegistry,
        IInterceptorInstance<TCandidate, TRunSearchSpace, TRunProblem, TSearchState>? resolvedInterceptor) =>
        throw new NotSupportedException("A bound algorithm builds its instance through its own creation method.");
}

public abstract class IterativeAlgorithmInstance<TCandidate, TSearchSpace, TProblem, TSearchState>
    : AlgorithmInstance<TCandidate, TSearchSpace, TProblem, TSearchState>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
    where TSearchState : class, ISearchState
{
    private readonly IInterceptorInstance<TCandidate, TSearchSpace, TProblem, TSearchState>? interceptor;

    protected IterativeAlgorithmInstance(IInterceptorInstance<TCandidate, TSearchSpace, TProblem, TSearchState>? interceptor)
    {
        this.interceptor = interceptor;
    }

    protected abstract TSearchState ExecuteStep(TSearchState? previousState, TProblem problem, IRandomNumberGenerator random);

    protected virtual bool TryExecuteStep(TSearchState? previousState, TProblem problem, IRandomNumberGenerator random, [NotNullWhen(true)] out TSearchState? nextState)
    {
        nextState = ExecuteStep(previousState, problem, random);
        return true;
    }

    protected virtual bool HasCompleted(int yieldedStateCount, TSearchState? previousState, TProblem problem) => false;

    protected virtual bool IsTerminalState(TSearchState state, int yieldedStateCount, TSearchState? previousState, TProblem problem) => false;

    public sealed override async IAsyncEnumerable<TSearchState> RunStreamingAsync(TProblem problem, IRandomNumberGenerator random, TSearchState? initialState = null, [EnumeratorCancellation] CancellationToken ct = default)
    {
        var previousState = initialState;

        foreach (var yieldedStateCount in Enumerable.InfiniteSequence(0, 1))
        {
            if (HasCompleted(yieldedStateCount, previousState, problem))
            {
                yield break;
            }

            ct.ThrowIfCancellationRequested();
            var iterationRandom = random.Fork(yieldedStateCount);
            if (!TryExecuteStep(previousState, problem, iterationRandom, out var newState))
            {
                yield break;
            }

            if (interceptor is not null)
            {
                newState = interceptor.Transform(newState, previousState, iterationRandom, problem.SearchSpace, problem);
            }

            var isTerminalState = IsTerminalState(newState, yieldedStateCount + 1, previousState, problem);
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
