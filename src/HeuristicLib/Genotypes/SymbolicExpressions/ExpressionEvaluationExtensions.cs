namespace HEAL.HeuristicLib.Genotypes.SymbolicExpressions;

public static class ExpressionEvaluationExtensions
{
    extension(ExpressionTree expression)
    {
        public double[] Evaluate(DataFrame data) =>
            ExpressionInterpreter.Interpret(expression.Compile(), data);

        public void Evaluate(DataFrame data, Span<double> destination) =>
            ExpressionInterpreter.Interpret(expression.Compile(), data, destination);

        public void Evaluate(DataFrame data, Span<double> destination, Span<double> workspace) =>
            ExpressionInterpreter.Interpret(expression.Compile(), data, destination, workspace);

        public double EvaluateSingleRow(IReadOnlyDictionary<string, double> variableValues) =>
            expression.Evaluate(CreateSingleRowDataFrame(variableValues))[0];

        public double EvaluateSingleRow(params (string Name, double Value)[] variableValues) =>
            expression.Evaluate(CreateSingleRowDataFrame(variableValues))[0];
    }

    private static DataFrame CreateSingleRowDataFrame(IReadOnlyDictionary<string, double> variableValues)
    {
        if (variableValues.Count == 0)
            return DataFrame.FromOwnedColumns([KeyValuePair.Create("__row", new[] { 0.0 })]);

        return DataFrame.FromOwnedColumns(variableValues.Select(variable => KeyValuePair.Create(variable.Key, new[] { variable.Value })));
    }

    private static DataFrame CreateSingleRowDataFrame((string Name, double Value)[] variableValues)
    {
        var valueByName = new Dictionary<string, double>(variableValues.Length, StringComparer.Ordinal);
        foreach (var variableValue in variableValues)
        {
            if (string.IsNullOrWhiteSpace(variableValue.Name))
                throw new ArgumentException("Variable names must not be empty.", nameof(variableValues));
            if (!valueByName.TryAdd(variableValue.Name, variableValue.Value))
                throw new ArgumentException($"Duplicate value for variable '{variableValue.Name}'.", nameof(variableValues));
        }

        return CreateSingleRowDataFrame(valueByName);
    }
}
