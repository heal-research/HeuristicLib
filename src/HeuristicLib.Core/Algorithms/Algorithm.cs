using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Evaluators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Algorithms;

public abstract record Algorithm<TGenotype, TSearchSpace, TProblem, TSearchState, TExecutionState>
  : IAlgorithm<TGenotype, TSearchSpace, TProblem, TSearchState>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
  where TSearchState : class, ISearchState
  where TExecutionState : Algorithm<TGenotype, TSearchSpace, TProblem, TSearchState, TExecutionState>.ExecutionState
{
  public class ExecutionState
  {
    public required IEvaluatorInstance<TGenotype, TSearchSpace, TProblem> Evaluator { get; init; }
  }

  // NOTE: Evaluator remains part of the base algorithm contract for now.
  public IEvaluator<TGenotype, TSearchSpace, TProblem> Evaluator { get; init; } = new DirectEvaluator<TGenotype>();

  protected abstract TExecutionState CreateInitialExecutionState(IExecutionInstanceResolver resolver);

  protected abstract IAlgorithmInstance<TGenotype, TSearchSpace, TProblem, TSearchState> CreateAlgorithmInstance(Run run, TExecutionState executionState);

  public IAlgorithmInstance<TGenotype, TSearchSpace, TProblem, TSearchState> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry)
  {
    return CreateAlgorithmInstance(instanceRegistry.Run, CreateInitialExecutionState(instanceRegistry));
  }
}

public abstract class AlgorithmInstance<TGenotype, TSearchSpace, TProblem, TSearchState>
  : IAlgorithmInstance<TGenotype, TSearchSpace, TProblem, TSearchState>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
  where TSearchState : class, ISearchState
{
  protected readonly IEvaluatorInstance<TGenotype, TSearchSpace, TProblem> Evaluator;

  public Run Run { get; }

  protected AlgorithmInstance(Run run, IEvaluatorInstance<TGenotype, TSearchSpace, TProblem> evaluator)
  {
    Run = run;
    Evaluator = evaluator;
  }

  public abstract IAsyncEnumerable<TSearchState> RunStreamingAsync(
    TProblem problem,
    IRandomNumberGenerator random,
    TSearchState? initialState = null,
    CancellationToken ct = default);
}

public static class AlgorithmExtensions
{
  extension<TGenotype, TSearchSpace, TProblem, TSearchState>(IAlgorithm<TGenotype, TSearchSpace, TProblem, TSearchState> algorithm)
    where TSearchSpace : class, ISearchSpace<TGenotype>
    where TProblem : class, IProblem<TGenotype, TSearchSpace>
    where TSearchState : class, ISearchState
  {
    public Run<TGenotype, TSearchSpace, TProblem, TSearchState> CreateRun(TProblem problem, params IReadOnlyList<IAnalyzer> analyzers)
    {
      return new Run<TGenotype, TSearchSpace, TProblem, TSearchState>(algorithm, problem, analyzers);
    }

    public IAsyncEnumerable<TSearchState> RunStreamingAsync(
      TProblem problem,
      IRandomNumberGenerator random,
      TSearchState? initialState = null,
      CancellationToken ct = default)
    {
      var run = algorithm.CreateRun(problem);
      return run.RunStreamingAsync(random, initialState, ct);

    }

    public async Task<TSearchState> RunToCompletionAsync(
      TProblem problem,
      IRandomNumberGenerator random,
      TSearchState? initialState = null,
      CancellationToken ct = default
    )
    {
      var run = algorithm.CreateRun(problem);
      return await run.RunToCompletionAsync(random, initialState, ct);
    }

    public IEnumerable<TSearchState> RunStreaming(
      TProblem problem,
      IRandomNumberGenerator random,
      TSearchState? initialState = null,
      CancellationToken ct = default
    )
    {
      var run = algorithm.CreateRun(problem);
      return run.RunStreaming(random, initialState, ct);
    }

    public TSearchState RunToCompletion(
      TProblem problem,
      IRandomNumberGenerator random,
      TSearchState? initialState = null,
      CancellationToken ct = default
    )
    {
      var run = algorithm.CreateRun(problem);
      return run.RunToCompletion(random, initialState, ct);
    }
  }

  extension<TGenotype, TSearchSpace, TProblem, TSearchState>(IAlgorithmInstance<TGenotype, TSearchSpace, TProblem, TSearchState> algorithmInstance)
    where TSearchSpace : class, ISearchSpace<TGenotype>
    where TProblem : class, IProblem<TGenotype, TSearchSpace>
    where TSearchState : class, ISearchState
  {
    public async Task<TSearchState> RunToCompletionAsync(
      TProblem problem,
      IRandomNumberGenerator random,
      TSearchState? initialState = null,
      CancellationToken ct = default
    )
    {
      return await algorithmInstance.RunStreamingAsync(problem, random, initialState, ct).LastAsync(ct);
    }

    public IEnumerable<TSearchState> RunStreaming(
      TProblem problem,
      IRandomNumberGenerator random,
      TSearchState? initialState = null,
      CancellationToken ct = default
    )
    {
      return algorithmInstance.RunStreamingAsync(problem, random, initialState, ct).ToBlockingEnumerable(ct);
    }

    public TSearchState RunToCompletion(
      TProblem problem,
      IRandomNumberGenerator random,
      TSearchState? initialState = null,
      CancellationToken ct = default
    )
    {
      return algorithmInstance.RunToCompletionAsync(problem, random, initialState, ct).GetAwaiter().GetResult();
    }
  }
}
