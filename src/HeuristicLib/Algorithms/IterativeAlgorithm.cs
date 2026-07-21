using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Algorithms;

public abstract record IterativeAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState>
    : Algorithm<TCandidate, TSearchSpace, TProblem, TSearchState>, IIterativeAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
    where TSearchState : class, ISearchState
{
    public IInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState>? Interceptor { get; init; }

    protected sealed override AlgorithmInstance<TCandidate, TSearchSpace, TProblem, TSearchState> CreateAlgorithmInstance(ExecutionInstanceRegistry registry)
    {
        var resolvedInterceptor = Interceptor is null ? null : registry.Resolve(Interceptor);
        return CreateIterativeAlgorithmInstance(registry, resolvedInterceptor);
    }

    protected abstract IterativeAlgorithmInstance<TCandidate, TSearchSpace, TProblem, TSearchState> CreateIterativeAlgorithmInstance(
        ExecutionInstanceRegistry registry, IInterceptorInstance<TCandidate, TSearchSpace, TProblem, TSearchState>? resolvedInterceptor);
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
                newState = interceptor.Transform(newState, previousState, problem.SearchSpace, problem);
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
