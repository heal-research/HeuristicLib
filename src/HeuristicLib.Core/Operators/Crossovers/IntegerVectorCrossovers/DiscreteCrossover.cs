using HEAL.HeuristicLib.Genotypes.Vectors;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces.Vectors;

namespace HEAL.HeuristicLib.Operators.Crossovers.IntegerVectorCrossovers;

/// <summary>
/// Discrete crossover for integer vectors.
/// </summary>
/// 
/// It is implemented as described in Gwiazda, T.D. 2006.
/// Genetic algorithms reference Volume I Crossover for single-objective numerical optimization problems, p.17.
public record DiscreteCrossover : SingleSolutionCrossover<IntegerVector, IntegerVectorSearchSpace>
{
  public override IntegerVector Cross(IParents<IntegerVector> parents, IRandomNumberGenerator random, IntegerVectorSearchSpace searchSpace)
    => Apply(random, [parents.Parent1, parents.Parent2]);

  public static IntegerVector Apply(IRandomNumberGenerator random, IReadOnlyList<IntegerVector> parents)
  {
    var n = parents.Count;
    if (n < 2)
      throw new ArgumentException("DiscreteCrossover: There are less than two parents to cross.");
    int length = parents[0].Count;

    for (int i = 0; i < n; i++) {
      if (parents[i].Count != length)
        throw new ArgumentException("DiscreteCrossover: The parents' vectors are of different length.", nameof(parents));
    }

    var result = new int[length];
    for (int i = 0; i < length; i++)
      result[i] = parents[random.NextInt(n)][i];

    return result;
  }
}
