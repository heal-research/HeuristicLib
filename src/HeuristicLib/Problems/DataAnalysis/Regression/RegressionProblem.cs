using HEAL.HeuristicLib.Collections;
using HEAL.HeuristicLib.Encodings;
using HEAL.HeuristicLib.Optimization;

namespace HEAL.HeuristicLib.Problems.DataAnalysis.Regression;

public abstract class RegressionProblem<TProblemData, TISolution, TEncoding> : DataAnalysisProblem<TProblemData, TISolution, TEncoding>
  where TProblemData : RegressionProblemData
  where TEncoding : class, IEncoding<TISolution> {
  public const double PunishmentFactor = 10.0;
  public IReadOnlyList<IRegressionEvaluator<TISolution>> Evaluators { get; set; }

  private readonly double[] trainingTargetCache;
  private readonly int[] rowIndicesCache; //unsure if this is faster than using the enumerable directly

  protected RegressionProblem(TProblemData problemData, ICollection<IRegressionEvaluator<TISolution>> objective, IComparer<ObjectiveVector> a, TEncoding encoding) : base(problemData, new Objective(objective.Select(x => x.Direction).ToArray(), a), encoding) {
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

  public override ObjectiveVector Evaluate(TISolution solution) {
    IRegressionModel solution1 = Decode(solution);
    var predictions = solution1.Predict(ProblemData.Dataset, rowIndicesCache).LimitToRange(LowerPredictionBound, UpperPredictionBound);
    if (Evaluators.Count == 1)
      return new ObjectiveVector(Evaluators[0].Evaluate(solution, predictions, trainingTargetCache));
    if (predictions is not ICollection<double> materialPredictions)
      materialPredictions = predictions.ToArray();
    return new ObjectiveVector(Evaluators.Select(x => x.Evaluate(solution, materialPredictions, trainingTargetCache)));
  }

  protected abstract IRegressionModel Decode(TISolution solution);
}
