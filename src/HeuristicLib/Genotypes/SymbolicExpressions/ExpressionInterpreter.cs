using System.Buffers;
using System.Numerics.Tensors;

namespace HEAL.HeuristicLib.Genotypes.SymbolicExpressions;

public static class ExpressionInterpreter
{
    private const int BatchSize = 4096;

    public static double[] Interpret(CompiledExpression expression, DataFrame data)
    {
        var result = new double[data.RowCount];
        Interpret(expression, data, result);
        return result;
    }

    public static void Interpret(CompiledExpression expression, DataFrame data, Span<double> destination)
    {
        var variables = ResolveVariables(expression, data);
        var workspaceLength = GetWorkspaceLength(expression, data.RowCount);
        if (workspaceLength == 0)
        {
            Interpret(expression, variables, data.RowCount, destination, []);
            return;
        }

        var workspace = ArrayPool<double>.Shared.Rent(workspaceLength);
        try
        {
            Interpret(expression, variables, data.RowCount, destination, workspace.AsSpan(0, workspaceLength));
        }
        finally
        {
            ArrayPool<double>.Shared.Return(workspace);
        }
    }

    public static void Interpret(CompiledExpression expression, DataFrame data, Span<double> destination, Span<double> workspace)
    {
        Interpret(expression, ResolveVariables(expression, data), data.RowCount, destination, workspace);
    }

    public static int GetWorkspaceLength(CompiledExpression expression, DataFrame data) => GetWorkspaceLength(expression, data.RowCount);

    private static void Interpret(CompiledExpression expression, ReadOnlyMemory<double>[] variables, int rowCount, Span<double> destination, Span<double> workspace)
    {
        if (destination.Length < rowCount)
            throw new ArgumentException($"Destination must contain at least {rowCount} values but contains {destination.Length}.", nameof(destination));

        var workspaceLength = GetWorkspaceLength(expression, rowCount);
        if (workspace.Length < workspaceLength)
            throw new ArgumentException($"Workspace must contain at least {workspaceLength} values but contains {workspace.Length}.", nameof(workspace));

        if (rowCount == 0)
            return;

        Span<EvaluationStackEntry> entries = expression.InstructionCount <= 256
            ? stackalloc EvaluationStackEntry[expression.InstructionCount]
            : new EvaluationStackEntry[expression.InstructionCount];

        for (var batchStart = 0; batchStart < rowCount; batchStart += BatchSize)
        {
            var batchSize = Math.Min(BatchSize, rowCount - batchStart);
            var stack = new EvaluationStack(workspace, entries, variables, batchStart, batchSize);
            foreach (var instruction in expression.InstructionsInPostOrder)
            {
                switch (instruction.OpCode)
                {
                    case OpCode.Variable:
                        stack.PushVariable(instruction.PayloadIndex);
                        break;
                    case OpCode.Constant:
                        stack.PushScalar(expression.GetConstant(instruction.PayloadIndex));
                        break;
                    case OpCode.Add:
                        ApplyAdd(ref stack);
                        break;
                    case OpCode.Subtract:
                        ApplySubtract(ref stack);
                        break;
                    case OpCode.Multiply:
                        ApplyMultiply(ref stack);
                        break;
                    case OpCode.Divide:
                        ApplyDivide(ref stack);
                        break;
                    case OpCode.Negate:
                        ApplyNegate(ref stack);
                        break;
                    case OpCode.Exp:
                        ApplyExp(ref stack);
                        break;
                    case OpCode.Log:
                        ApplyLog(ref stack);
                        break;
                    case OpCode.Sqrt:
                        ApplySqrt(ref stack);
                        break;
                    default:
                        throw new InvalidOperationException($"Unsupported opcode {instruction.OpCode}.");
                }
            }

            stack.MaterializeResult(destination.Slice(batchStart, batchSize));
        }
    }

    private static int GetWorkspaceLength(CompiledExpression expression, int rowCount)
    {
        Span<bool> vectorStack = expression.InstructionCount <= 256
            ? stackalloc bool[expression.InstructionCount]
            : new bool[expression.InstructionCount];
        var counter = new WorkspaceCounter(vectorStack);
        foreach (var instruction in expression.InstructionsInPostOrder)
        {
            if (instruction.OpCode == OpCode.Variable)
                counter.PushVariable();
            else if (instruction.OpCode == OpCode.Constant)
                counter.PushConstant();
            else
                counter.ApplyOperator(instruction.OpCode);
        }

        return counter.MaximumSlots * Math.Min(rowCount, BatchSize);
    }

