using HEAL.HeuristicLib.Genotypes.Vectors;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces.Vectors;

namespace HEAL.HeuristicLib.Operators.Crossovers.IntegerVectorCrossovers;

public record RoundedHeuristicCrossover : SingleSolutionCrossover<IntegerVector, IntegerVectorSearchSpace>
{
  public override IntegerVector Cross(IParents<IntegerVector> parents, IRandomNumberGenerator random, IntegerVectorSearchSpace searchSpace)
  {
    return Apply(random, parents.Parent1, parents.Parent2, searchSpace);
  }

  public static IntegerVector Apply(
    IRandomNumberGenerator random,
    IntegerVector betterParent,
    IntegerVector worseParent,
    IntegerVectorSearchSpace searchSpace)
  {
    if (betterParent.Count != worseParent.Count)
      throw new ArgumentException("Parents must have same length.", nameof(betterParent));

    int length = betterParent.Count;
    var result = new int[length];

    // HL uses one factor for the whole vector
    double factor = random.NextDouble(); // in [0,1)

    for (int i = 0; i < length; i++) {
      double x = betterParent[i] + factor * (betterParent[i] - worseParent[i]);
      result[i] = searchSpace.RoundFeasible(x, i);
    }

    return result;
  }
}
