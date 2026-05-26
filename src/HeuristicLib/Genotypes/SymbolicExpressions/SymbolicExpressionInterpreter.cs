using System.Numerics.Tensors;

namespace HEAL.HeuristicLib.Genotypes.SymbolicExpressions;

public static class SymbolicExpressionInterpreter
{
    public static double Interpret(SymbolicExpression expression, ReadOnlySpan<double> variableValues)
    {
        if (variableValues.Length != expression.VariableReferences.Count)
        {
            throw new ArgumentException(
                $"Expected {expression.VariableReferences.Count} variable values but received {variableValues.Length}.",
                nameof(variableValues));
        }

        var series = new KeyValuePair<string, Series<double>>[expression.VariableReferences.Count];
        for (var i = 0; i < series.Length; i++)
        {
            var variable = expression.VariableReferences[i];
            series[i] = KeyValuePair.Create(
                variable.Name,
                Series<double>.FromOwnedArray([variableValues[variable.Index]], variable.Name));
        }

        Span<double> result = stackalloc double[1];
        Interpret(expression, new DataFrame(series), result);
        return result[0];
    }

    public static double Interpret(SymbolicExpression expression, IReadOnlyList<string> variableNames, IReadOnlyList<double> variableValues)
    {
        if (variableNames.Count != variableValues.Count)
            throw new ArgumentException("Variable names and values must have the same count.", nameof(variableValues));

        var row = new DataFrame(
            variableNames.Select((name, index) =>
                KeyValuePair.Create(name, Series<double>.FromOwnedArray([variableValues[index]], name))));

        Span<double> result = stackalloc double[1];
        Interpret(expression, row, result);
        return result[0];
    }

    public static double Interpret(SymbolicExpression expression, IReadOnlyDictionary<string, double> variableValues)
    {
        var row = new DataFrame(
            variableValues.Select(pair => KeyValuePair.Create(pair.Key, Series<double>.FromOwnedArray([pair.Value], pair.Key))));

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
            throw new ArgumentException(
                $"Destination must contain at least {data.RowCount} values but contains {destination.Length}.",
                nameof(destination));
        }

        var workspaceLength = GetWorkspaceLength(expression, data);
        if (workspace.Length < workspaceLength)
        {
            throw new ArgumentException(
                $"Workspace must contain at least {workspaceLength} values but contains {workspace.Length}.",
                nameof(workspace));
        }

        var stack = new EvaluationStack(workspace, data.RowCount);
        Execute(expression, data, ref stack);
        stack.Peek().CopyTo(destination);
    }

    public static int GetWorkspaceLength(SymbolicExpression expression, DataFrame data) =>
        expression.Instructions.Count * data.RowCount;

    private static void Execute(SymbolicExpression expression, DataFrame data, ref EvaluationStack stack)
    {
        foreach (var instruction in expression.Instructions)
        {
            switch (instruction.OpCode)
            {
                case SymbolicExpressionOpCode.Variable:
                    data.GetDoubleSeries(expression.VariableReferences[instruction.PayloadIndex].Name).Values.CopyTo(stack.Push());
                    break;
                case SymbolicExpressionOpCode.NumericLiteral:
                    stack.Push().Fill(expression.NumericLiterals[instruction.PayloadIndex].Value);
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
        var right = stack.Pop();
        var left = stack.Peek();
        TensorPrimitives.Add(left, right, left);
    }

    private static void Subtract(ref EvaluationStack stack)
    {
        var right = stack.Pop();
        var left = stack.Peek();
        TensorPrimitives.Subtract(left, right, left);
    }

    private static void Multiply(ref EvaluationStack stack)
    {
        var right = stack.Pop();
        var left = stack.Peek();
        TensorPrimitives.Multiply(left, right, left);
    }

    private static void Divide(ref EvaluationStack stack)
    {
        var right = stack.Pop();
        var left = stack.Peek();
        TensorPrimitives.Divide(left, right, left);
    }

    private static void Log(ref EvaluationStack stack)
    {
        var value = stack.Peek();
        TensorPrimitives.Log(value, value);
    }

    private static void Sqrt(ref EvaluationStack stack)
    {
        var value = stack.Peek();
        TensorPrimitives.Sqrt(value, value);
    }

    private ref struct EvaluationStack
    {
        private readonly Span<double> workspace;
        private readonly int rowCount;
        private int count;

        public EvaluationStack(Span<double> workspace, int rowCount)
        {
            this.workspace = workspace;
            this.rowCount = rowCount;
            count = 0;
        }

        public Span<double> Push() => Slot(count++);

        public Span<double> Pop() => Slot(--count);

        public Span<double> Peek() => Slot(count - 1);

        private Span<double> Slot(int index) => workspace.Slice(index * rowCount, rowCount);
    }
}
