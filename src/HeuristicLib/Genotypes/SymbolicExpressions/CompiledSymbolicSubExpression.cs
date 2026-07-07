namespace HEAL.HeuristicLib.Genotypes.SymbolicExpressions;

public readonly struct CompiledSymbolicSubExpression
{
    private readonly CompiledSymbolicExpression expression;
    private readonly int startIndex;
    private readonly int rootInstructionIndex;

    internal CompiledSymbolicSubExpression(
        CompiledSymbolicExpression expression,
        int startIndex,
        int length,
        int rootInstructionIndex)
    {
        this.expression = expression;
        this.startIndex = startIndex;
        Length = length;
        this.rootInstructionIndex = rootInstructionIndex;
    }

    public int Length { get; }
    public ExpressionInstruction Instruction => expression.GetInstruction(rootInstructionIndex);
    public SymbolicExpressionOpCode OpCode => Instruction.OpCode;
    public int Arity => Instruction.Arity;
    public int SubtreeLength => Instruction.SubtreeLength;
    public SymbolicExpressionLocation Location => new(rootInstructionIndex);

    public IEnumerable<CompiledSymbolicSubExpression> TraverseChildren() => EnumerateChildren();
    public IEnumerable<CompiledSymbolicSubExpression> TraversePreOrder() => EnumeratePreOrder();
    public IEnumerable<CompiledSymbolicSubExpression> TraversePostOrder() => EnumeratePostOrder();
    public IEnumerable<CompiledSymbolicSubExpression> TraverseBreadthFirst() => EnumerateBreadthFirstOrder();

    public CompiledSymbolicSubExpression Child(int index)
    {
        if ((uint)index >= (uint)Arity)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        var childRootIndex = rootInstructionIndex - 1;
        for (var i = Arity - 1; i > index; i--)
        {
            childRootIndex -= expression.GetInstruction(childRootIndex).SubtreeLength;
        }

        var childRoot = expression.GetInstruction(childRootIndex);
        var childStartIndex = childRootIndex - childRoot.SubtreeLength + 1;
        return new CompiledSymbolicSubExpression(expression, childStartIndex, childRoot.SubtreeLength, childRootIndex);
    }

    public bool TryGetNumericLiteral(out NumericLiteral numericLiteral)
    {
        if (Instruction.OpCode != SymbolicExpressionOpCode.NumericLiteral)
        {
            numericLiteral = default;
            return false;
        }

        numericLiteral = expression.GetNumericLiteral(Instruction.PayloadIndex);
        return true;
    }

    public bool TryGetVariableReference(out VariableReference variableReference)
    {
        if (Instruction.OpCode != SymbolicExpressionOpCode.Variable)
        {
            variableReference = default;
            return false;
        }

        variableReference = expression.GetVariableReference(Instruction.PayloadIndex);
        return true;
    }

    private IEnumerable<CompiledSymbolicSubExpression> EnumerateChildren()
    {
        for (var i = 0; i < Arity; i++)
        {
            yield return Child(i);
        }
    }

    private IEnumerable<CompiledSymbolicSubExpression> EnumeratePostOrder()
    {
        for (var i = startIndex; i < startIndex + Length; i++)
        {
            yield return CreateSubExpression(i);
        }
    }

    private IEnumerable<CompiledSymbolicSubExpression> EnumeratePreOrder()
    {
        var stack = new Stack<int>();
        stack.Push(rootInstructionIndex);

        while (stack.Count > 0)
        {
            var nodeIndex = stack.Pop();
            yield return CreateSubExpression(nodeIndex);
            PushChildrenRightToLeft(stack, nodeIndex);
        }
    }

    private IEnumerable<CompiledSymbolicSubExpression> EnumerateBreadthFirstOrder()
    {
        var queue = new Queue<int>();
        queue.Enqueue(rootInstructionIndex);

        while (queue.Count > 0)
        {
            var nodeIndex = queue.Dequeue();
            yield return CreateSubExpression(nodeIndex);

            EnqueueChildrenLeftToRight(queue, nodeIndex);
        }
    }

    private void PushChildrenRightToLeft(Stack<int> stack, int nodeIndex)
    {
        var arity = expression.GetInstruction(nodeIndex).Arity;
        var rightRootIndex = nodeIndex - 1;
        switch (arity)
        {
            case 0:
                return;
            case 1:
                stack.Push(rightRootIndex);
                return;
            case 2:
                stack.Push(rightRootIndex);
                stack.Push(rightRootIndex - expression.GetInstruction(rightRootIndex).SubtreeLength);
                return;
            default:
                PushChildrenRightToLeftFallback(stack, arity, rightRootIndex);
                return;
        }
    }

    private void EnqueueChildrenLeftToRight(Queue<int> queue, int nodeIndex)
    {
        var arity = expression.GetInstruction(nodeIndex).Arity;
        var rightRootIndex = nodeIndex - 1;
        switch (arity)
        {
            case 0:
                return;
            case 1:
                queue.Enqueue(rightRootIndex);
                return;
            case 2:
                queue.Enqueue(rightRootIndex - expression.GetInstruction(rightRootIndex).SubtreeLength);
                queue.Enqueue(rightRootIndex);
                return;
            default:
                EnqueueChildrenLeftToRightFallback(queue, arity, rightRootIndex);
                return;
        }
    }

    private void PushChildrenRightToLeftFallback(Stack<int> stack, int arity, int rightRootIndex)
    {
        var childRootIndices = GetChildRootIndices(arity, rightRootIndex);
        for (var i = childRootIndices.Length - 1; i >= 0; i--)
        {
            stack.Push(childRootIndices[i]);
        }
    }

    private void EnqueueChildrenLeftToRightFallback(Queue<int> queue, int arity, int rightRootIndex)
    {
        foreach (var childRootIndex in GetChildRootIndices(arity, rightRootIndex))
        {
            queue.Enqueue(childRootIndex);
        }
    }

    private int[] GetChildRootIndices(int arity, int rightRootIndex)
    {
        var childRootIndices = new int[arity];
        var childRootIndex = rightRootIndex;
        for (var i = arity - 1; i >= 0; i--)
        {
            childRootIndices[i] = childRootIndex;
            childRootIndex -= expression.GetInstruction(childRootIndex).SubtreeLength;
        }

        return childRootIndices;
    }

    private CompiledSymbolicSubExpression CreateSubExpression(int instructionIndex)
    {
        var instruction = expression.GetInstruction(instructionIndex);
        var start = instructionIndex - instruction.SubtreeLength + 1;
        return new CompiledSymbolicSubExpression(expression, start, instruction.SubtreeLength, instructionIndex);
    }
}
