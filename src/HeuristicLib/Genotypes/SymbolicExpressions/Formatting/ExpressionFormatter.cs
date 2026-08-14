using System.Globalization;
using System.Text;

namespace HEAL.HeuristicLib.Genotypes.SymbolicExpressions;

public interface IExpressionFormatter
{
    string Format(ExpressionTree expression);
}

public abstract class ExpressionFormatter : IExpressionFormatter
{
    public string Format(ExpressionTree expression) =>
        Format(expression.Root, CreateVariableFormats(
            expression.TraversePreOrder().OfType<VariableExpressionNode>().Select(node => node.VariableName)));

    protected virtual string FormatVariable(string variableName) => variableName;

    protected virtual string FormatConstant(double value) => FormatNumber(value);

    protected virtual string FormatConstant(NumericConstantExpressionNode constant) => FormatConstant(constant.Value);

    protected virtual string FormatTerminal(string name) => name;

    protected abstract string FormatOperation(OperationSymbol symbol, IReadOnlyList<string> children);

    protected static string FormatFunction(IReadOnlyList<string> children, string functionName) =>
        $"{functionName}({string.Join(", ", children)})";

    protected static string FormatInfix(IReadOnlyList<string> children, string operation) =>
        $"({children[0]} {operation} {children[1]})";

    protected static string FormatNumber(double value) => value.ToString("G17", CultureInfo.InvariantCulture);

    protected static string FormatRoundTripNumber(double value) => value.ToString("R", CultureInfo.InvariantCulture);

    protected static string SanitizeIdentifier(string value, ISet<string>? reservedWords = null)
    {
        var builder = new StringBuilder(value.Length + 1);
        foreach (var character in value)
            builder.Append(char.IsLetterOrDigit(character) || character == '_' ? character : '_');

        if (builder.Length == 0 || char.IsDigit(builder[0]))
            builder.Insert(0, '_');

        var identifier = builder.ToString();
        return reservedWords?.Contains(identifier) == true ? $"_{identifier}" : identifier;
    }

    private string Format(ExpressionNode node, IReadOnlyDictionary<string, string> variableFormats)
    {
        return node switch
        {
            VariableExpressionNode variable => variableFormats[variable.VariableName],
            NumericConstantExpressionNode constant => FormatConstant(constant),
            OperationExpressionNode operation => FormatOperation(operation.Symbol, FormatChildren(operation, variableFormats)),
            TerminalExpressionNode terminal => FormatTerminal(terminal.Symbol.Name),
            _ => throw new NotSupportedException($"Formatting is not supported for node type '{node.GetType().Name}'.")
        };
    }

    private string[] FormatChildren(
        OperationExpressionNode operation,
        IReadOnlyDictionary<string, string> variableFormats)
    {
        var children = new string[operation.Arity];
        for (var i = 0; i < children.Length; i++)
            children[i] = Format(operation.GetChild(i), variableFormats);

        return children;
    }

    private Dictionary<string, string> CreateVariableFormats(IEnumerable<string> variableNames)
    {
        var formats = new Dictionary<string, string>(StringComparer.Ordinal);
        var usedFormats = new HashSet<string>(StringComparer.Ordinal);
        foreach (var variableName in variableNames.Distinct(StringComparer.Ordinal))
        {
            var baseFormat = FormatVariable(variableName);
            var format = baseFormat;
            for (var suffix = 2; !usedFormats.Add(format); suffix++)
                format = $"{baseFormat}_{suffix}";

            formats.Add(variableName, format);
        }

        return formats;
    }
}

public static class ExpressionFormatters
{
    public static InfixExpressionFormatter Infix { get; } = new();
    public static IExpressionFormatter CSharp { get; } = new CSharpExpressionFormatter();
    public static IExpressionFormatter Python { get; } = new PythonExpressionFormatter();
    public static IExpressionFormatter Latex { get; } = new LatexExpressionFormatter();
}
