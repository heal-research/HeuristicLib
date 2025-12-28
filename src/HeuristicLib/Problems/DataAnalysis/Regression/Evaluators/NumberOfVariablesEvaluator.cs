using HEAL.HeuristicLib.Encodings.Trees.SymbolicExpressionTree.Symbols.Math;
using HEAL.HeuristicLib.Genotypes.Trees;
using HEAL.HeuristicLib.Optimization;

namespace HEAL.HeuristicLib.Problems.DataAnalysis.Regression.Evaluators;

public class NumberOfVariablesEvaluator : IRegressionEvaluator<SymbolicExpressionTree> {
  public ObjectiveDirection Direction => ObjectiveDirection.Minimize;

  public double Evaluate(SymbolicExpressionTree solution, IEnumerable<double> predictedValues, IEnumerable<double> trueValues) {
    return solution.IterateNodesPostfix().OfType<VariableTreeNode>().Count();
  }
}
