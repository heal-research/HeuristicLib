using HEAL.HeuristicLib.Encodings.LegacySymbolicExpressions;

namespace HEAL.HeuristicLib.Problems.MachineLearning.Legacy;

public interface IRegressionModel
{
    IEnumerable<double> Predict(Dataset data, IEnumerable<int> rows);

    double Predict(Dataset data, int row) => Predict(data, [row]).First();
}

public readonly struct SymbolicRegressionModel(SymbolicExpressionTree tree, ISymbolicDataAnalysisExpressionTreeInterpreter interpreter) : IRegressionModel
{
    public IEnumerable<double> Predict(Dataset data, IEnumerable<int> rows) => interpreter.GetSymbolicExpressionTreeValues(tree, data, rows);
}
