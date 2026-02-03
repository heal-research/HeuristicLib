using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Algorithms;

// ToDo: Maybe different name "MultiStreamAlgorithm"?
public interface IMetaAlgorithm<TGenotype, in TSearchSpace, in TProblem, TAlgorithmState, TAlgorithmKey>
  : IAlgorithm<TGenotype, TSearchSpace, TProblem, TAlgorithmState>
  where TGenotype : class
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
  where TAlgorithmState : class, IAlgorithmState
  //where TAlgorithmKey,> : class, IAlgorithm<TGenotype, TSearchSpace, TProblem, TAlgorithmState>
{
  // ToDo: Maybe rename to "ExecuteManyStreamingAsync"?
  new IReadOnlyList<KeyValuePair<TAlgorithmKey, IAsyncEnumerable<TAlgorithmState>>> RunStreamingAsync(
    TAlgorithmState? initialState,
    TProblem problem,
    IRandomNumberGenerator random,
    CancellationToken ct = default
  );
}

// public interface IMetaAlgorithmExecution<TGenotype, in TSearchSpace, in TProblem, TAlgorithmState, TAlgorithmKey>
//   : 
//   where TGenotype : class
//   where TSearchSpace : class, ISearchSpace<TGenotype>
//   where TProblem : class, IProblem<TGenotype, TSearchSpace>
//   where TAlgorithmState : class, IAlgorithmState
//   where TAlgorithmKey : class, IAlgorithm<TGenotype, TSearchSpace, TProblem, TAlgorithmState>
// {
//   IReadOnlyList<(TAlgorithmKey, IAsyncEnumerable<TAlgorithmState>)> ExecuteStreamingAsync(TProblem problem, IRandomNumberGenerator random, TAlgorithmState? initialState = null, CancellationToken cancellationToken = default);
// }
