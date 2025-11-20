using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Operators.Prototypes;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;

namespace HEAL.HeuristicLib.Operators.Analyzer;

public static class BestMedianWorstAnalysis {
  public static BestMedianWorstAnalysis<TGenotype, TE, TP, TRes> Create<TGenotype, TE, TP, TRes>(IPrototype<TGenotype, TE, TP, TRes> prototype)
    where TE : class, IEncoding<TGenotype>
    where TP : class, IProblem<TGenotype, TE>
    where TRes : PopulationIterationResult<TGenotype> {
    var t = new BestMedianWorstAnalysis<TGenotype, TE, TP, TRes>();
    t.AddToProto(prototype);
    return t;
  }
}

public class BestMedianWorstAnalysis<TGenotype, TE, TP, TRes> : SimpleAnalysis<TGenotype, TE, TP, TRes> where TE : class, IEncoding<TGenotype> where TP : class, IProblem<TGenotype, TE> where TRes : PopulationIterationResult<TGenotype> {
  public readonly List<BestMedianWorstEntry<TGenotype>> BestSolutions = [];

  protected override void AfterInterception(TRes currentIterationResult, TRes? previousIterationResult, TE encoding, TP problem) {
    var ordered = currentIterationResult.Population.OrderBy(x => x.ObjectiveVector, problem.Objective.TotalOrderComparer).ToArray();
    BestSolutions.Add(new BestMedianWorstEntry<TGenotype>(ordered[0], ordered[ordered.Length / 2], ordered[^1]));
  }
}

public record BestMedianWorstEntry<T>(Solution<T> Best, Solution<T> Median, Solution<T> Worst);
