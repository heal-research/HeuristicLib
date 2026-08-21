namespace HEAL.HeuristicLib.Encodings.SymbolicExpressions;

/// <summary>Owns an immutable symbolic-expression genotype and coordinates edits to its node occurrences.</summary>
/// <remarks>
/// <see cref="ExpressionTree"/> is the aggregate root for editing. Identify an occurrence with an
/// <see cref="ExpressionPoint"/> and use the tree's replacement methods, which return a new tree while
/// preserving structurally shared, unaffected nodes.
/// </remarks>
public sealed class ExpressionTree : IEquatable<ExpressionTree>
{
    public ExpressionTree(ExpressionNode root)
    {
        Root = root;
    }

    public ExpressionNode Root { get; }
    public ExpressionPoint RootPoint => new(this, Root, parent: null, childIndex: -1);
    public int Length => Root.Length;
    public int Complexity => Length;
    public int Depth => Root.Depth;

    public IEnumerable<ExpressionNode> TraversePreOrder() => Root.TraversePreOrder();
    public IEnumerable<ExpressionNode> TraversePostOrder() => Root.TraversePostOrder();
    public IEnumerable<ExpressionNode> TraverseBreadthFirst() => Root.TraverseBreadthFirst();

    public CompiledExpression Compile(bool optimize = true)
    {
        return ExpressionCompiler.Compile(this, optimize);
    }

    public ExpressionTree Replace(ExpressionPoint point, ExpressionNode replacement)
    {
        if (!ReferenceEquals(point.Tree, this))
            throw new ArgumentException("The expression point belongs to a different tree.", nameof(point));
        if (point.Node.Equals(replacement))
            return this;

        var current = point;
        var updated = replacement;
        while (current.Parent is not null)
        {
            updated = current.Parent.Node.WithChild(current.ChildIndexValue, updated);
            current = current.Parent;
        }

        return new ExpressionTree(updated);
    }

    public ExpressionTree Replace(ExpressionPoint point, ExpressionTree replacement)
    {
        return Replace(point, replacement.Root);
    }

    public ExpressionTree ReplaceMany(IEnumerable<(ExpressionPoint Point, ExpressionNode Replacement)> replacements)
    {
        var effectiveReplacements = new List<(ExpressionPoint Point, ExpressionNode Replacement)>();
        foreach (var replacement in replacements)
        {
            if (!ReferenceEquals(replacement.Point.Tree, this))
                throw new ArgumentException("An expression point belongs to a different tree.", nameof(replacements));
            if (!replacement.Point.Node.Equals(replacement.Replacement))
                effectiveReplacements.Add(replacement);
        }

        if (effectiveReplacements.Count == 0)
            return this;
        if (effectiveReplacements.Count == 1)
            return Replace(effectiveReplacements[0].Point, effectiveReplacements[0].Replacement);

        var patch = new PatchNode();
        foreach (var replacement in effectiveReplacements)
            AddPatch(patch, replacement.Point, replacement.Replacement, nameof(replacements));

        return new ExpressionTree(ApplyPatch(Root, patch));
    }

    public ExpressionTree WithVariable(ExpressionPoint point, VariableSymbol symbol, string variableName)
    {
        return Replace(point, new VariableExpressionNode(symbol, variableName));
    }

    public ExpressionTree WithConstant(ExpressionPoint point, ConstantSymbol symbol, double value)
    {
        return Replace(point, new NumericConstantExpressionNode(symbol, value));
    }

    internal ExpressionPoint GetPoint(int index)
    {
        if (index < 0 || index >= Length)
            throw new ArgumentOutOfRangeException(nameof(index));

        return FindPoint(RootPoint, index);
    }

    public bool Equals(ExpressionTree? other)
    {
        if (other is null)
            return false;

        return ReferenceEquals(this, other) || ReferenceEquals(Root, other.Root) || Root.Equals(other.Root);
    }

    public override bool Equals(object? obj) => obj is ExpressionTree other && Equals(other);

    public override int GetHashCode() => Root.GetHashCode();

    public static bool operator ==(ExpressionTree? left, ExpressionTree? right) => Equals(left, right);

    public static bool operator !=(ExpressionTree? left, ExpressionTree? right) => !Equals(left, right);

    private static ExpressionPoint FindPoint(ExpressionPoint point, int index)
    {
        if (index == 0)
            return point;

        index--;
        for (var i = 0; i < point.Node.Arity; i++)
        {
            var child = point.Node.GetChild(i);
            if (index < child.Length)
                return FindPoint(point.Child(i), index);

            index -= child.Length;
        }

        throw new InvalidOperationException("The expression node metadata is inconsistent with its children.");
    }

    private static void AddPatch(PatchNode root, ExpressionPoint point, ExpressionNode replacement, string parameterName)
    {
        Span<int> childIndices = point.Depth <= 64
            ? stackalloc int[point.Depth]
            : new int[point.Depth];

        var current = point;
        for (var i = point.Depth - 1; i >= 0; i--)
        {
            childIndices[i] = current.ChildIndexValue;
            current = current.Parent!;
        }

        var patch = root;
        foreach (var childIndex in childIndices)
        {
            if (patch.Replacement is not null)
                throw new ArgumentException("Replacements must not overlap.", parameterName);

            patch.Children ??= new Dictionary<int, PatchNode>();
            if (!patch.Children.TryGetValue(childIndex, out var childPatch))
            {
                childPatch = new PatchNode();
                patch.Children.Add(childIndex, childPatch);
            }

            patch = childPatch;
        }

        if (patch.Replacement is not null)
            throw new ArgumentException("An expression point must not be replaced more than once.", parameterName);
        if (patch.Children is not null)
            throw new ArgumentException("Replacements must not overlap.", parameterName);

        patch.Replacement = replacement;
    }

    private static ExpressionNode ApplyPatch(ExpressionNode original, PatchNode patch)
    {
        if (patch.Replacement is not null)
            return patch.Replacement;
        if (patch.Children is null)
            return original;
        Dictionary<int, ExpressionNode>? updatedChildren = null;
        foreach (var (childIndex, childPatch) in patch.Children)
        {
            var child = original.GetChild(childIndex);
            var updatedChild = ApplyPatch(child, childPatch);
            if (child.Equals(updatedChild))
                continue;

            updatedChildren ??= new Dictionary<int, ExpressionNode>();
            updatedChildren.Add(childIndex, updatedChild);
        }

        return updatedChildren is null
            ? original
            : original.WithChildren(updatedChildren);
    }

    private sealed class PatchNode
    {
        internal ExpressionNode? Replacement { get; set; }
        internal Dictionary<int, PatchNode>? Children { get; set; }
    }
}
