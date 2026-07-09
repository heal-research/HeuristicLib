using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Algorithms;

public interface IAlgorithm<TCandidate, in TSearchSpace, in TProblem, TSearchState>
  : IExecutionInstanceResolvable<IAlgorithmInstance<TCandidate, TSearchSpace, TProblem, TSearchState>>
  where TSearchSpace : class, ISearchSpace<TCandidate>
  where TProblem : class, IProblem<TCandidate, TSearchSpace>
  where TSearchState : class, ISearchState
{
    IEvaluator<TCandidate, TSearchSpace, TProblem> Evaluator { get; }
}

public interface IAlgorithmInstance<TCandidate, in TSearchSpace, in TProblem, TSearchState>
  : IExecutionInstance
  where TSearchSpace : class, ISearchSpace<TCandidate>
  where TProblem : class, IProblem<TCandidate, TSearchSpace>
  where TSearchState : class, ISearchState
{
    IAsyncEnumerable<TSearchState> RunStreamingAsync(
      TProblem problem,
      IRandomNumberGenerator random,
      TSearchState? initialState = null,
      CancellationToken ct = default
    );
}
