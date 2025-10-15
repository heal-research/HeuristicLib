using HEAL.HeuristicLib.Optimization;

namespace HEAL.HeuristicLib.Problems.DataAnalysis.Regression;

public abstract class RegressionProblem<TProblemData, TSolution, TEncoding> : DataAnalysisProblem<TProblemData, TSolution, TEncoding>
  where TProblemData : RegressionProblemData
  where TEncoding : class, IEncoding<TSolution> {
  public const double PunishmentFactor = 10.0;
  public IReadOnlyList<IRegressionEvaluator> Evaluators { get; set; }

  private readonly double[] trainingTargetCache;
  private readonly int[] rowIndicesCache; //unsure if this is faster than using the enumerable directly

  protected RegressionProblem(TProblemData problemData, ICollection<IRegressionEvaluator> objective, IComparer<ObjectiveVector> a, TEncoding encoding) : base(problemData, new Objective(objective.Select(x => x.Direction).ToArray(), a), encoding) {
    Evaluators = objective.ToList();
    trainingTargetCache = problemData.TargetVariableValues(DataAnalysisProblemData.PartitionType.Training).ToArray();
    rowIndicesCache = problemData.Partitions[DataAnalysisProblemData.PartitionType.Training].Enumerate().ToArray();
    if (trainingTargetCache.Length == 0) return;
    var mean = trainingTargetCache.Average();
    var range = trainingTargetCache.Range();
    UpperPredictionBound = mean + PunishmentFactor * range;
    LowerPredictionBound = mean - PunishmentFactor * range;
  }

  public double UpperPredictionBound { get; set; }

  public double LowerPredictionBound { get; set; }

  public override ObjectiveVector Evaluate(TSolution solution) => RegressionProblemDataExtensions.Evaluate(Decode(solution), ProblemData.Dataset, rowIndicesCache, Evaluators, trainingTargetCache, LowerPredictionBound, UpperPredictionBound);

  protected abstract IRegressionModel Decode(TSolution solution);
}
