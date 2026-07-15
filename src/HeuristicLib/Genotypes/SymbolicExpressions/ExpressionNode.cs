using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Genotypes.SymbolicExpressions;

public sealed class ExpressionNode : IEquatable<ExpressionNode>
{
    private readonly ExpressionNode[] children;
    private readonly int hashCode;

    public ExpressionNode(Symbol symbol, params IEnumerable<ExpressionNode> children)
        : this(ValidatePayloadlessSymbol(symbol), variableName: null, numericValue: default, children.ToArray())
    {
    }

    internal ExpressionNode(VariableSymbol symbol, string variableName)
        : this(symbol, variableName, numericValue: default, [])
    {
    }

    internal ExpressionNode(ConstantSymbol symbol, double numericValue)
        : this(symbol, variableName: null, numericValue, [])
    {
    }

    /// <summary>
    /// Creates a payloadless node backed by <paramref name="children"/> without copying the array.
    /// The caller must not mutate the array after this method returns.
    /// </summary>
    public static ExpressionNode FromOwnedChildren(Symbol symbol, ExpressionNode[] children)
    {
        return new ExpressionNode(ValidatePayloadlessSymbol(symbol), variableName: null, numericValue: default, children);
    }

    /// <summary>
    /// Creates and initializes a node backed by <paramref name="children"/> without copying the array.
    /// The caller must not mutate the array after this method returns.
    /// </summary>
    public static ExpressionNode FromOwnedChildren(Symbol symbol, IRandomNumberGenerator random, ExpressionNode[] children)
    {
        return symbol switch
        {
            VariableSymbol variable => FromOwnedChildren(variable, variable.Sample(random), children),
            EvolvableConstantSymbol constant => FromOwnedChildren(constant, constant.SampleInitialValue(random), children),
            FixedConstantSymbol constant => FromOwnedChildren(constant, constant.Value, children),
            _ => FromOwnedChildren(symbol, children)
        };
    }

    private ExpressionNode(Symbol symbol, string? variableName, double numericValue, ExpressionNode[] children)
    {
        if (children.Length != symbol.Arity)
            throw new ArgumentException($"Symbol '{symbol.Name}' requires {symbol.Arity} children but received {children.Length}.", nameof(children));

        Symbol = symbol;
        VariableName = variableName;
        NumericValue = numericValue;
        this.children = children;

        var hash = new HashCode();
        hash.Add(Symbol);
        hash.Add(VariableName, StringComparer.Ordinal);
        if (Symbol is ConstantSymbol)
            hash.Add(NumericValue);

        var length = 1;
        var depth = 1;
        foreach (var child in this.children)
        {
            length += child.Length;
            depth = Math.Max(depth, child.Depth + 1);
            hash.Add(child.hashCode);
        }

        Length = length;
        Depth = depth;
        hashCode = hash.ToHashCode();
    }

    public Symbol Symbol { get; }
    public int Arity => children.Length;
    public int Length { get; }
    public int SubtreeLength => Length;
    public int Depth { get; }
    public string Name => Symbol.Name;
    internal string? VariableName { get; }
    internal double NumericValue { get; }
    internal bool HasVariableName => VariableName is not null;
    internal bool HasNumericValue => Symbol is ConstantSymbol;
    internal ExpressionNode[] Children => children;

    internal static ExpressionNode FromOwnedChildren(VariableSymbol symbol, string variableName, ExpressionNode[] children)
    {
        return new ExpressionNode(symbol, variableName, numericValue: default, children);
    }

    internal static ExpressionNode FromOwnedChildren(ConstantSymbol symbol, double numericValue, ExpressionNode[] children)
    {
        return new ExpressionNode(symbol, variableName: null, numericValue, children);
    }

    public ExpressionNode Child(int index)
    {
        if ((uint)index >= (uint)children.Length)
            throw new ArgumentOutOfRangeException(nameof(index));

        return children[index];
    }

    public IEnumerable<ExpressionNode> TraverseChildren()
    {
        foreach (var child in children)
            yield return child;
    }

    public IEnumerable<ExpressionNode> TraversePreOrder()
    {
        yield return this;
        foreach (var child in children)
        {
            foreach (var descendant in child.TraversePreOrder())
                yield return descendant;
        }
    }

    public IEnumerable<ExpressionNode> TraversePostOrder()
    {
        foreach (var child in children)
        {
            foreach (var descendant in child.TraversePostOrder())
                yield return descendant;
        }

        yield return this;
    }

    public IEnumerable<ExpressionNode> TraverseBreadthFirst()
    {
        var pending = new Queue<ExpressionNode>();
        pending.Enqueue(this);
        while (pending.Count > 0)
        {
            var current = pending.Dequeue();
            yield return current;

            foreach (var child in current.children)
                pending.Enqueue(child);
        }
    }

    public bool TryGetVariableName(out string variableName)
    {
        variableName = VariableName!;
        return Symbol is VariableSymbol;
    }

    public bool TryGetConstantValue(out double value)
    {
        value = NumericValue;
        return Symbol is ConstantSymbol;
    }

    public ExpressionNode WithSymbol(Symbol symbol)
    {
        if (symbol.Arity != Arity)
            throw new ArgumentException($"Replacement symbol arity {symbol.Arity} must match node arity {Arity}.", nameof(symbol));

        return FromOwnedChildren(symbol, children);
    }

    internal ExpressionNode WithChild(int index, ExpressionNode child)
    {
        if ((uint)index >= (uint)children.Length)
            throw new ArgumentOutOfRangeException(nameof(index));
        if (ReferenceEquals(children[index], child))
            return this;

        var updatedChildren = children.ToArray();
        updatedChildren[index] = child;
        return WithOwnedChildren(updatedChildren);
    }

    internal ExpressionNode WithOwnedChildren(ExpressionNode[] updatedChildren)
    {
        return Symbol switch
        {
            VariableSymbol variable => FromOwnedChildren(variable, VariableName!, updatedChildren),
            ConstantSymbol constant => FromOwnedChildren(constant, NumericValue, updatedChildren),
            _ => FromOwnedChildren(Symbol, updatedChildren)
        };
    }

    public bool Equals(ExpressionNode? other)
    {
        if (other is null)
            return false;
        if (ReferenceEquals(this, other))
            return true;
        if (hashCode != other.hashCode
            || Symbol != other.Symbol
            || !string.Equals(VariableName, other.VariableName, StringComparison.Ordinal)
            || Symbol is ConstantSymbol && !NumericValue.Equals(other.NumericValue)
            || children.Length != other.children.Length)
            return false;

        for (var i = 0; i < children.Length; i++)
        {
            if (!children[i].Equals(other.children[i]))
                return false;
        }

        return true;
    }

    public override bool Equals(object? obj) => obj is ExpressionNode other && Equals(other);

    public override int GetHashCode() => hashCode;

    public static bool operator ==(ExpressionNode? left, ExpressionNode? right) => Equals(left, right);

    public static bool operator !=(ExpressionNode? left, ExpressionNode? right) => !Equals(left, right);

    private static Symbol ValidatePayloadlessSymbol(Symbol symbol)
    {
        if (symbol is VariableSymbol or ConstantSymbol)
            throw new ArgumentException("Payload-bearing symbols require their corresponding node payload.", nameof(symbol));

        return symbol;
    }
}
