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
public partial record GridAlgorithm<TGenotype, TSearchSpace, TProblem, TAlgorithmState, TAlgorithm>
  : Experiment<TGenotype, TSearchSpace, TProblem, TAlgorithmState, TAlgorithm>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
  where TAlgorithmState : class, IAlgorithmState
  where TAlgorithm : class, IAlgorithm<TGenotype, TSearchSpace, TProblem, TAlgorithmState>
{
  [OrderedEquality]
  public required Grid<TAlgorithm> ParameterGrid { get; init; }

  public override GridAlgorithmInstance<TGenotype, TSearchSpace, TProblem, TAlgorithmState, TAlgorithm> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry)
  {
    return new GridAlgorithmInstance<TGenotype, TSearchSpace, TProblem, TAlgorithmState, TAlgorithm>(ParameterGrid);
  }
}

public class GridAlgorithmInstance<TGenotype, TSearchSpace, TProblem, TAlgorithmState, TAlgorithm>
  : ExperimentInstance<TGenotype, TSearchSpace, TProblem, TAlgorithmState, TAlgorithm>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
  where TAlgorithmState : class, IAlgorithmState
  where TAlgorithm : class, IAlgorithm<TGenotype, TSearchSpace, TProblem, TAlgorithmState>
{
  protected readonly Grid<TAlgorithm> ParameterGrid;

  public GridAlgorithmInstance(Grid<TAlgorithm> parameterGrid)
  {
    ParameterGrid = parameterGrid;
  }

  // ToDo: think about how to actually get key-value-pairs of (TAlgorithm, Run) out to be able to get results from the run, per configuration
  public override IReadOnlyList<KeyValuePair<TAlgorithm, IAsyncEnumerable<TAlgorithmState>>> RunStreamingAsync(TProblem problem, IRandomNumberGenerator random, TAlgorithmState? initialState = null, CancellationToken ct = default)
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
  extension<TAlgorithm, TGenotype, TSearchSpace, TProblem, TAlgorithmState>(IExperiment<TGenotype, TSearchSpace, TProblem, TAlgorithmState, TAlgorithm> algorithm)
    where TSearchSpace : class, ISearchSpace<TGenotype>
    where TProblem : class, IProblem<TGenotype, TSearchSpace>
    where TAlgorithmState : class, IAlgorithmState
    where TAlgorithm : class, IAlgorithm<TGenotype, TSearchSpace, TProblem, TAlgorithmState>
  { }

  // extension<TAlgorithm, TGenotype, TSearchSpace, TProblem, TAlgorithmState>(IMetaAlgorithm<TGenotype, TSearchSpace, TProblem, TAlgorithmState, TAlgorithm> executor)
  //   where TSearchSpace : class, ISearchSpace<TGenotype>
  //   where TProblem : class, IProblem<TGenotype, TSearchSpace>
  //   where TAlgorithmState : class, IAlgorithmState
  //   where TAlgorithm : class, IAlgorithm<TGenotype, TSearchSpace, TProblem, TAlgorithmState>
  // {
  //   public IReadOnlyList<(TAlgorithm, IAsyncEnumerable<TAlgorithmState>)> ExecuteStreamingAsync(TProblem problem, IRandomNumberGenerator random, TAlgorithmState? initialState = null, CancellationToken cancellationToken = default)
  //   {
  //     return executor.CreateExecution().ExecuteStreamingAsync(problem, random, initialState, cancellationToken);
  //   } 
  //   
  //   public IReadOnlyList<(TAlgorithm, TAlgorithmState)> Execute(TProblem problem, IRandomNumberGenerator random, TAlgorithmState? initialState = null, CancellationToken cancellationToken = default)
  //   {
  //     return executor.CreateExecution().Execute(problem, random, initialState, cancellationToken);
  //   }
  // }
}
