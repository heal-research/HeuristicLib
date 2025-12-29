using HEAL.HeuristicLib.Operators.Creators;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Problems.Special;

public class SpecialGenotypeCreator(int parameter) : Creator<SpecialGenotype, SpecialSearchSpace, SpecialProblem> {
  public int Parameter { get; set; } = parameter;

  public override SpecialGenotype Create(IRandomNumberGenerator random, SpecialSearchSpace searchSpace, SpecialProblem problem) {
    return new SpecialGenotype(random.Integer(0, Parameter));
  }
}
