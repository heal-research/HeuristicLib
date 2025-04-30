using HEAL.HeuristicLib.Genotypes;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.PermutationSpace;

public record class PartiallyMatchedCrossover : Crossover<Permutation, PermutationSearchSpace> {
  public override PartiallyMatchedCrossoverInstance CreateInstance() => new PartiallyMatchedCrossoverInstance(this);
}

public class PartiallyMatchedCrossoverInstance : CrossoverInstance<Permutation, PermutationSearchSpace, PartiallyMatchedCrossover> {
  public PartiallyMatchedCrossoverInstance(PartiallyMatchedCrossover parameters) : base(parameters) { }
  public override Permutation Cross(Permutation parent1, Permutation parent2, PermutationSearchSpace searchSpace, IRandomNumberGenerator random) {
    return Permutation.OrderCrossover(parent1, parent2, random); // implement PMX
  }
}
