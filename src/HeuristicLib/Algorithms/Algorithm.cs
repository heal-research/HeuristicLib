using HEAL.HeuristicLib.Observation;
using HEAL.HeuristicLib.Operators.Evaluators;
using HEAL.HeuristicLib.Operators.Interceptors;
using HEAL.HeuristicLib.Operators.Terminators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Algorithms;

public abstract class Algorithm<TGenotype, TSearchSpace, TProblem, TAlgorithmState>
  : IAlgorithm<TGenotype, TSearchSpace, TProblem, TAlgorithmState>
  where TGenotype : class 
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
  where TAlgorithmState : class, IAlgorithmState
{
  public required ITerminator<TGenotype, TAlgorithmState, TSearchSpace, TProblem> Terminator { get; init; }
  public IInterceptor<TGenotype, TAlgorithmState, TSearchSpace, TProblem>? Interceptor { get; init; }

  public required IEvaluator<TGenotype, TSearchSpace, TProblem> Evaluator { get; init; }
  
  public abstract TAlgorithmState ExecuteStep(TProblem problem, TAlgorithmState? previousState, IRandomNumberGenerator random);
  
  public TAlgorithmState Execute(
    TProblem problem,
    IRandomNumberGenerator random,
    TAlgorithmState? initialState = null,
    IIterationObserver<TGenotype, TSearchSpace, TProblem, TAlgorithmState>? observer = null
  ) {
    return ExecuteStreaming(problem, random, initialState, observer).Last();
  }

  public IEnumerable<TAlgorithmState> ExecuteStreaming(
    TProblem problem,
    IRandomNumberGenerator random,
    TAlgorithmState? initialState = null,
    IIterationObserver<TGenotype, TSearchSpace, TProblem, TAlgorithmState>? observer = null
  ) {
    TAlgorithmState? previousState = initialState;
    bool shouldContinue =
      previousState is null ||
      Terminator.ShouldContinue(previousState, previousIterationState: default, problem.SearchSpace, problem);

    while (shouldContinue) {
      var newIterationState = ExecuteStep(problem, previousState, random);
      if (Interceptor is not null) {
        newIterationState = Interceptor.Transform(newIterationState, previousState, problem.SearchSpace, problem);
      }

      observer?.OnIterationCompleted(newIterationState, previousState, problem.SearchSpace, problem);

      yield return newIterationState;

      shouldContinue = Terminator.ShouldContinue(newIterationState, previousState, problem.SearchSpace, problem);
      previousState = newIterationState;
    }
  }
}
