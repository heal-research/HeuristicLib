using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Operators.Prototypes;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;

namespace HEAL.HeuristicLib.Operators.Analyzer;

public static class QualityCurveAnalysis {
  public static QualityCurveAnalysis<TGenotype> Create<TGenotype, TE, TP, TRes>(IPrototype<TGenotype, TE, TP, TRes> prototype)
    where TE : class, IEncoding<TGenotype>
    where TP : class, IProblem<TGenotype, TE>
    where TRes : IIterationResult
    where TGenotype : class {
    var t = new QualityCurveAnalysis<TGenotype>();
    t.AddToProto(prototype);
    return t;
  }
}

public class QualityCurveAnalysis<TGenotype> : SimpleAnalysis<TGenotype> where TGenotype : class {
  public readonly List<(ISolution<TGenotype> best, int evalCount)> CurrentState = [];
  private ISolution<TGenotype>? best;
  private int evalCount;

  public override void AfterEvaluation(IReadOnlyList<TGenotype> genotypes, IReadOnlyList<ObjectiveVector> res, IEncoding<TGenotype> encoding, IProblem<TGenotype, IEncoding<TGenotype>> problem) {
    for (int i = 0; i < genotypes.Count; i++) {
      TGenotype genotype = genotypes[i];
      ObjectiveVector q = res[i];
      evalCount++;
      if (best is not null && problem.Objective.TotalOrderComparer.Compare(q, best.ObjectiveVector) >= 0) continue;
      best = new Solution<TGenotype>(genotype, q);
      CurrentState.Add((best, evalCount));
    }
  }
}
