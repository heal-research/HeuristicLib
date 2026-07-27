using HEAL.HeuristicLib.SearchSpaces.SymbolicExpressions;
using Parlot;
using Parlot.Fluent;
using static HEAL.HeuristicLib.Genotypes.SymbolicExpressions.ExpressionDraft;
using static Parlot.Fluent.Parsers;

namespace HEAL.HeuristicLib.Genotypes.SymbolicExpressions;

public enum NumericLiteralInterpretation
{
    /// <summary>Creates fixed constants from unmarked numeric literals.</summary>
    Fixed,

    /// <summary>Creates evolvable constants from unmarked numeric literals.</summary>
    Evolvable
}

/// <summary>
/// Parses mathematical infix notation into immutable symbolic expressions.
/// </summary>
/// <remarks>
/// Numeric literals use the requested <see cref="NumericLiteralInterpretation"/>. The explicit
/// <c>fixed(value)</c> and <c>param(value)</c> forms override that interpretation.
/// Identifiers containing punctuation, whitespace, or reserved names can be enclosed in backticks;
/// a backtick within a quoted identifier is escaped by doubling it.
/// Parsing with a search space binds variables, constants, and operations to the
/// matching symbols from that search space.
/// </remarks>
public static class InfixExpressionParser
{
    private static readonly Parser<SyntaxNode> Parser = CreateParser().Compile();

    public static ExpressionDraft ParseDraft(string text, NumericLiteralInterpretation numericLiterals = NumericLiteralInterpretation.Fixed)
    {
        if (TryParseDraft(text, symbols: null, out var draft, out var failure, numericLiterals))
            return draft;

        throw new FormatException(failure);
    }

    public static bool TryParseDraft(string text, out ExpressionDraft draft, NumericLiteralInterpretation numericLiterals = NumericLiteralInterpretation.Fixed)
    {
        return TryParseDraft(text, symbols: null, out draft, out _, numericLiterals);
    }

    public static ExpressionTree Parse(string text, NumericLiteralInterpretation numericLiterals = NumericLiteralInterpretation.Fixed)
    {
        if (TryParse(text, searchSpace: null, out var expression, out var failure, numericLiterals))
            return expression;

        throw new FormatException(failure);
    }

    public static ExpressionTree Parse(string text, ExpressionTreeSearchSpace searchSpace, NumericLiteralInterpretation numericLiterals = NumericLiteralInterpretation.Fixed)
    {
        if (TryParse(text, searchSpace, out var expression, out var failure, numericLiterals))
            return expression;

        throw new FormatException(failure);
    }

    public static bool TryParse(string text, out ExpressionTree expression, NumericLiteralInterpretation numericLiterals = NumericLiteralInterpretation.Fixed)
    {
        return TryParse(text, searchSpace: null, out expression, out _, numericLiterals);
    }

    public static bool TryParse(string text, ExpressionTreeSearchSpace searchSpace, out ExpressionTree expression, NumericLiteralInterpretation numericLiterals = NumericLiteralInterpretation.Fixed)
    {
        return TryParse(text, searchSpace, out expression, out _, numericLiterals);
    }

    private static void ValidateNumericLiteralInterpretation(NumericLiteralInterpretation numericLiterals)
    {
        if (numericLiterals < NumericLiteralInterpretation.Fixed || numericLiterals > NumericLiteralInterpretation.Evolvable)
            throw new ArgumentOutOfRangeException(nameof(numericLiterals));
    }

