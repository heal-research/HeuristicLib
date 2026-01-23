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

public static class AlgorithmExtensions 
{
  extension<TGenotype, TSearchSpace, TProblem, TAlgorithmState>(IAlgorithm<TGenotype, TSearchSpace, TProblem, TAlgorithmState> algorithm)
    where TGenotype : class
    where TSearchSpace : class, ISearchSpace<TGenotype>
    where TProblem : class, IProblem<TGenotype, TSearchSpace>
    where TAlgorithmState : class, IAlgorithmState
  {
    public async Task<TAlgorithmState> ExecuteAsync(
      TProblem problem,
      IRandomNumberGenerator random,
      TAlgorithmState? initialState = null
    ) {
      return await algorithm.ExecuteStreamingAsync(problem, random, initialState).LastAsync();
    }
    
    public IEnumerable<TAlgorithmState> ExecuteStreaming(
      TProblem problem,
      IRandomNumberGenerator random,
      TAlgorithmState? initialState = null
    ) {
      return algorithm.ExecuteStreamingAsync(problem, random, initialState).ToBlockingEnumerable();
    }
    
    public TAlgorithmState Execute(
      TProblem problem,
      IRandomNumberGenerator random,
      TAlgorithmState? initialState = null
    ) {
      return algorithm.ExecuteAsync(problem, random, initialState).GetAwaiter().GetResult();
    }
  }
}
