using HEAL.HeuristicLib.Observation;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Evaluators;
using HEAL.HeuristicLib.Operators.StateTransformers;
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
  //public IIterationObserver<TGenotype, TSearchSpace, TProblem, TAlgorithmState>? Observer { get; init; }

  public IEvaluator<TGenotype, TSearchSpace, TProblem> Evaluator { get; init; } = new DirectEvaluator<TGenotype>();

  //public abstract IAlgorithmInstance<TGenotype, TSearchSpace, TProblem, TAlgorithmState> IAlgorithm<TGenotype, TSearchSpace, TProblem, TAlgorithmState>.CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry);
  public abstract IAlgorithmInstance<TGenotype, TSearchSpace, TProblem, TAlgorithmState> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry);
}

public abstract class AlgorithmInstance<TGenotype, TSearchSpace, TProblem, TAlgorithmState>
  : IAlgorithmInstance<TGenotype, TSearchSpace, TProblem, TAlgorithmState>
  where TGenotype : class
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
  where TAlgorithmState : class, IAlgorithmState
{

  public abstract IAsyncEnumerable<TAlgorithmState> RunStreamingAsync(
    TProblem problem,
    IRandomNumberGenerator random,
    TAlgorithmState? initialState = null,
    CancellationToken ct = default);
  //public TAlgorithmState Transform(TAlgorithmState? state, IRandomNumberGenerator randomNumberGenerator, TSearchSpace searchSpace, TProblem problem) => throw new NotImplementedException();
}

public static class AlgorithmExtensions
{
  extension<TGenotype, TSearchSpace, TProblem, TAlgorithmState>(IAlgorithm<TGenotype, TSearchSpace, TProblem, TAlgorithmState> algorithm)
    where TGenotype : class
    where TSearchSpace : class, ISearchSpace<TGenotype>
    where TProblem : class, IProblem<TGenotype, TSearchSpace>
    where TAlgorithmState : class, IAlgorithmState
  {
    public IAsyncEnumerable<TAlgorithmState> RunStreamingAsync(
      TProblem problem,
      IRandomNumberGenerator random,
      TAlgorithmState? initialState = null,
      CancellationToken ct = default)
    {
      var algorithmInstance = algorithm.CreateExecutionInstance(new ExecutionInstanceRegistry());
      return algorithmInstance.RunStreamingAsync(problem, random, initialState, ct);
    }
    
    public async Task<TAlgorithmState> RunToCompletionAsync(
      TProblem problem,
      IRandomNumberGenerator random,
      TAlgorithmState? initialState = null,
      CancellationToken ct = default
    )
    {
      return await algorithm.RunStreamingAsync(problem, random, initialState, ct).LastAsync(cancellationToken: ct);
    }

    public IEnumerable<TAlgorithmState> RunStreaming(
      TProblem problem,
      IRandomNumberGenerator random,
      TAlgorithmState? initialState = null,
      CancellationToken ct = default
    )
    {
      return algorithm.RunStreamingAsync(problem, random, initialState, ct).ToBlockingEnumerable();
    }

    public TAlgorithmState RunToCompletion(
      TProblem problem,
      IRandomNumberGenerator random,
      TAlgorithmState? initialState = null,
      CancellationToken ct = default
    )
    {
      return algorithm.RunToCompletionAsync(problem, random, initialState, ct).GetAwaiter().GetResult();
    }
  }
  
  
  
  extension<TGenotype, TSearchSpace, TProblem, TAlgorithmState>(IAlgorithmInstance<TGenotype, TSearchSpace, TProblem, TAlgorithmState> algorithmInstance)
    where TGenotype : class
    where TSearchSpace : class, ISearchSpace<TGenotype>
    where TProblem : class, IProblem<TGenotype, TSearchSpace>
    where TAlgorithmState : class, IAlgorithmState
  {
    public async Task<TAlgorithmState> RunToCompletionAsync(
      TProblem problem,
      IRandomNumberGenerator random,
      TAlgorithmState? initialState = null,
      CancellationToken ct = default
    )
    {
      return await algorithmInstance.RunStreamingAsync(problem, random, initialState, ct).LastAsync(cancellationToken: ct);
    }

    public IEnumerable<TAlgorithmState> RunStreaming(
      TProblem problem,
      IRandomNumberGenerator random,
      TAlgorithmState? initialState = null,
      CancellationToken ct = default
    )
    {
      return algorithmInstance.RunStreamingAsync(problem, random, initialState, ct).ToBlockingEnumerable();
    }

    public TAlgorithmState RunToCompletion(
      TProblem problem,
      IRandomNumberGenerator random,
      TAlgorithmState? initialState = null,
      CancellationToken ct = default
    )
    {
      return algorithmInstance.RunToCompletionAsync(problem, random, initialState, ct).GetAwaiter().GetResult();
    }
  }
}
