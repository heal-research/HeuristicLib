using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Evaluators;

// ToDo: probably rename to ProblemEvaluator or DirectProblemEvaluator, to make it more clear that this evaluator talks to the Problem directly.
public class DirectEvaluator<TGenotype>
  : SingleSolutionStatelessEvaluator<TGenotype, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>>
  where TGenotype : class
{
  public override ObjectiveVector Evaluate(TGenotype genotype, IRandomNumberGenerator random, ISearchSpace<TGenotype> searchSpace, IProblem<TGenotype, ISearchSpace<TGenotype>> problem)
  {
    return problem.Evaluate(genotype, random);
  }
}
