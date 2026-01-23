using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Executors;

public class ExperimentExecutor<TGenotype, TSearchSpace, TProblem, TAlgorithmState, TAlgorithm> 
  : IExperimentExecutor<TGenotype, TSearchSpace, TProblem, TAlgorithmState, TAlgorithm>
  where TGenotype : class
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
  where TAlgorithmState : class, IAlgorithmState
  where TAlgorithm : class, IAlgorithm<TGenotype, TSearchSpace, TProblem, TAlgorithmState>
{
  public required IReadOnlyList<TAlgorithm> Algorithms { get; init; }
  
  public IReadOnlyList<(TAlgorithm, IAsyncEnumerable<TAlgorithmState>)> ExecuteStreamingAsync(TProblem problem, IRandomNumberGenerator random, TAlgorithmState? initialState = null, CancellationToken cancellationToken = default)
  {
    return Algorithms.Select((alg, index) => {
      var algRng = random.Fork(index);
      return (alg, alg.ExecuteStreamingAsync(problem, algRng, initialState, cancellationToken));
    }).ToList();
  }
}

public static class ExperimentExecutorExtensions
{
  extension<TAlgorithm, TGenotype, TSearchSpace, TProblem, TAlgorithmState>(IExperimentExecutor<TGenotype, TSearchSpace, TProblem, TAlgorithmState, TAlgorithm> executor)
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
