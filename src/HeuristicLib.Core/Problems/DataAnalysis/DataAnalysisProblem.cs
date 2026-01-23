using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Problems.DataAnalysis;

public abstract class DataAnalysisProblem<TProblemData, TSolution, TSearchSpace>(TProblemData problemData, Objective objective, TSearchSpace searchSpace)
  : Problem<TSolution, TSearchSpace>(objective, searchSpace)
  where TProblemData : DataAnalysisProblemData
  where TSearchSpace : class, ISearchSpace<TSolution>
{
  public virtual TProblemData ProblemData
  {
    get;
  } = problemData;

  // Maybe earlier in the hierarchy for general deterministic problems?
  public sealed override ObjectiveVector Evaluate(TSolution solution, IRandomNumberGenerator random) => Evaluate(solution);
  public abstract ObjectiveVector Evaluate(TSolution solution);
}
