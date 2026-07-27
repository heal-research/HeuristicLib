namespace HEAL.HeuristicLib.Genotypes.SymbolicExpressions;

public sealed class CSharpExpressionFormatter : ExpressionFormatter
{
    private static readonly HashSet<string> ReservedWords =
    [
        "abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", "checked", "class",
        "const", "continue", "decimal", "default", "delegate", "do", "double", "else", "enum", "event",
        "explicit", "extern", "false", "finally", "fixed", "float", "for", "foreach", "goto", "if",
        "implicit", "in", "int", "interface", "internal", "is", "lock", "long", "namespace", "new", "null",
        "object", "operator", "out", "override", "params", "private", "protected", "public", "readonly", "ref",
        "return", "sbyte", "sealed", "short", "sizeof", "stackalloc", "static", "string", "struct", "switch",
        "this", "throw", "true", "try", "typeof", "uint", "ulong", "unchecked", "unsafe", "ushort", "using",
        "virtual", "void", "volatile", "while"
    ];

    protected override string FormatVariable(string variableName) => SanitizeIdentifier(variableName, ReservedWords);

    protected override string FormatConstant(double value)
    {
        if (double.IsNaN(value))
            return "double.NaN";
        if (double.IsPositiveInfinity(value))
            return "double.PositiveInfinity";
        if (double.IsNegativeInfinity(value))
            return "double.NegativeInfinity";

        return FormatRoundTripNumber(value);
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
            ExponentialSymbol => FormatFunction(children, "Math.Exp"),
            SineSymbol => FormatFunction(children, "Math.Sin"),
            CosineSymbol => FormatFunction(children, "Math.Cos"),
            TangentSymbol => FormatFunction(children, "Math.Tan"),
            HyperbolicTangentSymbol => FormatFunction(children, "Math.Tanh"),
            LogarithmSymbol => FormatFunction(children, "Math.Log"),
            SquareRootSymbol => FormatFunction(children, "Math.Sqrt"),
            AbsoluteSymbol => FormatFunction(children, "Math.Abs"),
            SquareSymbol => $"Math.Pow({children[0]}, 2.0)",
            CubeSymbol => $"Math.Pow({children[0]}, 3.0)",
            CubeRootSymbol => FormatFunction(children, "Math.Cbrt"),
            PowerSymbol => FormatFunction(children, "Math.Pow"),
            RootSymbol => $"Math.Pow({children[0]}, 1.0 / {children[1]})",
            AnalyticQuotientSymbol => FormatAnalyticQuotient(children),
            _ => FormatFunction(children, SanitizeIdentifier(symbol.Name, ReservedWords))
        };

    private static string FormatAnalyticQuotient(IReadOnlyList<string> children) =>
        $"({children[0]} / Math.Sqrt(1.0 + ({children[1]} * {children[1]})))";
}

public static class CSharpExpressionFormattingExtensions
{
    extension(ExpressionTree expression)
    {
        public string ToCSharpString() => ExpressionFormatters.CSharp.Format(expression);
    }
}
