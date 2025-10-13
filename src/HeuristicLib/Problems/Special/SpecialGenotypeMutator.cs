using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Problems.Special;

public class SpecialGenotypeMutator : Mutator<SpecialGenotype, SpecialEncoding, SpecialProblem> {
  public override SpecialGenotype Mutate(SpecialGenotype genotype, IRandomNumberGenerator random, SpecialEncoding encoding, SpecialProblem problem) {
    int strength = (int)Math.Round(problem.Data);
    int offset = random.Integer(-strength, strength + 1);
    return new SpecialGenotype(genotype.Value + offset);
  }
}
