using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Observers;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Analyzers;

public class AllPopulationsTracker<T> : IInterceptorObserver<T, PopulationState<T>>
  where T : class
{
  public List<ISolution<T>[]> AllSolutions { get; } = [];

  public void AfterInterception(PopulationState<T> currentAlgorithmState, PopulationState<T>? previousIterationResult, ISearchSpace<T> searchSpace, IProblem<T, ISearchSpace<T>> problem) => AllSolutions.Add(currentAlgorithmState.Population.ToArray());
}
