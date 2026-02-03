using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems.DataAnalysis.OnlineCalculators;

namespace HEAL.HeuristicLib.Problems.DataAnalysis.Classification.Evaluators;

public class NormalizedGiniEvaluator : IClassificationEvaluator
{
  public ObjectiveDirection Direction => ObjectiveDirection.Minimize;

  public double Evaluate(IEnumerable<double> trueValues, IEnumerable<double> predictedValues)
  {
    var r = NormalizedGiniCalculator.Calculate(trueValues, predictedValues, out var state);
    if (state != OnlineCalculatorError.None) {
      throw new InvalidOperationException("can not calculate Gini coefficient");
    }

    return r;
  }
}
