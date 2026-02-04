using HEAL.HeuristicLib.Operators.Mutators;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Tests.SpecialTestEncoding;

public class SpecialGenotypeMutator : SingleSolutionMutator<SpecialGenotype, SpecialSearchSpace, SpecialProblem>
{
  public override SpecialGenotype Mutate(SpecialGenotype genotype, IRandomNumberGenerator random, SpecialSearchSpace searchSpace, SpecialProblem problem)
  {
    var strength = (int)Math.Round(problem.Data);
    var offset = random.NextInt(-strength, strength + 1);
    return new SpecialGenotype(genotype.Value + offset);
  }
}
