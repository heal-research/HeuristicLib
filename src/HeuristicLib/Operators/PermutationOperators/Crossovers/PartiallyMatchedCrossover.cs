using HEAL.HeuristicLib.Encodings;
using HEAL.HeuristicLib.Genotypes;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Operators.PermutationOperators;

public class PartiallyMatchedCrossover : Crossover<Permutation, PermutationEncoding>
{
  public override Permutation Cross((Permutation, Permutation) parents, IRandomNumberGenerator random, PermutationEncoding encoding) {
    var (parent1, parent2) = parents;
    return Permutation.OrderCrossover(parent1, parent2, random); // implement PMX
  }
}
