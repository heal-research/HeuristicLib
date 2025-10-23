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

  public static ObjectiveVector Evaluate(this RegressionProblemData data, IRegressionModel solution, DataAnalysisProblemData.PartitionType type, IReadOnlyList<RegressionEvaluator> evaluators, double lowerPredictionLimit = double.MinValue, double upperPredictionLimit = double.MaxValue) {
    IEnumerable<int> rows = data.Partitions[type].Enumerate();
    IReadOnlyList<double> targets = data.TargetVariableValues(type);
    var predictions = solution.Predict(data.Dataset, rows).LimitToRange(lowerPredictionLimit, upperPredictionLimit);
    if (evaluators.Count == 1)
      return new ObjectiveVector(evaluators[0].Evaluate(predictions, targets));
    if (predictions is not ICollection<double> materialPredictions)
      materialPredictions = predictions.ToArray();
    return new ObjectiveVector(evaluators.Select(x => x.Evaluate(materialPredictions, targets)));
  }
}
