using System.Buffers;
using System.Numerics.Tensors;

namespace HEAL.HeuristicLib.Genotypes.SymbolicExpressions;

public static class ExpressionInterpreter
{
    private const int DefaultBatchSize = 4096;

    public static double[] Interpret(CompiledExpressionTree expression, DataFrame data)
    {
        var resolvedVariables = ResolveVariables(expression, data);
        var rowCount = data.RowCount;
        var result = new double[rowCount];
        Interpret(expression, resolvedVariables, rowCount, result);
        return result;
    }

    public static void Interpret(CompiledExpressionTree expression, DataFrame data, Span<double> destination)
    {
        Interpret(expression, ResolveVariables(expression, data), data.RowCount, destination);
    }

    public static void Interpret(CompiledExpressionTree expression, DataFrame data, Span<double> destination, Span<double> workspace)
    {
        Interpret(expression, ResolveVariables(expression, data), data.RowCount, destination, workspace);
    }

    public static int GetWorkspaceLength(CompiledExpressionTree expression, DataFrame data) =>
        GetWorkspaceLength(expression, data.RowCount);

    private static void Interpret(CompiledExpressionTree expression, ReadOnlyMemory<double>[] variables, int rowCount, Span<double> destination)
    {
        var workspaceLength = GetWorkspaceLength(expression, rowCount);
        if (workspaceLength == 0)
        {
            Interpret(expression, variables, rowCount, destination, []);
            return;
        }

        var workspace = ArrayPool<double>.Shared.Rent(workspaceLength);
        try
        {
            Interpret(expression, variables, rowCount, destination, workspace.AsSpan(0, workspaceLength));
        }
        finally
        {
            ArrayPool<double>.Shared.Return(workspace);
        }
    }

    private static void Interpret(CompiledExpressionTree expression, ReadOnlyMemory<double>[] variables, int rowCount, Span<double> destination, Span<double> workspace)
    {
        if (destination.Length < rowCount)
        {
            throw new ArgumentException($"Destination must contain at least {rowCount} values but contains {destination.Length}.", nameof(destination));
        }

        var workspaceLength = GetWorkspaceLength(expression, rowCount);
        if (workspace.Length < workspaceLength)
        {
            throw new ArgumentException($"Workspace must contain at least {workspaceLength} values but contains {workspace.Length}.", nameof(workspace));
        }

        if (rowCount == 0)
            return;

        Span<StackEntry> entries = expression.InstructionCount <= 256
            ? stackalloc StackEntry[expression.InstructionCount]
            : new StackEntry[expression.InstructionCount];
        for (var batchStart = 0; batchStart < rowCount; batchStart += DefaultBatchSize)
        {
            var batchSize = Math.Min(DefaultBatchSize, rowCount - batchStart);
            var stack = new EvaluationStack(workspace, entries, variables, batchStart, batchSize);
            Execute(expression, ref stack);
            stack.MaterializeResult(destination.Slice(batchStart, batchSize));
        }
    }

    private static int GetWorkspaceLength(CompiledExpressionTree expression, int rowCount) =>
        GetWorkspaceSlotCount(expression) * Math.Min(rowCount, DefaultBatchSize);

    private static int GetWorkspaceSlotCount(CompiledExpressionTree expression)
    {
        Span<bool> vectorStack = expression.InstructionCount <= 256
            ? stackalloc bool[expression.InstructionCount]
            : new bool[expression.InstructionCount];
        var count = 0;
        var maxWorkspaceSlots = 0;

        foreach (var instruction in expression.InstructionsInPostOrder)
        {
            switch (instruction.OpCode)
            {
                case OpCode.Variable:
                    vectorStack[count++] = true;
                    break;
                case OpCode.Constant:
                    vectorStack[count++] = false;
                    break;
                case OpCode.Log:
                case OpCode.Sqrt:
                case OpCode.Negate:
                case OpCode.Exp:
                    var unaryOperandIsVector = vectorStack[--count];
                    if (unaryOperandIsVector)
                        maxWorkspaceSlots = Math.Max(maxWorkspaceSlots, count + 1);

                    vectorStack[count++] = unaryOperandIsVector;
                    break;
                default:
                    var rightOperandIsVector = vectorStack[--count];
                    var leftOperandIsVector = vectorStack[--count];
                    var resultIsVector = leftOperandIsVector || rightOperandIsVector;
                    if (resultIsVector)
                        maxWorkspaceSlots = Math.Max(maxWorkspaceSlots, count + 1);

                    vectorStack[count++] = resultIsVector;
                    break;
            }
        }

        return maxWorkspaceSlots;
    }

    private static ReadOnlyMemory<double>[] ResolveVariables(CompiledExpressionTree expression, DataFrame data)
    {
        var variables = new ReadOnlyMemory<double>[expression.VariableReferenceCount];
        for (var i = 0; i < variables.Length; i++)
        {
            var variable = expression.GetVariableReference(i);
            variables[variable.Index] = data.GetDoubleSeries(variable.Name).Values;
        }

        return variables;
    }

