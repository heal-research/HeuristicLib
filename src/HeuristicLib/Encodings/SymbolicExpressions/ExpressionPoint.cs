namespace HEAL.HeuristicLib.Encodings.SymbolicExpressions;

/// <summary>Identifies one node occurrence and its path within a particular <see cref="ExpressionTree"/>.</summary>
/// <remarks>
/// Use expression points to target immutable edits through <see cref="ExpressionTree"/>. A point distinguishes
/// occurrences even when multiple parts of a tree reference the same <see cref="ExpressionNode"/> instance.
/// </remarks>
public sealed class ExpressionPoint
{
    private readonly int childIndex;

    internal ExpressionPoint(ExpressionTree tree, ExpressionNode node, ExpressionPoint? parent, int childIndex)
    {
        Tree = tree;
        Node = node;
        Parent = parent;
        this.childIndex = childIndex;
        Depth = parent is null ? 0 : parent.Depth + 1;
    }

    public ExpressionTree Tree { get; }
    public ExpressionNode Node { get; }
    public ExpressionPoint? Parent { get; }
    public int Depth { get; }
    public bool IsRoot => Parent is null;
    public int? ChildIndex => IsRoot ? null : childIndex;
    internal int ChildIndexValue => childIndex;

    public ExpressionPoint Child(int index)
    {
        var child = Node.GetChild(index);
        return new ExpressionPoint(Tree, child, this, index);
    }

    public IEnumerable<ExpressionPoint> TraverseChildren()
    {
        for (var i = 0; i < Node.Arity; i++)
            yield return Child(i);
    }

    public IEnumerable<ExpressionPoint> TraversePreOrder()
    {
        yield return this;
        for (var i = 0; i < Node.Arity; i++)
        {
            foreach (var descendant in Child(i).TraversePreOrder())
                yield return descendant;
        }
    }

    public IEnumerable<ExpressionPoint> TraversePostOrder()
    {
        for (var i = 0; i < Node.Arity; i++)
        {
            foreach (var descendant in Child(i).TraversePostOrder())
                yield return descendant;
        }

        yield return this;
    }

    public IEnumerable<ExpressionPoint> TraverseBreadthFirst()
    {
        var pending = new Queue<ExpressionPoint>();
        pending.Enqueue(this);
        while (pending.Count > 0)
        {
            var current = pending.Dequeue();
            yield return current;

            for (var i = 0; i < current.Node.Arity; i++)
                pending.Enqueue(current.Child(i));
        }
    }

    public ExpressionTree ReplaceWith(ExpressionNode replacement)
    {
        return Tree.Replace(this, replacement);
    }

    public ExpressionPoint Rebind(ExpressionTree tree)
    {
        Span<int> childIndices = Depth <= 64 ? stackalloc int[Depth] : new int[Depth];
        var current = this;
        for (var i = Depth - 1; i >= 0; i--)
        {
            childIndices[i] = current.childIndex;
            current = current.Parent!;
        }

        var rebound = tree.RootPoint;
        foreach (var childIndex in childIndices)
            rebound = rebound.Child(childIndex);

        return rebound;
    }
}
