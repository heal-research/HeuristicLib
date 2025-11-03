using HEAL.HeuristicLib.Operators.Evaluator;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;

namespace HEAL.HeuristicLib.Operators.Analyzer;

public class QualityCurveEvaluationWrapper<TGenotype, TEncoding, TProblem>(
  IEvaluator<TGenotype, TEncoding, TProblem> innerProblem,
  QualityCurveTracker<TGenotype> tracker)
  : IEvaluator<TGenotype, TEncoding, TProblem> where TEncoding : class, IEncoding<TGenotype>
  where TProblem : IProblem<TGenotype, TEncoding> {
  public IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TGenotype> genotypes, TEncoding encoding, TProblem problem) {
    var res = innerProblem.Evaluate(genotypes, encoding, problem);
    for (int i = 0; i < genotypes.Count; i++) tracker.TrackEvaluation(genotypes[i], res[i], problem.Objective.TotalOrderComparer);
    return res;
  }
}