    private static void Execute(CompiledExpressionTree expression, ref EvaluationStack stack)
    {
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
                    Add(ref stack);
                    break;
                case OpCode.Subtract:
                    Subtract(ref stack);
                    break;
                case OpCode.Multiply:
                    Multiply(ref stack);
                    break;
                case OpCode.Divide:
                    Divide(ref stack);
                    break;
                case OpCode.Negate:
                    Negate(ref stack);
                    break;
                case OpCode.Exp:
                    Exp(ref stack);
                    break;
                case OpCode.Log:
                    Log(ref stack);
                    break;
                case OpCode.Sqrt:
                    Sqrt(ref stack);
                    break;
                default:
                    throw new InvalidOperationException($"Unsupported opcode {instruction.OpCode}.");
            }
        }
    }

    private static void Add(ref EvaluationStack stack)
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

    private static void Subtract(ref EvaluationStack stack)
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

    private static void Multiply(ref EvaluationStack stack)
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

    private static void Divide(ref EvaluationStack stack)
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

    private static void Negate(ref EvaluationStack stack)
    {
        var value = stack.Pop();
        if (value.Kind == StackEntryKind.Scalar)
        {
            stack.PushScalar(-value.Scalar);
            return;
        }

        var slotIndex = stack.Count;
        var result = stack.WorkspaceSlot(slotIndex);
        TensorPrimitives.Negate(stack.Vector(value), result);
        stack.PushWorkspace(slotIndex);
    }

    private static void Exp(ref EvaluationStack stack)
    {
        var value = stack.Pop();
        if (value.Kind == StackEntryKind.Scalar)
        {
            stack.PushScalar(Math.Exp(value.Scalar));
            return;
        }

        var slotIndex = stack.Count;
        var result = stack.WorkspaceSlot(slotIndex);
        TensorPrimitives.Exp(stack.Vector(value), result);
        stack.PushWorkspace(slotIndex);
    }

    private static void Log(ref EvaluationStack stack)
    {
        var value = stack.Pop();
        if (value.Kind == StackEntryKind.Scalar)
        {
            stack.PushScalar(Math.Log(value.Scalar));
            return;
        }

        var slotIndex = stack.Count;
        var result = stack.WorkspaceSlot(slotIndex);
        TensorPrimitives.Log(stack.Vector(value), result);
        stack.PushWorkspace(slotIndex);
    }

    private static void Sqrt(ref EvaluationStack stack)
    {
        var value = stack.Pop();
        if (value.Kind == StackEntryKind.Scalar)
        {
            stack.PushScalar(Math.Sqrt(value.Scalar));
            return;
        }

        var slotIndex = stack.Count;
        var result = stack.WorkspaceSlot(slotIndex);
        TensorPrimitives.Sqrt(stack.Vector(value), result);
        stack.PushWorkspace(slotIndex);
    }

    private ref struct EvaluationStack
    {
        private readonly Span<double> workspace;
        private readonly ReadOnlySpan<ReadOnlyMemory<double>> variables;
        private readonly int batchStart;
        private readonly int batchSize;
        private readonly Span<StackEntry> entries;
        private int count;

        public EvaluationStack(Span<double> workspace, Span<StackEntry> entries, ReadOnlySpan<ReadOnlyMemory<double>> variables, int batchStart, int batchSize)
        {
            this.workspace = workspace;
            this.entries = entries;
            this.variables = variables;
            this.batchStart = batchStart;
            this.batchSize = batchSize;
            count = 0;
        }

        public int Count => count;

        public void PushVariable(int variableIndex)
        {
            entries[count++] = new StackEntry(StackEntryKind.Variable, variableIndex);
        }

        public void PushScalar(double value)
        {
            entries[count++] = new StackEntry(value);
        }

        public void PushWorkspace(int slotIndex)
        {
            entries[count++] = new StackEntry(StackEntryKind.Workspace, slotIndex);
        }

        public StackEntry Pop() => entries[--count];

        public StackEntry Peek() => entries[count - 1];

        public Span<double> WorkspaceSlot(int index) => workspace.Slice(index * batchSize, batchSize);

        public ReadOnlySpan<double> Vector(StackEntry entry)
        {
            return entry.Kind switch
            {
                StackEntryKind.Variable => variables[entry.Id].Span.Slice(batchStart, batchSize),
                StackEntryKind.Workspace => WorkspaceSlot(entry.Id),
                _ => throw new InvalidOperationException("Scalar stack entries do not have vector values.")
            };
        }

        public void MaterializeResult(Span<double> destination)
        {
            var entry = Peek();
            if (entry.Kind == StackEntryKind.Scalar)
                destination.Fill(entry.Scalar);
            else
                Vector(entry).CopyTo(destination);
        }
    }

    private readonly struct StackEntry
    {
        public StackEntry(StackEntryKind kind, int id)
        {
            Kind = kind;
            Id = id;
            Scalar = 0.0;
        }

        public StackEntry(double scalar)
        {
            Kind = StackEntryKind.Scalar;
            Id = 0;
            Scalar = scalar;
        }

        public StackEntryKind Kind { get; }
        public int Id { get; }
        public double Scalar { get; }
    }

    private enum StackEntryKind
    {
        Variable,
        Workspace,
        Scalar
    }
}
