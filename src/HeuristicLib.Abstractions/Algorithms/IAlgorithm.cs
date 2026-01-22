using HEAL.HeuristicLib.Observation;
using HEAL.HeuristicLib.Operators.Interceptors;
using HEAL.HeuristicLib.Operators.Terminators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Algorithms;

public interface IAlgorithm<TGenotype, in TSearchSpace, in TProblem, TAlgorithmState> 
  where TGenotype : class
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
  where TAlgorithmState : class, IAlgorithmState
{
  // ToDo: Mayabe remove the Terminator and Interceptor properties?
  
  ITerminator<TGenotype, TAlgorithmState, TSearchSpace, TProblem> Terminator { get; }

  IInterceptor<TGenotype, TAlgorithmState, TSearchSpace, TProblem>? Interceptor { get; }
  
  IIterationObserver<TGenotype, TSearchSpace, TProblem, TAlgorithmState>? Observer { get; }

  // Maybe pull the ExecuteStep and ExecuteStreaming out into something like "iterable-algorithm"?
  TAlgorithmState ExecuteStep(TProblem problem, TAlgorithmState? previousState, IRandomNumberGenerator random);

  TAlgorithmState Execute(
    TProblem problem,
    IRandomNumberGenerator random,
    TAlgorithmState? initialState = null
  );

  IEnumerable<TAlgorithmState> ExecuteStreaming(
    TProblem problem,
    IRandomNumberGenerator random,
    TAlgorithmState? initialState = null
  );
}