    private static ReadOnlyMemory<double>[] ResolveVariables(CompiledExpression expression, DataFrame data)
    {
        var variables = new ReadOnlyMemory<double>[expression.VariableReferenceCount];
        for (var i = 0; i < variables.Length; i++)
        {
            var variable = expression.GetVariableReference(i);
            variables[variable.Index] = data.GetDoubleSeries(variable.Name).Values;
        }

        return variables;
    }

    private static void ApplyAdd(ref EvaluationStack stack)
    {
        var right = stack.Pop();
        var left = stack.Pop();
        if (left.Kind == StackEntryKind.Scalar && right.Kind == StackEntryKind.Scalar)
        {
            stack.PushScalar(left.Scalar + right.Scalar);
            return;
        }

        var slotIndex = stack.Count;
        var result = stack.WorkspaceSlot(slotIndex);
        if (left.Kind == StackEntryKind.Scalar)
            TensorPrimitives.Add(stack.Vector(right), left.Scalar, result);
        else if (right.Kind == StackEntryKind.Scalar)
            TensorPrimitives.Add(stack.Vector(left), right.Scalar, result);
        else
            TensorPrimitives.Add(stack.Vector(left), stack.Vector(right), result);

        stack.PushWorkspace(slotIndex);
    }

    private static void ApplySubtract(ref EvaluationStack stack)
    {
        var right = stack.Pop();
        var left = stack.Pop();
        if (left.Kind == StackEntryKind.Scalar && right.Kind == StackEntryKind.Scalar)
        {
            stack.PushScalar(left.Scalar - right.Scalar);
            return;
        }

        var slotIndex = stack.Count;
        var result = stack.WorkspaceSlot(slotIndex);
        if (left.Kind == StackEntryKind.Scalar)
            TensorPrimitives.Subtract(left.Scalar, stack.Vector(right), result);
        else if (right.Kind == StackEntryKind.Scalar)
            TensorPrimitives.Subtract(stack.Vector(left), right.Scalar, result);
        else
            TensorPrimitives.Subtract(stack.Vector(left), stack.Vector(right), result);

        stack.PushWorkspace(slotIndex);
    }

    private static void ApplyMultiply(ref EvaluationStack stack)
    {
        var right = stack.Pop();
        var left = stack.Pop();
        if (left.Kind == StackEntryKind.Scalar && right.Kind == StackEntryKind.Scalar)
        {
            stack.PushScalar(left.Scalar * right.Scalar);
            return;
        }

        var slotIndex = stack.Count;
        var result = stack.WorkspaceSlot(slotIndex);
        if (left.Kind == StackEntryKind.Scalar)
            TensorPrimitives.Multiply(stack.Vector(right), left.Scalar, result);
        else if (right.Kind == StackEntryKind.Scalar)
            TensorPrimitives.Multiply(stack.Vector(left), right.Scalar, result);
        else
            TensorPrimitives.Multiply(stack.Vector(left), stack.Vector(right), result);

        stack.PushWorkspace(slotIndex);
    }

    private static void ApplyDivide(ref EvaluationStack stack)
    {
        var right = stack.Pop();
        var left = stack.Pop();
        if (left.Kind == StackEntryKind.Scalar && right.Kind == StackEntryKind.Scalar)
        {
            stack.PushScalar(left.Scalar / right.Scalar);
            return;
        }

        var slotIndex = stack.Count;
        var result = stack.WorkspaceSlot(slotIndex);
        if (left.Kind == StackEntryKind.Scalar)
            TensorPrimitives.Divide(left.Scalar, stack.Vector(right), result);
        else if (right.Kind == StackEntryKind.Scalar)
            TensorPrimitives.Divide(stack.Vector(left), right.Scalar, result);
        else
            TensorPrimitives.Divide(stack.Vector(left), stack.Vector(right), result);

        stack.PushWorkspace(slotIndex);
    }

    private static void ApplyNegate(ref EvaluationStack stack)
    {
        var value = stack.Pop();
        if (value.Kind == StackEntryKind.Scalar)
        {
            stack.PushScalar(-value.Scalar);
            return;
        }

        var slotIndex = stack.Count;
        TensorPrimitives.Negate(stack.Vector(value), stack.WorkspaceSlot(slotIndex));
        stack.PushWorkspace(slotIndex);
    }

