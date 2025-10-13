using HEAL.HeuristicLib.Optimization;

namespace HEAL.HeuristicLib.Problems;

public class FuncProblem<TGenotype, TEncoding>(Func<TGenotype, double> evaluateFunc, TEncoding searchSpace, Objective objective)
  : Problem<TGenotype, TEncoding>(objective, searchSpace) /*, IDeterministicProblem<TGenotype>*/
  where TEncoding : class, IEncoding<TGenotype> {
  public Func<TGenotype, double> EvaluateFunc { get; } = evaluateFunc;
  // public IEvaluator<TGenotype> GetEvaluator() {
  //   return new DeterministicProblemEvaluator<TGenotype>(this);
  // }

  public override ObjectiveVector Evaluate(TGenotype solution) {
    return EvaluateFunc(solution);
  }
}
