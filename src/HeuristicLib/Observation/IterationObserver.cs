using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Observation;

public abstract class IterationObserver<TG, TS, TP, TR> 
  : IIterationObserver<TG, TS, TP, TR>
  where TG : class
  where TS : class, ISearchSpace<TG>
  where TP : class, IProblem<TG, TS>
  where TR : class, IAlgorithmState 
{
  public abstract void OnIterationCompleted(TR currentState, TR? previousState, TS searchSpace, TP problem);
}

public abstract class IterationObserver<TG, TS, TR>
  : IIterationObserver<TG, TS, IProblem<TG, TS>, TR>
  where TG : class
  where TS : class, ISearchSpace<TG>
  where TR : class, IAlgorithmState 
{
  public abstract void OnIterationCompleted(TR currentState, TR? previousState, TS searchSpace);
  
  void IIterationObserver<TG, TS, IProblem<TG, TS>, TR>.OnIterationCompleted(TR currentState, TR? previousState, TS searchSpace, IProblem<TG, TS> problem) {
    OnIterationCompleted(currentState, previousState, searchSpace);
  }
}

public abstract class IterationObserver<TG, TR>
  : IIterationObserver<TG, ISearchSpace<TG>, IProblem<TG, ISearchSpace<TG>>, TR>
  where TG : class
  where TR : class, IAlgorithmState 
{
  public abstract void OnIterationCompleted(TR currentState, TR? previousState);

 
  void IIterationObserver<TG, ISearchSpace<TG>, IProblem<TG, ISearchSpace<TG>>, TR>.OnIterationCompleted(TR currentState, TR? previousState, ISearchSpace<TG> searchSpace, IProblem<TG, ISearchSpace<TG>> problem) {
    OnIterationCompleted(currentState, previousState);
  }
}
