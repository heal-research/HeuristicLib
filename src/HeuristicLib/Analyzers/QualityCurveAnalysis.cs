// using HEAL.HeuristicLib.Optimization;
// using HEAL.HeuristicLib.Problems;
// using HEAL.HeuristicLib.SearchSpaces;
// using HEAL.HeuristicLib.States;
//
// namespace HEAL.HeuristicLib.Analyzers;
//
// public static class QualityCurveAnalysis {
//   public static QualityCurveAnalysis<TGenotype> Create<TGenotype, TS, TP, TRes>(IAlgorithmBuilder<TGenotype, TS, TP, TRes> prototype)
//     where TS : class, ISearchSpace<TGenotype>
//     where TP : class, IProblem<TGenotype, TS>
//     where TRes : IAlgorithmState
//     where TGenotype : class {
//     var t = new QualityCurveAnalysis<TGenotype>();
//     t.AttachTo(prototype);
//     return t;
//   }
// }
//
// public class QualityCurveAnalysis<TGenotype> : AttachedAnalysis<TGenotype> where TGenotype : class {
//   public readonly List<(ISolution<TGenotype> best, int evalCount)> CurrentState = [];
//   private ISolution<TGenotype>? best;
//   private int evalCount;
//
//   public override void AfterEvaluation(IReadOnlyList<TGenotype> genotypes, IReadOnlyList<ObjectiveVector> res, ISearchSpace<TGenotype> searchSpace, IProblem<TGenotype, ISearchSpace<TGenotype>> problem) {
//     for (int i = 0; i < genotypes.Count; i++) {
//       TGenotype genotype = genotypes[i];
//       ObjectiveVector q = res[i];
//       evalCount++;
//       if (best is not null && problem.Objective.TotalOrderComparer.Compare(q, best.ObjectiveVector) >= 0) continue;
//       best = new Solution<TGenotype>(genotype, q);
//       CurrentState.Add((best, evalCount));
//     }
//   }
// }
