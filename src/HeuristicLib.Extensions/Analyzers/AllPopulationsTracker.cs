using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators.Interceptors;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Analyzers;

public class AllPopulationsTracker<T> : IInterceptorObserver<T, PopulationState<T>>
{
  public IInterceptorObserverInstance<T, ISearchSpace<T>, IProblem<T, ISearchSpace<T>>, PopulationState<T>> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry)
    => new Instance();

  public sealed class Instance : IInterceptorObserverInstance<T, ISearchSpace<T>, IProblem<T, ISearchSpace<T>>, PopulationState<T>>
  {
    public List<ISolution<T>[]> AllSolutions { get; } = [];

    public void AfterInterception(PopulationState<T> newState, PopulationState<T> currentState, PopulationState<T>? previousState, ISearchSpace<T> searchSpace, IProblem<T, ISearchSpace<T>> problem)
    {
      AllSolutions.Add(currentState.Population.ToArray());
    }
  }
}
