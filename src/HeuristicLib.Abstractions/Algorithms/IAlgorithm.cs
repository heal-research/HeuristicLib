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
  // ToDo: Maybe remove the Terminator and Interceptor properties?
  
  ITerminator<TGenotype, TAlgorithmState, TSearchSpace, TProblem> Terminator { get; }

  IInterceptor<TGenotype, TAlgorithmState, TSearchSpace, TProblem>? Interceptor { get; }
  
  IIterationObserver<TGenotype, TSearchSpace, TProblem, TAlgorithmState>? Observer { get; }
  
  IAsyncEnumerable<TAlgorithmState> ExecuteStreamingAsync(
    TProblem problem,
    IRandomNumberGenerator random,
    TAlgorithmState? initialState = null, 
    CancellationToken ct = default
  );
}
