using System.Net.Mime;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Operators.Evaluator;

public class DirectEvaluator<TGenotype> : Evaluator<TGenotype, IEncoding<TGenotype>, IProblem<TGenotype, IEncoding<TGenotype>>> {
  public override ObjectiveVector Evaluate(TGenotype solution, IRandomNumberGenerator random, IEncoding<TGenotype> encoding, IProblem<TGenotype, IEncoding<TGenotype>> problem) {
    return problem.Evaluate(solution, random);
  }
}

public static class ProblemEvaluator {
  public static DirectEvaluator<TGenotype> CreateEvaluator<TGenotype, TEncoding>(this IProblem<TGenotype, TEncoding> problem) where TEncoding : class, IEncoding<TGenotype> {
    return new DirectEvaluator<TGenotype>();
  }
}
