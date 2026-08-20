using HEAL.HeuristicLib.DataAnalysis;

namespace HEAL.HeuristicLib.Genotypes.SymbolicExpressions;

public static class ExpressionEvaluationExtensions
{
    /// <remarks>
    /// Each of these compiles the tree and then evaluates it, so evaluating the same tree repeatedly compiles it
    /// repeatedly. Call <see cref="ExpressionTree.Compile"/> once and use the <see cref="CompiledExpression"/>
    /// overloads below when the same expression is evaluated more than once.
    /// </remarks>
    extension(ExpressionTree expression)
    {
        public double[] Evaluate(DataFrame data) =>
            expression.Compile().Evaluate(data);

        public void Evaluate(DataFrame data, Span<double> destination) =>
            expression.Compile().Evaluate(data, destination);

        public void Evaluate(DataFrame data, Span<double> destination, Span<double> workspace) =>
            expression.Compile().Evaluate(data, destination, workspace);

        public double EvaluateSingleRow(IReadOnlyDictionary<string, double> variableValues) =>
            expression.Compile().EvaluateSingleRow(variableValues);

        public double EvaluateSingleRow(params (string Name, double Value)[] variableValues) =>
            expression.Compile().EvaluateSingleRow(variableValues);
    }

    /// <remarks>
    /// Compiling once and evaluating the compiled expression many times is the efficient path. These carry the same
    /// shapes as the <see cref="ExpressionTree"/> overloads so that switching to it changes nothing but the receiver.
    /// </remarks>
    extension(CompiledExpression expression)
    {
        public double[] Evaluate(DataFrame data) =>
            ExpressionInterpreter.Interpret(expression, data);

        public void Evaluate(DataFrame data, Span<double> destination) =>
            ExpressionInterpreter.Interpret(expression, data, destination);

        public void Evaluate(DataFrame data, Span<double> destination, Span<double> workspace) =>
            ExpressionInterpreter.Interpret(expression, data, destination, workspace);

        public double EvaluateSingleRow(IReadOnlyDictionary<string, double> variableValues) =>
            expression.Evaluate(CreateSingleRowDataFrame(variableValues))[0];

        public double EvaluateSingleRow(params (string Name, double Value)[] variableValues) =>
            expression.Evaluate(CreateSingleRowDataFrame(variableValues))[0];
    }

    private static DataFrame CreateSingleRowDataFrame(IReadOnlyDictionary<string, double> variableValues)
    {
        if (variableValues.Count == 0)
            return new DataFrame([Series<double>.FromOwnedArray("__row", [0.0])]);

        return new DataFrame(variableValues.Select(
            variable => Series<double>.FromOwnedArray(variable.Key, [variable.Value])));
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
