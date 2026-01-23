using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Evaluators;

public class DirectEvaluator<TGenotype> : SingleSolutionEvaluator<TGenotype, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>>
{
  protected override ObjectiveVector Evaluate(TGenotype solution, IRandomNumberGenerator random, ISearchSpace<TGenotype> searchSpace, IProblem<TGenotype, ISearchSpace<TGenotype>> problem) => problem.Evaluate(solution, random);
}
