using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Problems;

public static class FuncProblem
{
  public static FuncProblem<TGenotype, TSearchSpace> Create<TGenotype, TSearchSpace>(Func<TGenotype, double> evaluateFunc, TSearchSpace encoding, Objective objective) where TSearchSpace : class, ISearchSpace<TGenotype> => new(evaluateFunc, encoding, objective);
}

public class FuncProblem<TGenotype, TSearchSpace>(Func<TGenotype, double> evaluateFunc, TSearchSpace searchSpace, Objective objective)
  : Problem<TGenotype, TSearchSpace>(objective, searchSpace)/*, IDeterministicProblem<TGenotype>*/
  where TSearchSpace : class, ISearchSpace<TGenotype>
{
  public Func<TGenotype, double> EvaluateFunc { get; } = evaluateFunc;
  // public IEvaluator<TGenotype> GetEvaluator() {
  //   return new DeterministicProblemEvaluator<TGenotype>(this);
  // }

  public override ObjectiveVector Evaluate(TGenotype solution, IRandomNumberGenerator random) => EvaluateFunc(solution);
}
