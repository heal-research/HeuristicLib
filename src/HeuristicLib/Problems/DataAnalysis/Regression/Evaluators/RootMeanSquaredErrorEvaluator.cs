using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems.DataAnalysis.OnlineCalculators;

namespace HEAL.HeuristicLib.Problems.DataAnalysis.Regression.Evaluators;

public class RootMeanSquaredErrorEvaluator : IRegressionEvaluator {
  public ObjectiveDirection Direction => ObjectiveDirection.Minimize;

  public double Evaluate(IEnumerable<double> trueValues, IEnumerable<double> predictedValues) {
    var r = OnlineMeanSquaredErrorCalculator.Calculate(trueValues, predictedValues, out var state);
    if (state != OnlineCalculatorError.None)
      throw new InvalidOperationException("can not calculate RMSE");
    return Math.Sqrt(r);
  }
}
