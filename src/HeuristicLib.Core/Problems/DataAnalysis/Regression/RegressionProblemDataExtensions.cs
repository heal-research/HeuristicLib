using HEAL.HeuristicLib.Optimization;

namespace HEAL.HeuristicLib.Problems.DataAnalysis.Regression;

public static class RegressionProblemDataExtensions
{
  public static IEnumerable<double> LimitToRange(this IEnumerable<double> values, double min, double max)
  {
    ArgumentOutOfRangeException.ThrowIfGreaterThan(min, max);
    foreach (var x in values) {
      if (double.IsNaN(x)) {
        yield return (max + min) / 2.0;
      } else if (x < min) {
        yield return min;
      } else if (x > max) {
        yield return max;
      } else {
        yield return x;
      }
    }
  }

  public static ObjectiveVector Evaluate(this RegressionProblemData data, IRegressionModel solution, DataAnalysisProblemData.PartitionType type, IReadOnlyList<RegressionEvaluator> evaluators, double lowerPredictionLimit = double.MinValue, double upperPredictionLimit = double.MaxValue)
  {
    var targets = data.TargetVariableValues(type);
    var predictions = solution.Predict(data.Dataset, data.Partitions[type].Enumerate());

    return Evaluate(evaluators, predictions, targets, lowerPredictionLimit, upperPredictionLimit);
  }

  private static ObjectiveVector Evaluate(IReadOnlyList<RegressionEvaluator> evaluators, IEnumerable<double> predictions, IEnumerable<double> targets, double lowerPredictionLimit = double.MinValue, double upperPredictionLimit = double.MaxValue)
  {
    predictions = predictions.LimitToRange(lowerPredictionLimit, upperPredictionLimit);
    if (evaluators.Count == 1) {
      return new ObjectiveVector(evaluators[0].Evaluate(predictions, targets));
    }
    if (predictions is not ICollection<double> materialPredictions) {
      materialPredictions = predictions.ToArray();
    }

    return new ObjectiveVector(evaluators.Select(x => x.Evaluate(materialPredictions, targets)));
  }

  public static ObjectiveVector Evaluate(this RegressionProblemData data, IRegressionModel solution, IReadOnlyList<int> rows, IReadOnlyList<RegressionEvaluator> evaluators, double lowerPredictionLimit = double.MinValue, double upperPredictionLimit = double.MaxValue)
  {
    var targets = data.Dataset.GetDoubleValues(data.TargetVariable, rows);
    var predictions = solution.Predict(data.Dataset, rows);

    return Evaluate(evaluators, predictions, targets, lowerPredictionLimit, upperPredictionLimit);
  }
}
