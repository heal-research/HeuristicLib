namespace HEAL.HeuristicLib.Genotypes.SymbolicExpressions;

public readonly struct CompiledExpressionSubtree
{
    private readonly CompiledExpressionTree expression;
    private readonly int startIndex;
    private readonly int rootInstructionIndex;

    internal CompiledExpressionSubtree(CompiledExpressionTree expression, int startIndex, int length, int rootInstructionIndex)
    {
        this.expression = expression;
        this.startIndex = startIndex;
        Length = length;
        this.rootInstructionIndex = rootInstructionIndex;
    }

    public Instruction Instruction => expression.GetInstruction(rootInstructionIndex);
    public OpCode OpCode => Instruction.OpCode;
    public int Arity => Instruction.Arity;
    public int Length { get; }
    public int SubtreeLength => Instruction.SubtreeLength;
    public ExpressionLocation Location => new(rootInstructionIndex);

    public IEnumerable<CompiledExpressionSubtree> TraverseChildren() => EnumerateChildren();
    public IEnumerable<CompiledExpressionSubtree> TraversePreOrder() => EnumeratePreOrder();
    public IEnumerable<CompiledExpressionSubtree> TraversePostOrder() => EnumeratePostOrder();
    public IEnumerable<CompiledExpressionSubtree> TraverseBreadthFirst() => EnumerateBreadthFirstOrder();

    public CompiledExpressionSubtree Child(int index)
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
        return new CompiledExpressionSubtree(expression, childStartIndex, childRoot.SubtreeLength, childRootIndex);
    }

    public bool TryGetConstantValue(out double value)
    {
        if (Instruction.OpCode != OpCode.Constant)
        {
            value = default;
            return false;
        }

        value = expression.GetConstant(Instruction.PayloadIndex);
        return true;
    }

    public bool TryGetVariableReference(out VariableReference variableReference)
    {
        if (Instruction.OpCode != OpCode.Variable)
        {
            variableReference = default;
            return false;
        }

        variableReference = expression.GetVariableReference(Instruction.PayloadIndex);
        return true;
    }

    private IEnumerable<CompiledExpressionSubtree> EnumerateChildren()
    {
        for (var i = 0; i < Arity; i++)
        {
            yield return Child(i);
        }
    }

    private IEnumerable<CompiledExpressionSubtree> EnumeratePostOrder()
    {
        for (var i = startIndex; i < startIndex + Length; i++)
        {
            yield return CreateSubtree(i);
        }
    }

    private IEnumerable<CompiledExpressionSubtree> EnumeratePreOrder()
    {
        var stack = new Stack<int>();
        stack.Push(rootInstructionIndex);

        while (stack.Count > 0)
        {
            var nodeIndex = stack.Pop();
            yield return CreateSubtree(nodeIndex);
            PushChildrenRightToLeft(stack, nodeIndex);
        }
    }

    private IEnumerable<CompiledExpressionSubtree> EnumerateBreadthFirstOrder()
    {
        var queue = new Queue<int>();
        queue.Enqueue(rootInstructionIndex);

        while (queue.Count > 0)
        {
            var nodeIndex = queue.Dequeue();
            yield return CreateSubtree(nodeIndex);

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

    private CompiledExpressionSubtree CreateSubtree(int instructionIndex)
    {
        var instruction = expression.GetInstruction(instructionIndex);
        var start = instructionIndex - instruction.SubtreeLength + 1;
        return new CompiledExpressionSubtree(expression, start, instruction.SubtreeLength, instructionIndex);
    }
}
