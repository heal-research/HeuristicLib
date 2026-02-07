using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Algorithms.MetaAlgorithms;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Observation;

public record BestMedianWorstEntry<T>(ISolution<T> Best, ISolution<T> Median, ISolution<T> Worst);

public class BestMedianWorstQualityObserver<TG, TS, TP, TR> : IAlgorithmObserver<TG, TS, TP, TR>
  where TS : class, ISearchSpace<TG>
  where TP : class, IProblem<TG, TS>
  where TR : PopulationState<TG>
{
  private readonly List<BestMedianWorstEntry<TG>> history = [];

  public IReadOnlyList<BestMedianWorstEntry<TG>> History => history;

  public void AfterIteration(TR currentState, TR? previousState, IRandomNumberGenerator random, TP problem)
  {
    if (currentState.Population.Solutions.Count <= 0) {
      throw new InvalidOperationException("Population is empty, cannot determine best, median, and worst solutions.");
    }

    var ordered = currentState.Population.OrderBy(x => x.ObjectiveVector, problem.Objective.TotalOrderComparer).ToArray();
    history.Add(new BestMedianWorstEntry<TG>(ordered[0], ordered[ordered.Length / 2], ordered[^1]));
  }
}

public static class BestMedianWorstQualityObserver
{
  extension<TG, TS, TP, TR>(IAlgorithm<TG, TS, TP, TR> algorithm)
    where TS : class, ISearchSpace<TG>
    where TP : class, IProblem<TG, TS>
    where TR : PopulationState<TG>
  {
    // ToDo: Think about if we want to return a History type rather than the observer itself.
    public IAlgorithm<TG, TS, TP, TR> WithBestMedianWorstQualityObserver(out BestMedianWorstQualityObserver<TG, TS, TP, TR> observer)
    {
      observer = new BestMedianWorstQualityObserver<TG, TS, TP, TR>();
      return algorithm.ObserveWith(observer);
    }

  }
}
