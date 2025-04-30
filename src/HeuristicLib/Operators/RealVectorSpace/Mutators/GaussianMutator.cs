using HEAL.HeuristicLib.Genotypes;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.RealVectorSpace;

public record class GaussianMutator : Mutator<RealVector, RealVectorSearchSpace> {
  public double MutationRate { get; }
  public double MutationStrength { get; }

  public GaussianMutator(double mutationRate, double mutationStrength) {
    MutationRate = mutationRate;
    MutationStrength = mutationStrength;
  }

  public override GaussianMutatorInstance CreateInstance() => new GaussianMutatorInstance(this);
}

public class GaussianMutatorInstance : MutatorInstance<RealVector, RealVectorSearchSpace, GaussianMutator> {
  public GaussianMutatorInstance(GaussianMutator parameters) : base(parameters) { }
  public override RealVector Mutate(RealVector solution, RealVectorSearchSpace searchSpace, IRandomNumberGenerator random) {
    double[] newElements = solution.ToArray();
    for (int i = 0; i < newElements.Length; i++) {
      if (random.Random() < Parameters.MutationRate) {
        newElements[i] += Parameters.MutationStrength * (random.Random() - 0.5);
      }
    }
    return RealVector.Clamp(new RealVector(newElements), searchSpace.Minimum, searchSpace.Maximum);
  }
}
