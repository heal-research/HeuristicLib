using HEAL.HeuristicLib.Operators.Creator;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Problems.Special;

public class SpecialGenotypeCreator(int parameter) : Creator<SpecialGenotype, SpecialEncoding, SpecialProblem> {
  public int Parameter { get; set; } = parameter;

  public override SpecialGenotype Create(IRandomNumberGenerator random, SpecialEncoding encoding, SpecialProblem problem) {
    return new SpecialGenotype(random.Integer(0, Parameter));
  }
}
