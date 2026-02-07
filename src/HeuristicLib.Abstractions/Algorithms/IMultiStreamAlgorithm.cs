using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Algorithms;

// ToDo: either make a MultiStreamAlgorithm an Algorithm or provide an adapter that interleaves results, or take last, or any other of compression to a single stream.

public interface IMultiStreamAlgorithm<TGenotype, in TSearchSpace, in TProblem, TAlgorithmState, TAlgorithmKey>
  : IExecutable<IMultiStreamAlgorithmInstance<TGenotype, TSearchSpace, TProblem, TAlgorithmState, TAlgorithmKey>>
  // : IAlgorithm<TGenotype, TSearchSpace, TProblem, TAlgorithmState>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
  where TAlgorithmState : class, IAlgorithmState
  //where TAlgorithmKey,> : class, IAlgorithm<TGenotype, TSearchSpace, TProblem, TAlgorithmState>
{
  
}

public interface IMultiStreamAlgorithmInstance<TGenotype, in TSearchSpace, in TProblem, TAlgorithmState, TAlgorithmKey>
  : IExecutionInstance
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
  where TAlgorithmState : class, IAlgorithmState
  //where TAlgorithmKey : class, IAlgorithm<TGenotype, TSearchSpace, TProblem, TAlgorithmState>
{
  // ToDo: Maybe rename to "ExecuteManyStreamingAsync"?
  IReadOnlyList<KeyValuePair<TAlgorithmKey, IAsyncEnumerable<TAlgorithmState>>> RunStreamingAsync(
    TProblem problem,
    IRandomNumberGenerator random,
    TAlgorithmState? initialState = null,
    CancellationToken ct = default
  );
}
