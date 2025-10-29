using HEAL.HeuristicLib.Optimization;

namespace HEAL.HeuristicLib.Problems.DataAnalysis;

public abstract class DataAnalysisProblem<TProblemData, TSolution, TEncoding>(TProblemData problemData, Objective objective, TEncoding encoding)
  : Problem<TSolution, TEncoding>(objective, encoding)
  where TProblemData : DataAnalysisProblemData
  where TEncoding : class, IEncoding<TSolution> {
  public virtual TProblemData ProblemData {
    get;
  } = problemData;
}
