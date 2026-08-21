namespace HEAL.HeuristicLib.Numerics.AutomaticDifferentiation;

internal sealed class Program
{
    private readonly Instruction[] instructions;
    private readonly double[] constants;
    private readonly int[] parameterInstructionIndices;

    internal Program(Instruction[] instructions, double[] constants, int[] parameterInstructionIndices, int inputCount, int vectorPrimalSlotCount, int adjointSlotCount)
    {
        this.instructions = instructions;
        this.constants = constants;
        this.parameterInstructionIndices = parameterInstructionIndices;
        InputCount = inputCount;
        VectorPrimalSlotCount = vectorPrimalSlotCount;
        AdjointSlotCount = adjointSlotCount;
    }

    internal ReadOnlySpan<Instruction> Instructions => instructions;

    internal ReadOnlySpan<double> Constants => constants;

    internal ReadOnlySpan<int> ParameterInstructionIndices => parameterInstructionIndices;

    internal int InstructionCount => instructions.Length;

    internal int InputCount { get; }

    internal int ParameterCount => parameterInstructionIndices.Length;

    internal int VectorPrimalSlotCount { get; }

    internal int AdjointSlotCount { get; }

    internal int RootInstructionIndex => instructions.Length - 1;

    internal Execution CreateExecution()
    {
        if (InputCount != 0)
            throw new InvalidOperationException("Input-dependent programs require a bound execution.");

        return CreateExecution([], 1, 1);
    }

    internal Execution CreateExecution(ReadOnlySpan<ReadOnlyMemory<double>> inputColumns, int rowCount, int batchCapacity = 256)
    {
        if (inputColumns.Length != InputCount)
            throw new ArgumentException($"Expected {InputCount} input columns but received {inputColumns.Length}.", nameof(inputColumns));

        if (rowCount <= 0)
            throw new ArgumentOutOfRangeException(nameof(rowCount), rowCount, "The row count must be positive.");

        if (batchCapacity <= 0)
            throw new ArgumentOutOfRangeException(nameof(batchCapacity), batchCapacity, "The batch capacity must be positive.");

        for (var inputIndex = 0; inputIndex < inputColumns.Length; inputIndex++)
        {
            if (inputColumns[inputIndex].Length != rowCount)
                throw new ArgumentException($"Input column {inputIndex} has length {inputColumns[inputIndex].Length}; expected {rowCount}.", nameof(inputColumns));
        }

        return new Execution(this, inputColumns, rowCount, Math.Min(rowCount, batchCapacity));
    }
}
