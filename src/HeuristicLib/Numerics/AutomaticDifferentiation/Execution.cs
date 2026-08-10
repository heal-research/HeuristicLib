using System.Buffers;

namespace HEAL.HeuristicLib.Numerics.AutomaticDifferentiation;

internal sealed partial class Execution : IDisposable
{
    private readonly Program program;
    private readonly ReadOnlyMemory<double>[] inputColumns;
    private readonly int rowCount;
    private readonly int batchCapacity;
    private double[] scalarPrimals;
    private double[] vectorPrimals;
    private double[] batchAdjoints;
    private bool isDisposed;

    internal Execution(Program program, ReadOnlySpan<ReadOnlyMemory<double>> inputColumns, int rowCount, int batchCapacity)
    {
        this.program = program;
        this.inputColumns = inputColumns.ToArray();
        this.rowCount = rowCount;
        this.batchCapacity = batchCapacity;
        var vectorBufferLength = checked(program.VectorPrimalSlotCount * batchCapacity);
        scalarPrimals = ArrayPool<double>.Shared.Rent(program.InstructionCount);
        vectorPrimals = vectorBufferLength == 0 ? [] : ArrayPool<double>.Shared.Rent(vectorBufferLength);
        var adjointBufferLength = checked(program.AdjointSlotCount * batchCapacity);
        batchAdjoints = adjointBufferLength == 0 ? [] : ArrayPool<double>.Shared.Rent(adjointBufferLength);
    }

    internal int ParameterCount => program.ParameterCount;

    internal int RowCount => rowCount;

    internal void ThrowIfDisposed()
    {
        if (isDisposed)
            throw new ObjectDisposedException(nameof(Execution));
    }

    internal void Evaluate(ReadOnlySpan<double> parameters, Span<double> outputs)
    {
        ValidateEvaluationArguments(parameters, outputs);
        EvaluateForward(parameters, outputs);
    }

    internal void EvaluateWithJacobian(ReadOnlySpan<double> parameters, Span<double> outputs, Span<double> jacobian)
    {
        ValidateEvaluationArguments(parameters, outputs);
        var expectedJacobianLength = checked(program.ParameterCount * rowCount);
        if (jacobian.Length != expectedJacobianLength)
            throw new ArgumentException($"Expected {expectedJacobianLength} Jacobian elements but received {jacobian.Length}.", nameof(jacobian));

        if (jacobian.Overlaps(parameters))
            throw new ArgumentException("The Jacobian buffer must not overlap the parameter buffer.", nameof(jacobian));

        if (jacobian.Overlaps(outputs))
            throw new ArgumentException("The Jacobian and output buffers must not overlap.", nameof(jacobian));

        if (OverlapsInputColumn(jacobian))
            throw new ArgumentException("The Jacobian buffer must not overlap a bound input column.", nameof(jacobian));

        EvaluateForwardAndDifferentiate(parameters, outputs, jacobian);
    }

    public void Dispose()
    {
        if (isDisposed)
            return;

        isDisposed = true;
        var buffer = scalarPrimals;
        scalarPrimals = [];
        ArrayPool<double>.Shared.Return(buffer);

        if (vectorPrimals.Length != 0)
        {
            buffer = vectorPrimals;
            vectorPrimals = [];
            ArrayPool<double>.Shared.Return(buffer);
        }

        if (batchAdjoints.Length != 0)
        {
            buffer = batchAdjoints;
            batchAdjoints = [];
            ArrayPool<double>.Shared.Return(buffer);
        }
    }

    private void ValidateEvaluationArguments(ReadOnlySpan<double> parameters, Span<double> outputs)
    {
        ThrowIfDisposed();

        if (parameters.Length != program.ParameterCount)
            throw new ArgumentException($"Expected {program.ParameterCount} parameters but received {parameters.Length}.", nameof(parameters));

        if (outputs.Length != rowCount)
            throw new ArgumentException($"Expected {rowCount} output elements but received {outputs.Length}.", nameof(outputs));

        if (outputs.Overlaps(parameters))
            throw new ArgumentException("The output buffer must not overlap the parameter buffer.", nameof(outputs));

        if (OverlapsInputColumn(outputs))
            throw new ArgumentException("The output buffer must not overlap a bound input column.", nameof(outputs));
    }

    private bool OverlapsInputColumn(ReadOnlySpan<double> buffer)
    {
        for (var inputIndex = 0; inputIndex < inputColumns.Length; inputIndex++)
        {
            if (buffer.Overlaps(inputColumns[inputIndex].Span))
                return true;
        }

        return false;
    }

    private void EvaluateForward(ReadOnlySpan<double> parameters, Span<double> outputs)
    {
        EvaluateScalarInstructions(parameters);
        if (program.InputCount == 0)
        {
            outputs.Fill(scalarPrimals[program.RootInstructionIndex]);
            return;
        }

        for (var offset = 0; offset < rowCount; offset += batchCapacity)
        {
            var batch = new BatchRange(offset, Math.Min(batchCapacity, rowCount - offset));
            EvaluateInputDependentInstructions(batch);
            GetVectorPrimal(program.RootInstructionIndex, batch).CopyTo(outputs.Slice(batch.Offset, batch.Count));
        }
    }

    private void EvaluateForwardAndDifferentiate(ReadOnlySpan<double> parameters, Span<double> outputs, Span<double> jacobian)
    {
        EvaluateScalarInstructions(parameters);
        if (program.InputCount == 0)
        {
            outputs.Fill(scalarPrimals[program.RootInstructionIndex]);
            for (var offset = 0; offset < rowCount; offset += batchCapacity)
                DifferentiateBatch(new BatchRange(offset, Math.Min(batchCapacity, rowCount - offset)), jacobian);

            return;
        }

        for (var offset = 0; offset < rowCount; offset += batchCapacity)
        {
            var batch = new BatchRange(offset, Math.Min(batchCapacity, rowCount - offset));
            EvaluateInputDependentInstructions(batch);
            GetVectorPrimal(program.RootInstructionIndex, batch).CopyTo(outputs.Slice(batch.Offset, batch.Count));
            DifferentiateBatch(batch, jacobian);
        }
    }

    private readonly record struct BatchRange(int Offset, int Count);
}
