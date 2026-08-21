namespace HEAL.HeuristicLib.Numerics.AutomaticDifferentiation;

internal sealed partial class Execution
{
    /// <remarks>
    /// The sweep runs backwards, so an instruction's own adjoints are complete by the time it is reached and can be
    /// pushed onto its operands. What each operation contributes is declared with the operation; this decides only
    /// which operands are active, gathers the forward values a rule may need, and calls it.
    /// </remarks>
    private void DifferentiateBatch(BatchRange batch, Span<double> jacobian)
    {
        if (program.AdjointSlotCount == 0)
            return;

        batchAdjoints.AsSpan(0, program.AdjointSlotCount * batch.Count).Clear();
        var instructions = program.Instructions;
        var root = instructions[program.RootInstructionIndex];
        if (root.DependsOnParameter)
        {
            GetAdjointsBySlot(root.AdjointSlot, batch.Count).Fill(1.0);
        }

        for (var instructionIndex = instructions.Length - 1; instructionIndex >= 0; instructionIndex--)
        {
            var instruction = instructions[instructionIndex];
            if (!instruction.DependsOnParameter || instruction.Operation == Operation.Parameter)
            {
                continue;
            }

            var arity = OperationCatalog.GetInfo(instruction.Operation).Arity;
            if (arity == 1)
            {
                AccumulateUnaryAdjoints(instructionIndex, instruction, batch);
            }
            else if (arity == 2)
            {
                AccumulateBinaryAdjoints(instructionIndex, instruction, batch);
            }
            else
            {
                throw new NotSupportedException($"Operation {instruction.Operation} has unsupported arity {arity}.");
            }
        }

        var parameterInstructionIndices = program.ParameterInstructionIndices;
        for (var parameterIndex = 0; parameterIndex < parameterInstructionIndices.Length; parameterIndex++)
        {
            var parameterInstruction = instructions[parameterInstructionIndices[parameterIndex]];
            GetAdjointsBySlot(parameterInstruction.AdjointSlot, batch.Count).CopyTo(jacobian.Slice((parameterIndex * rowCount) + batch.Offset, batch.Count));
        }
    }

    /// <remarks>
    /// A passive operand leaves a unary rule nothing to do, so the rule is skipped rather than told about it.
    /// </remarks>
    private void AccumulateUnaryAdjoints(int instructionIndex, Instruction instruction, BatchRange batch)
    {
        if (!TryGetAdjoints(instruction.LeftOperand, batch.Count, out var operandAdjoints))
            return;

        OperationCatalog.GetUnary(instruction.Operation).Adjoint(
            GetAdjointsBySlot(instruction.AdjointSlot, batch.Count),
            GetPrimalValues(instruction.LeftOperand, batch),
            GetPrimalValues(instructionIndex, batch),
            operandAdjoints,
            AdjointScratchFor(batch.Count));
    }

    /// <remarks>
    /// Either operand can be passive while the other is active, so activity is passed through rather than resolved
    /// here. Only both being passive makes the whole rule pointless.
    /// </remarks>
    private void AccumulateBinaryAdjoints(int instructionIndex, Instruction instruction, BatchRange batch)
    {
        var leftIsActive = TryGetAdjoints(instruction.LeftOperand, batch.Count, out var leftAdjoints);
        var rightIsActive = TryGetAdjoints(instruction.RightOperand, batch.Count, out var rightAdjoints);
        if (!leftIsActive && !rightIsActive)
            return;

        OperationCatalog.GetBinary(instruction.Operation).Adjoint(
            GetAdjointsBySlot(instruction.AdjointSlot, batch.Count),
            GetPrimalValues(instruction.LeftOperand, batch),
            GetPrimalValues(instruction.RightOperand, batch),
            GetPrimalValues(instructionIndex, batch),
            leftAdjoints, leftIsActive,
            rightAdjoints, rightIsActive,
            AdjointScratchFor(batch.Count));
    }

    /// <remarks>
    /// The instruction's own forward value is gathered for every rule, including the rules that differentiate from
    /// their operand instead. It is a slot lookup rather than a computation, and paying it unconditionally keeps the
    /// caller from having to know which derivatives reuse the result.
    /// </remarks>
    private Operand GetPrimalValues(int instructionIndex, BatchRange batch)
    {
        var instruction = program.Instructions[instructionIndex];
        return instruction.DependsOnInput
            ? Operand.FromSpan(GetVectorPrimal(instructionIndex, batch))
            : Operand.FromScalar(scalarPrimals[instructionIndex]);
    }

    private bool TryGetAdjoints(int instructionIndex, int count, out Span<double> adjoints)
    {
        var adjointSlot = program.Instructions[instructionIndex].AdjointSlot;
        if (adjointSlot < 0)
        {
            adjoints = [];
            return false;
        }

        adjoints = GetAdjointsBySlot(adjointSlot, count);
        return true;
    }

    private Span<double> GetAdjointsBySlot(int slot, int count) => batchAdjoints.AsSpan(slot * count, count);

    private ScratchSpans AdjointScratchFor(int count) =>
        adjointScratchSpanCount == 0 ? ScratchSpans.None : new ScratchSpans(adjointScratch.AsSpan(0, adjointScratchSpanCount * count), count);
}
