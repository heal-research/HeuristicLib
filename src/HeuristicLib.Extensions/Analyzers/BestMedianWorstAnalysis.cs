using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Observers;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Analyzers;

public record BestMedianWorstEntry<T>(ISolution<T> Best, ISolution<T> Median, ISolution<T> Worst);

public class BestMedianWorstAnalysis<TGenotype> : IInterceptorObserver<TGenotype, PopulationState<TGenotype>> where TGenotype : class
{
  public readonly List<BestMedianWorstEntry<TGenotype>> BestISolutions = [];

  public void AfterInterception(PopulationState<TGenotype> currentAlgorithmState, PopulationState<TGenotype>? previousIterationResult, ISearchSpace<TGenotype> searchSpace, IProblem<TGenotype, ISearchSpace<TGenotype>> problem)
  {
    var comp = problem.Objective.TotalOrderComparer is NoTotalOrderComparer ? new LexicographicComparer(problem.Objective.Directions) : problem.Objective.TotalOrderComparer;
    var ordered = currentAlgorithmState.Population.OrderBy(keySelector: x => x.ObjectiveVector, comp).ToArray();
    if (ordered.Length == 0) {
      BestISolutions.Add(null!);

      return;
    }

    BestISolutions.Add(new BestMedianWorstEntry<TGenotype>(ordered[0], ordered[ordered.Length / 2], ordered[^1]));
  }
}
