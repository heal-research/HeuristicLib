using HEAL.HeuristicLib.Encodings.SymbolicExpression;
using HEAL.HeuristicLib.Encodings.SymbolicExpression.Symbols.Math;
using HEAL.HeuristicLib.Optimization;

namespace HEAL.HeuristicLib.Problems.DataAnalysis.Regression.Evaluators;

public class NumberOfVariablesEvaluator : IRegressionEvaluator<SymbolicExpressionTree> {
  public ObjectiveDirection Direction => ObjectiveDirection.Minimize;

  public double Evaluate(SymbolicExpressionTree solution, IEnumerable<double> predictedValues, IEnumerable<double> trueValues) {
    return solution.IterateNodesPostfix().OfType<VariableTreeNode>().Count();
  }
}
