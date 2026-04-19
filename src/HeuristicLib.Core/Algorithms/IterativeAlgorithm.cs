using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Algorithms;

public abstract record IterativeAlgorithm<TGenotype, TSearchSpace, TProblem, TSearchState>
  : StatefulIterativeAlgorithm<TGenotype, TSearchSpace, TProblem, TSearchState, NoState>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
  where TSearchState : class, IAlgorithmState
{
  protected sealed override NoState CreateInitialRuntimeState()
  {
    return NoState.Instance;
  }

  protected sealed override TSearchState ExecuteStep(
    TSearchState? previousState,
    NoState runtimeState,
    IOperatorExecutor executor,
    TProblem problem,
    IRandomNumberGenerator random)
  {
    return ExecuteStep(previousState, executor, problem, random);
  }

  protected abstract TSearchState ExecuteStep(
    TSearchState? previousState,
    IOperatorExecutor executor,
    TProblem problem,
    IRandomNumberGenerator random);
}
