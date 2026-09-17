using System.Globalization;
using System.Text;

namespace HEAL.HeuristicLib.Encodings.SymbolicExpressions;

public sealed class LatexExpressionFormatter : ExpressionFormatter
{
    protected override string FormatVariable(string variableName) => $"\\mathrm{{{Escape(variableName)}}}";

    protected override string FormatConstant(double value)
    {
        if (double.IsNaN(value))
            return "\\mathrm{NaN}";
        if (double.IsPositiveInfinity(value))
            return "\\infty";
        if (double.IsNegativeInfinity(value))
            return "-\\infty";

        var formatted = FormatRoundTripNumber(value);
        var exponentSeparator = formatted.IndexOf('E');
        if (exponentSeparator < 0)
            return formatted;

        var mantissa = formatted[..exponentSeparator];
        var exponent = int.Parse(formatted[(exponentSeparator + 1)..], CultureInfo.InvariantCulture);
        return $"{mantissa} \\times 10^{{{exponent}}}";
    }

    protected override string FormatTerminal(string name) => $"\\operatorname{{{Escape(name)}}}";

    protected override string FormatOperation(OperationSymbol symbol, IReadOnlyList<string> children) =>
        symbol switch
        {
            AdditionSymbol => FormatInfix(children, "+"),
            SubtractionSymbol => FormatInfix(children, "-"),
            MultiplicationSymbol => FormatInfix(children, "\\cdot"),
            DivisionSymbol => $"\\frac{{{children[0]}}}{{{children[1]}}}",
            NegationSymbol => $"-{children[0]}",
            ExponentialSymbol => $"\\exp\\left({children[0]}\\right)",
            SineSymbol => FormatNamedFunction(children, "sin"),
            CosineSymbol => FormatNamedFunction(children, "cos"),
            TangentSymbol => FormatNamedFunction(children, "tan"),
            HyperbolicTangentSymbol => FormatNamedFunction(children, "tanh"),
            LogarithmSymbol => FormatNamedFunction(children, "log"),
            SquareRootSymbol => $"\\sqrt{{{children[0]}}}",
            AbsoluteSymbol => $"\\left|{children[0]}\\right|",
            SquareSymbol => $"\\left({children[0]}\\right)^2",
            CubeSymbol => $"\\left({children[0]}\\right)^3",
            CubeRootSymbol => $"\\sqrt[3]{{{children[0]}}}",
            PowerSymbol => $"\\left({children[0]}\\right)^{{{children[1]}}}",
            RootSymbol => $"\\sqrt[{children[1]}]{{{children[0]}}}",
            AnalyticQuotientSymbol => FormatAnalyticQuotient(children),
            _ => $"\\operatorname{{{Escape(symbol.Name)}}}\\left({string.Join(", ", children)}\\right)"
        };

    private static string FormatNamedFunction(IReadOnlyList<string> children, string name) =>
        $"\\{name}\\left({children[0]}\\right)";

    private static string FormatAnalyticQuotient(IReadOnlyList<string> children) =>
        $"\\frac{{{children[0]}}}{{\\sqrt{{1 + \\left({children[1]}\\right)^2}}}}";

    private static string Escape(string value)
    {
        var builder = new StringBuilder(value.Length);
        foreach (var character in value)
        {
            builder.Append(character switch
            {
                '\\' => "\\backslash{}",
                '{' => "\\{",
                '}' => "\\}",
                '_' => "\\_",
                '%' => "\\%",
                '$' => "\\$",
                '#' => "\\#",
                '&' => "\\&",
                '^' => "\\^{}",
                '~' => "\\~{}",
                ' ' => "\\ ",
                _ => character.ToString()
            });
        }

        return builder.ToString();
    }
}

public static class LatexExpressionFormattingExtensions
{
    extension(ExpressionTree expression)
    {
        public string ToLatexString() => ExpressionFormatters.Latex.Format(expression);
    }
}
