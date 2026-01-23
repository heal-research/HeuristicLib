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
  
  public IReadOnlyList<(TAlgorithm, IAsyncEnumerable<TAlgorithmState>)> ExecuteStreamingAsync(TProblem problem, IRandomNumberGenerator random, TAlgorithmState? initialState = null, CancellationToken cancellationToken = default)
  {
    var algorithms = ParameterGrid.GetConfigurations();
    return algorithms.Select((alg, index) => {
      var algRng = random.Fork(index);
      return (alg, alg.ExecuteStreamingAsync(problem, algRng, initialState, cancellationToken));
    }).ToList();
  }
}

public static class GridExecutorExtensions
{
  extension<TAlgorithm, TGenotype, TSearchSpace, TProblem, TAlgorithmState>(IExecutor<TGenotype, TSearchSpace, TProblem, TAlgorithmState, TAlgorithm> executor)
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
