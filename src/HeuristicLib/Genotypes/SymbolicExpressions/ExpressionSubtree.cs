namespace HEAL.HeuristicLib.Genotypes.SymbolicExpressions;

public readonly struct ExpressionSubtree
{
    private readonly ExpressionTree expression;
    private readonly int startIndex;
    private readonly int rootIndex;

    internal ExpressionSubtree(
        ExpressionTree expression,
        int startIndex,
        int length,
        int rootIndex)
    {
        this.expression = expression;
        this.startIndex = startIndex;
        Length = length;
        this.rootIndex = rootIndex;
    }

    public int Length { get; }
    public ExpressionNode Node => expression.GetNode(rootIndex);
    public Symbol Symbol => Node.Symbol;
    public int Arity => Node.Arity;
    public int SubtreeLength => Length;
    public ExpressionLocation Location => new(rootIndex);

    public IEnumerable<ExpressionSubtree> TraverseChildren() => EnumerateChildren();
    public IEnumerable<ExpressionSubtree> TraversePreOrder() => EnumeratePreOrder();
    public IEnumerable<ExpressionSubtree> TraversePostOrder() => EnumeratePostOrder();
    public IEnumerable<ExpressionSubtree> TraverseBreadthFirst() => EnumerateBreadthFirstOrder();

    public ExpressionSubtree Child(int index)
    {
        var childRootIndex = expression.GetChildRootIndex(rootIndex, index);
        var childLength = expression.GetSubtreeLength(childRootIndex);
        var childStartIndex = childRootIndex - childLength + 1;
        return new ExpressionSubtree(expression, childStartIndex, childLength, childRootIndex);
    }

    public bool TryGetConstantValue(out double value)
    {
        if (Node.Symbol is not ConstantSymbol)
        {
            value = default;
            return false;
        }

        value = Node.NumericValue;
        return true;
    }

    public bool TryGetVariableReference(out VariableReference variableReference)
    {
        if (Node.Symbol is not VariableSymbol)
        {
            variableReference = default;
            return false;
        }

        variableReference = new VariableReference(Node.VariableName!, 0);
        return true;
    }

    private IEnumerable<ExpressionSubtree> EnumerateChildren()
    {
        for (var i = 0; i < Arity; i++)
        {
            yield return Child(i);
        }
    }

    private IEnumerable<ExpressionSubtree> EnumeratePostOrder()
    {
        for (var i = startIndex; i < startIndex + Length; i++)
        {
            yield return CreateSubtree(i);
        }
    }

    private IEnumerable<ExpressionSubtree> EnumeratePreOrder()
    {
        var stack = new Stack<int>();
        stack.Push(rootIndex);

        while (stack.Count > 0)
        {
            var nodeIndex = stack.Pop();
            yield return CreateSubtree(nodeIndex);
            PushChildrenRightToLeft(stack, nodeIndex);
        }
    }

    private IEnumerable<ExpressionSubtree> EnumerateBreadthFirstOrder()
    {
        var queue = new Queue<int>();
        queue.Enqueue(rootIndex);

        while (queue.Count > 0)
        {
            var nodeIndex = queue.Dequeue();
            yield return CreateSubtree(nodeIndex);
            EnqueueChildrenLeftToRight(queue, nodeIndex);
        }
    }

    private void PushChildrenRightToLeft(Stack<int> stack, int nodeIndex)
    {
        var childRootIndices = GetChildRootIndices(nodeIndex);
        for (var i = childRootIndices.Length - 1; i >= 0; i--)
        {
            stack.Push(childRootIndices[i]);
        }
    }

    private void EnqueueChildrenLeftToRight(Queue<int> queue, int nodeIndex)
    {
        foreach (var childRootIndex in GetChildRootIndices(nodeIndex))
        {
            queue.Enqueue(childRootIndex);
        }
    }

    private int[] GetChildRootIndices(int nodeIndex)
    {
        var arity = expression.GetNode(nodeIndex).Arity;
        var childRootIndices = new int[arity];
        for (var i = 0; i < arity; i++)
        {
            childRootIndices[i] = expression.GetChildRootIndex(nodeIndex, i);
        }

        return childRootIndices;
    }

    private ExpressionSubtree CreateSubtree(int index)
    {
        var length = expression.GetSubtreeLength(index);
        var start = index - length + 1;
        return new ExpressionSubtree(expression, start, length, index);
    }
}
