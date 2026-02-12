using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Evaluators;

public record DirectEvaluator<TGenotype>
  : SingleSolutionStatelessEvaluator<TGenotype, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>>
{
  public override ObjectiveVector Evaluate(TGenotype genotype, IRandomNumberGenerator random, ISearchSpace<TGenotype> searchSpace, IProblem<TGenotype, ISearchSpace<TGenotype>> problem)
  {
    return problem.Evaluate(genotype, random);
  }
}

public static class DirectEvaluatorExtensions
{
  public static DirectEvaluator<TGenotype> CreateEvaluator<TGenotype>(this IProblem<TGenotype, ISearchSpace<TGenotype>> algorithm) => new();
}
