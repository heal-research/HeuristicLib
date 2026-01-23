using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Observation;

public class ObservationPipeline<TG, TS, TP, TR> : IterationObserver<TG, TS, TP, TR>
  where TG : class
  where TS : class, ISearchSpace<TG>
  where TP : class, IProblem<TG, TS>
  where TR : class, IAlgorithmState
{
  private readonly IReadOnlyList<IIterationObserver<TG, TS, TP, TR>> observers;

  public ObservationPipeline(params IEnumerable<IIterationObserver<TG, TS, TP, TR>> observers) => this.observers = observers.ToList();

  public override void OnIterationCompleted(TR currentState, TR? previousState, TS searchSpace, TP problem)
  {
    foreach (var observer in observers) {
      observer.OnIterationCompleted(currentState, previousState, searchSpace, problem);
    }
  }
}

public static class ObservationPipeline
{
  public static ObservationPipeline<TG, TS, TP, TR> Create<TG, TS, TP, TR>(params IEnumerable<IIterationObserver<TG, TS, TP, TR>> observers) where TG : class where TS : class, ISearchSpace<TG> where TP : class, IProblem<TG, TS> where TR : class, IAlgorithmState => new(observers);
}
