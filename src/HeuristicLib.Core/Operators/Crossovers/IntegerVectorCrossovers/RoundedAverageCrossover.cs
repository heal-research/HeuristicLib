using HEAL.HeuristicLib.Genotypes.Vectors;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces.Vectors;

namespace HEAL.HeuristicLib.Operators.Crossovers.IntegerVectorCrossovers;

public record RoundedAverageCrossover : SingleSolutionCrossover<IntegerVector, IntegerVectorSearchSpace>
{
  public override IntegerVector Cross(IParents<IntegerVector> parents, IRandomNumberGenerator random, IntegerVectorSearchSpace searchSpace)
    => Apply(random, [parents.Parent1, parents.Parent2], searchSpace);

  public static IntegerVector Apply(IRandomNumberGenerator random, IReadOnlyList<IntegerVector> parents, IntegerVectorSearchSpace searchSpace)
  {
    int length = parents[0].Count, parentsCount = parents.Count;
    var result = new int[length];
    for (int i = 0; i < length; i++) {
      long avg = 0;
      for (int j = 0; j < parentsCount; j++)
        avg += parents[j][i];
      result[i] = searchSpace.RoundFeasible(avg / (double)parentsCount, i);
    }

    return new IntegerVector(result);
  }
}
