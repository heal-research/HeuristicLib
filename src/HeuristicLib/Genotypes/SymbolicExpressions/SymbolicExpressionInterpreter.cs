using System.Numerics.Tensors;

namespace HEAL.HeuristicLib.Genotypes.SymbolicExpressions;

public static class SymbolicExpressionInterpreter
{
    private const int DefaultBatchSize = 4096;

    public static double Interpret(SymbolicExpression expression, ReadOnlySpan<double> variableValues)
    {
        if (variableValues.Length != expression.VariableReferenceCount)
        {
            throw new ArgumentException($"Expected {expression.VariableReferenceCount} variable values but received {variableValues.Length}.", nameof(variableValues));
        }

        var series = new KeyValuePair<string, Series<double>>[expression.VariableReferenceCount];
        for (var i = 0; i < series.Length; i++)
        {
            var variable = expression.GetVariableReference(i);
            series[i] = KeyValuePair.Create(variable.Name, Series<double>.FromOwnedArray([variableValues[variable.Index]], variable.Name));
        }

        Span<double> result = stackalloc double[1];
        Interpret(expression, new DataFrame(series), result);
        return result[0];
    }

    public static double Interpret(SymbolicExpression expression, IReadOnlyList<string> variableNames, IReadOnlyList<double> variableValues)
    {
        if (variableNames.Count != variableValues.Count)
            throw new ArgumentException("Variable names and values must have the same count.", nameof(variableValues));

        var row = new DataFrame(variableNames.Select((name, index) => KeyValuePair.Create(name, Series<double>.FromOwnedArray([variableValues[index]], name))));

        Span<double> result = stackalloc double[1];
        Interpret(expression, row, result);
        return result[0];
    }

    public static double Interpret(SymbolicExpression expression, IReadOnlyDictionary<string, double> variableValues)
    {
        var row = new DataFrame(variableValues.Select(pair => KeyValuePair.Create(pair.Key, Series<double>.FromOwnedArray([pair.Value], pair.Key))));

        Span<double> result = stackalloc double[1];
        Interpret(expression, row, result);
        return result[0];
    }

    public static double[] Interpret(SymbolicExpression expression, DataFrame data)
    {
        var result = new double[data.RowCount];
        Interpret(expression, data, result);
        return result;
    }

    public static void Interpret(SymbolicExpression expression, DataFrame data, Span<double> destination)
    {
        Interpret(expression, data, destination, new double[GetWorkspaceLength(expression, data)]);
    }

    public static void Interpret(SymbolicExpression expression, DataFrame data, Span<double> destination, Span<double> workspace)
    {
        if (destination.Length < data.RowCount)
        {
            throw new ArgumentException($"Destination must contain at least {data.RowCount} values but contains {destination.Length}.", nameof(destination));
        }

        var workspaceLength = GetWorkspaceLength(expression, data);
        if (workspace.Length < workspaceLength)
        {
            throw new ArgumentException($"Workspace must contain at least {workspaceLength} values but contains {workspace.Length}.", nameof(workspace));
        }

        var variables = ResolveVariables(expression, data);
        if (data.RowCount == 0)
            return;

        Span<StackEntry> entries = expression.InstructionCount <= 256
            ? stackalloc StackEntry[expression.InstructionCount]
            : new StackEntry[expression.InstructionCount];
        for (var batchStart = 0; batchStart < data.RowCount; batchStart += DefaultBatchSize)
        {
            var batchSize = Math.Min(DefaultBatchSize, data.RowCount - batchStart);
            var stack = new EvaluationStack(workspace, entries, variables, batchStart, batchSize);
            Execute(expression, ref stack);
            stack.MaterializeResult(destination.Slice(batchStart, batchSize));
        }
    }

    public static int GetWorkspaceLength(SymbolicExpression expression, DataFrame data) =>
        GetWorkspaceSlotCount(expression) * Math.Min(data.RowCount, DefaultBatchSize);

    private static int GetWorkspaceSlotCount(SymbolicExpression expression)
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
                case SymbolicExpressionOpCode.Variable:
                    vectorStack[count++] = true;
                    break;
                case SymbolicExpressionOpCode.NumericLiteral:
                    vectorStack[count++] = false;
                    break;
                case SymbolicExpressionOpCode.Log:
                case SymbolicExpressionOpCode.Sqrt:
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

    private static Series<double>[] ResolveVariables(SymbolicExpression expression, DataFrame data)
    {
        var variables = new Series<double>[expression.VariableReferenceCount];
        for (var i = 0; i < variables.Length; i++)
        {
            var variable = expression.GetVariableReference(i);
            variables[variable.Index] = data.GetDoubleSeries(variable.Name);
        }

        return variables;
    }

    private static void Execute(SymbolicExpression expression, ref EvaluationStack stack)
    {
        foreach (var instruction in expression.InstructionsInPostOrder)
        {
            switch (instruction.OpCode)
            {
                case SymbolicExpressionOpCode.Variable:
                    stack.PushVariable(instruction.PayloadIndex);
                    break;
                case SymbolicExpressionOpCode.NumericLiteral:
                    stack.PushScalar(expression.GetNumericLiteral(instruction.PayloadIndex).Value);
                    break;
                case SymbolicExpressionOpCode.Add:
                    Add(ref stack);
                    break;
                case SymbolicExpressionOpCode.Subtract:
                    Subtract(ref stack);
                    break;
                case SymbolicExpressionOpCode.Multiply:
                    Multiply(ref stack);
                    break;
                case SymbolicExpressionOpCode.Divide:
                    Divide(ref stack);
                    break;
                case SymbolicExpressionOpCode.Log:
                    Log(ref stack);
                    break;
                case SymbolicExpressionOpCode.Sqrt:
                    Sqrt(ref stack);
                    break;
                default:
                    throw new InvalidOperationException($"Unsupported opcode {instruction.OpCode}.");
            }
        }
    }

    private static void Add(ref EvaluationStack stack)
    {
        // TODO: Consider fusing a deferred multiply operand with TensorPrimitives.MultiplyAdd.
        // This requires representing multiplication as a lazy stack entry until Add consumes it.
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
        private readonly ReadOnlySpan<Series<double>> variables;
        private readonly int batchStart;
        private readonly int batchSize;
        private readonly Span<StackEntry> entries;
        private int count;

        public EvaluationStack(Span<double> workspace, Span<StackEntry> entries, ReadOnlySpan<Series<double>> variables, int batchStart, int batchSize)
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
                StackEntryKind.Variable => variables[entry.Id].Values.Slice(batchStart, batchSize),
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
