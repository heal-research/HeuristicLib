using HEAL.HeuristicLib.Encodings.SymbolicExpressionTree;
using HEAL.HeuristicLib.Optimization;

namespace HEAL.HeuristicLib.Problems.DataAnalysis.Regression.Evaluators;

public class TreeComplexityEvaluator : IRegressionEvaluator<SymbolicExpressionTree> {
  public ObjectiveDirection Direction => ObjectiveDirection.Minimize;

  public double Evaluate(SymbolicExpressionTree solution, IEnumerable<double> predictedValues, IEnumerable<double> trueValues) {
    return TreeComplexityCalculator.CalculateComplexity(solution);
  }
}
