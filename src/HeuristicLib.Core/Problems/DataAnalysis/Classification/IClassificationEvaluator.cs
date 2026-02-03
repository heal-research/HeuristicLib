using HEAL.HeuristicLib.Optimization;

namespace HEAL.HeuristicLib.Problems.DataAnalysis.Classification;

public interface IClassificationEvaluator
{
  ObjectiveDirection Direction { get; }
  double Evaluate(IEnumerable<double> trueValues, IEnumerable<double> predictedValues);
}
