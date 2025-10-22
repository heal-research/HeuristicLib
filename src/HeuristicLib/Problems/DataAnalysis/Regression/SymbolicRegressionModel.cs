using HEAL.HeuristicLib.Encodings.SymbolicExpression;
using HEAL.HeuristicLib.Problems.DataAnalysis.Symbolic;

namespace HEAL.HeuristicLib.Problems.DataAnalysis.Regression;

public readonly struct SymbolicRegressionModel(SymbolicExpressionTree tree, ISymbolicDataAnalysisExpressionTreeInterpreter interpreter) : IRegressionModel {
  public SymbolicExpressionTree Tree => tree;
  public IEnumerable<double> Predict(Dataset data, IEnumerable<int> rows) => interpreter.GetSymbolicExpressionTreeValues(tree, data, rows);
}
