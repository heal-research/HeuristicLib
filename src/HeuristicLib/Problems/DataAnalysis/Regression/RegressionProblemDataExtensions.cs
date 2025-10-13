using HEAL.HeuristicLib.Optimization;

namespace HEAL.HeuristicLib.Problems.DataAnalysis.Regression;

public static class RegressionProblemDataExtensions {
  public static ObjectiveVector Evaluate(IRegressionModel solution, Dataset dataset, IEnumerable<int> rows, IReadOnlyList<IRegressionEvaluator> evaluators, double[] targets) {
    var predictions = solution.Predict(dataset, rows);
    if (evaluators.Count == 1)
      return new(evaluators[0].Evaluate(targets, predictions));

    if (predictions is not ICollection<double> materialPredictions)
      materialPredictions = predictions.ToArray();
    return new(evaluators.Select(x => x.Evaluate(targets, materialPredictions)));
  }

  public static ObjectiveVector Evaluate(this RegressionProblemData data, IRegressionModel solution, DataAnalysisProblemData.PartitionType type, IReadOnlyList<IRegressionEvaluator> evaluators) {
    var targets = data.TargetVariableValues(type).ToArray();
    var rows = data.Partitions[type].Enumerate();
    return Evaluate(solution, data.Dataset, rows, evaluators, targets);
  }
}
