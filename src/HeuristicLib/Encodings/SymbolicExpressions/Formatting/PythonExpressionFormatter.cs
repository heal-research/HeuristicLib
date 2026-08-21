namespace HEAL.HeuristicLib.Encodings.SymbolicExpressions;

public sealed class PythonExpressionFormatter : ExpressionFormatter
{
    private static readonly HashSet<string> ReservedWords =
    [
        "False", "None", "True", "and", "as", "assert", "async", "await", "break", "class", "continue",
        "def", "del", "elif", "else", "except", "finally", "for", "from", "global", "if", "import", "in",
        "is", "lambda", "nonlocal", "not", "or", "pass", "raise", "return", "try", "while", "with", "yield"
    ];

    protected override string FormatVariable(string variableName) => SanitizeIdentifier(variableName, ReservedWords);

    protected override string FormatConstant(double value)
    {
        if (double.IsNaN(value))
            return "math.nan";
        if (double.IsPositiveInfinity(value))
            return "math.inf";
        if (double.IsNegativeInfinity(value))
            return "-math.inf";

        return FormatNumber(value);
    }

    protected override string FormatTerminal(string name) => SanitizeIdentifier(name, ReservedWords);

    protected override string FormatOperation(OperationSymbol symbol, IReadOnlyList<string> children) =>
        symbol switch
        {
            AdditionSymbol => FormatInfix(children, "+"),
            SubtractionSymbol => FormatInfix(children, "-"),
            MultiplicationSymbol => FormatInfix(children, "*"),
            DivisionSymbol => FormatInfix(children, "/"),
            NegationSymbol => $"(-{children[0]})",
            ExponentialSymbol => FormatFunction(children, "math.exp"),
            SineSymbol => FormatFunction(children, "math.sin"),
            CosineSymbol => FormatFunction(children, "math.cos"),
            TangentSymbol => FormatFunction(children, "math.tan"),
            HyperbolicTangentSymbol => FormatFunction(children, "math.tanh"),
            LogarithmSymbol => FormatFunction(children, "math.log"),
            SquareRootSymbol => FormatFunction(children, "math.sqrt"),
            AbsoluteSymbol => FormatFunction(children, "abs"),
            SquareSymbol => $"({children[0]} ** 2.0)",
            CubeSymbol => $"({children[0]} ** 3.0)",
            CubeRootSymbol => FormatFunction(children, "math.cbrt"),
            PowerSymbol => FormatFunction(children, "math.pow"),
            RootSymbol => $"math.pow({children[0]}, 1.0 / {children[1]})",
            AnalyticQuotientSymbol => FormatAnalyticQuotient(children),
            _ => FormatFunction(children, SanitizeIdentifier(symbol.Name, ReservedWords))
        };

    private static string FormatAnalyticQuotient(IReadOnlyList<string> children) =>
        $"({children[0]} / math.sqrt(1.0 + ({children[1]} * {children[1]})))";
}

public static class PythonExpressionFormattingExtensions
{
    extension(ExpressionTree expression)
    {
        public string ToPythonString() => ExpressionFormatters.Python.Format(expression);
    }
}
