using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Operators.Prototypes;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;

namespace HEAL.HeuristicLib.Operators.Analyzer;

public record BestMedianWorstEntry<T>(ISolution<T> Best, ISolution<T> Median, ISolution<T> Worst);

public class BestMedianWorstAnalysis<TGenotype> : AttachedAnalysis<TGenotype, PopulationIterationResult<TGenotype>> where TGenotype : class {
  public readonly List<BestMedianWorstEntry<TGenotype>> BestISolutions = [];

  public override void AfterInterception(PopulationIterationResult<TGenotype> currentIterationResult, PopulationIterationResult<TGenotype>? previousIterationResult, IEncoding<TGenotype> encoding, IProblem<TGenotype, IEncoding<TGenotype>> problem) {
    var ordered = currentIterationResult.Population.OrderBy(x => x.ObjectiveVector, problem.Objective.TotalOrderComparer).ToArray();
    if (ordered.Length == 0) {
      BestISolutions.Add(default);
      return;
    }

    BestISolutions.Add(new BestMedianWorstEntry<TGenotype>(ordered[0], ordered[ordered.Length / 2], ordered[^1]));
  }
}

public static class BestMedianWorstAnalysis {
  public static BestMedianWorstAnalysis<TGenotype> Analyze<TGenotype, TE, TP, TR>(
    IAlgorithmBuilder<TGenotype, TE, TP, TR> ga)
    where TE : class, IEncoding<TGenotype>
    where TP : class, IProblem<TGenotype, TE>
    where TR : PopulationIterationResult<TGenotype>
    where TGenotype : class {
    var r = new BestMedianWorstAnalysis<TGenotype>();
    ga.AddAttachment(r);
    return r;
  }
}
