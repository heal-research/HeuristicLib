namespace HEAL.HeuristicLib.Genotypes.SymbolicExpressions;

public static class SymbolicExpressionEvaluationExtensions
{
    extension(SymbolicExpression expression)
    {
        public double Evaluate(ReadOnlySpan<double> variableValues) =>
          SymbolicExpressionInterpreter.Interpret(expression, variableValues);

        public double Evaluate(
          IReadOnlyList<string> variableNames,
          IReadOnlyList<double> variableValues) =>
          SymbolicExpressionInterpreter.Interpret(expression, variableNames, variableValues);

        public double Evaluate(IReadOnlyDictionary<string, double> variableValues) =>
          SymbolicExpressionInterpreter.Interpret(expression, variableValues);

        public double[] Evaluate(DataFrame data) =>
          SymbolicExpressionInterpreter.Interpret(expression, data);

        public void Evaluate(DataFrame data, Span<double> destination) =>
          SymbolicExpressionInterpreter.Interpret(expression, data, destination);

        public void Evaluate(DataFrame data, Span<double> destination, Span<double> workspace) =>
          SymbolicExpressionInterpreter.Interpret(expression, data, destination, workspace);
    }
}
