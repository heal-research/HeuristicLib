using System.Runtime.CompilerServices;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Algorithms;

public abstract record IterativeAlgorithm<TGenotype, TSearchSpace, TProblem, TSearchState, TExecutionState>
  : Algorithm<TGenotype, TSearchSpace, TProblem, TSearchState, TExecutionState>,
    IIterativeAlgorithm<TGenotype, TSearchSpace, TProblem, TSearchState>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
  where TSearchState : class, ISearchState
  where TExecutionState : IterativeAlgorithm<TGenotype, TSearchSpace, TProblem, TSearchState, TExecutionState>.ExecutionState
{
  public new class ExecutionState
    : Algorithm<TGenotype, TSearchSpace, TProblem, TSearchState, TExecutionState>.ExecutionState
  {
    public IInterceptorInstance<TGenotype, TSearchSpace, TProblem, TSearchState>? Interceptor { get; init; }
  }

  public IInterceptor<TGenotype, TSearchSpace, TProblem, TSearchState>? Interceptor { get; init; }

  protected abstract TSearchState ExecuteStep(
    TSearchState? previousState,
    TExecutionState executionState,
    TProblem problem,
    IRandomNumberGenerator random);

  protected sealed override IAlgorithmInstance<TGenotype, TSearchSpace, TProblem, TSearchState> CreateAlgorithmInstance(Run run, TExecutionState executionState)
  {
    return new Instance(this, run, executionState);
  }

  private sealed class Instance(
    IterativeAlgorithm<TGenotype, TSearchSpace, TProblem, TSearchState, TExecutionState> algorithm,
    Run run,
    TExecutionState executionState)
    : AlgorithmInstance<TGenotype, TSearchSpace, TProblem, TSearchState>(run, executionState.Evaluator)
  {
    private readonly IInterceptorInstance<TGenotype, TSearchSpace, TProblem, TSearchState>? interceptor = executionState.Interceptor;

    private TSearchState ExecuteStep(TSearchState? previousState, TProblem problem, IRandomNumberGenerator random)
    {
      return algorithm.ExecuteStep(previousState, executionState, problem, random);
    }

    public override async IAsyncEnumerable<TSearchState> RunStreamingAsync(
      TProblem problem,
      IRandomNumberGenerator random,
      TSearchState? initialState = null,
      [EnumeratorCancellation] CancellationToken ct = default)
    {
      var previousState = initialState;

      foreach (var currentIteration in Enumerable.InfiniteSequence(0, 1)) {
        ct.ThrowIfCancellationRequested();
        var iterationRandom = random.Fork(currentIteration);
        var newState = ExecuteStep(previousState, problem, iterationRandom);
        if (interceptor is not null) {
          newState = interceptor.Transform(newState, previousState, problem.SearchSpace, problem);
        }

        yield return newState;

        await Task.Yield();

        previousState = newState;
      }
    }
  }
}
