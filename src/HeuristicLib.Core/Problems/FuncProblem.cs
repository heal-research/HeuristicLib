using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Problems;

public static class FuncProblem
{
  public static FuncProblem<TGenotype, TSearchSpace> Create<TGenotype, TSearchSpace>(Func<TGenotype, double> evaluateFunc, TSearchSpace encoding, Objective objective) where TSearchSpace : class, ISearchSpace<TGenotype> => new(evaluateFunc, encoding, objective);
}

public class FuncProblem<TGenotype, TSearchSpace> : SingleSolutionProblem<TGenotype, TSearchSpace>
  where TSearchSpace : class, ISearchSpace<TGenotype>
{
  public FuncProblem(Func<TGenotype, ObjectiveVector> evaluateFunc, TSearchSpace searchSpace, Objective objective) : base(objective, searchSpace)
  {
    EvaluateFunc = evaluateFunc;
  }

  public FuncProblem(Func<TGenotype, double> evaluateFunc, TSearchSpace searchSpace, Objective objective) : base(objective, searchSpace)
  {
    EvaluateFunc = sol => evaluateFunc(sol);
  }

  public FuncProblem(Func<TGenotype, double[]> evaluateFunc, TSearchSpace searchSpace, Objective objective) : base(objective, searchSpace)
  {
    EvaluateFunc = sol => evaluateFunc(sol);
  }

  private Func<TGenotype, ObjectiveVector> EvaluateFunc { get; }

  public override ObjectiveVector Evaluate(TGenotype solution, IRandomNumberGenerator random) => EvaluateFunc(solution);
}
