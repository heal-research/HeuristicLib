using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems.DataAnalysis.OnlineCalculators;

namespace HEAL.HeuristicLib.Problems.DataAnalysis.Regression.Evaluators;

public class PearsonR2Evaluator : RegressionEvaluator {
  public override ObjectiveDirection Direction => ObjectiveDirection.Maximize;

  public override double Evaluate(IEnumerable<double> predictedValues, IEnumerable<double> trueValues) {
    var p = predictedValues.ToArray();
    var t = trueValues.ToArray();

    var r = OnlinePearsonsRCalculator.Calculate(p, t, out var state);
    if (state != OnlineCalculatorError.None) throw new InvalidOperationException("can not calculate Pearson R2");
    return r * r;
  }
}
