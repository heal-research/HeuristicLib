using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems.DataAnalysis.OnlineCalculators;

namespace HEAL.HeuristicLib.Problems.DataAnalysis.Regression.Evaluators;

public class MeanRelativeErrorEvaluator : RegressionEvaluator {
  public override ObjectiveDirection Direction => ObjectiveDirection.Minimize;

  public override double Evaluate(IEnumerable<double> predictedValues, IEnumerable<double> trueValues) {
    var t = predictedValues.Zip(trueValues, (e, t) => Math.Abs(t - e) / (Math.Abs(t) + 1.0));
    OnlineMeanAndVarianceCalculator.Calculate(t, out var mean, out var var, out var state, out var state2);
    if (state != OnlineCalculatorError.None) throw new InvalidOperationException("can not calculate Mean Relative Error");
    return mean;
  }
}
