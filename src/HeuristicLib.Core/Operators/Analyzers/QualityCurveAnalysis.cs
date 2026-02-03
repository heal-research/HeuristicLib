using HEAL.HeuristicLib.Operators.Prototypes;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Operators.Analyzers;

public static class QualityCurveAnalysis
{
  public static QualityCurveAnalysis<TGenotype> Create<TGenotype, TE, TP, TRes>(IAlgorithmBuilder<TGenotype, TE, TP, TRes> prototype)
    where TE : class, ISearchSpace<TGenotype>
    where TP : class, IProblem<TGenotype, TE>
    where TRes : IAlgorithmState
    where TGenotype : class
  {
    var t = new QualityCurveAnalysis<TGenotype>();
    t.AttachTo(prototype);

    return t;
  }
}

public class QualityCurveAnalysis<TGenotype> : AttachedAnalysis<TGenotype> where TGenotype : class
{
  public readonly List<(ISolution<TGenotype> best, int evalCount)> CurrentState = [];
  private ISolution<TGenotype>? best;
  private int evalCount;

  public override void AfterEvaluation(IReadOnlyList<TGenotype> genotypes, IReadOnlyList<ObjectiveVector> values, ISearchSpace<TGenotype> searchSpace, IProblem<TGenotype, ISearchSpace<TGenotype>> problem)
  {
    for (var i = 0; i < genotypes.Count; i++) {
      var genotype = genotypes[i];
      var q = values[i];
      evalCount++;

      if (best is not null) {
        var comp = problem.Objective.TotalOrderComparer;
        if (NoTotalOrderComparer.Instance.Equals(comp)) {
          comp = new LexicographicComparer(problem.Objective.Directions);
        }

        if (comp.Compare(q, best.ObjectiveVector) >= 0) {
          continue;
        }
      }

      best = new Solution<TGenotype>(genotype, q);
      CurrentState.Add((best, evalCount));
    }
  }
}
