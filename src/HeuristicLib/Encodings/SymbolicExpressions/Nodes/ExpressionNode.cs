namespace HEAL.HeuristicLib.Encodings.SymbolicExpressions;

/// <summary>Represents an immutable node in an <see cref="ExpressionTree"/>.</summary>
/// <remarks>
/// Nodes expose expression structure for inspection and may be shared by multiple occurrences.
/// Apply edits through <see cref="ExpressionTree"/> using an <see cref="ExpressionPoint"/>.
/// </remarks>
public abstract record ExpressionNode
{
    private protected ExpressionNode(int length, int depth)
    {
        Length = length;
        Depth = depth;
    }

    public abstract Symbol Symbol { get; }

    public int Arity => Symbol.Arity;

    public int Length { get; }

    public int Depth { get; }

    public string Name => Symbol.Name;

    /// <summary>Returns the child at <paramref name="index"/>.</summary>
    /// <param name="index">A child index in the range <c>[0, Arity)</c>.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="index"/> is outside the range <c>[0, Arity)</c>.
    /// </exception>
    public abstract ExpressionNode GetChild(int index);

    internal abstract ExpressionNode WithChild(int index, ExpressionNode replacement);

    internal abstract ExpressionNode WithChildren(IReadOnlyDictionary<int, ExpressionNode> replacements);

    public IEnumerable<ExpressionNode> TraversePreOrder()
    {
        yield return this;
        for (var i = 0; i < Arity; i++)
        {
            var child = GetChild(i);
            foreach (var descendant in child.TraversePreOrder())
                yield return descendant;
        }
    }

    public IEnumerable<ExpressionNode> TraversePostOrder()
    {
        for (var i = 0; i < Arity; i++)
        {
            var child = GetChild(i);
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

            for (var i = 0; i < current.Arity; i++)
                pending.Enqueue(current.GetChild(i));
        }
    }

}
