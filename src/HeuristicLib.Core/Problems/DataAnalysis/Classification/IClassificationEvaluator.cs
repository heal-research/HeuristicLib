using HEAL.HeuristicLib.Optimization;

namespace HEAL.HeuristicLib.Problems.DataAnalysis.Classification;

public interface IClassificationEvaluator {
  public ObjectiveDirection Direction { get; }
  public double Evaluate(IEnumerable<double> trueValues, IEnumerable<double> predictedValues);
}
