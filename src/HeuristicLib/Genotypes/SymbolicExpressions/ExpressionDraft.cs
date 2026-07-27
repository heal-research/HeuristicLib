using HEAL.HeuristicLib.SearchSpaces.SymbolicExpressions;

namespace HEAL.HeuristicLib.Genotypes.SymbolicExpressions;

public abstract record ExpressionDraft
{
    public ExpressionTree Build()
    {
        return new ExpressionTree(BuildNode(this, searchSpace: null));
    }

    public ExpressionTree Build(ExpressionTreeSearchSpace searchSpace)
    {
        return new ExpressionTree(BuildNode(this, searchSpace));
    }

    public ExpressionTree Build(IEnumerable<Symbol> symbols)
    {
        return Build(new ExpressionTreeSearchSpace(int.MaxValue, int.MaxValue, symbols));
    }

    public bool TryBuild(ExpressionTreeSearchSpace searchSpace, out ExpressionTree expression)
    {
        try
        {
            expression = Build(searchSpace);
            return true;
        }
        catch (InvalidOperationException)
        {
            expression = null!;
            return false;
        }
    }

    public bool TryBuild(IEnumerable<Symbol> symbols, out ExpressionTree expression)
    {
        try
        {
            expression = Build(symbols);
            return true;
        }
        catch (InvalidOperationException)
        {
            expression = null!;
            return false;
        }
    }

    private static ExpressionNode BuildNode(ExpressionDraft draft, ExpressionTreeSearchSpace? searchSpace)
    {
        switch (draft)
        {
            case VariableDraft(var name, var localVariableSymbol):
                var resolvedVariable = Resolve(
                    localVariableSymbol ?? new VariableSymbol([name]), searchSpace,
                    candidate => localVariableSymbol is not null ? candidate == localVariableSymbol : candidate.Variables.Contains(name, StringComparer.Ordinal),
                    $"variable '{name}'");
                return new VariableExpressionNode(resolvedVariable, name);
            case FixedConstantDraft(var value, var localFixedSymbol):
                var resolvedFixedSymbol = Resolve(localFixedSymbol, searchSpace, candidate => candidate == localFixedSymbol, "fixed constant");
                return new NumericConstantExpressionNode(resolvedFixedSymbol, value);
            case EvolvableConstantDraft(var value, var localEvolvableSymbol):
                var evolvableSymbol = Resolve(localEvolvableSymbol ?? new EvolvableConstantSymbol(), searchSpace,
                    candidate => localEvolvableSymbol is null || candidate == localEvolvableSymbol,
                    "evolvable constant");
                return new NumericConstantExpressionNode(evolvableSymbol, value);
            case UnaryDraft(var localUnarySymbol, var childDraft):
                var resolvedUnarySymbol = Resolve(localUnarySymbol, searchSpace, candidate => candidate == localUnarySymbol, localUnarySymbol.Name);
                var childNode = BuildNode(childDraft, searchSpace);
                return new UnaryExpressionNode(resolvedUnarySymbol, childNode);
            case BinaryDraft(var localBinarySymbol, var leftChildDraft, var rightChildDraft):
                var resolvedBinarySymbol = Resolve(localBinarySymbol, searchSpace, candidate => candidate == localBinarySymbol, localBinarySymbol.Name);
                var leftChildNode = BuildNode(leftChildDraft, searchSpace);
                var rightChildNode = BuildNode(rightChildDraft, searchSpace);
                return new BinaryExpressionNode(resolvedBinarySymbol, leftChildNode, rightChildNode);
            case OperationDraft(var localOperationSymbol, var childDrafts):
                var symbol = Resolve(localOperationSymbol, searchSpace, candidate => candidate == localOperationSymbol, localOperationSymbol.Name);
                var childNodes = childDrafts.Select(child => BuildNode(child, searchSpace)).ToImmutableArray();
                return childNodes.Length switch
                {
                    1 => new UnaryExpressionNode(symbol, childNodes[0]),
                    2 => new BinaryExpressionNode(symbol, childNodes[0], childNodes[1]),
                    _ => new NaryExpressionNode(symbol, childNodes)
                };
            default:
                throw new InvalidOperationException($"Unsupported expression draft node {draft.GetType()}.");
        }
    }

    private static TSymbol Resolve<TSymbol>(TSymbol localSymbol, ExpressionTreeSearchSpace? searchSpace, Func<TSymbol, bool> matches, string term)
        where TSymbol : Symbol
    {
        if (searchSpace is null)
            return localSymbol;

        var matching = searchSpace.Symbols.OfType<TSymbol>().Where(matches).ToArray();
        return matching.Length switch
        {
            1 => matching[0],
            0 => throw new InvalidOperationException($"Draft {term} does not match a symbol in the supplied search space."),
            _ => throw new InvalidOperationException($"Draft {term} matches multiple symbols in the supplied search space.")
        };
    }

