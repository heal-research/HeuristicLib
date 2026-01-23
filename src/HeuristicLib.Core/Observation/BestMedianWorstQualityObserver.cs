using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Observation;

public record BestMedianWorstEntry<T>(ISolution<T> Best, ISolution<T> Median, ISolution<T> Worst);

public class BestMedianWorstQualityObserver<TG, TS, TP, TR> : IterationObserver<TG, TS, TP, TR>
  where TG : class
  where TS : class, ISearchSpace<TG>
  where TP : class, IProblem<TG, TS>
  where TR : PopulationIterationState<TG>
{
  private readonly List<BestMedianWorstEntry<TG>> history = [];

  public IReadOnlyList<BestMedianWorstEntry<TG>> History => history;

  public override void OnIterationCompleted(TR currentState, TR? previousState, TS searchSpace, TP problem)
  {
    if (currentState.Population.Solutions.Count <= 0) {
      throw new InvalidOperationException("Population is empty, cannot determine best, median, and worst solutions.");
    }

    var ordered = currentState.Population.OrderBy(x => x.ObjectiveVector, problem.Objective.TotalOrderComparer).ToArray();
    history.Add(new BestMedianWorstEntry<TG>(ordered[0], ordered[ordered.Length / 2], ordered[^1]));
  }
}
