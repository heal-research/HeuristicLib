using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Problems;

public class FuncProblem {
  public static FuncProblem<TGenotype, TEncoding> Create<TGenotype, TEncoding>(
    Func<TGenotype, double> evaluateFunc,
    TEncoding encoding,
    Objective objective
  ) where TEncoding : class, IEncoding<TGenotype> {
    return new FuncProblem<TGenotype, TEncoding>(evaluateFunc, encoding, objective);
  }
}

public class FuncProblem<TGenotype, TEncoding>(Func<TGenotype, double> evaluateFunc, TEncoding searchSpace, Objective objective)
  : Problem<TGenotype, TEncoding>(objective, searchSpace) /*, IDeterministicProblem<TGenotype>*/
  where TEncoding : class, IEncoding<TGenotype> {
  public Func<TGenotype, double> EvaluateFunc { get; } = evaluateFunc;
  // public IEvaluator<TGenotype> GetEvaluator() {
  //   return new DeterministicProblemEvaluator<TGenotype>(this);
  // }

  public override ObjectiveVector Evaluate(TGenotype solution, IRandomNumberGenerator random) {
    return EvaluateFunc(solution);
  }
}
