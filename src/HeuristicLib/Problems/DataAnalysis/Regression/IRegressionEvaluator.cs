using HEAL.HeuristicLib.Optimization;

namespace HEAL.HeuristicLib.Problems.DataAnalysis.Regression;

public interface IRegressionEvaluator {
  ObjectiveDirection Direction { get; }

  double Evaluate(IEnumerable<double> trueValues, IEnumerable<double> predictedValues);
  //double Evaluate(ReadOnlySpan<double> targets, ReadOnlySpan<double> predictions);
}
