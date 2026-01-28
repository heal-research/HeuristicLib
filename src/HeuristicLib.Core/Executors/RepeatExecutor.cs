using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Executors;

public class RepeatExecutor<TGenotype, TSearchSpace, TProblem, TAlgorithmState, TAlgorithm>
  : IExecutor<TGenotype, TSearchSpace, TProblem, TAlgorithmState, TAlgorithm>
  where TGenotype : class
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
  where TAlgorithmState : class, IAlgorithmState
  where TAlgorithm : class, IAlgorithm<TGenotype, TSearchSpace, TProblem, TAlgorithmState>
{
  public required TAlgorithm Algorithm { get; init; }
  public int Repetitions { get; init; } = 5;
  
  IExecutorExecution<TGenotype, TSearchSpace, TProblem, TAlgorithmState, TAlgorithm> IExecutor<TGenotype, TSearchSpace, TProblem, TAlgorithmState, TAlgorithm>.CreateExecution() => this.CreateExecution();
  public virtual Execution CreateExecution() => new Execution(this);

  public class Execution : IExecutorExecution<TGenotype, TSearchSpace, TProblem, TAlgorithmState, TAlgorithm>
  {
    private readonly RepeatExecutor<TGenotype, TSearchSpace, TProblem, TAlgorithmState, TAlgorithm> executor;

    public Execution(RepeatExecutor<TGenotype, TSearchSpace, TProblem, TAlgorithmState, TAlgorithm> executor)
    {
      this.executor = executor;
    }
    
    public IReadOnlyList<(TAlgorithm, IAsyncEnumerable<TAlgorithmState>)> ExecuteStreamingAsync(TProblem problem, IRandomNumberGenerator random, TAlgorithmState? initialState = null, CancellationToken cancellationToken = default)
    {
      return Enumerable.Range(0, executor.Repetitions)
        .Select(repetitionsIndex => {
          var repetitionRng = random.Fork(repetitionsIndex);
          return (executor.Algorithm, executor.Algorithm.ExecuteStreamingAsync(problem, repetitionRng, initialState, cancellationToken));
        }).ToList();
    }
  }
}

public static class RepeatExecutionExtensions
{
  extension<TGenotype, TSearchSpace, TProblem, TAlgorithmState, TAlgorithm>(RepeatExecutor<TGenotype, TSearchSpace, TProblem, TAlgorithmState, TAlgorithm> executor)
    where TGenotype : class
    where TSearchSpace : class, ISearchSpace<TGenotype>
    where TProblem : class, IProblem<TGenotype, TSearchSpace>
    where TAlgorithmState : class, IAlgorithmState
    where TAlgorithm : class, IAlgorithm<TGenotype, TSearchSpace, TProblem, TAlgorithmState>
  {
    public IReadOnlyList<(TAlgorithm, TAlgorithmState)> Execute(TProblem problem, IRandomNumberGenerator random, TAlgorithmState? initialState = null, CancellationToken cancellationToken = default)
    {
      return executor.ExecuteStreamingAsync(problem, random, initialState, cancellationToken)
        .Select(stream => (stream.Item1, stream.Item2.LastAsync(cancellationToken).AsTask().Result))
        .ToList();
    }

    public IAsyncEnumerable<(TAlgorithm, int, TAlgorithmState)> ExecuteInterleavedStreamingAsync(TProblem problem, IRandomNumberGenerator random, TAlgorithmState? initialState = null, CancellationToken cancellationToken = default)
    {
      var sourceStreams = executor.ExecuteStreamingAsync(problem, random, initialState, cancellationToken)
        .Select((stream, index) => stream.Item2.Select(state => (stream.Item1, index, state)));
      return sourceStreams.Merge();
    }

    public IReadOnlyList<(TAlgorithm, IReadOnlyList<TAlgorithmState>)> ExecuteEager(TProblem problem, IRandomNumberGenerator random, TAlgorithmState? initialState = null, CancellationToken cancellationToken = default)
    {
      var sourceLists = executor.ExecuteStreamingAsync(problem, random, initialState, cancellationToken)
        .Select(stream => (stream.Item1, (IReadOnlyList<TAlgorithmState>)stream.Item2.ToListAsync(cancellationToken).AsTask().Result));
      return sourceLists.ToList();
    }
  }

  extension<TGenotype, TSearchSpace, TProblem, TAlgorithmState, TAlgorithm>(TAlgorithm algorithm)
    where TGenotype : class
    where TSearchSpace : class, ISearchSpace<TGenotype>
    where TProblem : class, IProblem<TGenotype, TSearchSpace>
    where TAlgorithmState : class, IAlgorithmState
    where TAlgorithm : class, IAlgorithm<TGenotype, TSearchSpace, TProblem, TAlgorithmState>
  {
    public RepeatExecutor<TGenotype, TSearchSpace, TProblem, TAlgorithmState, TAlgorithm> Repeat(int repetitions)
    {
      return new RepeatExecutor<TGenotype, TSearchSpace, TProblem, TAlgorithmState, TAlgorithm> {
        Algorithm = algorithm,
        Repetitions = repetitions
      };
    }
  }
}
