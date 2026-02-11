using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;
using HEAL.HeuristicLib.Operators.Interceptors;

namespace HEAL.HeuristicLib.Analyzers;

public class AllPopulationsTracker<T> : IInterceptorObserver<T, PopulationState<T>>
  where T : class
{
  public List<ISolution<T>[]> AllSolutions { get; } = [];

  public void AfterInterception(PopulationState<T> newState, PopulationState<T> currentState, PopulationState<T>? previousState, ISearchSpace<T> searchSpace, IProblem<T, ISearchSpace<T>> problem)
  {
    AllSolutions.Add(currentState.Population.ToArray());
  }
}
