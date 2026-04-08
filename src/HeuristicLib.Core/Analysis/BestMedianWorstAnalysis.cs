using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Analysis;

public record BestMedianWorstEntry<T>(ISolution<T> Best, ISolution<T> Median, ISolution<T> Worst);

public record BestMedianWorstAnalysis<T, TS, TP, TR>(IAlgorithm<T, TS, TP, TR> Algorithm, params IInterceptor<T, TS, TP, TR>[] Interceptor)
  : Analyzer<T, TS, TP, TR, List<BestMedianWorstEntry<T>>>(Algorithm)
  where TS : class, ISearchSpace<T>
  where TP : class, IProblem<T, TS>
  where TR : PopulationState<T>
{
  public override List<BestMedianWorstEntry<T>> CreateInitialState() => [];

  public override void RegisterObservations(IObservationRegistry observationRegistry, List<BestMedianWorstEntry<T>> state)
  {
    foreach (var interceptor in Interceptor) {
      observationRegistry.Add(interceptor, (populationState, _, _, _, problem) => AfterInterception(state, populationState, problem));
    }
  }

  private static void AfterInterception(List<BestMedianWorstEntry<T>> bestSolutions, TR currentState, TP problem)
  {
    var comp = problem.Objective.TotalOrderComparer is NoTotalOrderComparer ? new LexicographicComparer(problem.Objective.Directions) : problem.Objective.TotalOrderComparer;
    var ordered = currentState.Population.OrderBy(keySelector: x => x.ObjectiveVector, comp).ToArray();
    if (ordered.Length == 0) {
      bestSolutions.Add(null!);
      return;
    }

    bestSolutions.Add(new BestMedianWorstEntry<T>(ordered[0], ordered[ordered.Length / 2], ordered[^1]));
  }
}
