using HEAL.HeuristicLib.Genotypes.Trees;
using HEAL.HeuristicLib.Problems.DataAnalysis.Symbolic;

namespace HEAL.HeuristicLib.Problems.DataAnalysis.Regression;

public readonly struct BoundedSymbolicRegressionModel(SymbolicExpressionTree tree, ISymbolicDataAnalysisExpressionTreeInterpreter interpreter, double lowerPredictionBound, double upperPredictionBound) :
  IRegressionModel
{
  public IEnumerable<double> Predict(Dataset data, IEnumerable<int> rows)
  {
    var u = upperPredictionBound;
    var l = lowerPredictionBound;
    return interpreter.GetSymbolicExpressionTreeValues(tree, data, rows).Select(v => Math.Min(u, Math.Max(l, v)));
  }
}