    private static Parser<SyntaxNode> CreateParser()
    {
        var expression = Deferred<SyntaxNode>();
        var openParenthesis = Terms.Char('(');
        var closeParenthesis = Terms.Char(')');
        var comma = Terms.Char(',');

        var number = Terms.Number<double>(NumberOptions.Float).Then<SyntaxNode>(static value => new NumberSyntax(value));
        var identifier = Terms.Identifier().Then(static value => new ParsedName(value.ToString(), WasQuoted: false));
        var escapedBacktick = Literals.Text("``").Then(static _ => "`");
        var quotedCharacter = escapedBacktick.Or(
            Literals.Pattern(static character => character != '`', 1, 1).Then(static value => value.ToString()));
        var quotedIdentifier = Terms.Char('`')
            .SkipAnd(ZeroOrMany(quotedCharacter))
            .AndSkip(Literals.Char('`'))
            .Then(static characters => new ParsedName(string.Concat(characters), WasQuoted: true));
        var name = identifier.Or(quotedIdentifier);

        var arguments = Separated(comma, expression);
        var function = name
            .And(openParenthesis.SkipAnd(arguments).AndSkip(closeParenthesis))
            .Then<SyntaxNode>(static value => new FunctionSyntax(value.Item1.Value, value.Item1.WasQuoted, value.Item2.ToImmutableArray()));
        var group = Between(openParenthesis, expression, closeParenthesis);
        var variable = name.Then<SyntaxNode>(static value => new IdentifierSyntax(value.Value, value.WasQuoted));
        var primary = function.Or(number).Or(group).Or(variable);

        var power = primary.RightAssociative(
            (Terms.Char('^'), static (left, right) => new BinarySyntax(BinaryOperator.Power, left, right)));
        var unary = power.Unary(
            (Terms.Char('+'), static value => value),
            (Terms.Char('-'), static value => new UnarySyntax(value)));
        var multiplicative = unary.LeftAssociative(
            (Terms.Char('*'), static (left, right) => new BinarySyntax(BinaryOperator.Multiply, left, right)),
            (Terms.Char('/'), static (left, right) => new BinarySyntax(BinaryOperator.Divide, left, right)));
        var additive = multiplicative.LeftAssociative(
            (Terms.Char('+'), static (left, right) => new BinarySyntax(BinaryOperator.Add, left, right)),
            (Terms.Char('-'), static (left, right) => new BinarySyntax(BinaryOperator.Subtract, left, right)));

        expression.Parser = additive;
        return expression
            .ElseError("Expected an expression.")
            .Eof()
            .ElseError("Unexpected token.");
    }

    private static SyntaxNode ParseSyntax(string text) => Parser.Parse(text)!;

    private static bool TryParse(string text, ExpressionTreeSearchSpace? searchSpace, out ExpressionTree expression, out string failure, NumericLiteralInterpretation numericLiterals)
    {
        if (!TryParseDraft(text, searchSpace?.Symbols, out var draft, out failure, numericLiterals))
        {
            expression = null!;
            return false;
        }

        try
        {
            expression = searchSpace is null ? draft.Build() : draft.Build(searchSpace);
            if (searchSpace is not null && !searchSpace.Contains(expression))
            {
                expression = null!;
                failure = "The infix expression exceeds the supplied search-space constraints.";
                return false;
            }

            return true;
        }
        catch (InvalidOperationException exception)
        {
            expression = null!;
            failure = $"The infix expression could not be bound: {exception.Message}";
            return false;
        }
    }

    private static bool TryParseDraft(string text, IReadOnlyList<Symbol>? symbols, out ExpressionDraft draft, out string failure, NumericLiteralInterpretation numericLiterals)
    {
        ValidateNumericLiteralInterpretation(numericLiterals);
        try
        {
            draft = Bind(ParseSyntax(text), symbols, numericLiterals);
            failure = string.Empty;
            return true;
        }
        catch (ParseException exception)
        {
            draft = null!;
            failure = $"Invalid infix expression at line {exception.Position.Line}, column {exception.Position.Column}: {exception.Message}";
            return false;
        }
        catch (InvalidOperationException exception)
        {
            draft = null!;
            failure = $"The infix expression could not be bound: {exception.Message}";
            return false;
        }
    }

