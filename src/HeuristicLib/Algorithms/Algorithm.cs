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
  
  public IIterationObserver<TGenotype, TSearchSpace, TProblem, TAlgorithmState>? Observer { get; init; }

  public required IEvaluator<TGenotype, TSearchSpace, TProblem> Evaluator { get; init; }
  
  public abstract TAlgorithmState ExecuteStep(TProblem problem, TAlgorithmState? previousState, IRandomNumberGenerator random);
  
  public TAlgorithmState Execute(TProblem problem, IRandomNumberGenerator random) {
    IAlgorithm<TGenotype, TSearchSpace, TProblem, TAlgorithmState> alg = this;
    return alg.Execute(problem, random, initialState: null);
  }
}
