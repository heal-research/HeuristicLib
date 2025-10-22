using HEAL.HeuristicLib.Encodings;
using HEAL.HeuristicLib.Genotypes;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Operators.RealVectorOperators.Mutators;

public class GaussianMutator(double mutationRate, double mutationStrength) : Mutator<RealVector, RealVectorEncoding>, IVariableStrengthMutator<RealVector, RealVectorEncoding, IProblem<RealVector, RealVectorEncoding>> {
  public double MutationRate { get; set; } = mutationRate;
  public double MutationStrength { get; set; } = mutationStrength;

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

public interface IVariableStrengthMutator<T, in T1, in T2> : IMutator<T, T1, T2> where T1 : class, IEncoding<T> where T2 : class, IProblem<T, T1> {
  public double MutationStrength { get; set; }
}
