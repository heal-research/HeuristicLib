using HEAL.HeuristicLib.Encodings.LegacySymbolicExpressions;

namespace HEAL.HeuristicLib.Problems.MachineLearning.Legacy;

public readonly struct SymbolicRegressionModel(SymbolicExpressionTree tree, ISymbolicDataAnalysisExpressionTreeInterpreter interpreter) : IRegressionModel
{
    public IEnumerable<double> Predict(Dataset data, IEnumerable<int> rows) => interpreter.GetSymbolicExpressionTreeValues(tree, data, rows);
}
