using HEAL.HeuristicLib.Optimization;

namespace HEAL.HeuristicLib.Problems.DataAnalysis.Regression;

public abstract class RegressionProblem<TProblemData, TSolution, TEncoding>(TProblemData problemData, ICollection<IRegressionEvaluator> objective, IComparer<ObjectiveVector> a, TEncoding encoding)
  : DataAnalysisProblem<TProblemData, TSolution, TEncoding>(problemData, new(objective.Select(x => x.Direction).ToArray(), a), encoding)
  where TProblemData : RegressionProblemData
  where TEncoding : class, IEncoding<TSolution> {
  public IReadOnlyList<IRegressionEvaluator> Evaluators { get; set; } = objective.ToList();

  private readonly double[] trainingTargetCache = problemData.TargetVariableValues(DataAnalysisProblemData.PartitionType.Training).ToArray();
  private readonly int[] rowIndicesCache = problemData.Partitions[DataAnalysisProblemData.PartitionType.Training].Enumerate().ToArray(); //unsure if this is faster than using the enumerable directly

  public override ObjectiveVector Evaluate(TSolution solution) => RegressionProblemDataExtensions.Evaluate(Decode(solution), ProblemData.Dataset, rowIndicesCache, Evaluators, trainingTargetCache);

  protected abstract IRegressionModel Decode(TSolution solution);
}
