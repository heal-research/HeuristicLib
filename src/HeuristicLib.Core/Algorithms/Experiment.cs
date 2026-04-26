using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Experiments;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Algorithms;

// ToDo: think about how to offer better parallel execution.
public abstract record Experiment<TGenotype, TSearchSpace, TProblem, TSearchState, TAlgorithmKey>
  : IExperiment<TGenotype, TSearchSpace, TProblem, TSearchState, TAlgorithmKey>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
  where TSearchState : class, ISearchState
{
  public abstract IExperimentInstance<TGenotype, TSearchSpace, TProblem, TSearchState, TAlgorithmKey> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry);
}

public abstract class ExperimentInstance<TGenotype, TSearchSpace, TProblem, TSearchState, TAlgorithmKey>
  : IExperimentInstance<TGenotype, TSearchSpace, TProblem, TSearchState, TAlgorithmKey>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
  where TSearchState : class, ISearchState
{
  public abstract IReadOnlyList<KeyValuePair<TAlgorithmKey, IAsyncEnumerable<TSearchState>>> RunStreamingAsync(TProblem problem, IRandomNumberGenerator random, TSearchState? initialState = null, CancellationToken ct = default);
}

public static class MultiStreamAlgorithmExtensions
{
  extension<TGenotype, TSearchSpace, TProblem, TSearchState, TAlgorithm, TAlgorithmKey>(IExperiment<TGenotype, TSearchSpace, TProblem, TSearchState, TAlgorithmKey> algorithm)
    where TSearchSpace : class, ISearchSpace<TGenotype>
    where TProblem : class, IProblem<TGenotype, TSearchSpace>
    where TSearchState : class, ISearchState
    where TAlgorithm : class, IAlgorithm<TGenotype, TSearchSpace, TProblem, TSearchState>
  {
    // public Run CreateRuns(TProblem problem)
    // {
    //   return new Run<TGenotype, TSearchSpace, TProblem, TSearchState>(algorithm, problem);
    //   
    //   return algorithmInstance.RunStreamingAsync(problem, null!, null).Select(kvp => new Run<TGenotype, TSearchSpace, TProblem, TSearchState>(kvp.Key, problem)).ToList();
    // }


    public IReadOnlyList<KeyValuePair<TAlgorithmKey, IAsyncEnumerable<TSearchState>>> RunStreamingAsync(TProblem problem, IRandomNumberGenerator random, TSearchState? initialState = null, CancellationToken ct = default)
    {
      //var run = new Run<TGenotype, TSearchSpace, TProblem, TSearchState>(algorithm, problem);
      // ToDo: think about to avoid two run types
      Run run = null!;
      var algorithmInstance = algorithm.CreateExecutionInstance(run);
      return algorithmInstance.RunStreamingAsync(problem, random, initialState, ct);
    }

    public async Task<IReadOnlyList<KeyValuePair<TAlgorithmKey, TSearchState>>> RunToCompletionAsync(TProblem problem, IRandomNumberGenerator random, TSearchState? initialState = null, CancellationToken cancellationToken = default)
    {
      Run run = null!;
      var algorithmInstance = algorithm.CreateExecutionInstance(run);
      var tasks = algorithmInstance.RunStreamingAsync(problem, random, initialState, cancellationToken)
                                   .Select(async kvp => KeyValuePair.Create(kvp.Key, await kvp.Value.LastAsync(cancellationToken)))
                                   .ToList();
      return await Task.WhenAll(tasks);
    }

    public IReadOnlyList<KeyValuePair<TAlgorithmKey, IEnumerable<TSearchState>>> RunStreaming(
      TProblem problem,
      IRandomNumberGenerator random,
      TSearchState? initialState = null,
      CancellationToken ct = default
    )
    {
      Run run = null!;
      var algorithmInstance = algorithm.CreateExecutionInstance(run);
      return algorithmInstance.RunStreamingAsync(problem, random, initialState, ct).Select(kvp => KeyValuePair.Create(kvp.Key, kvp.Value.ToBlockingEnumerable(ct))).ToList();
    }

    public IReadOnlyList<KeyValuePair<TAlgorithmKey, TSearchState>> RunToCompletion(
      TProblem problem,
      IRandomNumberGenerator random,
      TSearchState? initialState = null,
      CancellationToken ct = default
    )
    {
      Run run = null!;
      var algorithmInstance = algorithm.CreateExecutionInstance(run);
      return algorithmInstance.RunToCompletionAsync<TGenotype, TSearchSpace, TProblem, TSearchState, TAlgorithm, TAlgorithmKey>(problem, random, initialState, ct).GetAwaiter().GetResult();
    }
  }

  extension<TGenotype, TSearchSpace, TProblem, TSearchState, TAlgorithm, TAlgorithmKey>(IExperimentInstance<TGenotype, TSearchSpace, TProblem, TSearchState, TAlgorithmKey> algorithmInstance)
    where TSearchSpace : class, ISearchSpace<TGenotype>
    where TProblem : class, IProblem<TGenotype, TSearchSpace>
    where TSearchState : class, ISearchState
    where TAlgorithm : class, IAlgorithm<TGenotype, TSearchSpace, TProblem, TSearchState>
  {
    public async Task<IReadOnlyList<KeyValuePair<TAlgorithmKey, TSearchState>>> RunToCompletionAsync(TProblem problem, IRandomNumberGenerator random, TSearchState? initialState = null, CancellationToken cancellationToken = default)
    {
      var tasks = algorithmInstance.RunStreamingAsync(problem, random, initialState, cancellationToken)
                                   .Select(async kvp => KeyValuePair.Create(kvp.Key, await kvp.Value.LastAsync(cancellationToken)))
                                   .ToList();
      return await Task.WhenAll(tasks);
    }

    public IReadOnlyList<KeyValuePair<TAlgorithmKey, IEnumerable<TSearchState>>> RunStreaming(
      TProblem problem,
      IRandomNumberGenerator random,
      TSearchState? initialState = null,
      CancellationToken ct = default
    )
    {
      return algorithmInstance.RunStreamingAsync(problem, random, initialState, ct).Select(kvp => KeyValuePair.Create(kvp.Key, kvp.Value.ToBlockingEnumerable(ct))).ToList();
    }

    public IReadOnlyList<KeyValuePair<TAlgorithmKey, TSearchState>> RunToCompletion(
      TProblem problem,
      IRandomNumberGenerator random,
      TSearchState? initialState = null,
      CancellationToken ct = default
    )
    {
      return algorithmInstance.RunToCompletionAsync<TGenotype, TSearchSpace, TProblem, TSearchState, TAlgorithm, TAlgorithmKey>(problem, random, initialState, ct).GetAwaiter().GetResult();
    }
  }
}
