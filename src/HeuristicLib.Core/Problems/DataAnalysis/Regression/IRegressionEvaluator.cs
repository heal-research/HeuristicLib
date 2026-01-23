using HEAL.HeuristicLib.Optimization;

namespace HEAL.HeuristicLib.Problems.DataAnalysis.Regression;

public interface IRegressionEvaluator<in TGenotype>
{
  ObjectiveDirection Direction { get; }

  double Evaluate(TGenotype solution, IEnumerable<double> predictedValues, IEnumerable<double> trueValues);
  //double Evaluate(ReadOnlySpan<double> targets, ReadOnlySpan<double> predictions);
}
