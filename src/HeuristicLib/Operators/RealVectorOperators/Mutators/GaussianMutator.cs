using HEAL.HeuristicLib.Encodings;
using HEAL.HeuristicLib.Genotypes;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Operators.RealVectorOperators;

public class GaussianMutator : Mutator<RealVector, RealVectorEncoding> 
{
  public double MutationRate { get; set; }
  public double MutationStrength { get; set; }

  public GaussianMutator(double mutationRate, double mutationStrength) {
    MutationRate = mutationRate;
    MutationStrength = mutationStrength;
  }
  
  public override RealVector Mutate(RealVector solution, IRandomNumberGenerator random, RealVectorEncoding encoding) {
    double[] newElements = solution.ToArray();
    for (int i = 0; i < newElements.Length; i++) {
      if (random.Random() < MutationRate) {
        newElements[i] += MutationStrength * (random.Random() - 0.5);
      }
    }
    return RealVector.Clamp(new RealVector(newElements), encoding.Minimum, encoding.Maximum);
  }
}