    internal sealed record VariableDraft(string Name, VariableSymbol? Symbol) : ExpressionDraft;
    internal sealed record FixedConstantDraft(double Value, FixedConstantSymbol Symbol) : ExpressionDraft;
    internal sealed record EvolvableConstantDraft(double Value, EvolvableConstantSymbol? Symbol) : ExpressionDraft;
    internal sealed record UnaryDraft(OperationSymbol Symbol, ExpressionDraft Child) : ExpressionDraft;
    internal sealed record BinaryDraft(OperationSymbol Symbol, ExpressionDraft Left, ExpressionDraft Right) : ExpressionDraft;
    internal sealed record OperationDraft(OperationSymbol Symbol, ImmutableArray<ExpressionDraft> Children) : ExpressionDraft;

    public static ExpressionDraft Variable(string name, VariableSymbol? variableSymbol = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Variable name must not be empty.", nameof(name));
        if (variableSymbol is not null && !variableSymbol.Variables.Contains(name, StringComparer.Ordinal))
            throw new ArgumentException($"Variable symbol does not allow variable '{name}'.", nameof(variableSymbol));

        return new VariableDraft(name, variableSymbol);
    }

    public static ExpressionDraft FixedConstant(double value, string? displayName = null) => new FixedConstantDraft(value, new FixedConstantSymbol(value, displayName));
    public static ExpressionDraft Constant(double value, EvolvableConstantSymbol? symbol = null) => new EvolvableConstantDraft(value, symbol);

    public static ExpressionDraft Apply(OperationSymbol symbol, params ExpressionDraft[] children)
    {
        if (children.Length != symbol.Arity)
            throw new ArgumentException($"Symbol '{symbol.Name}' requires {symbol.Arity} children but received {children.Length}.", nameof(children));

        return new OperationDraft(symbol, children.ToImmutableArray());
    }

    public static ExpressionDraft Add(ExpressionDraft left, ExpressionDraft right) => new BinaryDraft(Symbols.Addition, left, right);
    public static ExpressionDraft Subtract(ExpressionDraft left, ExpressionDraft right) => new BinaryDraft(Symbols.Subtraction, left, right);
    public static ExpressionDraft Multiply(ExpressionDraft left, ExpressionDraft right) => new BinaryDraft(Symbols.Multiplication, left, right);
    public static ExpressionDraft Divide(ExpressionDraft left, ExpressionDraft right) => new BinaryDraft(Symbols.Division, left, right);
    public static ExpressionDraft Negate(ExpressionDraft child) => new UnaryDraft(Symbols.Negation, child);
    public static ExpressionDraft Exp(ExpressionDraft child) => new UnaryDraft(Symbols.Exponential, child);
    public static ExpressionDraft Sin(ExpressionDraft child) => new UnaryDraft(Symbols.Sine, child);
    public static ExpressionDraft Cos(ExpressionDraft child) => new UnaryDraft(Symbols.Cosine, child);
    public static ExpressionDraft Tan(ExpressionDraft child) => new UnaryDraft(Symbols.Tangent, child);
    public static ExpressionDraft Tanh(ExpressionDraft child) => new UnaryDraft(Symbols.HyperbolicTangent, child);
    public static ExpressionDraft Log(ExpressionDraft child) => new UnaryDraft(Symbols.Logarithm, child);
    public static ExpressionDraft Sqrt(ExpressionDraft child) => new UnaryDraft(Symbols.SquareRoot, child);
    public static ExpressionDraft Abs(ExpressionDraft child) => new UnaryDraft(Symbols.Absolute, child);
    public static ExpressionDraft Square(ExpressionDraft child) => new UnaryDraft(Symbols.Square, child);
    public static ExpressionDraft Cube(ExpressionDraft child) => new UnaryDraft(Symbols.Cube, child);
    public static ExpressionDraft CubeRoot(ExpressionDraft child) => new UnaryDraft(Symbols.CubeRoot, child);
    public static ExpressionDraft Power(ExpressionDraft value, ExpressionDraft exponent) => new BinaryDraft(Symbols.Power, value, exponent);
    public static ExpressionDraft Root(ExpressionDraft value, ExpressionDraft degree) => new BinaryDraft(Symbols.Root, value, degree);
    public static ExpressionDraft AnalyticQuotient(ExpressionDraft numerator, ExpressionDraft denominator) => new BinaryDraft(Symbols.AnalyticQuotient, numerator, denominator);
    public static ExpressionDraft Sigmoid(ExpressionDraft child) => new UnaryDraft(Symbols.Sigmoid, child);

    public static ExpressionDraft operator +(ExpressionDraft left, ExpressionDraft right) => Add(left, right);
    public static ExpressionDraft operator -(ExpressionDraft left, ExpressionDraft right) => Subtract(left, right);
    public static ExpressionDraft operator *(ExpressionDraft left, ExpressionDraft right) => Multiply(left, right);
    public static ExpressionDraft operator /(ExpressionDraft left, ExpressionDraft right) => Divide(left, right);
}
