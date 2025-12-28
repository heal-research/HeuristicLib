using HEAL.HeuristicLib.Encodings;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Problems.DataAnalysis;

public abstract class DataAnalysisProblem<TProblemData, TISolution, TEncoding>(TProblemData problemData, Objective objective, TEncoding encoding)
  : Problem<TISolution, TEncoding>(objective, encoding)
  where TProblemData : DataAnalysisProblemData
  where TEncoding : class, IEncoding<TISolution> {
  public virtual TProblemData ProblemData {
    get;
  } = problemData;

  // Maybe earlier in the hierarchy for general deterministic problems?
  public sealed override ObjectiveVector Evaluate(TISolution solution, IRandomNumberGenerator random) => Evaluate(solution);
  public abstract ObjectiveVector Evaluate(TISolution solution);
}
