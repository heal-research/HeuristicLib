using HEAL.HeuristicLib.Observation;
using HEAL.HeuristicLib.Operators.Interceptors;
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
  
  // IMetaAlgorithmExecution<TGenotype, TSearchSpace, TProblem, TAlgorithmState, TAlgorithm> IMetaAlgorithm<TGenotype, TSearchSpace, TProblem, TAlgorithmState, TAlgorithm>.CreateExecution() => this.CreateExecution();
  // public virtual Execution CreateExecution() => new Execution(this);
  
    public IInterceptor<TGenotype, TAlgorithmState, TSearchSpace, TProblem>? Interceptor { get; init; }
    public IIterationObserver<TGenotype, TSearchSpace, TProblem, TAlgorithmState>? Observer { get; init; }

    public  override IReadOnlyList<KeyValuePair<int, IAsyncEnumerable<TAlgorithmState>>> RunStreamingAsync(TProblem problem, IRandomNumberGenerator random, TAlgorithmState? initialState = null, CancellationToken ct = default)
    {
      return Enumerable.Range(0, Repetitions)
        .Select(repetitionsIndex => {
          var repetitionRng = random.Fork(repetitionsIndex);
          return KeyValuePair.Create(repetitionsIndex, Algorithm.RunStreamingAsync(problem, repetitionRng, initialState, ct));
      }).ToList();
    }

    // async IAsyncEnumerable<TAlgorithmState> IAlgorithm<TGenotype, TSearchSpace, TProblem, TAlgorithmState>.RunStreamingAsync(TProblem problem, IRandomNumberGenerator random, TAlgorithmState? initialState, [EnumeratorCancellation] CancellationToken ct)
    // {
    //   foreach (var stream in RunStreamingAsync(problem, random, initialState, ct)) {
    //     await foreach (var state in stream.Value.WithCancellation(ct)) {
    //       yield return state;
    //     }
    //   }
    // }


    // public IReadOnlyList<(TAlgorithm, IAsyncEnumerable<TAlgorithmState>)> ExecuteStreamingAsync(TProblem problem, IRandomNumberGenerator random, TAlgorithmState? initialState = null, CancellationToken cancellationToken = default)
    // {
    //   return Enumerable.Range(0, executor.Repetitions)
    //     .Select(repetitionsIndex => {
    //       var repetitionRng = random.Fork(repetitionsIndex);
    //       return (executor.Algorithm, executor.Algorithm.ExecuteStreamingAsync(problem, repetitionRng, initialState, cancellationToken));
    //     }).ToList();
    // }

  
  
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
      var sourceStreams = algorithm.RunStreamingAsync(problem, random, initialState, cancellationToken)
        .Select((stream, index) => stream.Value.Select(state => KeyValuePair.Create((stream.Key, index), state)));
      return sourceStreams.Merge();
    }
    public IEnumerable<KeyValuePair<(int Repetition, int Iteration), TAlgorithmState>> RunInterleavedStreaming(TProblem problem, IRandomNumberGenerator random, TAlgorithmState? initialState = null, CancellationToken cancellationToken = default)
    {
      return algorithm.RunInterleavedStreamingAsync(problem, random, initialState, cancellationToken).ToBlockingEnumerable(cancellationToken);
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
