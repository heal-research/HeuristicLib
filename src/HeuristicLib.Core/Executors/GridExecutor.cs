using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Executors;

public class GridExecutor<TGenotype, TSearchSpace, TProblem, TAlgorithmState, TAlgorithm> 
  : IExecutor<TGenotype, TSearchSpace, TProblem, TAlgorithmState, TAlgorithm>
  where TGenotype : class
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
  where TAlgorithmState : class, IAlgorithmState
  where TAlgorithm : class, IAlgorithm<TGenotype, TSearchSpace, TProblem, TAlgorithmState>
{
  public required Grid<TAlgorithm> ParameterGrid { get; init; }

  IExecutorExecution<TGenotype, TSearchSpace, TProblem, TAlgorithmState, TAlgorithm> IExecutor<TGenotype, TSearchSpace, TProblem, TAlgorithmState, TAlgorithm>.CreateExecution() => this.CreateExecution();
  public virtual Execution CreateExecution() => new Execution(this);

  public class Execution : IExecutorExecution<TGenotype, TSearchSpace, TProblem, TAlgorithmState, TAlgorithm>
  {
    private readonly GridExecutor<TGenotype, TSearchSpace, TProblem, TAlgorithmState, TAlgorithm> executor;
    
    public Execution(GridExecutor<TGenotype, TSearchSpace, TProblem, TAlgorithmState, TAlgorithm> executor) {
      this.executor = executor;
    }
    
    public IReadOnlyList<(TAlgorithm, IAsyncEnumerable<TAlgorithmState>)> ExecuteStreamingAsync(TProblem problem, IRandomNumberGenerator random, TAlgorithmState? initialState = null, CancellationToken cancellationToken = default)
    {
      var algorithms = executor.ParameterGrid.GetConfigurations();
      return algorithms.Select((alg, index) => {
        var algRng = random.Fork(index);
        var algExecution = alg.CreateExecution();
        return (alg, algExecution.ExecuteStreamingAsync(problem, algRng, initialState, cancellationToken));
      }).ToList();
    }
  }
}

public static class GridExecutorExtensions
{
  extension<TAlgorithm, TGenotype, TSearchSpace, TProblem, TAlgorithmState>(IExecutorExecution<TGenotype, TSearchSpace, TProblem, TAlgorithmState, TAlgorithm> executor)
    where TGenotype : class
    where TSearchSpace : class, ISearchSpace<TGenotype>
    where TProblem : class, IProblem<TGenotype, TSearchSpace>
    where TAlgorithmState : class, IAlgorithmState
    where TAlgorithm : class, IAlgorithm<TGenotype, TSearchSpace, TProblem, TAlgorithmState>
  {
    public IReadOnlyList<(TAlgorithm, TAlgorithmState)> Execute(TProblem problem, IRandomNumberGenerator random, TAlgorithmState? initialState = null, CancellationToken cancellationToken = default)
    {
      return executor.ExecuteStreamingAsync(problem, random, initialState, cancellationToken)
        .Select(alg => (alg.Item1, alg.Item2.LastAsync(cancellationToken).AsTask().Result))
        .ToList();
    }
  }
  
  extension<TAlgorithm, TGenotype, TSearchSpace, TProblem, TAlgorithmState>(IExecutor<TGenotype, TSearchSpace, TProblem, TAlgorithmState, TAlgorithm> executor)
    where TGenotype : class
    where TSearchSpace : class, ISearchSpace<TGenotype>
    where TProblem : class, IProblem<TGenotype, TSearchSpace>
    where TAlgorithmState : class, IAlgorithmState
    where TAlgorithm : class, IAlgorithm<TGenotype, TSearchSpace, TProblem, TAlgorithmState>
  {
    public IReadOnlyList<(TAlgorithm, IAsyncEnumerable<TAlgorithmState>)> ExecuteStreamingAsync(TProblem problem, IRandomNumberGenerator random, TAlgorithmState? initialState = null, CancellationToken cancellationToken = default)
    {
      return executor.CreateExecution().ExecuteStreamingAsync(problem, random, initialState, cancellationToken);
    } 
    
    public IReadOnlyList<(TAlgorithm, TAlgorithmState)> Execute(TProblem problem, IRandomNumberGenerator random, TAlgorithmState? initialState = null, CancellationToken cancellationToken = default)
    {
      return executor.CreateExecution().Execute(problem, random, initialState, cancellationToken);
    }
  }
}

public static class GridExecutor
{
  public static GridExecutor<TGenotype, TSearchSpace, TProblem, TAlgorithmState, TAlgorithm> Create<TGenotype, TSearchSpace, TProblem, TAlgorithmState, TAlgorithm>(
    TAlgorithm algorithm, Grid<TAlgorithm> grid
  )
    where TGenotype : class
    where TSearchSpace : class, ISearchSpace<TGenotype>
    where TProblem : class, IProblem<TGenotype, TSearchSpace>
    where TAlgorithmState : class, IAlgorithmState
    where TAlgorithm : class, IAlgorithm<TGenotype, TSearchSpace, TProblem, TAlgorithmState>
  {
    return new GridExecutor<TGenotype, TSearchSpace, TProblem, TAlgorithmState, TAlgorithm> {
      ParameterGrid = grid
    };
  }
}
