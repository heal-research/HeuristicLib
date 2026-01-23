using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Executors;

public class RepeatedExecutor<TGenotype, TSearchSpace, TProblem, TAlgorithmState> 
  : IRepeatedExecutor<TGenotype, TSearchSpace, TProblem, TAlgorithmState>
  where TGenotype : class
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
  where TAlgorithmState : class, IAlgorithmState
{
  public required IAlgorithm<TGenotype, TSearchSpace, TProblem, TAlgorithmState> Algorithm { get; init; }
  public int Repetitions { get; init; } = 5;
  
  public IReadOnlyList<IAsyncEnumerable<TAlgorithmState>> ExecuteStreamingAsync(TProblem problem, IRandomNumberGenerator random, TAlgorithmState? initialState = null, CancellationToken cancellationToken = default) {
    return Enumerable.Range(0, Repetitions)
      .Select(repetitionsIndex => {
        var repetitionRng = random.Fork(repetitionsIndex);
        return Algorithm.ExecuteStreamingAsync(problem, repetitionRng, initialState, cancellationToken);
      }).ToList();
  }
}

// Maybe a Tuple would be enough
public record RepetitionResult<TAlgorithmState>(
  int RepetitionIndex,
  TAlgorithmState State
) where TAlgorithmState : class, IAlgorithmState;

public static class RepeatedExecutionExtensions
{
  extension<TAlgorithm, TGenotype, TSearchSpace, TProblem, TAlgorithmState>(IRepeatedExecutor<TGenotype, TSearchSpace, TProblem, TAlgorithmState> executor)
    where TAlgorithm : IAlgorithm<TGenotype, TSearchSpace, TProblem, TAlgorithmState>
    where TGenotype : class
    where TSearchSpace : class, ISearchSpace<TGenotype>
    where TProblem : class, IProblem<TGenotype, TSearchSpace>
    where TAlgorithmState : class, IAlgorithmState 
  {
    public IReadOnlyList<TAlgorithmState> Execute(TProblem problem, IRandomNumberGenerator random, TAlgorithmState? initialState = null, CancellationToken cancellationToken = default) {
      return executor.ExecuteStreamingAsync(problem, random, initialState, cancellationToken)
        .Select(stream => stream.LastAsync(cancellationToken).AsTask().Result)
        .ToList();
    }
    
    public IAsyncEnumerable<RepetitionResult<TAlgorithmState>> ExecuteInterleavedStreamingAsync(TProblem problem, IRandomNumberGenerator random, TAlgorithmState? initialState = null, CancellationToken cancellationToken = default) 
    {
      var sourceStreams = executor.ExecuteStreamingAsync(problem, random, initialState, cancellationToken)
        .Select((stream, index) => stream.Select(state => new RepetitionResult<TAlgorithmState>(index, state)));
      return sourceStreams.Merge();
    }

    public IReadOnlyList<IReadOnlyList<TAlgorithmState>> ExecuteEagerly(TProblem problem, IRandomNumberGenerator random, TAlgorithmState? initialState = null, CancellationToken cancellationToken = default)
    {
      var sourceLists = executor.ExecuteStreamingAsync(problem, random, initialState, cancellationToken)
        .Select(stream => stream.ToListAsync(cancellationToken).AsTask().Result);
      return sourceLists.ToList();
    }
  }
}
