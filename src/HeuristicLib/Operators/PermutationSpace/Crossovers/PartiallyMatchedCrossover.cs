using HEAL.HeuristicLib.Genotypes;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.PermutationSpace;

public record class PartiallyMatchedCrossover : Crossover<Permutation, PermutationSearchSpace>
{
  public override PartiallyMatchedCrossoverExecution CreateExecution(PermutationSearchSpace searchSpace) => new PartiallyMatchedCrossoverExecution(this, searchSpace);
}

public class PartiallyMatchedCrossoverExecution : CrossoverExecution<Permutation, PermutationSearchSpace, PartiallyMatchedCrossover> 
{
  public PartiallyMatchedCrossoverExecution(PartiallyMatchedCrossover parameters, PermutationSearchSpace searchSpace) : base(parameters, searchSpace) { }
  public override Permutation Cross(Permutation parent1, Permutation parent2, IRandomNumberGenerator random) {
    return Permutation.OrderCrossover(parent1, parent2, random); // implement PMX
  }
}
