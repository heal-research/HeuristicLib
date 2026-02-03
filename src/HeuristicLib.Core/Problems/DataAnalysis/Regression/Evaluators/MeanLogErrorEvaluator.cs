using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems.DataAnalysis.OnlineCalculators;

namespace HEAL.HeuristicLib.Problems.DataAnalysis.Regression.Evaluators;

public class MeanLogErrorEvaluator : RegressionEvaluator
{
  public override ObjectiveDirection Direction => ObjectiveDirection.Minimize;

  public override double Evaluate(IEnumerable<double> predictedValues, IEnumerable<double> trueValues)
  {
    var t = predictedValues.Zip(trueValues, resultSelector: (e, t) => Math.Log(1.0 + Math.Abs(e - t)));
    OnlineMeanAndVarianceCalculator.Calculate(t, out var mean, out _, out var state, out _);

    return state != OnlineCalculatorError.None ? throw new InvalidOperationException("can not calculate Mean Log Error") : mean;
  }
}
