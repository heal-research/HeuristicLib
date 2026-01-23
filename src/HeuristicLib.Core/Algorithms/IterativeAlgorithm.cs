using System.Runtime.CompilerServices;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Algorithms;

public abstract class IterativeAlgorithm<TGenotype, TSearchSpace, TProblem, TAlgorithmState> 
  : Algorithm<TGenotype, TSearchSpace, TProblem, TAlgorithmState>
  , IIterativeAlgorithm<TGenotype, TSearchSpace, TProblem, TAlgorithmState>
  where TGenotype : class 
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
  where TAlgorithmState : class, IAlgorithmState
{
  public abstract TAlgorithmState ExecuteStep(TProblem problem, TAlgorithmState? previousState, IRandomNumberGenerator random);
  
  public virtual ValueTask<TAlgorithmState> ExecuteStepAsync(TProblem problem, TAlgorithmState? previousState, IRandomNumberGenerator random)
  {
    var result = ExecuteStep(problem, previousState, random);
    return new ValueTask<TAlgorithmState>(result);
  }
  
  public override async IAsyncEnumerable<TAlgorithmState> ExecuteStreamingAsync(TProblem problem,
    IRandomNumberGenerator random,
    TAlgorithmState? initialState = null,
    [EnumeratorCancellation] CancellationToken ct = default)
  {
    TAlgorithmState? previousState = initialState;
    bool shouldContinue = previousState is null ||
                          Terminator.ShouldContinue(previousState, previousIterationState: null, problem.SearchSpace, problem);

    while (shouldContinue) {
      ct.ThrowIfCancellationRequested();
      var newIterationState = await ExecuteStepAsync(problem, previousState, random);
      if (Interceptor is not null) {
        newIterationState = Interceptor.Transform(newIterationState, previousState, problem.SearchSpace, problem);
      }

      Observer?.OnIterationCompleted(newIterationState, previousState, problem.SearchSpace, problem);

      yield return newIterationState;

      await Task.Yield();

      shouldContinue = Terminator.ShouldContinue(newIterationState, previousState, problem.SearchSpace, problem);
      previousState = newIterationState;
    }
  }
}
