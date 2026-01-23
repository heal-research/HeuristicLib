using HEAL.HeuristicLib.Optimization;

namespace HEAL.HeuristicLib.Problems.DataAnalysis.Regression;

public abstract class RegressionEvaluator : IRegressionEvaluator<object>
{
  public abstract double Evaluate(IEnumerable<double> predictedValues, IEnumerable<double> trueValues);
  public abstract ObjectiveDirection Direction { get; }
  public double Evaluate(object solution, IEnumerable<double> predictedValues, IEnumerable<double> trueValues) => Evaluate(predictedValues, trueValues);
}
