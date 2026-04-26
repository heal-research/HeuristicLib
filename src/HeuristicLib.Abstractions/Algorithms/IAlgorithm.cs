using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Algorithms;

public interface IAlgorithm<TGenotype, in TSearchSpace, in TProblem, TSearchState>
  : IExecutable<IAlgorithmInstance<TGenotype, TSearchSpace, TProblem, TSearchState>>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
  where TSearchState : class, ISearchState
{
  IEvaluator<TGenotype, TSearchSpace, TProblem> Evaluator { get; }
}

public interface IAlgorithmInstance<TGenotype, in TSearchSpace, in TProblem, TSearchState>
  : IExecutionInstance
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
  where TSearchState : class, ISearchState
{
  IAsyncEnumerable<TSearchState> RunStreamingAsync(
    TProblem problem,
    IRandomNumberGenerator random,
    TSearchState? initialState = null,
    CancellationToken ct = default
  );
}
