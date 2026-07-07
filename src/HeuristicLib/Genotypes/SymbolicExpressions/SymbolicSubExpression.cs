namespace HEAL.HeuristicLib.Genotypes.SymbolicExpressions;

public readonly struct SymbolicSubExpression
{
    private readonly SymbolicExpression expression;
    private readonly int startIndex;
    private readonly int rootSymbolIndex;

    internal SymbolicSubExpression(
        SymbolicExpression expression,
        int startIndex,
        int length,
        int rootSymbolIndex)
    {
        this.expression = expression;
        this.startIndex = startIndex;
        Length = length;
        this.rootSymbolIndex = rootSymbolIndex;
    }

    public int Length { get; }
    public Symbol Symbol => expression.GetSymbol(rootSymbolIndex);
    public int Arity => Symbol.Arity;
    public int SubtreeLength => Length;
    public SymbolicExpressionLocation Location => new(rootSymbolIndex);

    public IEnumerable<SymbolicSubExpression> TraverseChildren() => EnumerateChildren();
    public IEnumerable<SymbolicSubExpression> TraversePreOrder() => EnumeratePreOrder();
    public IEnumerable<SymbolicSubExpression> TraversePostOrder() => EnumeratePostOrder();
    public IEnumerable<SymbolicSubExpression> TraverseBreadthFirst() => EnumerateBreadthFirstOrder();

    public SymbolicSubExpression Child(int index)
    {
        var childRootIndex = expression.GetChildRootIndex(rootSymbolIndex, index);
        var childLength = expression.GetSubtreeLength(childRootIndex);
        var childStartIndex = childRootIndex - childLength + 1;
        return new SymbolicSubExpression(expression, childStartIndex, childLength, childRootIndex);
    }

    public bool TryGetNumericLiteral(out NumericLiteral numericLiteral)
    {
        if (Symbol is not NumericLiteralSymbol literal)
        {
            numericLiteral = default;
            return false;
        }

        numericLiteral = literal.Literal;
        return true;
    }

    public bool TryGetVariableReference(out VariableReference variableReference)
    {
        if (Symbol is not VariableSymbol variable)
        {
            variableReference = default;
            return false;
        }

        variableReference = new VariableReference(variable.VariableName, 0);
        return true;
    }

    private IEnumerable<SymbolicSubExpression> EnumerateChildren()
    {
        for (var i = 0; i < Arity; i++)
        {
            yield return Child(i);
        }
    }

    private IEnumerable<SymbolicSubExpression> EnumeratePostOrder()
    {
        for (var i = startIndex; i < startIndex + Length; i++)
        {
            yield return CreateSubExpression(i);
        }
    }

    private IEnumerable<SymbolicSubExpression> EnumeratePreOrder()
    {
        var stack = new Stack<int>();
        stack.Push(rootSymbolIndex);

        while (stack.Count > 0)
        {
            var nodeIndex = stack.Pop();
            yield return CreateSubExpression(nodeIndex);
            PushChildrenRightToLeft(stack, nodeIndex);
        }
    }

    private IEnumerable<SymbolicSubExpression> EnumerateBreadthFirstOrder()
    {
        var queue = new Queue<int>();
        queue.Enqueue(rootSymbolIndex);

        while (queue.Count > 0)
        {
            var nodeIndex = queue.Dequeue();
            yield return CreateSubExpression(nodeIndex);
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
        var arity = expression.GetSymbol(nodeIndex).Arity;
        var childRootIndices = new int[arity];
        for (var i = 0; i < arity; i++)
        {
            childRootIndices[i] = expression.GetChildRootIndex(nodeIndex, i);
        }

        return childRootIndices;
    }

    private SymbolicSubExpression CreateSubExpression(int symbolIndex)
    {
        var length = expression.GetSubtreeLength(symbolIndex);
        var start = symbolIndex - length + 1;
        return new SymbolicSubExpression(expression, start, length, symbolIndex);
    }
}
