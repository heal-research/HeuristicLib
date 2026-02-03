using HEAL.HeuristicLib.Observation;
using HEAL.HeuristicLib.Operators.Evaluators;
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
  //public required ITerminator<TGenotype, TAlgorithmState, TSearchSpace, TProblem> Terminator { get; init; }
  //public IInterceptor<TGenotype, TAlgorithmState, TSearchSpace, TProblem>? Interceptor { get; init; }
  public IIterationObserver<TGenotype, TSearchSpace, TProblem, TAlgorithmState>? Observer { get; init; }

  public IEvaluator<TGenotype, TSearchSpace, TProblem> Evaluator { get; init; } = new DirectEvaluator<TGenotype>();

  public abstract IAsyncEnumerable<TAlgorithmState> RunStreamingAsync(
    TAlgorithmState? initialState,
    TProblem problem,
    IRandomNumberGenerator random,
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
    
    public IAsyncEnumerable<TAlgorithmState> RunStreamingAsync(
      TProblem problem,
      IRandomNumberGenerator random,
      CancellationToken ct = default
    )
    {
      return algorithm.RunStreamingAsync(null, problem, random, ct);
    }
    
    public async Task<TAlgorithmState> RunToCompletionAsync(
      TProblem problem,
      IRandomNumberGenerator random,
      CancellationToken ct = default
    )
    {
      return await algorithm.RunStreamingAsync(null, problem, random, ct).LastAsync(cancellationToken: ct);
    }

    public IEnumerable<TAlgorithmState> RunStreaming(
      TProblem problem,
      IRandomNumberGenerator random,
      CancellationToken ct = default
    )
    {
      return algorithm.RunStreamingAsync(null, problem, random, ct).ToBlockingEnumerable();
    }

    public TAlgorithmState RunToCompletion(
      TProblem problem,
      IRandomNumberGenerator random,
      CancellationToken ct = default
    )
    {
      return algorithm.RunToCompletionAsync(null, problem, random, ct).GetAwaiter().GetResult();
    }
  }
}
