using HEAL.HeuristicLib.Operators.Creators;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Tests.SpecialTestEncoding;

public class SpecialGenotypeCreator(int parameter) : SingleSolutionCreator<SpecialGenotype, SpecialSearchSpace, SpecialProblem>
{
  public int Parameter { get; set; } = parameter;

  public override SpecialGenotype Create(IRandomNumberGenerator random, SpecialSearchSpace searchSpace, SpecialProblem problem) => new(random.NextInt(0, Parameter));
}
