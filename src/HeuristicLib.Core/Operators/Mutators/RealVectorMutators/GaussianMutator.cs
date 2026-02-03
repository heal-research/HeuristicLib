using HEAL.HeuristicLib.Genotypes.Vectors;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces.Vectors;

namespace HEAL.HeuristicLib.Operators.Mutators.RealVectorMutators;

public class GaussianMutator(double mutationRate, double mutationStrength) : Mutator<RealVector, RealVectorSearchSpace>, IVariableStrengthMutator<RealVector, RealVectorSearchSpace, IProblem<RealVector, RealVectorSearchSpace>>
{
  public double MutationRate { get; set; } = mutationRate;
  public double MutationStrength { get; set; } = mutationStrength;

  public override RealVector Mutate(RealVector solution, IRandomNumberGenerator random, RealVectorSearchSpace searchSpace)
  {
    var newElements = solution.ToArray();
    for (var i = 0; i < newElements.Length; i++) {
      if (random.Random() < MutationRate) {
        newElements[i] += MutationStrength * (random.Random() - 0.5);
      }
    }

    return RealVector.Clamp(new RealVector(newElements), searchSpace.Minimum, searchSpace.Maximum);
  }
}
