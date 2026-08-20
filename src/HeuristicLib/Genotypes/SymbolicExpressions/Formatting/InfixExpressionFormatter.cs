namespace HEAL.HeuristicLib.Genotypes.SymbolicExpressions;

public enum InfixConstantNotation
{
    /// <summary>Formats every numeric constant as an unmarked numeric literal.</summary>
    NumericLiterals,

    /// <summary>Marks evolvable constants with <c>param(...)</c>.</summary>
    MarkParameters,

    /// <summary>Marks fixed constants with <c>fixed(...)</c>.</summary>
    MarkFixedConstants,

    /// <summary>Marks evolvable and fixed constants explicitly.</summary>
    MarkAll
}

public sealed class InfixExpressionFormatter : ExpressionFormatter
{
    public InfixExpressionFormatter(InfixConstantNotation constantNotation = InfixConstantNotation.NumericLiterals)
    {
        if (constantNotation < InfixConstantNotation.NumericLiterals || constantNotation > InfixConstantNotation.MarkAll)
            throw new ArgumentOutOfRangeException(nameof(constantNotation));

        ConstantNotation = constantNotation;
    }

    public InfixConstantNotation ConstantNotation { get; }

    public string Format(ExpressionTree expression, InfixConstantNotation constantNotation) =>
        constantNotation == ConstantNotation
            ? Format(expression)
            : new InfixExpressionFormatter(constantNotation).Format(expression);

    protected override string FormatConstant(NumericConstantExpressionNode constant)
    {
        var value = FormatNumber(constant.Value);
        return (ConstantNotation, constant.Symbol) switch
        {
            (InfixConstantNotation.MarkParameters, EvolvableConstantSymbol) => $"param({value})",
            (InfixConstantNotation.MarkFixedConstants, FixedConstantSymbol) => $"fixed({value})",
            (InfixConstantNotation.MarkAll, EvolvableConstantSymbol) => $"param({value})",
            (InfixConstantNotation.MarkAll, FixedConstantSymbol) => $"fixed({value})",
            _ => value
        };
    }

    protected override string FormatVariable(string variableName)
    {
        if (IsIdentifier(variableName) &&
            !variableName.Equals("nan", StringComparison.OrdinalIgnoreCase) &&
            !variableName.Equals("infinity", StringComparison.OrdinalIgnoreCase))
        {
            return variableName;
        }

        return QuoteIdentifier(variableName);
    }

    protected override string FormatOperation(OperationSymbol symbol, IReadOnlyList<string> children) =>
        symbol switch
        {
            AdditionSymbol => FormatInfix(children, "+"),
            SubtractionSymbol => FormatInfix(children, "-"),
            MultiplicationSymbol => FormatInfix(children, "*"),
            DivisionSymbol => FormatInfix(children, "/"),
            _ => FormatFunction(children, FormatOperationName(symbol.Name))
        };

    private static string FormatOperationName(string name) =>
        IsIdentifier(name) &&
        !name.Equals("fixed", StringComparison.OrdinalIgnoreCase) &&
        !name.Equals("param", StringComparison.OrdinalIgnoreCase)
            ? name
            : QuoteIdentifier(name);

    private static string QuoteIdentifier(string name) => $"`{name.Replace("`", "``", StringComparison.Ordinal)}`";

    private static bool IsIdentifier(string value)
    {
        if (value.Length == 0 || !(char.IsLetter(value[0]) || value[0] == '_'))
            return false;

        for (var i = 1; i < value.Length; i++)
        {
            if (!(char.IsLetterOrDigit(value[i]) || value[i] == '_'))
                return false;
        }

        return true;
    }
}

public static class InfixExpressionFormattingExtensions
{
    extension(ExpressionTree expression)
    {
        public string ToInfixString() => ExpressionFormatters.Infix.Format(expression);

        public string ToInfixString(InfixConstantNotation constantNotation) =>
            ExpressionFormatters.Infix.Format(expression, constantNotation);
    }
}
