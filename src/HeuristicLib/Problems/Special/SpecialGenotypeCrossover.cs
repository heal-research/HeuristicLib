using HEAL.HeuristicLib.Operators.Crossover;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Problems.Special;

public class SpecialGenotypeCrossover : Crossover<SpecialGenotype, SpecialEncoding, SpecialProblem> {
  public override SpecialGenotype Cross((SpecialGenotype, SpecialGenotype) parents, IRandomNumberGenerator random, SpecialEncoding encoding, SpecialProblem problem) {
    var (parent1, parent2) = parents;
    return new SpecialGenotype(random.Random() < 0.5 ? parent1.Value : parent2.Value);
  }
}
