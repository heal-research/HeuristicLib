using HEAL.HeuristicLib.SearchSpaces.SymbolicExpressions;

namespace HEAL.HeuristicLib.Genotypes.SymbolicExpressions;

public abstract record ExpressionDraft
{
    public ExpressionTree Build() => BuildCore(searchSpace: null);

    public ExpressionTree Build(ExpressionTreeSearchSpace searchSpace)
    {
        return BuildCore(searchSpace);
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

    private ExpressionTree BuildCore(ExpressionTreeSearchSpace? searchSpace)
    {
        return new ExpressionTree(BuildNode(this, searchSpace));
    }

    private static ExpressionNode BuildNode(ExpressionDraft draft, ExpressionTreeSearchSpace? searchSpace)
    {
        switch (draft)
        {
            case VariableDraft variable:
                var localVariableSymbol = variable.Symbol ?? new VariableSymbol([variable.Name]);
                return new ExpressionNode(Resolve(localVariableSymbol, searchSpace,
                    candidate => variable.Symbol is not null ? candidate == variable.Symbol : candidate.Variables.Contains(variable.Name, StringComparer.Ordinal),
                    $"variable '{variable.Name}'"), variable.Name);
            case FixedConstantDraft constant:
                var fixedSymbol = Resolve(constant.Symbol, searchSpace, candidate => candidate == constant.Symbol, "fixed constant");
                return new ExpressionNode(fixedSymbol, constant.Value);
            case EvolvableConstantDraft constant:
                var localEvolvableSymbol = constant.Symbol ?? new EvolvableConstantSymbol();
                var evolvableSymbol = Resolve(localEvolvableSymbol, searchSpace,
                    candidate => constant.Symbol is null || candidate == constant.Symbol,
                    "evolvable constant");
                return new ExpressionNode(evolvableSymbol, constant.Value);
            case UnaryDraft unary:
                return ExpressionNode.FromOwnedChildren(
                    Resolve(unary.Symbol, searchSpace, candidate => candidate == unary.Symbol, unary.Symbol.Name),
                    [BuildNode(unary.Child, searchSpace)]);
            case BinaryDraft binary:
                return ExpressionNode.FromOwnedChildren(
                    Resolve(binary.Symbol, searchSpace, candidate => candidate == binary.Symbol, binary.Symbol.Name),
                    [BuildNode(binary.Left, searchSpace), BuildNode(binary.Right, searchSpace)]);
            case OperationDraft operation:
                var children = operation.Children.Select(child => BuildNode(child, searchSpace)).ToArray();
                return ExpressionNode.FromOwnedChildren(
                    Resolve(operation.Symbol, searchSpace, candidate => candidate == operation.Symbol, operation.Symbol.Name),
                    children);
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
    public static ExpressionDraft Log(ExpressionDraft child) => new UnaryDraft(Symbols.Logarithm, child);
    public static ExpressionDraft Sqrt(ExpressionDraft child) => new UnaryDraft(Symbols.SquareRoot, child);
    public static ExpressionDraft Sigmoid(ExpressionDraft child) => new UnaryDraft(Symbols.Sigmoid, child);

    public static ExpressionDraft operator +(ExpressionDraft left, ExpressionDraft right) => Add(left, right);
    public static ExpressionDraft operator -(ExpressionDraft left, ExpressionDraft right) => Subtract(left, right);
    public static ExpressionDraft operator *(ExpressionDraft left, ExpressionDraft right) => Multiply(left, right);
    public static ExpressionDraft operator /(ExpressionDraft left, ExpressionDraft right) => Divide(left, right);
}
