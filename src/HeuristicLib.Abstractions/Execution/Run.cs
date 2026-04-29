using System.Diagnostics.CodeAnalysis;
using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Execution;

public abstract class Run
{
  private readonly Lazy<ExecutionInstanceRegistry> rootRegistry;
  protected ExecutionInstanceRegistry RootRegistry => rootRegistry.Value;

  private readonly ObservationPlan observationPlan = new();

  private readonly Dictionary<IAnalyzer, IAnalyzerRunState> analyzerStates;

  protected Run(IReadOnlyList<IAnalyzer>? analyzers = null)
  {
    analyzerStates = new Dictionary<IAnalyzer, IAnalyzerRunState>(ReferenceEqualityComparer.Instance);

    foreach (var analyzer in analyzers ?? []) {
      var analyzerState = analyzer.CreateAnalyzerState();
      analyzerStates.Add(analyzer, analyzerState);
      analyzerState.RegisterObservations(observationPlan);
    }

    rootRegistry = new Lazy<ExecutionInstanceRegistry>(() => {
      var registry = new ExecutionInstanceRegistry(this, parentRegistry: null);
      InstallObservations(registry, observationPlan);
      return registry;
    });

    _ = RootRegistry;
  }

  private static void InstallObservations(ExecutionInstanceRegistry registry, ObservationPlan observationPlan)
  {
    observationPlan.Install(registry);
  }

  public ExecutionInstanceRegistry CreateNewRegistry()
  {
    var registry = new ExecutionInstanceRegistry(this, parentRegistry: null);
    InstallObservations(registry, observationPlan);
    return registry;
  }

  public ExecutionInstanceRegistry CreateChildRegistry()
  {
    return RootRegistry.CreateChildRegistry();
  }

  public TAnalyzerRunState GetAnalyzerResult<TAnalyzerRunState>(IAnalyzer<TAnalyzerRunState> analyzer)
    where TAnalyzerRunState : class, IAnalyzerRunState
  {
    if (analyzerStates.TryGetValue(analyzer, out var state)) {
      return (TAnalyzerRunState)state;
    }

    throw new KeyNotFoundException($"No analyzer found for analyzer {analyzer}");
  }

  public bool TryGetAnalyzerResult<TAnalyzerRunState>(IAnalyzer<TAnalyzerRunState> analyzer, [MaybeNullWhen(false)] out TAnalyzerRunState analyzerRunState)
    where TAnalyzerRunState : class, IAnalyzerRunState
  {
    if (analyzerStates.TryGetValue(analyzer, out var state)) {
      analyzerRunState = (TAnalyzerRunState)state;
      return true;
    }

    analyzerRunState = null;
    return false;
  }

  public TAnalyzerRunState GetResult<TAnalyzerRunState>(IAnalyzer<TAnalyzerRunState> analyzer)
    where TAnalyzerRunState : class, IAnalyzerRunState
    => GetAnalyzerResult(analyzer);

  public bool TryGetResult<TAnalyzerRunState>(IAnalyzer<TAnalyzerRunState> analyzer, [MaybeNullWhen(false)] out TAnalyzerRunState analyzerRunState)
    where TAnalyzerRunState : class, IAnalyzerRunState
    => TryGetAnalyzerResult(analyzer, out analyzerRunState);
}

public class Run<TGenotype, TSearchSpace, TProblem, TSearchState> : Run
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
  where TSearchState : class, ISearchState
{
  public IAlgorithm<TGenotype, TSearchSpace, TProblem, TSearchState> Algorithm { get; }

  public TProblem Problem { get; }

  public Run(IAlgorithm<TGenotype, TSearchSpace, TProblem, TSearchState> algorithm, TProblem problem, IReadOnlyList<IAnalyzer>? analyzers = null)
    : base(analyzers)
  {
    Algorithm = algorithm;
    Problem = problem;
  }

  public IAsyncEnumerable<TSearchState> RunStreamingAsync(IRandomNumberGenerator random, TSearchState? initialState = null, CancellationToken cancellationToken = default)
  {
    var instance = RootRegistry.Resolve(Algorithm);
    return instance.RunStreamingAsync(Problem, random, initialState, cancellationToken);
  }

  public async Task<TSearchState> RunToCompletionAsync(IRandomNumberGenerator random, TSearchState? initialState = null, CancellationToken cancellationToken = default)
  {
    return await RunStreamingAsync(random, initialState, cancellationToken).LastAsync(cancellationToken);
  }

  public IEnumerable<TSearchState> RunStreaming(IRandomNumberGenerator random, TSearchState? initialState = null, CancellationToken cancellationToken = default)
  {
    return RunStreamingAsync(random, initialState, cancellationToken).ToBlockingEnumerable(cancellationToken);
  }

  public TSearchState RunToCompletion(IRandomNumberGenerator random, TSearchState? initialState = null, CancellationToken cancellationToken = default)
  {
    return RunToCompletionAsync(random, initialState, cancellationToken).GetAwaiter().GetResult();
  }
}
