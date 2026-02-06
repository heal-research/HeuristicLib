using HEAL.HeuristicLib.Genotypes.Vectors;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces.Vectors;

namespace HEAL.HeuristicLib.Operators.Mutators.RealVectorMutators;

public class GaussianMutator 
  : SingleSolutionStatelessMutator<RealVector, RealVectorSearchSpace>,
    IVariableStrengthMutator<RealVector, RealVectorSearchSpace, IProblem<RealVector, RealVectorSearchSpace>>
{
  public GaussianMutator(double mutationRate, double mutationStrength)
  {
    MutationRate = mutationRate;
    MutationStrength = mutationStrength;
  }
  public double MutationRate { get; set; }
  public double MutationStrength { get; set; }

  public override RealVector Mutate(RealVector solution, IRandomNumberGenerator random, RealVectorSearchSpace searchSpace)
  {
    var newElements = solution.ToArray();
    for (var i = 0; i < newElements.Length; i++) {
      if (random.NextDouble() < MutationRate) {
        newElements[i] += MutationStrength * (random.NextDouble() - 0.5);
      }
    }

    return RealVector.Clamp(new RealVector(newElements), searchSpace.Minimum, searchSpace.Maximum);
  }
}
