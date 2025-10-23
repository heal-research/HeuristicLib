using HEAL.HeuristicLib.Optimization;

namespace HEAL.HeuristicLib.Problems.DataAnalysis.Regression;

public static class RegressionProblemDataExtensions {
  public static IEnumerable<double> LimitToRange(this IEnumerable<double> values, double min, double max) {
    ArgumentOutOfRangeException.ThrowIfGreaterThan(min, max);
    foreach (var x in values) {
      if (double.IsNaN(x)) yield return (max + min) / 2.0;
      else if (x < min) yield return min;
      else if (x > max) yield return max;
      else yield return x;
    }
  }

  public static ObjectiveVector Evaluate(IRegressionModel solution, Dataset dataset, IEnumerable<int> rows, IReadOnlyList<IRegressionEvaluator> evaluators, IReadOnlyList<double> targets, double lowerPredictionLimit = double.MinValue, double upperPredictionLimit = double.MaxValue) {
    var predictions = solution.Predict(dataset, rows).LimitToRange(lowerPredictionLimit, upperPredictionLimit);
    if (evaluators.Count == 1)
      return new ObjectiveVector(evaluators[0].Evaluate(targets, predictions));
    if (predictions is not ICollection<double> materialPredictions)
      materialPredictions = predictions.ToArray();
    return new ObjectiveVector(evaluators.Select(x => x.Evaluate(targets, materialPredictions)));
  }

  public static ObjectiveVector Evaluate(this RegressionProblemData data, IRegressionModel solution, DataAnalysisProblemData.PartitionType type, IReadOnlyList<IRegressionEvaluator> evaluators, double lowerPredictionLimit = double.MinValue, double upperPredictionLimit = double.MaxValue) {
    return Evaluate(solution, data.Dataset, data.Partitions[type].Enumerate(), evaluators, data.TargetVariableValues(type), lowerPredictionLimit, upperPredictionLimit);
  }
}