    private static ExpressionDraft Bind(SyntaxNode syntax, IReadOnlyList<Symbol>? symbols, NumericLiteralInterpretation numericLiterals)
    {
        return syntax switch
        {
            NumberSyntax number => BindNumericLiteral(number.Value, numericLiterals),
            IdentifierSyntax identifier => BindIdentifier(identifier, symbols, numericLiterals),
            UnarySyntax unary when TryGetSignedNumber(unary, out var value) => BindNumericLiteral(value, numericLiterals),
            UnarySyntax unary => Negate(Bind(unary.Operand, symbols, numericLiterals)),
            BinarySyntax binary => BindBinary(binary, symbols, numericLiterals),
            FunctionSyntax function => BindFunction(function, symbols, numericLiterals),
            _ => throw new InvalidOperationException($"Unsupported infix syntax node '{syntax.GetType().Name}'.")
        };
    }

    private static ExpressionDraft BindNumericLiteral(double value, NumericLiteralInterpretation interpretation) =>
        interpretation switch
        {
            NumericLiteralInterpretation.Fixed => FixedConstant(value),
            NumericLiteralInterpretation.Evolvable => Constant(value),
            _ => throw new ArgumentOutOfRangeException(nameof(interpretation))
        };

    private static ExpressionDraft BindIdentifier(IdentifierSyntax identifier, IReadOnlyList<Symbol>? symbols, NumericLiteralInterpretation numericLiterals)
    {
        if (!identifier.WasQuoted && TryGetSpecialNumber(identifier.Name, out var value))
            return BindNumericLiteral(value, numericLiterals);

        if (symbols is null)
            return Variable(identifier.Name);

        var fixedConstants = symbols
            .OfType<FixedConstantSymbol>()
            .Where(symbol => string.Equals(symbol.Name, identifier.Name, StringComparison.Ordinal))
            .ToArray();
        var variableSymbols = symbols
            .OfType<VariableSymbol>()
            .Where(symbol => symbol.Variables.Contains(identifier.Name, StringComparer.Ordinal))
            .ToArray();

        if (fixedConstants.Length + variableSymbols.Length > 1)
            throw new InvalidOperationException($"Identifier '{identifier.Name}' is ambiguous in the supplied search space.");
        if (fixedConstants.Length == 1)
            return FixedConstant(fixedConstants[0].Value, fixedConstants[0].DisplayName);
        if (variableSymbols.Length == 1)
            return Variable(identifier.Name, variableSymbols[0]);

        return Variable(identifier.Name);
    }

    private static ExpressionDraft BindBinary(BinarySyntax binary, IReadOnlyList<Symbol>? symbols, NumericLiteralInterpretation numericLiterals)
    {
        var left = Bind(binary.Left, symbols, numericLiterals);
        var right = Bind(binary.Right, symbols, numericLiterals);
        return binary.Operator switch
        {
            BinaryOperator.Add => Add(left, right),
            BinaryOperator.Subtract => Subtract(left, right),
            BinaryOperator.Multiply => Multiply(left, right),
            BinaryOperator.Divide => Divide(left, right),
            BinaryOperator.Power => Power(left, right),
            _ => throw new InvalidOperationException($"Unsupported binary operator '{binary.Operator}'.")
        };
    }

    private static ExpressionDraft BindFunction(FunctionSyntax function, IReadOnlyList<Symbol>? symbols, NumericLiteralInterpretation numericLiterals)
    {
        if (!function.WasQuoted && function.Name.Equals("fixed", StringComparison.OrdinalIgnoreCase))
            return BindExplicitConstant(function, evolvable: false);
        if (!function.WasQuoted && function.Name.Equals("param", StringComparison.OrdinalIgnoreCase))
            return BindExplicitConstant(function, evolvable: true);

        var children = function.Arguments.Select(argument => Bind(argument, symbols, numericLiterals)).ToArray();
        if (symbols is not null)
        {
            var matches = symbols
                .OfType<OperationSymbol>()
                .Where(symbol => symbol.Arity == children.Length && string.Equals(symbol.Name, function.Name, StringComparison.Ordinal))
                .ToArray();
            if (matches.Length > 1)
                throw new InvalidOperationException($"Function '{function.Name}' with arity {children.Length} is ambiguous in the supplied search space.");
            if (matches.Length == 1)
                return Apply(matches[0], children);
        }

        var builtIn = ResolveBuiltInFunction(function.Name, children);
        return builtIn ?? throw new InvalidOperationException(
            symbols is null
                ? $"Unknown function '{function.Name}'."
                : $"Function '{function.Name}' with arity {children.Length} is not available in the supplied search space.");
    }

