using HEAL.HeuristicLib.Encodings.SymbolicExpression;
using HEAL.HeuristicLib.Problems.DataAnalysis.Symbolic;

namespace HEAL.HeuristicLib.Problems.DataAnalysis.Regression;

public record SymbolicRegressionModel(SymbolicExpressionTree tree, ISymbolicDataAnalysisExpressionTreeInterpreter interpreter) : IRegressionModel {
  public virtual IEnumerable<double> Predict(Dataset data, IEnumerable<int> rows) => interpreter.GetSymbolicExpressionTreeValues(tree, data, rows);
}
