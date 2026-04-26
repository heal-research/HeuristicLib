using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Experiments;

// ToDo: either make a MultiStreamAlgorithm an Algorithm or provide an adapter that interleaves results, or take last, or any other of compression to a single stream.
public interface IExperiment<TGenotype, in TSearchSpace, in TProblem, TSearchState, TAlgorithmKey>
  : IExecutable<IExperimentInstance<TGenotype, TSearchSpace, TProblem, TSearchState, TAlgorithmKey>>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
  where TSearchState : class, ISearchState
{
}

public interface IExperimentInstance<TGenotype, in TSearchSpace, in TProblem, TSearchState, TAlgorithmKey>
  : IExecutionInstance
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
  where TSearchState : class, ISearchState
{
  IReadOnlyList<KeyValuePair<TAlgorithmKey, IAsyncEnumerable<TSearchState>>> RunStreamingAsync(
    TProblem problem,
    IRandomNumberGenerator random,
    TSearchState? initialState = null,
    CancellationToken ct = default
  );
}
