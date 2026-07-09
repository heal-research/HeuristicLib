using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Algorithms;

public abstract record IterativeAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState, TExecutionState>
  : Algorithm<TCandidate, TSearchSpace, TProblem, TSearchState, TExecutionState>,
    IIterativeAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState>
  where TSearchSpace : class, ISearchSpace<TCandidate>
  where TProblem : class, IProblem<TCandidate, TSearchSpace>
  where TSearchState : class, ISearchState
  where TExecutionState : IterativeAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState, TExecutionState>.ExecutionState
{
    public new class ExecutionState
      : Algorithm<TCandidate, TSearchSpace, TProblem, TSearchState, TExecutionState>.ExecutionState
    {
        public IInterceptorInstance<TCandidate, TSearchSpace, TProblem, TSearchState>? Interceptor { get; init; }
    }

    public IInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState>? Interceptor { get; init; }

    protected abstract TSearchState ExecuteStep(
      TSearchState? previousState,
      TExecutionState executionState,
      TProblem problem,
      IRandomNumberGenerator random);

    protected virtual bool TryExecuteStep(
      TSearchState? previousState,
      TExecutionState executionState,
      TProblem problem,
      IRandomNumberGenerator random,
      [NotNullWhen(true)] out TSearchState? nextState)
    {
        nextState = ExecuteStep(previousState, executionState, problem, random);
        return true;
    }

    protected virtual bool HasCompleted(
      int yieldedStateCount,
      TSearchState? previousState,
      TExecutionState executionState,
      TProblem problem) => false;

    protected virtual bool IsTerminalState(
      TSearchState state,
      int yieldedStateCount,
      TSearchState? previousState,
      TExecutionState executionState,
      TProblem problem) => false;

    protected sealed override IAlgorithmInstance<TCandidate, TSearchSpace, TProblem, TSearchState> CreateAlgorithmInstance(Run run, TExecutionState executionState)
    {
        return new Instance(this, run, executionState);
    }

    private sealed class Instance(
      IterativeAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState, TExecutionState> algorithm,
      Run run,
      TExecutionState executionState)
      : AlgorithmInstance<TCandidate, TSearchSpace, TProblem, TSearchState>(run, executionState.Evaluator)
    {
        private readonly IInterceptorInstance<TCandidate, TSearchSpace, TProblem, TSearchState>? interceptor = executionState.Interceptor;

        private bool TryExecuteStep(TSearchState? previousState, TProblem problem, IRandomNumberGenerator random, [NotNullWhen(true)] out TSearchState? nextState)
        {
            return algorithm.TryExecuteStep(previousState, executionState, problem, random, out nextState);
        }

        public override async IAsyncEnumerable<TSearchState> RunStreamingAsync(
          TProblem problem,
          IRandomNumberGenerator random,
          TSearchState? initialState = null,
          [EnumeratorCancellation] CancellationToken ct = default)
        {
            var previousState = initialState;

            foreach (var yieldedStateCount in Enumerable.InfiniteSequence(0, 1))
            {
                if (algorithm.HasCompleted(yieldedStateCount, previousState, executionState, problem))
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

                var isTerminalState = algorithm.IsTerminalState(newState, yieldedStateCount + 1, previousState, executionState, problem);

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
}
