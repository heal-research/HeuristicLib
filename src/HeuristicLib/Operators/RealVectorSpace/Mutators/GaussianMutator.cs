using HEAL.HeuristicLib.Genotypes;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.RealVectorSpace;

public record class GaussianMutator : Mutator<RealVector, RealVectorSearchSpace> 
{
  public double MutationRate { get; }
  public double MutationStrength { get; }

  public GaussianMutator(double mutationRate, double mutationStrength) {
    MutationRate = mutationRate;
    MutationStrength = mutationStrength;
  }

  public override GaussianMutatorExecution CreateExecution(RealVectorSearchSpace searchSpace) => new GaussianMutatorExecution(this, searchSpace);
}

public class GaussianMutatorExecution : MutatorExecution<RealVector, RealVectorSearchSpace, GaussianMutator>
{
  public GaussianMutatorExecution(GaussianMutator parameters, RealVectorSearchSpace searchSpace) : base(parameters, searchSpace) { }
  public override RealVector Mutate(RealVector solution, IRandomNumberGenerator random) {
    double[] newElements = solution.ToArray();
    for (int i = 0; i < newElements.Length; i++) {
      if (random.Random() < Parameters.MutationRate) {
        newElements[i] += Parameters.MutationStrength * (random.Random() - 0.5);
      }
    }
    return RealVector.Clamp(new RealVector(newElements), SearchSpace.Minimum, SearchSpace.Maximum);
  }
}
