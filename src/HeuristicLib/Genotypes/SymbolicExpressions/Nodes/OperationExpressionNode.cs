using Generator.Equals;

namespace HEAL.HeuristicLib.Genotypes.SymbolicExpressions;

[Equatable]
public abstract partial record OperationExpressionNode : ExpressionNode
{
    private protected OperationExpressionNode(OperationSymbol symbol, int length, int depth)
        : base(length, depth)
    {
        Symbol = symbol;
    }

    [IgnoreEquality]
    public sealed override OperationSymbol Symbol { get; }
}

[Equatable]
public sealed partial record UnaryExpressionNode : OperationExpressionNode
{
    public UnaryExpressionNode(OperationSymbol symbol, ExpressionNode operand)
        : base(ValidateSymbol(symbol), operand.Length + 1, operand.Depth + 1)
    {
        Operand = operand;
    }

    public ExpressionNode Operand { get; }

    public override ExpressionNode GetChild(int index)
    {
        return index == 0
            ? Operand
            : throw new ArgumentOutOfRangeException(nameof(index));
    }

    internal override ExpressionNode WithChild(int index, ExpressionNode replacement)
    {
        if (index != 0)
            throw new ArgumentOutOfRangeException(nameof(index));

        return Operand.Equals(replacement)
            ? this
            : new UnaryExpressionNode(Symbol, replacement);
    }

    internal override ExpressionNode WithChildren(IReadOnlyDictionary<int, ExpressionNode> replacements)
    {
        if (replacements.Count == 0)
            return this;
        if (replacements.Count != 1 || !replacements.TryGetValue(0, out var operand))
            throw new ArgumentException("A unary node only accepts a replacement for child zero.", nameof(replacements));

        return WithChild(0, operand);
    }

    private static OperationSymbol ValidateSymbol(OperationSymbol symbol)
    {
        if (symbol.Arity != 1)
            throw new ArgumentException("A unary node requires an arity-one operation symbol.", nameof(symbol));

        return symbol;
    }
}

[Equatable]
public sealed partial record BinaryExpressionNode : OperationExpressionNode
{
    public BinaryExpressionNode(OperationSymbol symbol, ExpressionNode left, ExpressionNode right)
        : base(ValidateSymbol(symbol), left.Length + right.Length + 1, Math.Max(left.Depth, right.Depth) + 1)
    {
        Left = left;
        Right = right;
    }

    public ExpressionNode Left { get; }
    public ExpressionNode Right { get; }

    public override ExpressionNode GetChild(int index)
    {
        return index switch
        {
            0 => Left,
            1 => Right,
            _ => throw new ArgumentOutOfRangeException(nameof(index))
        };
    }

    internal override ExpressionNode WithChild(int index, ExpressionNode replacement)
    {
        return index switch
        {
            0 when Left.Equals(replacement) => this,
            0 => new BinaryExpressionNode(Symbol, replacement, Right),
            1 when Right.Equals(replacement) => this,
            1 => new BinaryExpressionNode(Symbol, Left, replacement),
            _ => throw new ArgumentOutOfRangeException(nameof(index))
        };
    }

    internal override ExpressionNode WithChildren(IReadOnlyDictionary<int, ExpressionNode> replacements)
    {
        if (replacements.Count == 0)
            return this;

        if (!replacements.Keys.All(index => index is 0 or 1))
            throw new ArgumentException("A binary node only accepts replacements for children zero and one.", nameof(replacements));

        var left = replacements.GetValueOrDefault(0, Left);
        var right = replacements.GetValueOrDefault(1, Right);
        return Left.Equals(left) && Right.Equals(right)
            ? this
            : new BinaryExpressionNode(Symbol, left, right);
    }

    private static OperationSymbol ValidateSymbol(OperationSymbol symbol)
    {
        if (symbol.Arity != 2)
            throw new ArgumentException("A binary node requires an arity-two operation symbol.", nameof(symbol));

        return symbol;
    }
}

[Equatable]
public sealed partial record NaryExpressionNode : OperationExpressionNode
{
    public NaryExpressionNode(OperationSymbol symbol, params IEnumerable<ExpressionNode> children)
        : this(symbol, children.ToImmutableArray())
    {
    }

    public NaryExpressionNode(OperationSymbol symbol, ImmutableArray<ExpressionNode> children)
        : base(ValidateSymbol(symbol, children), CalculateLength(children), CalculateDepth(children))
    {
        Children = children;
    }

    [OrderedEquality]
    public ImmutableArray<ExpressionNode> Children { get; }

    public override ExpressionNode GetChild(int index)
    {
        if (index < 0 || index >= Children.Length)
            throw new ArgumentOutOfRangeException(nameof(index));

        return Children[index];
    }

    internal override ExpressionNode WithChild(int index, ExpressionNode replacement)
    {
        if (index < 0 || index >= Children.Length)
            throw new ArgumentOutOfRangeException(nameof(index));
        if (Children[index].Equals(replacement))
            return this;

        return new NaryExpressionNode(Symbol, Children.SetItem(index, replacement));
    }

    internal override ExpressionNode WithChildren(IReadOnlyDictionary<int, ExpressionNode> replacements)
    {
        if (replacements.Count == 0)
            return this;

        ImmutableArray<ExpressionNode>.Builder? updated = null;
        foreach (var (index, replacement) in replacements)
        {
            if (index < 0 || index >= Children.Length)
                throw new ArgumentException($"Child index {index} is outside the n-ary node.", nameof(replacements));
            if (Children[index].Equals(replacement))
                continue;

            updated ??= Children.ToBuilder();
            updated[index] = replacement;
        }

        return updated is null
            ? this
            : new NaryExpressionNode(Symbol, updated.MoveToImmutable());
    }

    private static OperationSymbol ValidateSymbol(OperationSymbol symbol, ImmutableArray<ExpressionNode> children)
    {
        if (children.IsDefault)
            throw new ArgumentException("Children must be initialized.", nameof(children));
        if (children.Length < 3)
            throw new ArgumentException("An n-ary node requires at least three children.", nameof(children));
        if (symbol.Arity != children.Length)
            throw new ArgumentException($"Symbol '{symbol.Name}' requires {symbol.Arity} children but received {children.Length}.", nameof(children));

        return symbol;
    }

    private static int CalculateLength(ImmutableArray<ExpressionNode> children)
    {
        var length = 1;
        foreach (var child in children)
            length += child.Length;
        return length;
    }

    private static int CalculateDepth(ImmutableArray<ExpressionNode> children)
    {
        var depth = 0;
        foreach (var child in children)
            depth = Math.Max(depth, child.Depth);
        return depth + 1;
    }
}
