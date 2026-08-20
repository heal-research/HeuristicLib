using HEAL.HeuristicLib.Genotypes.SymbolicExpressions;

namespace HEAL.HeuristicLib.DataAnalysis.Regression;

public sealed class SymbolicRegressor : IRegressor
{
    public SymbolicRegressor(ExpressionTree expression, string predictionName = "prediction")
    {
        if (string.IsNullOrWhiteSpace(predictionName))
            throw new ArgumentException("Prediction name must not be empty.", nameof(predictionName));

        Expression = expression;
        CompiledExpression = expression.Compile();
        PredictionName = predictionName;
    }

    public ExpressionTree Expression { get; }
    public CompiledExpression CompiledExpression { get; }
    public string PredictionName { get; }

    public Series<double> Predict(DataFrame inputs)
    {
        var values = new double[inputs.RowCount];
        Predict(inputs, values);
        return Series<double>.FromOwnedArray(PredictionName, values);
    }

    public void Predict(DataFrame inputs, Span<double> destination)
    {
        if (destination.Length != inputs.RowCount)
            throw new ArgumentException($"Destination must contain exactly {inputs.RowCount} values but contains {destination.Length}.", nameof(destination));

        ExpressionInterpreter.Interpret(CompiledExpression, inputs, destination);
    }
}

public static class SymbolicRegressorExtensions
{
    extension(ExpressionTree expression)
    {
        public SymbolicRegressor ToRegressor(string predictionName = "prediction")
        {
            return new SymbolicRegressor(expression, predictionName);
        }
    }
}