    private static void ApplyExp(ref EvaluationStack stack)
    {
        var value = stack.Pop();
        if (value.Kind == StackEntryKind.Scalar)
        {
            stack.PushScalar(Math.Exp(value.Scalar));
            return;
        }

        var slotIndex = stack.Count;
        TensorPrimitives.Exp(stack.Vector(value), stack.WorkspaceSlot(slotIndex));
        stack.PushWorkspace(slotIndex);
    }

    private static void ApplyLog(ref EvaluationStack stack)
    {
        var value = stack.Pop();
        if (value.Kind == StackEntryKind.Scalar)
        {
            stack.PushScalar(Math.Log(value.Scalar));
            return;
        }

        var slotIndex = stack.Count;
        TensorPrimitives.Log(stack.Vector(value), stack.WorkspaceSlot(slotIndex));
        stack.PushWorkspace(slotIndex);
    }

    private static void ApplySqrt(ref EvaluationStack stack)
    {
        var value = stack.Pop();
        if (value.Kind == StackEntryKind.Scalar)
        {
            stack.PushScalar(Math.Sqrt(value.Scalar));
            return;
        }

        var slotIndex = stack.Count;
        TensorPrimitives.Sqrt(stack.Vector(value), stack.WorkspaceSlot(slotIndex));
        stack.PushWorkspace(slotIndex);
    }

    private ref struct WorkspaceCounter
    {
        private readonly Span<bool> vectorStack;
        private int count;

        internal WorkspaceCounter(Span<bool> vectorStack)
        {
            this.vectorStack = vectorStack;
        }

        internal int MaximumSlots { get; private set; }
        internal void PushVariable() => vectorStack[count++] = true;
        internal void PushConstant() => vectorStack[count++] = false;

        internal void ApplyOperator(OpCode opCode)
        {
            var arity = OpCodes.GetArity(opCode);
            var resultIsVector = false;
            for (var i = 0; i < arity; i++)
                resultIsVector |= vectorStack[--count];

            if (resultIsVector)
                MaximumSlots = Math.Max(MaximumSlots, count + 1);

            vectorStack[count++] = resultIsVector;
        }
    }

    private ref struct EvaluationStack
    {
        private readonly Span<double> workspace;
        private readonly ReadOnlySpan<ReadOnlyMemory<double>> variables;
        private readonly int batchStart;
        private readonly int batchSize;
        private readonly Span<EvaluationStackEntry> entries;
        private int count;

        internal EvaluationStack(Span<double> workspace, Span<EvaluationStackEntry> entries, ReadOnlySpan<ReadOnlyMemory<double>> variables, int batchStart, int batchSize)
        {
            this.workspace = workspace;
            this.entries = entries;
            this.variables = variables;
            this.batchStart = batchStart;
            this.batchSize = batchSize;
        }

        internal int Count => count;
        internal void PushVariable(int variableIndex) => entries[count++] = new EvaluationStackEntry(StackEntryKind.Variable, variableIndex);
        internal void PushScalar(double value) => entries[count++] = new EvaluationStackEntry(value);
        internal void PushWorkspace(int slotIndex) => entries[count++] = new EvaluationStackEntry(StackEntryKind.Workspace, slotIndex);
        internal EvaluationStackEntry Pop() => entries[--count];
        internal Span<double> WorkspaceSlot(int index) => workspace.Slice(index * batchSize, batchSize);

        internal ReadOnlySpan<double> Vector(EvaluationStackEntry entry)
        {
            return entry.Kind switch
            {
                StackEntryKind.Variable => variables[entry.Id].Span.Slice(batchStart, batchSize),
                StackEntryKind.Workspace => WorkspaceSlot(entry.Id),
                _ => throw new InvalidOperationException("Scalar stack entries do not have vector values.")
            };
        }

        internal void MaterializeResult(Span<double> destination)
        {
            var entry = entries[count - 1];
            if (entry.Kind == StackEntryKind.Scalar)
                destination.Fill(entry.Scalar);
            else
                Vector(entry).CopyTo(destination);
        }
    }

    private readonly struct EvaluationStackEntry
    {
        internal EvaluationStackEntry(StackEntryKind kind, int id)
        {
            Kind = kind;
            Id = id;
            Scalar = 0.0;
        }

        internal EvaluationStackEntry(double scalar)
        {
            Kind = StackEntryKind.Scalar;
            Id = 0;
            Scalar = scalar;
        }

        internal StackEntryKind Kind { get; }
        internal int Id { get; }
        internal double Scalar { get; }
    }

    private enum StackEntryKind
    {
        Variable,
        Workspace,
        Scalar
    }
}
