using HEAL.HeuristicLib.Operators.Interceptors;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Algorithms;

public abstract class IterativeAlgorithm<TGenotype, TSearchSpace, TProblem, TAlgorithmState>
  : Algorithm<TGenotype, TSearchSpace, TProblem, TAlgorithmState>,
    IIterativeAlgorithm<TGenotype, TSearchSpace, TProblem, TAlgorithmState>
  where TGenotype : class
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
  where TAlgorithmState : class, IAlgorithmState
{
  //public int? MaximumIterations { get; init; }
  //public ITerminator<TGenotype, TAlgorithmState, TSearchSpace, TProblem> Terminator { get; init; } = new NeverTerminator<TGenotype>();
  public IInterceptor<TGenotype, TAlgorithmState, TSearchSpace, TProblem>? Interceptor { get; init; }

  public abstract TAlgorithmState ExecuteStep(TAlgorithmState? previousState, TProblem problem, IRandomNumberGenerator random);
  
  // private ValueTask<TAlgorithmState> ExecuteStepAsync(TProblem problem, TAlgorithmState? previousState, IRandomNumberGenerator random)
  // {
  //   return new ValueTask<TAlgorithmState>(ExecuteStep(problem, previousState, random));
  // }
  
  public override async IAsyncEnumerable<TAlgorithmState> RunStreamingAsync(TAlgorithmState? initialState, TProblem problem, IRandomNumberGenerator random, CancellationToken ct = default)
  {
    var previousState = initialState;

    foreach (var currentIteration in Enumerable.InfiniteSequence(0, 1)) {
      ct.ThrowIfCancellationRequested();
      var iterationRandom = random.Fork(currentIteration);
      var newState = ExecuteStep(previousState, problem, iterationRandom);
      if (Interceptor is not null) {
        newState = Interceptor.Transform(newState, previousState, problem.SearchSpace, problem);
      }

      Observer?.OnIterationCompleted(newState, previousState, problem.SearchSpace, problem);

      yield return newState;
      
      await Task.Yield();
      
      previousState = newState;
    }
  }
}

public static class IterativeAlgorithmExtensions
{
  extension<TGenotype, TSearchSpace, TProblem, TAlgorithmState>(IIterativeAlgorithm<TGenotype, TSearchSpace, TProblem, TAlgorithmState> algorithm)
    where TGenotype : class
    where TSearchSpace : class, ISearchSpace<TGenotype>
    where TProblem : class, IProblem<TGenotype, TSearchSpace>
    where TAlgorithmState : class, IAlgorithmState
  {
    public async Task<TAlgorithmState> RunToCompletionAsync(
      TAlgorithmState? initialState,
      TProblem problem,
      IRandomNumberGenerator random,
      CancellationToken ct = default
    )
    {
      return await algorithm.RunStreamingAsync(initialState, problem, random, ct).LastAsync(cancellationToken: ct);
    }

    public IEnumerable<TAlgorithmState> RunStreaming(
      TAlgorithmState? initialState,
      TProblem problem,
      IRandomNumberGenerator random,
      CancellationToken ct = default
    )
    {
      return algorithm.RunStreamingAsync(initialState, problem, random, ct).ToBlockingEnumerable();
    }

    public TAlgorithmState RunToCompletion(
      TAlgorithmState? initialState,
      TProblem problem,
      IRandomNumberGenerator random,
      CancellationToken ct = default
    )
    {
      return algorithm.RunToCompletionAsync(initialState, problem, random, ct).GetAwaiter().GetResult();
    }
  }
}
