using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems.DataAnalysis.OnlineCalculators;

namespace HEAL.HeuristicLib.Problems.DataAnalysis.Regression.Evaluators;

public class RootMeanSquaredErrorEvaluator : RegressionEvaluator
{
  public override ObjectiveDirection Direction => ObjectiveDirection.Minimize;

  public override double Evaluate(IEnumerable<double> predictedValues, IEnumerable<double> trueValues)
  {
    var r = OnlineMeanSquaredErrorCalculator.Calculate(trueValues, predictedValues, out var state);
    if (state != OnlineCalculatorError.None) {
      throw new InvalidOperationException("can not calculate RMSE");
    }

    return Math.Sqrt(r);
  }
}
