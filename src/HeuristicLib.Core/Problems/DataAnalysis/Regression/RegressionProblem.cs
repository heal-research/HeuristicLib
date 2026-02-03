using HEAL.HeuristicLib.Collections;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Problems.DataAnalysis.Regression;

public abstract class RegressionProblem<TProblemData, TSolution, TSearchSpace> : DataAnalysisProblem<TProblemData, TSolution, TSearchSpace>
  where TProblemData : RegressionProblemData
  where TSearchSpace : class, ISearchSpace<TSolution>
{
  public const double PunishmentFactor = 10.0;
  private readonly int[] rowIndicesCache;//unsure if this is faster than using the enumerable directly

  private readonly double[] trainingTargetCache;

  protected RegressionProblem(TProblemData problemData, ICollection<IRegressionEvaluator<TSolution>> objective, IComparer<ObjectiveVector> a, TSearchSpace encoding) : base(problemData, new Objective(objective.Select(x => x.Direction).ToArray(), a), encoding)
  {
    Evaluators = objective.ToList();
    trainingTargetCache = problemData.TargetVariableValues(DataAnalysisProblemData.PartitionType.Training).ToArray();
    rowIndicesCache = problemData.Partitions[DataAnalysisProblemData.PartitionType.Training].Enumerate().ToArray();
    if (trainingTargetCache.Length == 0) {
      return;
    }
    var mean = trainingTargetCache.Average();
    var range = trainingTargetCache.Range();
    UpperPredictionBound = mean + PunishmentFactor * range;
    LowerPredictionBound = mean - PunishmentFactor * range;
  }
  public IReadOnlyList<IRegressionEvaluator<TSolution>> Evaluators { get; set; }

  public double UpperPredictionBound { get; set; }

  public double LowerPredictionBound { get; set; }

  public override ObjectiveVector Evaluate(TSolution solution) => Evaluate(solution, rowIndicesCache, trainingTargetCache);

  public ObjectiveVector Evaluate(TSolution solution, IReadOnlyList<int> rows, IReadOnlyList<double> targets)
  {
    var predictions = PredictAndTrain(solution, rows, targets)
      .LimitToRange(LowerPredictionBound, UpperPredictionBound);
    if (Evaluators.Count == 1) {
      return new ObjectiveVector(Evaluators[0].Evaluate(solution, predictions, targets));
    }
    if (predictions is not ICollection<double> materialPredictions) {
      materialPredictions = predictions.ToArray();
    }

    return new ObjectiveVector(Evaluators.Select(x => x.Evaluate(solution, materialPredictions, trainingTargetCache)));
  }

  public abstract IEnumerable<double> PredictAndTrain(TSolution solution, IReadOnlyList<int> rows, IReadOnlyList<double> targets);
}
