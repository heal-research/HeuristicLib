using System.Net.Mime;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Operators.Evaluator;

public class ProblemEvaluator<TGenotype, TEncoding, TProblem> : Evaluator<TGenotype, TEncoding, TProblem> 
  where TEncoding : class, IEncoding<TGenotype>
  where TProblem : class, IProblem<TGenotype, TEncoding>
{
  public override ObjectiveVector Evaluate(TGenotype solution, IRandomNumberGenerator random, TEncoding encoding, TProblem problem) {
    return problem.Evaluate(solution, random);
  }
}

public static class ProblemEvaluator {
  public static ProblemEvaluator<TGenotype, TEncoding, TProblem> Create<TGenotype, TEncoding, TProblem>()
    where TEncoding : class, IEncoding<TGenotype>
    where TProblem : class, IProblem<TGenotype, TEncoding> 
  {
    return new ProblemEvaluator<TGenotype, TEncoding, TProblem>();
  }

  public static ProblemEvaluator<TGenotype, TEncoding, TProblem> CreateEvaluator<TGenotype, TEncoding, TProblem>(this TProblem problem)
    where TEncoding : class, IEncoding<TGenotype>
    where TProblem : class, IProblem<TGenotype, TEncoding> {
    return Create<TGenotype, TEncoding, TProblem>();
  }
}
