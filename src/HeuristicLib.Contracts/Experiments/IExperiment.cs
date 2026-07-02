using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Experiments;

// ToDo: either make a MultiStreamAlgorithm an Algorithm or provide an adapter that interleaves results, or take last, or any other of compression to a single stream.
public interface IExperiment<TCandidate, in TSearchSpace, in TProblem, TSearchState, TAlgorithmKey>
  : IExecutionInstanceResolvable<IExperimentInstance<TCandidate, TSearchSpace, TProblem, TSearchState, TAlgorithmKey>>
  where TSearchSpace : class, ISearchSpace<TCandidate>
  where TProblem : class, IProblem<TCandidate, TSearchSpace>
  where TSearchState : class, ISearchState
{
}

public interface IExperimentInstance<TCandidate, in TSearchSpace, in TProblem, TSearchState, TAlgorithmKey>
  : IExecutionInstance
  where TSearchSpace : class, ISearchSpace<TCandidate>
  where TProblem : class, IProblem<TCandidate, TSearchSpace>
  where TSearchState : class, ISearchState
{
    IReadOnlyList<KeyValuePair<TAlgorithmKey, IAsyncEnumerable<TSearchState>>> RunStreamingAsync(
      TProblem problem,
      IRandomNumberGenerator random,
      TSearchState? initialState = null,
      CancellationToken ct = default
    );
}
