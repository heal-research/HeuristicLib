using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Algorithms.MultiStreamAlgorithms;

public class RepeatAlgorithm<TGenotype, TSearchSpace, TProblem, TAlgorithmState, TAlgorithm>
  : MultiStreamAlgorithm<TGenotype, TSearchSpace, TProblem, TAlgorithmState, int>
  where TGenotype : class
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
  where TAlgorithmState : class, IAlgorithmState
  where TAlgorithm : class, IAlgorithm<TGenotype, TSearchSpace, TProblem, TAlgorithmState>
{
  public required TAlgorithm Algorithm { get; init; }
  public int Repetitions { get; init; } = 5;

  public override RepeatedAlgorithmInstance<TGenotype, TSearchSpace, TProblem, TAlgorithmState, TAlgorithm> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry)
  {
    return new RepeatedAlgorithmInstance<TGenotype,TSearchSpace,TProblem,TAlgorithmState,TAlgorithm>(Algorithm, Repetitions);
  }
}

public class RepeatedAlgorithmInstance<TGenotype, TSearchSpace, TProblem, TAlgorithmState, TAlgorithm>
  : MultiStreamAlgorithmInstance<TGenotype, TSearchSpace, TProblem, TAlgorithmState, int>
  where TGenotype : class
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
  where TAlgorithmState : class, IAlgorithmState
  where TAlgorithm : class, IAlgorithm<TGenotype, TSearchSpace, TProblem, TAlgorithmState>
{
  protected readonly TAlgorithm Algorithm;
  protected readonly int Repetitions;

  public RepeatedAlgorithmInstance(TAlgorithm algorithm, int repetitions)
  {
    Algorithm = algorithm;
    Repetitions = repetitions;
  }

  public override IReadOnlyList<KeyValuePair<int, IAsyncEnumerable<TAlgorithmState>>> RunStreamingAsync(TProblem problem, IRandomNumberGenerator random, TAlgorithmState? initialState = null, CancellationToken ct = default)
  {
    return Enumerable.Range(0, Repetitions)
      .Select(repetitionsIndex => {
        var repetitionRng = random.Fork(repetitionsIndex);

        return KeyValuePair.Create(repetitionsIndex, Algorithm.RunStreamingAsync(problem, repetitionRng, initialState, ct));
      }).ToList();
  }
}

public static class RepeatExecutionExtensions
{
  extension<TGenotype, TSearchSpace, TProblem, TAlgorithmState, TAlgorithm>(RepeatAlgorithm<TGenotype, TSearchSpace, TProblem, TAlgorithmState, TAlgorithm> algorithm)
    where TGenotype : class
    where TSearchSpace : class, ISearchSpace<TGenotype>
    where TProblem : class, IProblem<TGenotype, TSearchSpace>
    where TAlgorithmState : class, IAlgorithmState
    where TAlgorithm : class, IAlgorithm<TGenotype, TSearchSpace, TProblem, TAlgorithmState>
  {
    public IAsyncEnumerable<KeyValuePair<(int Repetition, int Iteration), TAlgorithmState>> RunInterleavedStreamingAsync(TProblem problem, IRandomNumberGenerator random, TAlgorithmState? initialState = null, CancellationToken cancellationToken = default)
    {
      var algorithmInstance = algorithm.CreateExecutionInstance(new ExecutionInstanceRegistry());
      return algorithmInstance.RunInterleavedStreamingAsync(problem, random, initialState, cancellationToken);
    }
    
    public IEnumerable<KeyValuePair<(int Repetition, int Iteration), TAlgorithmState>> RunInterleavedStreaming(TProblem problem, IRandomNumberGenerator random, TAlgorithmState? initialState = null, CancellationToken cancellationToken = default)
    {
      var algorithmInstance = algorithm.CreateExecutionInstance(new ExecutionInstanceRegistry());
      return algorithmInstance.RunInterleavedStreaming(problem, random, initialState, cancellationToken);
    }
  }
  
  extension<TGenotype, TSearchSpace, TProblem, TAlgorithmState, TAlgorithm>(RepeatedAlgorithmInstance<TGenotype, TSearchSpace, TProblem, TAlgorithmState, TAlgorithm> algorithmInstance)
    where TGenotype : class
    where TSearchSpace : class, ISearchSpace<TGenotype>
    where TProblem : class, IProblem<TGenotype, TSearchSpace>
    where TAlgorithmState : class, IAlgorithmState
    where TAlgorithm : class, IAlgorithm<TGenotype, TSearchSpace, TProblem, TAlgorithmState>
  {
    public IAsyncEnumerable<KeyValuePair<(int Repetition, int Iteration), TAlgorithmState>> RunInterleavedStreamingAsync(TProblem problem, IRandomNumberGenerator random, TAlgorithmState? initialState = null, CancellationToken cancellationToken = default)
    {
      var sourceStreams = algorithmInstance.RunStreamingAsync(problem, random, initialState, cancellationToken)
        .Select((stream, index) => stream.Value.Select(state => KeyValuePair.Create((stream.Key, index), state)));
      return sourceStreams.Merge();
    }
    public IEnumerable<KeyValuePair<(int Repetition, int Iteration), TAlgorithmState>> RunInterleavedStreaming(TProblem problem, IRandomNumberGenerator random, TAlgorithmState? initialState = null, CancellationToken cancellationToken = default)
    {
      return algorithmInstance.RunInterleavedStreamingAsync(problem, random, initialState, cancellationToken).ToBlockingEnumerable(cancellationToken);
    }
  }

  extension<TGenotype, TSearchSpace, TProblem, TAlgorithmState, TAlgorithm>(TAlgorithm algorithm)
    where TGenotype : class
    where TSearchSpace : class, ISearchSpace<TGenotype>
    where TProblem : class, IProblem<TGenotype, TSearchSpace>
    where TAlgorithmState : class, IAlgorithmState
    where TAlgorithm : class, IAlgorithm<TGenotype, TSearchSpace, TProblem, TAlgorithmState>
  {
    public RepeatAlgorithm<TGenotype, TSearchSpace, TProblem, TAlgorithmState, TAlgorithm> Repeat(int repetitions)
    {
      return new RepeatAlgorithm<TGenotype, TSearchSpace, TProblem, TAlgorithmState, TAlgorithm> {
        Algorithm = algorithm,
        Repetitions = repetitions
      };
    }
  }
}
