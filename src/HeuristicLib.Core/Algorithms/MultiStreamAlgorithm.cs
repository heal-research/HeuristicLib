using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Algorithms;

// ToDo: think about how to offer better parallel execution.
public abstract class MultiStreamAlgorithm<TGenotype, TSearchSpace, TProblem, TAlgorithmState, TAlgorithmKey>
  : IMultiStreamAlgorithm<TGenotype, TSearchSpace, TProblem, TAlgorithmState, TAlgorithmKey>
  where TGenotype : class
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
  where TAlgorithmState : class, IAlgorithmState
{
  public abstract IMultiStreamAlgorithmInstance<TGenotype, TSearchSpace, TProblem, TAlgorithmState, TAlgorithmKey> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry);
}

public abstract class MultiStreamAlgorithmInstance<TGenotype, TSearchSpace, TProblem, TAlgorithmState, TAlgorithmKey>
  : IMultiStreamAlgorithmInstance<TGenotype, TSearchSpace, TProblem, TAlgorithmState, TAlgorithmKey>
  where TGenotype : class
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
  where TAlgorithmState : class, IAlgorithmState
{
  public abstract IReadOnlyList<KeyValuePair<TAlgorithmKey, IAsyncEnumerable<TAlgorithmState>>> RunStreamingAsync(TProblem problem, IRandomNumberGenerator random, TAlgorithmState? initialState = null, CancellationToken ct = default);
}

public static class MultiStreamAlgorithmExtensions
{
  extension<TGenotype, TSearchSpace, TProblem, TAlgorithmState, TAlgorithm, TAlgorithmKey>(IMultiStreamAlgorithm<TGenotype, TSearchSpace, TProblem, TAlgorithmState, TAlgorithmKey> algorithm)
    where TGenotype : class
    where TSearchSpace : class, ISearchSpace<TGenotype>
    where TProblem : class, IProblem<TGenotype, TSearchSpace>
    where TAlgorithmState : class, IAlgorithmState
    where TAlgorithm : class, IAlgorithm<TGenotype, TSearchSpace, TProblem, TAlgorithmState>
  {
    public IReadOnlyList<KeyValuePair<TAlgorithmKey, IAsyncEnumerable<TAlgorithmState>>> RunStreamingAsync(TProblem problem, IRandomNumberGenerator random, TAlgorithmState? initialState = null, CancellationToken ct = default)
    {
      var algorithmInstance = algorithm.CreateExecutionInstance(new ExecutionInstanceRegistry());
      return algorithmInstance.RunStreamingAsync(problem, random, initialState, ct);
    }
    
    
    public async Task<IReadOnlyList<KeyValuePair<TAlgorithmKey, TAlgorithmState>>> RunToCompletionAsync(TProblem problem, IRandomNumberGenerator random, TAlgorithmState? initialState = null, CancellationToken cancellationToken = default)
    {
      var algorithmInstance = algorithm.CreateExecutionInstance(new ExecutionInstanceRegistry());
      var tasks = algorithmInstance.RunStreamingAsync(problem, random, initialState, cancellationToken)
        .Select(async kvp => KeyValuePair.Create(kvp.Key, await kvp.Value.LastAsync(cancellationToken)))
        .ToList();
      return await Task.WhenAll(tasks);
    }
    
    public IReadOnlyList<KeyValuePair<TAlgorithmKey, IEnumerable<TAlgorithmState>>> RunStreaming(
      TProblem problem,
      IRandomNumberGenerator random,
      TAlgorithmState? initialState = null,
      CancellationToken ct = default
    )
    {
      var algorithmInstance = algorithm.CreateExecutionInstance(new ExecutionInstanceRegistry());
      return algorithmInstance.RunStreamingAsync(problem, random, initialState, ct).Select(kvp => KeyValuePair.Create(kvp.Key, kvp.Value.ToBlockingEnumerable(ct))).ToList();
    }
    
    public IReadOnlyList<KeyValuePair<TAlgorithmKey, TAlgorithmState>> RunToCompletion(
      TProblem problem,
      IRandomNumberGenerator random,
      TAlgorithmState? initialState = null,
      CancellationToken ct = default
    )
    {
      var algorithmInstance = algorithm.CreateExecutionInstance(new ExecutionInstanceRegistry());
      return algorithmInstance.RunToCompletionAsync<TGenotype, TSearchSpace, TProblem, TAlgorithmState, TAlgorithm, TAlgorithmKey>(problem, random, initialState, ct).GetAwaiter().GetResult();
    }
  }
  
  extension<TGenotype, TSearchSpace, TProblem, TAlgorithmState, TAlgorithm, TAlgorithmKey>(IMultiStreamAlgorithmInstance<TGenotype, TSearchSpace, TProblem, TAlgorithmState, TAlgorithmKey> algorithmInstance)
    where TGenotype : class
    where TSearchSpace : class, ISearchSpace<TGenotype>
    where TProblem : class, IProblem<TGenotype, TSearchSpace>
    where TAlgorithmState : class, IAlgorithmState
    where TAlgorithm : class, IAlgorithm<TGenotype, TSearchSpace, TProblem, TAlgorithmState>
  {
    public async Task<IReadOnlyList<KeyValuePair<TAlgorithmKey, TAlgorithmState>>> RunToCompletionAsync(TProblem problem, IRandomNumberGenerator random, TAlgorithmState? initialState = null, CancellationToken cancellationToken = default)
    {
      var tasks = algorithmInstance.RunStreamingAsync(problem, random, initialState, cancellationToken)
        .Select(async kvp => KeyValuePair.Create(kvp.Key, await kvp.Value.LastAsync(cancellationToken)))
        .ToList();
      return await Task.WhenAll(tasks);
    }
    
    public IReadOnlyList<KeyValuePair<TAlgorithmKey, IEnumerable<TAlgorithmState>>> RunStreaming(
      TProblem problem,
      IRandomNumberGenerator random,
      TAlgorithmState? initialState = null,
      CancellationToken ct = default
    )
    {
      return algorithmInstance.RunStreamingAsync(problem, random, initialState, ct).Select(kvp => KeyValuePair.Create(kvp.Key, kvp.Value.ToBlockingEnumerable(ct))).ToList();
    }
    
    public IReadOnlyList<KeyValuePair<TAlgorithmKey, TAlgorithmState>> RunToCompletion(
      TProblem problem,
      IRandomNumberGenerator random,
      TAlgorithmState? initialState = null,
      CancellationToken ct = default
    )
    {
      return algorithmInstance.RunToCompletionAsync<TGenotype, TSearchSpace, TProblem, TAlgorithmState, TAlgorithm, TAlgorithmKey>(problem, random, initialState, ct).GetAwaiter().GetResult();
    }
  }
}
