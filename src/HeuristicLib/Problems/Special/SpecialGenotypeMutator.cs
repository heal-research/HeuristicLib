using HEAL.HeuristicLib.Operators.Mutator;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Problems.Special;

public class SpecialGenotypeMutator : Mutator<SpecialGenotype, SpecialEncoding, SpecialProblem> {
  public override SpecialGenotype Mutate(SpecialGenotype genotype, IRandomNumberGenerator random, SpecialEncoding encoding, SpecialProblem problem) {
    var strength = (int)Math.Round(problem.Data);
    var offset = random.Integer(-strength, strength + 1);
    return new SpecialGenotype(genotype.Value + offset);
  }
}
