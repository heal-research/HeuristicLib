using HEAL.HeuristicLib.Operators.Mutator;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Encodings.RealVector.Mutators;

public class GaussianMutator(double mutationRate, double mutationStrength) : Mutator<RealVector, RealVectorEncoding>, IVariableStrengthMutator<RealVector, RealVectorEncoding, IProblem<RealVector, RealVectorEncoding>> {
  public double MutationRate { get; set; } = mutationRate;
  public double MutationStrength { get; set; } = mutationStrength;

  public override RealVector Mutate(RealVector solution, IRandomNumberGenerator random, RealVectorEncoding encoding) {
    var newElements = solution.ToArray();
    for (var i = 0; i < newElements.Length; i++) {
      if (random.Random() < MutationRate) {
        newElements[i] += MutationStrength * (random.Random() - 0.5);
      }
    }

    return RealVector.Clamp(new RealVector(newElements), encoding.Minimum, encoding.Maximum);
  }
}
