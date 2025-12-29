using HEAL.HeuristicLib.OperatorPrototypes;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Analyzers;

public record BestMedianWorstEntry<T>(ISolution<T> Best, ISolution<T> Median, ISolution<T> Worst);

public class BestMedianWorstAnalysis<TGenotype> : AttachedAnalysis<TGenotype, PopulationIterationResult<TGenotype>> where TGenotype : class {
  public readonly List<BestMedianWorstEntry<TGenotype>> BestISolutions = [];

  public override void AfterInterception(PopulationIterationResult<TGenotype> currentIterationResult, PopulationIterationResult<TGenotype>? previousIterationResult, ISearchSpace<TGenotype> searchSpace, IProblem<TGenotype, ISearchSpace<TGenotype>> problem) {
    var ordered = currentIterationResult.Population.OrderBy(x => x.ObjectiveVector, problem.Objective.TotalOrderComparer).ToArray();
    if (ordered.Length == 0) {
      BestISolutions.Add(default);
      return;
    }

    BestISolutions.Add(new BestMedianWorstEntry<TGenotype>(ordered[0], ordered[ordered.Length / 2], ordered[^1]));
  }
}

public static class BestMedianWorstAnalysis {
  public static BestMedianWorstAnalysis<TGenotype> Analyze<TGenotype, TS, TP, TR>(
    IAlgorithmBuilder<TGenotype, TS, TP, TR> ga)
    where TS : class, ISearchSpace<TGenotype>
    where TP : class, IProblem<TGenotype, TS>
    where TR : PopulationIterationResult<TGenotype>
    where TGenotype : class {
    var r = new BestMedianWorstAnalysis<TGenotype>();
    ga.AddAttachment(r);
    return r;
  }
}
