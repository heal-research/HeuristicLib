using HEAL.HeuristicLib.Operators.Evaluator;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Operators.Analyzer;

public class QualityCurveEvaluationWrapper<TGenotype, TEncoding, TProblem>(
  IEvaluator<TGenotype, TEncoding, TProblem> innerProblem,
  QualityCurveTracker<TGenotype> tracker)
  : BatchEvaluator<TGenotype, TEncoding, TProblem> where TEncoding : class, IEncoding<TGenotype>
  where TProblem : class, IProblem<TGenotype, TEncoding> {
  public override IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TGenotype> genotypes, IRandomNumberGenerator random, TEncoding encoding, TProblem problem) {
    var res = innerProblem.Evaluate(genotypes, random, encoding, problem);
    for (int i = 0; i < genotypes.Count; i++) tracker.TrackEvaluation(genotypes[i], res[i], problem.Objective.TotalOrderComparer);
    return res;
  }
}
