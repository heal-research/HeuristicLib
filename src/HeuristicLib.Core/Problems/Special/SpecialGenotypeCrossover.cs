using HEAL.HeuristicLib.Operators.Crossovers;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Problems.Special;

public class SpecialGenotypeCrossover : SingleSolutionCrossover<SpecialGenotype, SpecialSearchSpace, SpecialProblem>
{
  public override SpecialGenotype Cross(IParents<SpecialGenotype> parents, IRandomNumberGenerator random, SpecialSearchSpace searchSpace, SpecialProblem problem)
  {
    var (parent1, parent2) = (parents.Parent1, parents.Parent2);
    return new SpecialGenotype(random.NextDouble() < 0.5 ? parent1.Value : parent2.Value);
  }
}
