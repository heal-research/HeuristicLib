using System.Buffers;
using HEAL.HeuristicLib.Data;
using HEAL.HeuristicLib.Numerics;

namespace HEAL.HeuristicLib.Encodings.SymbolicExpressions;

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
                ref readonly var info = ref OperationCatalog.GetInfo(instruction.Operation);
                switch (info)
                {
                    case { PayloadKind: PayloadKind.VariableReference }:
                        stack.PushVariable(instruction.PayloadIndex);
                        break;
                    case { PayloadKind: PayloadKind.Constant }:
                        stack.PushScalar(expression.GetConstant(instruction.PayloadIndex));
                        break;
                    case { IsTerminal: false }:
                        Apply(ref stack, instruction.Operation, in info);
                        break;
                    default:
                        throw new NotSupportedException($"Operation {instruction.Operation} cannot appear in an expression program.");
                }
            }

            stack.MaterializeResult(destination.Slice(batchStart, batchSize));
        }
    }

    /// <summary>
    /// Applies one operation, taking its behavior from the operation catalog.
    /// </summary>
    /// <remarks>
    /// The choice between an operand that is one value and one that is a column is made here, once, rather than
    /// inside each operation. An operation supplies the shapes; this picks among them.
    /// </remarks>
    private static void Apply(ref EvaluationStack stack, Operation operation, ref readonly OperationInfo info)
    {
        if (info.Arity == 1)
        {
            ref readonly var unary = ref OperationCatalog.GetUnary(operation);
            var value = stack.Pop();
            if (value.Kind == StackEntryKind.Scalar)
            {
                stack.PushScalar(unary.Scalar(value.Scalar));
            }
            else
            {
                var slotIndex = stack.Count;
                unary.Span(stack.Vector(value), stack.WorkspaceSlot(slotIndex), stack.ScratchSpans(slotIndex + 1, info.ScratchSpanCount));
                stack.PushWorkspace(slotIndex);
            }
        }
        else if (info.Arity == 2)
        {
            ref readonly var binary = ref OperationCatalog.GetBinary(operation);
            var right = stack.Pop();
            var left = stack.Pop();
            if (left.Kind == StackEntryKind.Scalar && right.Kind == StackEntryKind.Scalar)
            {
                stack.PushScalar(binary.Scalar(left.Scalar, right.Scalar));
            }
            else
            {
                var slotIndex = stack.Count;
                var result = stack.WorkspaceSlot(slotIndex);
                var scratch = stack.ScratchSpans(slotIndex + 1, info.ScratchSpanCount);

                var leftOperand = left.Kind == StackEntryKind.Scalar
                    ? Operand.FromScalar(left.Scalar)
                    : Operand.FromSpan(stack.Vector(left));
                var rightOperand = right.Kind == StackEntryKind.Scalar
                    ? Operand.FromScalar(right.Scalar)
                    : Operand.FromSpan(stack.Vector(right));

                OperationCatalog.ApplyToSpan(in binary, leftOperand, rightOperand, result, scratch);
                stack.PushWorkspace(slotIndex);
            }
        }
        else
        {
            throw new NotSupportedException($"Operation {operation} has unsupported arity {info.Arity}.");
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
            ref readonly var info = ref OperationCatalog.GetInfo(instruction.Operation);
            switch (info)
            {
                case { PayloadKind: PayloadKind.VariableReference }:
                    counter.PushVariable();
                    break;
                case { PayloadKind: PayloadKind.Constant }:
                    counter.PushConstant();
                    break;
                case { IsTerminal: false }:
                    counter.ApplyOperation(in info);
                    break;
                default:
                    throw new NotSupportedException($"Operation {instruction.Operation} cannot appear in an expression program.");
            }
        }

        return counter.MaximumSlots * Math.Min(rowCount, BatchSize);
    }

    private static ReadOnlyMemory<double>[] ResolveVariables(CompiledExpression expression, DataFrame data)
    {
        var variables = new ReadOnlyMemory<double>[expression.VariableReferenceCount];
        for (var i = 0; i < variables.Length; i++)
        {
            var variable = expression.GetVariableReference(i);
            variables[variable.Index] = data.Get<double>(variable.Name).Values;
        }

        return variables;
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

        // Which operations need scratch is declared with the operation rather than listed here, so sizing the
        // workspace and using it cannot disagree.
        internal void ApplyOperation(ref readonly OperationInfo info)
        {
            var resultIsVector = false;
            for (var i = 0; i < info.Arity; i++)
                resultIsVector |= vectorStack[--count];

            // One slot for the result plus however many working spans the operation declared. Reading the count from
            // the operation is what keeps sizing the workspace and using it from disagreeing.
            if (resultIsVector)
                MaximumSlots = Math.Max(MaximumSlots, count + 1 + info.ScratchSpanCount);

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

        internal ScratchSpans ScratchSpans(int firstSlot, int count) =>
            count == 0 ? Numerics.ScratchSpans.None : new ScratchSpans(workspace.Slice(firstSlot * batchSize, count * batchSize), batchSize);

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
