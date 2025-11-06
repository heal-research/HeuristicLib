using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Problems.DataAnalysis;

public abstract class DataAnalysisProblem<TProblemData, TSolution, TEncoding>(TProblemData problemData, Objective objective, TEncoding encoding)
  : Problem<TSolution, TEncoding>(objective, encoding)
  where TProblemData : DataAnalysisProblemData
  where TEncoding : class, IEncoding<TSolution> {
  public virtual TProblemData ProblemData {
    get;
  } = problemData;

  // Maybe earlier in the hierarchy for general deterministic problems?
  public sealed override ObjectiveVector Evaluate(TSolution solution, IRandomNumberGenerator random) => Evaluate(solution);
  public abstract ObjectiveVector Evaluate(TSolution solution);
}
