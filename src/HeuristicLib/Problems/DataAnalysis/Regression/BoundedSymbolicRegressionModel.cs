using HEAL.HeuristicLib.Encodings.SymbolicExpression;
using HEAL.HeuristicLib.Problems.DataAnalysis.Symbolic;

namespace HEAL.HeuristicLib.Problems.DataAnalysis.Regression;

public record BoundedSymbolicRegressionModel(SymbolicExpressionTree tree, ISymbolicDataAnalysisExpressionTreeInterpreter interpreter, double lowerPredictionBound, double upperPredictionBound) :
  SymbolicRegressionModel(tree, interpreter) {
  public virtual IEnumerable<double> Predict(Dataset data, IEnumerable<int> rows) {
    return interpreter.GetSymbolicExpressionTreeValues(tree, data, rows).Select(v => Math.Min(upperPredictionBound, Math.Max(lowerPredictionBound, v)));
  }
}
