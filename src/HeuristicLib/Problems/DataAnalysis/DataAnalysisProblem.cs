using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Problems.DataAnalysis;

public abstract class DataAnalysisProblem<TProblemData, TISolution, TSearchSpace>(TProblemData problemData, Objective objective, TSearchSpace searchSpace)
  : Problem<TISolution, TSearchSpace>(objective, searchSpace)
  where TProblemData : DataAnalysisProblemData
  where TSearchSpace : class, ISearchSpace<TISolution> {
  public virtual TProblemData ProblemData {
    get;
  } = problemData;

  // Maybe earlier in the hierarchy for general deterministic problems?
  public sealed override ObjectiveVector Evaluate(TISolution solution, IRandomNumberGenerator random) => Evaluate(solution);
  public abstract ObjectiveVector Evaluate(TISolution solution);
}
