namespace HEAL.HeuristicLib.Numerics.AutomaticDifferentiation;

internal sealed partial class Execution
{
    /// <remarks>
    /// An instruction independent of every input has one value rather than a span of them, so the whole subexpression
    /// is evaluated once instead of once per row.
    /// </remarks>
    private void EvaluateScalarInstructions(ReadOnlySpan<double> parameters)
    {
        var instructions = program.Instructions;
        var constants = program.Constants;

        for (var instructionIndex = 0; instructionIndex < instructions.Length; instructionIndex++)
        {
            var instruction = instructions[instructionIndex];
            if (instruction.DependsOnInput)
            {
                continue;
            }

            ref readonly var info = ref OperationCatalog.GetInfo(instruction.Operation);
            scalarPrimals[instructionIndex] = info.PayloadKind switch
            {
                PayloadKind.Parameter => parameters[instruction.PayloadIndex],
                PayloadKind.Constant => constants[instruction.PayloadIndex],
                PayloadKind.VariableReference => throw new InvalidOperationException("An input instruction cannot be evaluated as a scalar."),
                _ => info.Arity switch
                {
                    1 => OperationCatalog.GetUnary(instruction.Operation).Scalar(scalarPrimals[instruction.LeftOperand]),
                    2 => OperationCatalog.GetBinary(instruction.Operation).Scalar(scalarPrimals[instruction.LeftOperand], scalarPrimals[instruction.RightOperand]),
                    _ => throw new NotSupportedException($"Operation {instruction.Operation} has unsupported arity {info.Arity}.")
                }
            };
        }
    }

    private void EvaluateInputDependentInstructions(BatchRange batch)
    {
        var instructions = program.Instructions;
        for (var instructionIndex = 0; instructionIndex < instructions.Length; instructionIndex++)
        {
            var instruction = instructions[instructionIndex];
            if (!instruction.DependsOnInput || instruction.Operation == Operation.Variable)
            {
                continue;
            }

            var destination = GetVectorPrimalSlot(instruction.VectorPrimalSlot, batch.Count);
            var arity = OperationCatalog.GetInfo(instruction.Operation).Arity;
            switch (arity)
            {
                case 1:
                    EvaluateUnary(instruction, batch, destination);
                    break;
                case 2:
                    EvaluateBinary(instruction, batch, destination);
                    break;
                default:
                    throw new NotSupportedException($"Operation {instruction.Operation} has unsupported arity {arity}.");
            }
        }
    }

    /// <remarks>
    /// At least one operand depends on an input, because otherwise this instruction would not, so the operands are
    /// never both single values and the catalog always has a span-producing shape to apply.
    /// </remarks>
    private void EvaluateBinary(Instruction instruction, BatchRange batch, Span<double> destination)
    {
        var left = ResolveOperand(instruction.LeftOperand, batch);
        var right = ResolveOperand(instruction.RightOperand, batch);

        OperationCatalog.ApplyToSpan(
            in OperationCatalog.GetBinary(instruction.Operation), left, right, destination, ScratchFor(batch.Count));
    }

    private Operand ResolveOperand(int operandIndex, BatchRange batch) =>
        program.Instructions[operandIndex].DependsOnInput
            ? Operand.FromSpan(GetVectorPrimal(operandIndex, batch))
            : Operand.FromScalar(scalarPrimals[operandIndex]);

    private void EvaluateUnary(Instruction instruction, BatchRange batch, Span<double> destination) =>
        OperationCatalog.GetUnary(instruction.Operation).Span(GetVectorPrimal(instruction.LeftOperand, batch), destination, ScratchFor(batch.Count));

    private ReadOnlySpan<double> GetVectorPrimal(int instructionIndex, BatchRange batch)
    {
        var instruction = program.Instructions[instructionIndex];
        return instruction.Operation == Operation.Variable
            ? inputColumns[instruction.PayloadIndex].Span.Slice(batch.Offset, batch.Count)
            : GetVectorPrimalSlot(instruction.VectorPrimalSlot, batch.Count);
    }

    private Span<double> GetVectorPrimalSlot(int slot, int count) => vectorPrimals.AsSpan(slot * batchCapacity, count);

    private ScratchSpans ScratchFor(int count) =>
        scratchSpanCount == 0 ? ScratchSpans.None : new ScratchSpans(scratch.AsSpan(0, scratchSpanCount * count), count);
}
