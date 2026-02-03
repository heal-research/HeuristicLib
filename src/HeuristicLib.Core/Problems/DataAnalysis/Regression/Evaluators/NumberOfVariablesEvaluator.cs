using HEAL.HeuristicLib.Encodings.SymbolicExpressionTree;
using HEAL.HeuristicLib.Encodings.SymbolicExpressionTree.Symbols.Math;
using HEAL.HeuristicLib.Optimization;

namespace HEAL.HeuristicLib.Problems.DataAnalysis.Regression.Evaluators;

public class NumberOfVariablesEvaluator : IRegressionEvaluator<SymbolicExpressionTree> {
  public ObjectiveDirection Direction => ObjectiveDirection.Minimize;

  public double Evaluate(SymbolicExpressionTree solution, IEnumerable<double> predictedValues, IEnumerable<double> trueValues) {
    return solution.IterateNodesPostfix().OfType<VariableTreeNode>().Count();
  }
}
