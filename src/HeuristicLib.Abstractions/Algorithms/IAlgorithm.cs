using HEAL.HeuristicLib.Observation;
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
  // ToDo: Maybe remove the Terminator and Interceptor properties?

  
  
  // ToDo: Shouldn't any algorithm be terminatable?

  IIterationObserver<TGenotype, TSearchSpace, TProblem, TAlgorithmState>? Observer { get; }

  // ToDo: think about moving the initialState after problem/random (OptimizationContext) to allow null default and thus reduce number of overloads 
  IAsyncEnumerable<TAlgorithmState> RunStreamingAsync(
    TAlgorithmState? initialState,
    TProblem problem,
    IRandomNumberGenerator random,
    CancellationToken ct = default
  );
}
