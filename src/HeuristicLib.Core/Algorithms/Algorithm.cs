using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Evaluators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Algorithms;

public abstract record Algorithm<TGenotype, TSearchSpace, TProblem, TAlgorithmState, TState>
  : IAlgorithm<TGenotype, TSearchSpace, TProblem, TAlgorithmState>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
  where TAlgorithmState : class, IAlgorithmState
//where TState : Algorithm<TGenotype, TSearchSpace, TProblem, TAlgorithmState, TState>.AlgorithmState
{
  protected abstract TState CreateInitialState(ExecutionInstanceRegistry instanceRegistry);

  // ToDo: Since we have an evaluator, should we rename the class to "Optimization-Algorithm" or "Solver" or something like this?
  public IEvaluator<TGenotype, TSearchSpace, TProblem> Evaluator { get; init; } = new DirectEvaluator<TGenotype>();

  public IAlgorithmInstance<TGenotype, TSearchSpace, TProblem, TAlgorithmState> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) => new AlgorithmInstance(this, CreateInitialState(instanceRegistry));

  protected abstract IAsyncEnumerable<TAlgorithmState> RunStreamingAsync(TState state,
                                                                         TProblem problem,
                                                                         IRandomNumberGenerator random,
                                                                         TAlgorithmState? initialState = null,
                                                                         CancellationToken ct = default);

  protected record AlgorithmInstance(Algorithm<TGenotype, TSearchSpace, TProblem, TAlgorithmState, TState> Algorithm, TState State)
    : IAlgorithmInstance<TGenotype, TSearchSpace, TProblem, TAlgorithmState>
  {
    public IAsyncEnumerable<TAlgorithmState> RunStreamingAsync(TProblem problem, IRandomNumberGenerator random, TAlgorithmState? initialState = null, CancellationToken ct = default)
    {
      return Algorithm.RunStreamingAsync(State, problem, random, initialState, ct);
    }
  }

  public class AlgorithmState
  {
    public IEvaluatorInstance<TGenotype, TSearchSpace, TProblem> Evaluator { get; }
    public Run Run { get; }

    protected AlgorithmState(ExecutionInstanceRegistry instanceRegistry, Algorithm<TGenotype, TSearchSpace, TProblem, TAlgorithmState, TState> algorithm)
    {
      Evaluator = instanceRegistry.Resolve(algorithm.Evaluator);
      Run = instanceRegistry.Run;
    }
  }
}

public class NoState
{
  public static readonly NoState Instance = new();
  private NoState() { }
}

public static class AlgorithmExtensions
{
  extension<TGenotype, TSearchSpace, TProblem, TAlgorithmState>(IAlgorithm<TGenotype, TSearchSpace, TProblem, TAlgorithmState> algorithm)
    where TSearchSpace : class, ISearchSpace<TGenotype>
    where TProblem : class, IProblem<TGenotype, TSearchSpace>
    where TAlgorithmState : class, IAlgorithmState
  {
    public Run<TGenotype, TSearchSpace, TProblem, TAlgorithmState> CreateRun(TProblem problem, params IReadOnlyList<IAnalyzer> analyzers)
    {
      return new Run<TGenotype, TSearchSpace, TProblem, TAlgorithmState>(algorithm, problem, analyzers);
    }

    public IAsyncEnumerable<TAlgorithmState> RunStreamingAsync(
      TProblem problem,
      IRandomNumberGenerator random,
      TAlgorithmState? initialState = null,
      CancellationToken ct = default)
    {
      var run = algorithm.CreateRun(problem);
      return run.RunStreamingAsync(random, initialState, ct);
    }

    public async Task<TAlgorithmState> RunToCompletionAsync(
      TProblem problem,
      IRandomNumberGenerator random,
      TAlgorithmState? initialState = null,
      CancellationToken ct = default
    )
    {
      var run = algorithm.CreateRun(problem);
      return await run.RunToCompletionAsync(random, initialState, ct);
    }

    public IEnumerable<TAlgorithmState> RunStreaming(
      TProblem problem,
      IRandomNumberGenerator random,
      TAlgorithmState? initialState = null,
      CancellationToken ct = default
    )
    {
      var run = algorithm.CreateRun(problem);
      return run.RunStreaming(random, initialState, ct);
    }

    public TAlgorithmState RunToCompletion(
      TProblem problem,
      IRandomNumberGenerator random,
      TAlgorithmState? initialState = null,
      CancellationToken ct = default
    )
    {
      var run = algorithm.CreateRun(problem);
      return run.RunToCompletion(random, initialState, ct);
    }
  }

  extension<TGenotype, TSearchSpace, TProblem, TAlgorithmState>(IAlgorithmInstance<TGenotype, TSearchSpace, TProblem, TAlgorithmState> algorithmInstance)
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
      return await algorithmInstance.RunStreamingAsync(problem, random, initialState, ct).LastAsync(ct);
    }

    public IEnumerable<TAlgorithmState> RunStreaming(
      TProblem problem,
      IRandomNumberGenerator random,
      TAlgorithmState? initialState = null,
      CancellationToken ct = default
    )
    {
      return algorithmInstance.RunStreamingAsync(problem, random, initialState, ct).ToBlockingEnumerable(ct);
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
