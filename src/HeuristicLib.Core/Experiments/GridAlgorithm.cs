using Generator.Equals;
using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Experiments;

// ToDo: Think if we need a better name for this: "PortfolioAlgorithm" or something like this, since this algorithm is not an algorithm that is actually doing something on a grid.
[Equatable]
public partial record GridAlgorithm<TGenotype, TSearchSpace, TProblem, TSearchState, TAlgorithm>
  : Experiment<TGenotype, TSearchSpace, TProblem, TSearchState, TAlgorithm>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
  where TSearchState : class, ISearchState
  where TAlgorithm : class, IAlgorithm<TGenotype, TSearchSpace, TProblem, TSearchState>
{
  [OrderedEquality] public required Grid<TAlgorithm> ParameterGrid { get; init; }

  public override GridAlgorithmInstance<TGenotype, TSearchSpace, TProblem, TSearchState, TAlgorithm> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry)
  {
    return new GridAlgorithmInstance<TGenotype, TSearchSpace, TProblem, TSearchState, TAlgorithm>(ParameterGrid);
  }
}

public class GridAlgorithmInstance<TGenotype, TSearchSpace, TProblem, TSearchState, TAlgorithm>
  : ExperimentInstance<TGenotype, TSearchSpace, TProblem, TSearchState, TAlgorithm>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
  where TSearchState : class, ISearchState
  where TAlgorithm : class, IAlgorithm<TGenotype, TSearchSpace, TProblem, TSearchState>
{
  protected readonly Grid<TAlgorithm> ParameterGrid;

  public GridAlgorithmInstance(Grid<TAlgorithm> parameterGrid)
  {
    ParameterGrid = parameterGrid;
  }

  // ToDo: think about how to actually get key-value-pairs of (TAlgorithm, Run) out to be able to get results from the run, per configuration
  public override IReadOnlyList<KeyValuePair<TAlgorithm, IAsyncEnumerable<TSearchState>>> RunStreamingAsync(TProblem problem, IRandomNumberGenerator random, TSearchState? initialState = null, CancellationToken ct = default)
  {
    var algorithms = ParameterGrid.GetConfigurations();
    return algorithms.Select((alg, index) => {
      var algRng = random.Fork(index);
      return KeyValuePair.Create(alg, alg.RunStreamingAsync(problem, algRng, initialState, ct));
    }).ToList();
  }
}

public static class GridAlgorithmExtensions
{
  extension<TAlgorithm, TGenotype, TSearchSpace, TProblem, TSearchState>(IExperiment<TGenotype, TSearchSpace, TProblem, TSearchState, TAlgorithm> algorithm)
    where TSearchSpace : class, ISearchSpace<TGenotype>
    where TProblem : class, IProblem<TGenotype, TSearchSpace>
    where TSearchState : class, ISearchState
    where TAlgorithm : class, IAlgorithm<TGenotype, TSearchSpace, TProblem, TSearchState>
  {
  }

  // extension<TAlgorithm, TGenotype, TSearchSpace, TProblem, TSearchState>(IMetaAlgorithm<TGenotype, TSearchSpace, TProblem, TSearchState, TAlgorithm> executor)
  //   where TSearchSpace : class, ISearchSpace<TGenotype>
  //   where TProblem : class, IProblem<TGenotype, TSearchSpace>
  //   where TSearchState : class, ISearchState
  //   where TAlgorithm : class, IAlgorithm<TGenotype, TSearchSpace, TProblem, TSearchState>
  // {
  //   public IReadOnlyList<(TAlgorithm, IAsyncEnumerable<TSearchState>)> ExecuteStreamingAsync(TProblem problem, IRandomNumberGenerator random, TSearchState? initialState = null, CancellationToken cancellationToken = default)
  //   {
  //     return executor.CreateExecution().ExecuteStreamingAsync(problem, random, initialState, cancellationToken);
  //   } 
  //   
  //   public IReadOnlyList<(TAlgorithm, TSearchState)> Execute(TProblem problem, IRandomNumberGenerator random, TSearchState? initialState = null, CancellationToken cancellationToken = default)
  //   {
  //     return executor.CreateExecution().Execute(problem, random, initialState, cancellationToken);
  //   }
  // }
}

