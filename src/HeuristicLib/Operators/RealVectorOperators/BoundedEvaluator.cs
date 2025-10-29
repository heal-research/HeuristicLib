using HEAL.HeuristicLib.Encodings;
using HEAL.HeuristicLib.Genotypes;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;

namespace HEAL.HeuristicLib.Operators.RealVectorOperators;

public class BoundedEvaluator(IEvaluator<RealVector, RealVectorEncoding> innerEvaluator) : IEvaluator<RealVector, RealVectorEncoding> {
  public IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<RealVector> genotypes, RealVectorEncoding encoding, IProblem<RealVector, RealVectorEncoding> problem) {
    var bounded = genotypes.Select(x => RealVector.Clamp(x, encoding.Minimum, encoding.Maximum)).ToArray();
    return innerEvaluator.Evaluate(bounded, encoding, problem);
  }
}
