using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Operators.Evaluator;

public class DirectEvaluator<TGenotype> : Evaluator<TGenotype, IEncoding<TGenotype>, IProblem<TGenotype, IEncoding<TGenotype>>> {
  protected override ObjectiveVector Evaluate(TGenotype solution, IRandomNumberGenerator random, IEncoding<TGenotype> searchSpace, IProblem<TGenotype, IEncoding<TGenotype>> problem) {
    return problem.Evaluate(solution, random);
  }
}
