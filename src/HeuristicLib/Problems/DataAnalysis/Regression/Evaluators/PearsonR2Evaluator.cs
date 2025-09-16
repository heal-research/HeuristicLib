using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems.DataAnalysis.OnlineCalculators;

namespace HEAL.HeuristicLib.Problems.DataAnalysis.Regression.Evaluators;

public class PearsonR2Evaluator : IRegressionEvaluator {
  public ObjectiveDirection Direction => ObjectiveDirection.Maximize;

  public double Evaluate(IEnumerable<double> trueValues, IEnumerable<double> predictedValues) {
    return EvaluateStatic(trueValues, predictedValues);
  }

  public static double EvaluateStatic(IEnumerable<double> trueValues, IEnumerable<double> predictedValues) {
    var r = OnlinePearsonsRCalculator.Calculate(trueValues, predictedValues, out var state);
    if (state != OnlineCalculatorError.None) throw new InvalidOperationException("can not calculate Pearson R2");
    return r * r;
  }
}
