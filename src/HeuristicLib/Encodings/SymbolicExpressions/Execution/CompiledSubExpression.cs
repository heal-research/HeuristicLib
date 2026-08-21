using HEAL.HeuristicLib.Numerics;

namespace HEAL.HeuristicLib.Encodings.SymbolicExpressions;

public readonly struct CompiledSubExpression
{
    private readonly CompiledExpression expression;
    private readonly int rootInstructionIndex;

    internal CompiledSubExpression(CompiledExpression expression, int rootInstructionIndex)
    {
        this.expression = expression;
        this.rootInstructionIndex = rootInstructionIndex;
    }

    public Instruction Instruction => expression.GetInstruction(rootInstructionIndex);
    public Operation Operation => Instruction.Operation;
    public int Arity => Instruction.Arity;
    public int Length => Instruction.SubtreeLength;

    public IEnumerable<CompiledSubExpression> TraverseChildren() => EnumerateChildren();
    public IEnumerable<CompiledSubExpression> TraversePreOrder() => EnumeratePreOrder();
    public IEnumerable<CompiledSubExpression> TraversePostOrder() => EnumeratePostOrder();
    public IEnumerable<CompiledSubExpression> TraverseBreadthFirst() => EnumerateBreadthFirstOrder();

    public CompiledSubExpression Child(int index)
    {
        if (index < 0 || index >= Arity)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        var childRootIndex = rootInstructionIndex - 1;
        for (var i = Arity - 1; i > index; i--)
        {
            childRootIndex -= expression.GetInstruction(childRootIndex).SubtreeLength;
        }

        return new CompiledSubExpression(expression, childRootIndex);
    }

    public bool TryGetConstantValue(out double value)
    {
        if (Instruction.Operation != Operation.Constant)
        {
            value = default;
            return false;
        }

        value = expression.GetConstant(Instruction.PayloadIndex);
        return true;
    }

    public bool TryGetVariableReference(out VariableReference variableReference)
    {
        if (Instruction.Operation != Operation.Variable)
        {
            variableReference = default;
            return false;
        }

        variableReference = expression.GetVariableReference(Instruction.PayloadIndex);
        return true;
    }

    private IEnumerable<CompiledSubExpression> EnumerateChildren()
    {
        for (var i = 0; i < Arity; i++)
        {
            yield return Child(i);
        }
    }

    private IEnumerable<CompiledSubExpression> EnumeratePostOrder()
    {
        var startIndex = rootInstructionIndex - Length + 1;
        for (var i = startIndex; i <= rootInstructionIndex; i++)
        {
            yield return CreateSubExpression(i);
        }
    }

    private IEnumerable<CompiledSubExpression> EnumeratePreOrder()
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

    private IEnumerable<CompiledSubExpression> EnumerateBreadthFirstOrder()
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

    private CompiledSubExpression CreateSubExpression(int instructionIndex)
    {
        return new CompiledSubExpression(expression, instructionIndex);
    }
}
