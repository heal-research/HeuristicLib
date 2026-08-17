using System.Numerics;
using System.Numerics.Tensors;

namespace HEAL.HeuristicLib.Numerics.AutomaticDifferentiation;

internal sealed partial class Execution
{
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

            var upstreamAdjoints = GetAdjointsBySlot(instruction.AdjointSlot, batch.Count);
            switch (instruction.Operation)
            {
                case Operation.Add:
                    AccumulateAdjointsForAdd(instructionIndex, upstreamAdjoints, batch);
                    break;
                case Operation.Subtract:
                    AccumulateAdjointsForSubtract(instructionIndex, upstreamAdjoints, batch);
                    break;
                case Operation.Multiply:
                    AccumulateAdjointsForMultiply(instructionIndex, upstreamAdjoints, batch);
                    break;
                case Operation.Divide:
                    AccumulateAdjointsForDivide(instructionIndex, upstreamAdjoints, batch);
                    break;
                case Operation.Negate:
                    AccumulateAdjointsForNegate(instructionIndex, upstreamAdjoints, batch);
                    break;
                case Operation.Exp:
                    AccumulateAdjointsForExp(instructionIndex, upstreamAdjoints, batch);
                    break;
                case Operation.Log:
                    AccumulateAdjointsForLog(instructionIndex, upstreamAdjoints, batch);
                    break;
                case Operation.Sqrt:
                    AccumulateAdjointsForSqrt(instructionIndex, upstreamAdjoints, batch);
                    break;
                case Operation.Abs:
                    AccumulateAdjointsForAbs(instructionIndex, upstreamAdjoints, batch);
                    break;
                case Operation.Square:
                    AccumulateAdjointsForSquare(instructionIndex, upstreamAdjoints, batch);
                    break;
                case Operation.Cube:
                    AccumulateAdjointsForCube(instructionIndex, upstreamAdjoints, batch);
                    break;
                case Operation.CubeRoot:
                    AccumulateAdjointsForCubeRoot(instructionIndex, upstreamAdjoints, batch);
                    break;
                case Operation.Power:
                    AccumulateAdjointsForPower(instructionIndex, upstreamAdjoints, batch);
                    break;
                case Operation.Root:
                    AccumulateAdjointsForRoot(instructionIndex, upstreamAdjoints, batch);
                    break;
                case Operation.AnalyticQuotient:
                    AccumulateAdjointsForAnalyticQuotient(instructionIndex, upstreamAdjoints, batch);
                    break;
                case Operation.Sin:
                    AccumulateAdjointsForSin(instructionIndex, upstreamAdjoints, batch);
                    break;
                case Operation.Cos:
                    AccumulateAdjointsForCos(instructionIndex, upstreamAdjoints, batch);
                    break;
                case Operation.Tan:
                    AccumulateAdjointsForTan(instructionIndex, upstreamAdjoints, batch);
                    break;
                case Operation.Tanh:
                    AccumulateAdjointsForTanh(instructionIndex, upstreamAdjoints, batch);
                    break;
                default:
                    throw new InvalidOperationException($"Unsupported reverse automatic-differentiation operation: {instruction.Operation}.");
            }
        }

        var parameterInstructionIndices = program.ParameterInstructionIndices;
        for (var parameterIndex = 0; parameterIndex < parameterInstructionIndices.Length; parameterIndex++)
        {
            var parameterInstruction = instructions[parameterInstructionIndices[parameterIndex]];
            GetAdjointsBySlot(parameterInstruction.AdjointSlot, batch.Count).CopyTo(jacobian.Slice((parameterIndex * rowCount) + batch.Offset, batch.Count));
        }
    }

    private void AccumulateAdjointsForAdd(int instructionIndex, ReadOnlySpan<double> upstreamAdjoints, BatchRange batch)
    {
        var instruction = program.Instructions[instructionIndex];
        AccumulateScaled(instruction.LeftOperand, upstreamAdjoints, 1.0, batch.Count);
        AccumulateScaled(instruction.RightOperand, upstreamAdjoints, 1.0, batch.Count);
    }

    private void AccumulateAdjointsForSubtract(int instructionIndex, ReadOnlySpan<double> upstreamAdjoints, BatchRange batch)
    {
        var instruction = program.Instructions[instructionIndex];
        AccumulateScaled(instruction.LeftOperand, upstreamAdjoints, 1.0, batch.Count);
        AccumulateScaled(instruction.RightOperand, upstreamAdjoints, -1.0, batch.Count);
    }

    private void AccumulateAdjointsForMultiply(int instructionIndex, ReadOnlySpan<double> upstreamAdjoints, BatchRange batch)
    {
        var instruction = program.Instructions[instructionIndex];
        AccumulateWithPrimalFactor(instruction.LeftOperand, upstreamAdjoints, GetPrimalValues(instruction.RightOperand, batch), batch.Count);
        AccumulateWithPrimalFactor(instruction.RightOperand, upstreamAdjoints, GetPrimalValues(instruction.LeftOperand, batch), batch.Count);
    }

    private void AccumulateAdjointsForDivide(int instructionIndex, ReadOnlySpan<double> upstreamAdjoints, BatchRange batch)
    {
        var instruction = program.Instructions[instructionIndex];
        var numerator = GetPrimalValues(instruction.LeftOperand, batch);
        var denominator = GetPrimalValues(instruction.RightOperand, batch);
        AccumulateDividedByPrimal(instruction.LeftOperand, upstreamAdjoints, denominator, batch.Count);

        if (TryGetAdjoints(instruction.RightOperand, batch.Count, out var denominatorAdjoints))
        {
            AccumulateDivisionDenominatorAdjoints(upstreamAdjoints, numerator, denominator, denominatorAdjoints);
        }
    }

    private void AccumulateAdjointsForNegate(int instructionIndex, ReadOnlySpan<double> upstreamAdjoints, BatchRange batch)
    {
        var instruction = program.Instructions[instructionIndex];
        AccumulateScaled(instruction.LeftOperand, upstreamAdjoints, -1.0, batch.Count);
    }

    private void AccumulateAdjointsForExp(int instructionIndex, ReadOnlySpan<double> upstreamAdjoints, BatchRange batch)
    {
        var instruction = program.Instructions[instructionIndex];
        AccumulateWithPrimalFactor(instruction.LeftOperand, upstreamAdjoints, GetPrimalValues(instructionIndex, batch), batch.Count);
    }

    private void AccumulateAdjointsForLog(int instructionIndex, ReadOnlySpan<double> upstreamAdjoints, BatchRange batch)
    {
        var instruction = program.Instructions[instructionIndex];
        AccumulateDividedByPrimal(instruction.LeftOperand, upstreamAdjoints, GetPrimalValues(instruction.LeftOperand, batch), batch.Count);
    }

    private void AccumulateAdjointsForSin(int instructionIndex, ReadOnlySpan<double> upstreamAdjoints, BatchRange batch)
    {
        var instruction = program.Instructions[instructionIndex];
        if (!TryGetAdjoints(instruction.LeftOperand, batch.Count, out var operandAdjoints))
            return;

        var operand = GetPrimalValues(instruction.LeftOperand, batch);
        if (operand.IsScalar)
        {
            TensorPrimitives.MultiplyAdd(upstreamAdjoints, Math.Cos(operand.Scalar), operandAdjoints, operandAdjoints);
            return;
        }

        var elementIndex = 0;
        var vectorizedElementCount = GetVectorizedElementCount(batch.Count);
        for (; elementIndex < vectorizedElementCount; elementIndex += Vector<double>.Count)
        {
            var upstream = new Vector<double>(upstreamAdjoints.Slice(elementIndex));
            var primal = new Vector<double>(operand.Vector.Slice(elementIndex));
            var adjoints = new Vector<double>(operandAdjoints.Slice(elementIndex));
            (adjoints + (upstream * Vector.Cos(primal))).CopyTo(operandAdjoints.Slice(elementIndex));
        }

        for (; elementIndex < batch.Count; elementIndex++)
        {
            operandAdjoints[elementIndex] += upstreamAdjoints[elementIndex] * Math.Cos(operand.Vector[elementIndex]);
        }
    }

    private void AccumulateAdjointsForCos(int instructionIndex, ReadOnlySpan<double> upstreamAdjoints, BatchRange batch)
    {
        var instruction = program.Instructions[instructionIndex];
        if (!TryGetAdjoints(instruction.LeftOperand, batch.Count, out var operandAdjoints))
            return;

        var operand = GetPrimalValues(instruction.LeftOperand, batch);
        if (operand.IsScalar)
        {
            TensorPrimitives.MultiplyAdd(upstreamAdjoints, -Math.Sin(operand.Scalar), operandAdjoints, operandAdjoints);
            return;
        }

        var elementIndex = 0;
        var vectorizedElementCount = GetVectorizedElementCount(batch.Count);
        for (; elementIndex < vectorizedElementCount; elementIndex += Vector<double>.Count)
        {
            var upstream = new Vector<double>(upstreamAdjoints.Slice(elementIndex));
            var primal = new Vector<double>(operand.Vector.Slice(elementIndex));
            var adjoints = new Vector<double>(operandAdjoints.Slice(elementIndex));
            (adjoints - (upstream * Vector.Sin(primal))).CopyTo(operandAdjoints.Slice(elementIndex));
        }

        for (; elementIndex < batch.Count; elementIndex++)
        {
            operandAdjoints[elementIndex] -= upstreamAdjoints[elementIndex] * Math.Sin(operand.Vector[elementIndex]);
        }
    }

    private void AccumulateAdjointsForTan(int instructionIndex, ReadOnlySpan<double> upstreamAdjoints, BatchRange batch)
    {
        var instruction = program.Instructions[instructionIndex];
        if (!TryGetAdjoints(instruction.LeftOperand, batch.Count, out var operandAdjoints))
            return;

        var output = GetPrimalValues(instructionIndex, batch);
        if (output.IsScalar)
        {
            TensorPrimitives.MultiplyAdd(upstreamAdjoints, 1.0 + (output.Scalar * output.Scalar), operandAdjoints, operandAdjoints);
            return;
        }

        var elementIndex = 0;
        var vectorizedElementCount = GetVectorizedElementCount(batch.Count);
        for (; elementIndex < vectorizedElementCount; elementIndex += Vector<double>.Count)
        {
            var upstream = new Vector<double>(upstreamAdjoints.Slice(elementIndex));
            var primal = new Vector<double>(output.Vector.Slice(elementIndex));
            var adjoints = new Vector<double>(operandAdjoints.Slice(elementIndex));
            (adjoints + (upstream * (Vector<double>.One + (primal * primal)))).CopyTo(operandAdjoints.Slice(elementIndex));
        }

        for (; elementIndex < batch.Count; elementIndex++)
        {
            operandAdjoints[elementIndex] += upstreamAdjoints[elementIndex] * (1.0 + (output.Vector[elementIndex] * output.Vector[elementIndex]));
        }
    }

    private void AccumulateAdjointsForTanh(int instructionIndex, ReadOnlySpan<double> upstreamAdjoints, BatchRange batch)
    {
        var instruction = program.Instructions[instructionIndex];
        if (!TryGetAdjoints(instruction.LeftOperand, batch.Count, out var operandAdjoints))
            return;

        var output = GetPrimalValues(instructionIndex, batch);
        if (output.IsScalar)
        {
            TensorPrimitives.MultiplyAdd(upstreamAdjoints, 1.0 - (output.Scalar * output.Scalar), operandAdjoints, operandAdjoints);
            return;
        }

        var elementIndex = 0;
        var vectorizedElementCount = GetVectorizedElementCount(batch.Count);
        for (; elementIndex < vectorizedElementCount; elementIndex += Vector<double>.Count)
        {
            var upstream = new Vector<double>(upstreamAdjoints.Slice(elementIndex));
            var primal = new Vector<double>(output.Vector.Slice(elementIndex));
            var adjoints = new Vector<double>(operandAdjoints.Slice(elementIndex));
            (adjoints + (upstream * (Vector<double>.One - (primal * primal)))).CopyTo(operandAdjoints.Slice(elementIndex));
        }

        for (; elementIndex < batch.Count; elementIndex++)
        {
            operandAdjoints[elementIndex] += upstreamAdjoints[elementIndex] * (1.0 - (output.Vector[elementIndex] * output.Vector[elementIndex]));
        }
    }

    /// <remarks>
    /// The derivative of <c>sqrt(x)</c> is <c>1 / (2 * sqrt(x))</c>, which is <c>0.5 / output</c>, so the retained
    /// output is used rather than recomputing a root. At <c>x = 0</c> this divides by zero and yields an infinite
    /// derivative, which is the true one; ordinary IEEE propagation applies as everywhere else in the engine.
    /// </remarks>
    private void AccumulateAdjointsForSqrt(int instructionIndex, ReadOnlySpan<double> upstreamAdjoints, BatchRange batch)
    {
        var instruction = program.Instructions[instructionIndex];
        if (!TryGetAdjoints(instruction.LeftOperand, batch.Count, out var operandAdjoints))
            return;

        var output = GetPrimalValues(instructionIndex, batch);
        if (output.IsScalar)
        {
            TensorPrimitives.MultiplyAdd(upstreamAdjoints, 0.5 / output.Scalar, operandAdjoints, operandAdjoints);
            return;
        }

        var elementIndex = 0;
        var vectorizedElementCount = GetVectorizedElementCount(batch.Count);
        var half = new Vector<double>(0.5);
        for (; elementIndex < vectorizedElementCount; elementIndex += Vector<double>.Count)
        {
            var upstream = new Vector<double>(upstreamAdjoints.Slice(elementIndex));
            var primal = new Vector<double>(output.Vector.Slice(elementIndex));
            var adjoints = new Vector<double>(operandAdjoints.Slice(elementIndex));
            (adjoints + (upstream * (half / primal))).CopyTo(operandAdjoints.Slice(elementIndex));
        }

        for (; elementIndex < batch.Count; elementIndex++)
        {
            operandAdjoints[elementIndex] += upstreamAdjoints[elementIndex] * (0.5 / output.Vector[elementIndex]);
        }
    }

    /// <remarks>
    /// The derivative of <c>abs(x)</c> is <c>sign(x)</c>. It does not exist at <c>x = 0</c>, where this returns zero:
    /// that is the subgradient the established automatic-differentiation frameworks also choose, and it keeps the
    /// derivative finite where the one-sided limits disagree.
    /// </remarks>
    private void AccumulateAdjointsForAbs(int instructionIndex, ReadOnlySpan<double> upstreamAdjoints, BatchRange batch)
    {
        var instruction = program.Instructions[instructionIndex];
        if (!TryGetAdjoints(instruction.LeftOperand, batch.Count, out var operandAdjoints))
            return;

        var operand = GetPrimalValues(instruction.LeftOperand, batch);
        if (operand.IsScalar)
        {
            TensorPrimitives.MultiplyAdd(upstreamAdjoints, Math.Sign(operand.Scalar), operandAdjoints, operandAdjoints);
            return;
        }

        for (var elementIndex = 0; elementIndex < batch.Count; elementIndex++)
        {
            operandAdjoints[elementIndex] += upstreamAdjoints[elementIndex] * Math.Sign(operand.Vector[elementIndex]);
        }
    }

    /// <remarks>The derivative of <c>x * x</c> is <c>2x</c>.</remarks>
    private void AccumulateAdjointsForSquare(int instructionIndex, ReadOnlySpan<double> upstreamAdjoints, BatchRange batch)
    {
        var instruction = program.Instructions[instructionIndex];
        if (!TryGetAdjoints(instruction.LeftOperand, batch.Count, out var operandAdjoints))
            return;

        var operand = GetPrimalValues(instruction.LeftOperand, batch);
        if (operand.IsScalar)
        {
            TensorPrimitives.MultiplyAdd(upstreamAdjoints, 2.0 * operand.Scalar, operandAdjoints, operandAdjoints);
            return;
        }

        var elementIndex = 0;
        var vectorizedElementCount = GetVectorizedElementCount(batch.Count);
        var two = new Vector<double>(2.0);
        for (; elementIndex < vectorizedElementCount; elementIndex += Vector<double>.Count)
        {
            var upstream = new Vector<double>(upstreamAdjoints.Slice(elementIndex));
            var primal = new Vector<double>(operand.Vector.Slice(elementIndex));
            var adjoints = new Vector<double>(operandAdjoints.Slice(elementIndex));
            (adjoints + (upstream * two * primal)).CopyTo(operandAdjoints.Slice(elementIndex));
        }

        for (; elementIndex < batch.Count; elementIndex++)
        {
            operandAdjoints[elementIndex] += upstreamAdjoints[elementIndex] * 2.0 * operand.Vector[elementIndex];
        }
    }

    /// <remarks>The derivative of <c>x * x * x</c> is <c>3x²</c>.</remarks>
    private void AccumulateAdjointsForCube(int instructionIndex, ReadOnlySpan<double> upstreamAdjoints, BatchRange batch)
    {
        var instruction = program.Instructions[instructionIndex];
        if (!TryGetAdjoints(instruction.LeftOperand, batch.Count, out var operandAdjoints))
            return;

        var operand = GetPrimalValues(instruction.LeftOperand, batch);
        if (operand.IsScalar)
        {
            TensorPrimitives.MultiplyAdd(upstreamAdjoints, 3.0 * operand.Scalar * operand.Scalar, operandAdjoints, operandAdjoints);
            return;
        }

        var elementIndex = 0;
        var vectorizedElementCount = GetVectorizedElementCount(batch.Count);
        var three = new Vector<double>(3.0);
        for (; elementIndex < vectorizedElementCount; elementIndex += Vector<double>.Count)
        {
            var upstream = new Vector<double>(upstreamAdjoints.Slice(elementIndex));
            var primal = new Vector<double>(operand.Vector.Slice(elementIndex));
            var adjoints = new Vector<double>(operandAdjoints.Slice(elementIndex));
            (adjoints + (upstream * three * primal * primal)).CopyTo(operandAdjoints.Slice(elementIndex));
        }

        for (; elementIndex < batch.Count; elementIndex++)
        {
            var primal = operand.Vector[elementIndex];
            operandAdjoints[elementIndex] += upstreamAdjoints[elementIndex] * 3.0 * primal * primal;
        }
    }

    /// <remarks>
    /// The derivative of <c>cbrt(x)</c> is <c>1 / (3 * cbrt(x)²)</c>, so the retained output is used rather than
    /// recomputing a root. At <c>x = 0</c> this divides by zero and yields an infinite derivative, which is the true
    /// one.
    /// </remarks>
    private void AccumulateAdjointsForCubeRoot(int instructionIndex, ReadOnlySpan<double> upstreamAdjoints, BatchRange batch)
    {
        var instruction = program.Instructions[instructionIndex];
        if (!TryGetAdjoints(instruction.LeftOperand, batch.Count, out var operandAdjoints))
            return;

        var output = GetPrimalValues(instructionIndex, batch);
        if (output.IsScalar)
        {
            TensorPrimitives.MultiplyAdd(upstreamAdjoints, 1.0 / (3.0 * output.Scalar * output.Scalar), operandAdjoints, operandAdjoints);
            return;
        }

        var elementIndex = 0;
        var vectorizedElementCount = GetVectorizedElementCount(batch.Count);
        var three = new Vector<double>(3.0);
        for (; elementIndex < vectorizedElementCount; elementIndex += Vector<double>.Count)
        {
            var upstream = new Vector<double>(upstreamAdjoints.Slice(elementIndex));
            var primal = new Vector<double>(output.Vector.Slice(elementIndex));
            var adjoints = new Vector<double>(operandAdjoints.Slice(elementIndex));
            (adjoints + (upstream / (three * primal * primal))).CopyTo(operandAdjoints.Slice(elementIndex));
        }

        for (; elementIndex < batch.Count; elementIndex++)
        {
            var primal = output.Vector[elementIndex];
            operandAdjoints[elementIndex] += upstreamAdjoints[elementIndex] / (3.0 * primal * primal);
        }
    }

    /// <remarks>
    /// For <c>a ^ b</c> the partials are <c>b * a^(b-1)</c> and <c>a^b * ln(a)</c>. The exponent partial is only
    /// defined for a positive base; a nonpositive one yields NaN through ordinary IEEE propagation, as elsewhere in
    /// the engine.
    /// </remarks>
    private void AccumulateAdjointsForPower(int instructionIndex, ReadOnlySpan<double> upstreamAdjoints, BatchRange batch)
    {
        var instruction = program.Instructions[instructionIndex];
        var hasBase = TryGetAdjoints(instruction.LeftOperand, batch.Count, out var baseAdjoints);
        var hasExponent = TryGetAdjoints(instruction.RightOperand, batch.Count, out var exponentAdjoints);
        if (!hasBase && !hasExponent)
            return;

        var baseValues = GetPrimalValues(instruction.LeftOperand, batch);
        var exponentValues = GetPrimalValues(instruction.RightOperand, batch);
        var output = GetPrimalValues(instructionIndex, batch);

        for (var elementIndex = 0; elementIndex < batch.Count; elementIndex++)
        {
            var upstream = upstreamAdjoints[elementIndex];
            var baseValue = ValueAt(baseValues, elementIndex);
            var exponent = ValueAt(exponentValues, elementIndex);

            if (hasBase)
                baseAdjoints[elementIndex] += upstream * exponent * Math.Pow(baseValue, exponent - 1.0);

            if (hasExponent)
                exponentAdjoints[elementIndex] += upstream * ValueAt(output, elementIndex) * Math.Log(baseValue);
        }
    }

    /// <remarks>
    /// For <c>a ^ (1/b)</c> the partials are <c>(1/b) * a^(1/b - 1)</c> and <c>-a^(1/b) * ln(a) / b²</c>. As with
    /// <see cref="AccumulateAdjointsForPower"/>, the second partial requires a positive base.
    /// </remarks>
    private void AccumulateAdjointsForRoot(int instructionIndex, ReadOnlySpan<double> upstreamAdjoints, BatchRange batch)
    {
        var instruction = program.Instructions[instructionIndex];
        var hasBase = TryGetAdjoints(instruction.LeftOperand, batch.Count, out var baseAdjoints);
        var hasDegree = TryGetAdjoints(instruction.RightOperand, batch.Count, out var degreeAdjoints);
        if (!hasBase && !hasDegree)
            return;

        var baseValues = GetPrimalValues(instruction.LeftOperand, batch);
        var degreeValues = GetPrimalValues(instruction.RightOperand, batch);
        var output = GetPrimalValues(instructionIndex, batch);

        for (var elementIndex = 0; elementIndex < batch.Count; elementIndex++)
        {
            var upstream = upstreamAdjoints[elementIndex];
            var baseValue = ValueAt(baseValues, elementIndex);
            var degree = ValueAt(degreeValues, elementIndex);
            var exponent = 1.0 / degree;

            if (hasBase)
                baseAdjoints[elementIndex] += upstream * exponent * Math.Pow(baseValue, exponent - 1.0);

            if (hasDegree)
                degreeAdjoints[elementIndex] -= upstream * ValueAt(output, elementIndex) * Math.Log(baseValue) / (degree * degree);
        }
    }

    /// <remarks>
    /// The analytic quotient <c>a / sqrt(1 + b²)</c> is the division substitute that avoids a pole, so both partials
    /// are defined everywhere: <c>1 / sqrt(1 + b²)</c> and <c>-a * b / (1 + b²)^(3/2)</c>.
    /// </remarks>
    private void AccumulateAdjointsForAnalyticQuotient(int instructionIndex, ReadOnlySpan<double> upstreamAdjoints, BatchRange batch)
    {
        var instruction = program.Instructions[instructionIndex];
        var hasNumerator = TryGetAdjoints(instruction.LeftOperand, batch.Count, out var numeratorAdjoints);
        var hasDenominator = TryGetAdjoints(instruction.RightOperand, batch.Count, out var denominatorAdjoints);
        if (!hasNumerator && !hasDenominator)
            return;

        var numeratorValues = GetPrimalValues(instruction.LeftOperand, batch);
        var denominatorValues = GetPrimalValues(instruction.RightOperand, batch);

        for (var elementIndex = 0; elementIndex < batch.Count; elementIndex++)
        {
            var upstream = upstreamAdjoints[elementIndex];
            var numerator = ValueAt(numeratorValues, elementIndex);
            var denominator = ValueAt(denominatorValues, elementIndex);
            var squared = 1.0 + (denominator * denominator);
            var scale = Math.Sqrt(squared);

            if (hasNumerator)
                numeratorAdjoints[elementIndex] += upstream / scale;

            if (hasDenominator)
                denominatorAdjoints[elementIndex] -= upstream * numerator * denominator / (squared * scale);
        }
    }

    private static double ValueAt(PrimalValues values, int elementIndex) =>
        values.IsScalar ? values.Scalar : values.Vector[elementIndex];

    private void AccumulateScaled(int operandInstructionIndex, ReadOnlySpan<double> upstreamAdjoints, double scale, int count)
    {
        if (TryGetAdjoints(operandInstructionIndex, count, out var operandAdjoints))
        {
            TensorPrimitives.MultiplyAdd(upstreamAdjoints, scale, operandAdjoints, operandAdjoints);
        }
    }

    private void AccumulateWithPrimalFactor(int operandInstructionIndex, ReadOnlySpan<double> upstreamAdjoints, PrimalValues factor, int count)
    {
        if (!TryGetAdjoints(operandInstructionIndex, count, out var operandAdjoints))
            return;

        if (factor.IsScalar)
        {
            TensorPrimitives.MultiplyAdd(upstreamAdjoints, factor.Scalar, operandAdjoints, operandAdjoints);
        }
        else
        {
            TensorPrimitives.MultiplyAdd(upstreamAdjoints, factor.Vector, operandAdjoints, operandAdjoints);
        }
    }

    private void AccumulateDividedByPrimal(int operandInstructionIndex, ReadOnlySpan<double> upstreamAdjoints, PrimalValues divisor, int count)
    {
        if (!TryGetAdjoints(operandInstructionIndex, count, out var operandAdjoints))
            return;

        if (divisor.IsScalar)
        {
            TensorPrimitives.MultiplyAdd(upstreamAdjoints, 1.0 / divisor.Scalar, operandAdjoints, operandAdjoints);
            return;
        }

        var elementIndex = 0;
        var vectorizedElementCount = GetVectorizedElementCount(count);
        for (; elementIndex < vectorizedElementCount; elementIndex += Vector<double>.Count)
        {
            var upstream = new Vector<double>(upstreamAdjoints.Slice(elementIndex));
            var primal = new Vector<double>(divisor.Vector.Slice(elementIndex));
            var adjoints = new Vector<double>(operandAdjoints.Slice(elementIndex));
            (adjoints + (upstream / primal)).CopyTo(operandAdjoints.Slice(elementIndex));
        }

        for (; elementIndex < count; elementIndex++)
        {
            operandAdjoints[elementIndex] += upstreamAdjoints[elementIndex] / divisor.Vector[elementIndex];
        }
    }

    private static void AccumulateDivisionDenominatorAdjoints(ReadOnlySpan<double> upstreamAdjoints, PrimalValues numerator, PrimalValues denominator, Span<double> denominatorAdjoints)
    {
        if (numerator.IsScalar && denominator.IsScalar)
        {
            var factor = -numerator.Scalar / (denominator.Scalar * denominator.Scalar);
            TensorPrimitives.MultiplyAdd(upstreamAdjoints, factor, denominatorAdjoints, denominatorAdjoints);
            return;
        }

        var elementIndex = 0;
        var vectorizedElementCount = GetVectorizedElementCount(upstreamAdjoints.Length);
        if (numerator.IsScalar)
        {
            var numeratorValue = new Vector<double>(numerator.Scalar);
            for (; elementIndex < vectorizedElementCount; elementIndex += Vector<double>.Count)
            {
                var upstream = new Vector<double>(upstreamAdjoints.Slice(elementIndex));
                var denominatorValue = new Vector<double>(denominator.Vector.Slice(elementIndex));
                var adjoints = new Vector<double>(denominatorAdjoints.Slice(elementIndex));
                (adjoints - ((upstream * numeratorValue) / (denominatorValue * denominatorValue))).CopyTo(denominatorAdjoints.Slice(elementIndex));
            }

            for (; elementIndex < upstreamAdjoints.Length; elementIndex++)
            {
                denominatorAdjoints[elementIndex] -= upstreamAdjoints[elementIndex] * numerator.Scalar / (denominator.Vector[elementIndex] * denominator.Vector[elementIndex]);
            }

            return;
        }

        if (denominator.IsScalar)
        {
            var inverseSquared = -1.0 / (denominator.Scalar * denominator.Scalar);
            var scale = new Vector<double>(inverseSquared);
            for (; elementIndex < vectorizedElementCount; elementIndex += Vector<double>.Count)
            {
                var upstream = new Vector<double>(upstreamAdjoints.Slice(elementIndex));
                var numeratorValue = new Vector<double>(numerator.Vector.Slice(elementIndex));
                var adjoints = new Vector<double>(denominatorAdjoints.Slice(elementIndex));
                (adjoints + (upstream * numeratorValue * scale)).CopyTo(denominatorAdjoints.Slice(elementIndex));
            }

            for (; elementIndex < upstreamAdjoints.Length; elementIndex++)
            {
                denominatorAdjoints[elementIndex] += upstreamAdjoints[elementIndex] * numerator.Vector[elementIndex] * inverseSquared;
            }

            return;
        }

        for (; elementIndex < vectorizedElementCount; elementIndex += Vector<double>.Count)
        {
            var upstream = new Vector<double>(upstreamAdjoints.Slice(elementIndex));
            var numeratorValue = new Vector<double>(numerator.Vector.Slice(elementIndex));
            var denominatorValue = new Vector<double>(denominator.Vector.Slice(elementIndex));
            var adjoints = new Vector<double>(denominatorAdjoints.Slice(elementIndex));
            (adjoints - ((upstream * numeratorValue) / (denominatorValue * denominatorValue))).CopyTo(denominatorAdjoints.Slice(elementIndex));
        }

        for (; elementIndex < upstreamAdjoints.Length; elementIndex++)
        {
            denominatorAdjoints[elementIndex] -= upstreamAdjoints[elementIndex] * numerator.Vector[elementIndex] / (denominator.Vector[elementIndex] * denominator.Vector[elementIndex]);
        }
    }

    private static int GetVectorizedElementCount(int count) => count - (count % Vector<double>.Count);

    private PrimalValues GetPrimalValues(int instructionIndex, BatchRange batch)
    {
        var instruction = program.Instructions[instructionIndex];
        return instruction.DependsOnInput
            ? new PrimalValues(GetVectorPrimal(instructionIndex, batch))
            : new PrimalValues(scalarPrimals[instructionIndex]);
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

    private readonly ref struct PrimalValues
    {
        internal PrimalValues(double scalar)
        {
            IsScalar = true;
            Scalar = scalar;
            Vector = [];
        }

        internal PrimalValues(ReadOnlySpan<double> vector)
        {
            IsScalar = false;
            Scalar = default;
            Vector = vector;
        }

        internal bool IsScalar { get; }
        internal double Scalar { get; }
        internal ReadOnlySpan<double> Vector { get; }
    }
}