    private static ExpressionDraft BindExplicitConstant(FunctionSyntax function, bool evolvable)
    {
        if (function.Arguments.Length != 1 || !TryGetSignedNumber(function.Arguments[0], out var value))
            throw new InvalidOperationException($"Function '{function.Name}' requires one numeric literal argument.");

        return evolvable ? Constant(value) : FixedConstant(value);
    }

    private static ExpressionDraft? ResolveBuiltInFunction(string name, IReadOnlyList<ExpressionDraft> children)
    {
        return (name.ToLowerInvariant(), children.Count) switch
        {
            ("negate", 1) => Negate(children[0]),
            ("exp", 1) => Exp(children[0]),
            ("sin", 1) => Sin(children[0]),
            ("cos", 1) => Cos(children[0]),
            ("tan", 1) => Tan(children[0]),
            ("tanh", 1) => Tanh(children[0]),
            ("log", 1) => Log(children[0]),
            ("sqrt", 1) => Sqrt(children[0]),
            ("abs", 1) => Abs(children[0]),
            ("square", 1) => Square(children[0]),
            ("cube", 1) => Cube(children[0]),
            ("cbrt", 1) => CubeRoot(children[0]),
            ("pow", 2) => Power(children[0], children[1]),
            ("root", 2) => Root(children[0], children[1]),
            ("aq", 2) => AnalyticQuotient(children[0], children[1]),
            ("sigmoid", 1) => Sigmoid(children[0]),
            _ => null
        };
    }

    private static bool TryGetSignedNumber(SyntaxNode syntax, out double value)
    {
        switch (syntax)
        {
            case NumberSyntax number:
                value = number.Value;
                return true;
            case UnarySyntax { Operand: NumberSyntax number }:
                value = -number.Value;
                return true;
            case IdentifierSyntax { WasQuoted: false } identifier when TryGetSpecialNumber(identifier.Name, out value):
                return true;
            case UnarySyntax { Operand: IdentifierSyntax { WasQuoted: false } identifier } when TryGetSpecialNumber(identifier.Name, out value):
                value = -value;
                return true;
            default:
                value = default;
                return false;
        }
    }

    private static bool TryGetSpecialNumber(string text, out double value)
    {
        if (text.Equals("nan", StringComparison.OrdinalIgnoreCase))
        {
            value = double.NaN;
            return true;
        }

        if (text.Equals("infinity", StringComparison.OrdinalIgnoreCase))
        {
            value = double.PositiveInfinity;
            return true;
        }

        value = default;
        return false;
    }

    private readonly record struct ParsedName(string Value, bool WasQuoted);
    private abstract record SyntaxNode;
    private sealed record NumberSyntax(double Value) : SyntaxNode;
    private sealed record IdentifierSyntax(string Name, bool WasQuoted) : SyntaxNode;
    private sealed record UnarySyntax(SyntaxNode Operand) : SyntaxNode;
    private sealed record BinarySyntax(BinaryOperator Operator, SyntaxNode Left, SyntaxNode Right) : SyntaxNode;
    private sealed record FunctionSyntax(string Name, bool WasQuoted, ImmutableArray<SyntaxNode> Arguments) : SyntaxNode;

    private enum BinaryOperator
    {
        Add,
        Subtract,
        Multiply,
        Divide,
        Power
    }
}
