using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Observation;

public static class IterationObserver {
  public static IIterationObserver<TG, TS, TP, TR> Create<TG, TS, TP, TR>(Action<TR, TR?, TS, TP> onIterationCompleted)
    where TG : class
    where TS : class, ISearchSpace<TG>
    where TP : class, IProblem<TG, TS>
    where TR : class, IAlgorithmState
  {
    return new FuncIterationObserver<TG, TS, TP, TR>(onIterationCompleted);
  }
}

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

public sealed class FuncIterationObserver<TG, TS, TP, TR>(Action<TR, TR?, TS, TP> onIterationCompleted) : IterationObserver<TG, TS, TP, TR> 
  where TG : class
  where TS : class, ISearchSpace<TG>
  where TP : class, IProblem<TG, TS>
  where TR : class, IAlgorithmState  
{
  public override void OnIterationCompleted(TR currentState, TR? previousState, TS searchSpace, TP problem) {
    onIterationCompleted(currentState, previousState, searchSpace, problem);
  }
}
