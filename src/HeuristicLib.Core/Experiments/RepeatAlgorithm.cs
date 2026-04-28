using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Experiments;

public record RepeatAlgorithm<TGenotype, TSearchSpace, TProblem, TSearchState, TAlgorithm>
  : Experiment<TGenotype, TSearchSpace, TProblem, TSearchState, int>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
  where TSearchState : class, ISearchState
  where TAlgorithm : class, IAlgorithm<TGenotype, TSearchSpace, TProblem, TSearchState>
{
  public required TAlgorithm Algorithm { get; init; }
  public int Repetitions { get; init; } = 5;

  public override RepeatedAlgorithmInstance<TGenotype, TSearchSpace, TProblem, TSearchState, TAlgorithm> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry)
  {
    return new RepeatedAlgorithmInstance<TGenotype, TSearchSpace, TProblem, TSearchState, TAlgorithm>(Algorithm, Repetitions);
  }
}

public class RepeatedAlgorithmInstance<TGenotype, TSearchSpace, TProblem, TSearchState, TAlgorithm>
  : ExperimentInstance<TGenotype, TSearchSpace, TProblem, TSearchState, int>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
  where TSearchState : class, ISearchState
  where TAlgorithm : class, IAlgorithm<TGenotype, TSearchSpace, TProblem, TSearchState>
{
  protected readonly TAlgorithm Algorithm;
  protected readonly int Repetitions;

  public RepeatedAlgorithmInstance(TAlgorithm algorithm, int repetitions)
  {
    Algorithm = algorithm;
    Repetitions = repetitions;
  }

  public override IReadOnlyList<KeyValuePair<int, IAsyncEnumerable<TSearchState>>> RunStreamingAsync(TProblem problem, IRandomNumberGenerator random, TSearchState? initialState = null, CancellationToken ct = default)
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
  extension<TGenotype, TSearchSpace, TProblem, TSearchState, TAlgorithm>(RepeatAlgorithm<TGenotype, TSearchSpace, TProblem, TSearchState, TAlgorithm> algorithm)
    where TSearchSpace : class, ISearchSpace<TGenotype>
    where TProblem : class, IProblem<TGenotype, TSearchSpace>
    where TSearchState : class, ISearchState
    where TAlgorithm : class, IAlgorithm<TGenotype, TSearchSpace, TProblem, TSearchState>
  {
    public IAsyncEnumerable<KeyValuePair<(int Repetition, int Iteration), TSearchState>> RunInterleavedStreamingAsync(TProblem problem, IRandomNumberGenerator random, TSearchState? initialState = null, CancellationToken cancellationToken = default)
    {
      return algorithm.RunInterleavedStreamingAsync(problem, random, initialState, cancellationToken);
    }

    public IEnumerable<KeyValuePair<(int Repetition, int Iteration), TSearchState>> RunInterleavedStreaming(TProblem problem, IRandomNumberGenerator random, TSearchState? initialState = null, CancellationToken cancellationToken = default)
    {
      return algorithm.RunInterleavedStreaming(problem, random, initialState, cancellationToken);
    }
  }

  extension<TGenotype, TSearchSpace, TProblem, TSearchState, TAlgorithm>(RepeatedAlgorithmInstance<TGenotype, TSearchSpace, TProblem, TSearchState, TAlgorithm> algorithmInstance)
    where TSearchSpace : class, ISearchSpace<TGenotype>
    where TProblem : class, IProblem<TGenotype, TSearchSpace>
    where TSearchState : class, ISearchState
    where TAlgorithm : class, IAlgorithm<TGenotype, TSearchSpace, TProblem, TSearchState>
  {
    public IAsyncEnumerable<KeyValuePair<(int Repetition, int Iteration), TSearchState>> RunInterleavedStreamingAsync(TProblem problem, IRandomNumberGenerator random, TSearchState? initialState = null, CancellationToken cancellationToken = default)
    {
      var sourceStreams = algorithmInstance.RunStreamingAsync(problem, random, initialState, cancellationToken)
                                           .Select((stream, index) => stream.Value.Select(state => KeyValuePair.Create((stream.Key, index), state)));
      return sourceStreams.Merge();
    }

    public IEnumerable<KeyValuePair<(int Repetition, int Iteration), TSearchState>> RunInterleavedStreaming(TProblem problem, IRandomNumberGenerator random, TSearchState? initialState = null, CancellationToken cancellationToken = default)
    {
      return algorithmInstance.RunInterleavedStreamingAsync(problem, random, initialState, cancellationToken).ToBlockingEnumerable(cancellationToken);
    }
  }

  extension<TGenotype, TSearchSpace, TProblem, TSearchState, TAlgorithm>(TAlgorithm algorithm)
    where TSearchSpace : class, ISearchSpace<TGenotype>
    where TProblem : class, IProblem<TGenotype, TSearchSpace>
    where TSearchState : class, ISearchState
    where TAlgorithm : class, IAlgorithm<TGenotype, TSearchSpace, TProblem, TSearchState>
  {
    public RepeatAlgorithm<TGenotype, TSearchSpace, TProblem, TSearchState, TAlgorithm> Repeat(int repetitions)
    {
      return new RepeatAlgorithm<TGenotype, TSearchSpace, TProblem, TSearchState, TAlgorithm> {
        Algorithm = algorithm,
        Repetitions = repetitions
      };
    }
  }
}
