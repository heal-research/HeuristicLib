using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems.DataAnalysis.OnlineCalculators;

namespace HEAL.HeuristicLib.Problems.DataAnalysis.Regression.Evaluators;

public class NormalizedMeanSquaredErrorEvaluator : RegressionEvaluator {
  public override ObjectiveDirection Direction => ObjectiveDirection.Minimize;

  public override double Evaluate(IEnumerable<double> predictedValues, IEnumerable<double> trueValues) {
    var r = OnlineNormalizedMeanSquaredErrorCalculator.Calculate(trueValues, predictedValues, out var state);
    if (state != OnlineCalculatorError.None) throw new InvalidOperationException("can not calculate Normalized Mean Squared Error");
    return r;
  }
}
