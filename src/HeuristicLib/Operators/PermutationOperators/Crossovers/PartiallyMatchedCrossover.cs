using HEAL.HeuristicLib.Encodings;
using HEAL.HeuristicLib.Genotypes;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Operators.PermutationOperators;

public class PartiallyMatchedCrossover : Crossover<Permutation, PermutationEncoding>
{
  public override Permutation Cross(Permutation parent1, Permutation parent2, IRandomNumberGenerator random, PermutationEncoding encoding) {
    return Permutation.OrderCrossover(parent1, parent2, random); // implement PMX
  }
}
