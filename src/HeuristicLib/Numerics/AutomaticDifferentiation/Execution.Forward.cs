using System.Numerics.Tensors;

namespace HEAL.HeuristicLib.Numerics.AutomaticDifferentiation;

internal sealed partial class Execution
{
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

            scalarPrimals[instructionIndex] = instruction.Operation switch
            {
                Operation.Parameter => parameters[instruction.PayloadIndex],
                Operation.Constant => constants[instruction.PayloadIndex],
                Operation.Add => scalarPrimals[instruction.LeftOperand] + scalarPrimals[instruction.RightOperand],
                Operation.Subtract => scalarPrimals[instruction.LeftOperand] - scalarPrimals[instruction.RightOperand],
                Operation.Multiply => scalarPrimals[instruction.LeftOperand] * scalarPrimals[instruction.RightOperand],
                Operation.Divide => scalarPrimals[instruction.LeftOperand] / scalarPrimals[instruction.RightOperand],
                Operation.Negate => -scalarPrimals[instruction.LeftOperand],
                Operation.Exp => Math.Exp(scalarPrimals[instruction.LeftOperand]),
                Operation.Log => Math.Log(scalarPrimals[instruction.LeftOperand]),
                Operation.Sqrt => Math.Sqrt(scalarPrimals[instruction.LeftOperand]),
                Operation.Abs => Math.Abs(scalarPrimals[instruction.LeftOperand]),
                Operation.Square => scalarPrimals[instruction.LeftOperand] * scalarPrimals[instruction.LeftOperand],
                Operation.Cube => scalarPrimals[instruction.LeftOperand] * scalarPrimals[instruction.LeftOperand] * scalarPrimals[instruction.LeftOperand],
                Operation.CubeRoot => Math.Cbrt(scalarPrimals[instruction.LeftOperand]),
                Operation.Power => Math.Pow(scalarPrimals[instruction.LeftOperand], scalarPrimals[instruction.RightOperand]),
                Operation.Root => Math.Pow(scalarPrimals[instruction.LeftOperand], 1.0 / scalarPrimals[instruction.RightOperand]),
                Operation.AnalyticQuotient => scalarPrimals[instruction.LeftOperand] / Math.Sqrt(1.0 + (scalarPrimals[instruction.RightOperand] * scalarPrimals[instruction.RightOperand])),
                Operation.Sin => Math.Sin(scalarPrimals[instruction.LeftOperand]),
                Operation.Cos => Math.Cos(scalarPrimals[instruction.LeftOperand]),
                Operation.Tan => Math.Tan(scalarPrimals[instruction.LeftOperand]),
                Operation.Tanh => Math.Tanh(scalarPrimals[instruction.LeftOperand]),
                Operation.Input => throw new InvalidOperationException("An input instruction cannot be evaluated as a scalar."),
                _ => throw new InvalidOperationException($"Unsupported automatic-differentiation operation: {instruction.Operation}."),
            };
        }
    }

    private void EvaluateInputDependentInstructions(BatchRange batch)
    {
        var instructions = program.Instructions;
        for (var instructionIndex = 0; instructionIndex < instructions.Length; instructionIndex++)
        {
            var instruction = instructions[instructionIndex];
            if (!instruction.DependsOnInput || instruction.Operation == Operation.Input)
            {
                continue;
            }

            var destination = GetVectorPrimalSlot(instruction.VectorPrimalSlot, batch.Count);
            switch (instruction.Operation)
            {
                case Operation.Add:
                case Operation.Subtract:
                case Operation.Multiply:
                case Operation.Power:
                case Operation.Root:
                case Operation.AnalyticQuotient:
                case Operation.Divide:
                    EvaluateBinary(instruction, batch, destination);
                    break;
                case Operation.Negate:
                case Operation.Exp:
                case Operation.Log:
                case Operation.Sqrt:
                case Operation.Abs:
                case Operation.Square:
                case Operation.Cube:
                case Operation.CubeRoot:
                case Operation.Sin:
                case Operation.Cos:
                case Operation.Tan:
                case Operation.Tanh:
                    EvaluateUnary(instruction, batch, destination);
                    break;
                default:
                    throw new InvalidOperationException($"Unsupported batched automatic-differentiation operation: {instruction.Operation}.");
            }
        }
    }

    private void EvaluateBinary(Instruction instruction, BatchRange batch, Span<double> destination)
    {
        var instructions = program.Instructions;
        var leftInstruction = instructions[instruction.LeftOperand];
        var rightInstruction = instructions[instruction.RightOperand];

        if (leftInstruction.DependsOnInput)
        {
            var left = GetVectorPrimal(instruction.LeftOperand, batch);
            if (rightInstruction.DependsOnInput)
            {
                EvaluateBinary(instruction.Operation, left, GetVectorPrimal(instruction.RightOperand, batch), destination);
            }
            else
            {
                EvaluateBinary(instruction.Operation, left, scalarPrimals[instruction.RightOperand], destination);
            }
        }
        else
        {
            EvaluateBinary(instruction.Operation, scalarPrimals[instruction.LeftOperand], GetVectorPrimal(instruction.RightOperand, batch), destination);
        }
    }

    private static void EvaluateBinary(Operation operation, ReadOnlySpan<double> left, ReadOnlySpan<double> right, Span<double> destination)
    {
        switch (operation)
        {
            case Operation.Add:
                TensorPrimitives.Add(left, right, destination);
                break;
            case Operation.Subtract:
                TensorPrimitives.Subtract(left, right, destination);
                break;
            case Operation.Multiply:
                TensorPrimitives.Multiply(left, right, destination);
                break;
            case Operation.Divide:
                TensorPrimitives.Divide(left, right, destination);
                break;
            case Operation.Power:
                for (var i = 0; i < destination.Length; i++)
                    destination[i] = Math.Pow(left[i], right[i]);
                break;
            case Operation.Root:
                for (var i = 0; i < destination.Length; i++)
                    destination[i] = Math.Pow(left[i], 1.0 / right[i]);
                break;
            case Operation.AnalyticQuotient:
                for (var i = 0; i < destination.Length; i++)
                    destination[i] = left[i] / Math.Sqrt(1.0 + (right[i] * right[i]));
                break;
            default:
                throw new InvalidOperationException($"Operation {operation} is not binary.");
        }
    }

    private static void EvaluateBinary(Operation operation, ReadOnlySpan<double> left, double right, Span<double> destination)
    {
        switch (operation)
        {
            case Operation.Add:
                TensorPrimitives.Add(left, right, destination);
                break;
            case Operation.Subtract:
                TensorPrimitives.Subtract(left, right, destination);
                break;
            case Operation.Multiply:
                TensorPrimitives.Multiply(left, right, destination);
                break;
            case Operation.Divide:
                TensorPrimitives.Divide(left, right, destination);
                break;
            case Operation.Power:
                for (var i = 0; i < destination.Length; i++)
                    destination[i] = Math.Pow(left[i], right);
                break;
            case Operation.Root:
                for (var i = 0; i < destination.Length; i++)
                    destination[i] = Math.Pow(left[i], 1.0 / right);
                break;
            case Operation.AnalyticQuotient:
                for (var i = 0; i < destination.Length; i++)
                    destination[i] = left[i] / Math.Sqrt(1.0 + (right * right));
                break;
            default:
                throw new InvalidOperationException($"Operation {operation} is not binary.");
        }
    }

    private static void EvaluateBinary(Operation operation, double left, ReadOnlySpan<double> right, Span<double> destination)
    {
        switch (operation)
        {
            case Operation.Add:
                TensorPrimitives.Add(right, left, destination);
                break;
            case Operation.Subtract:
                TensorPrimitives.Subtract(left, right, destination);
                break;
            case Operation.Multiply:
                TensorPrimitives.Multiply(right, left, destination);
                break;
            case Operation.Divide:
                TensorPrimitives.Divide(left, right, destination);
                break;
            case Operation.Power:
                for (var i = 0; i < destination.Length; i++)
                    destination[i] = Math.Pow(left, right[i]);
                break;
            case Operation.Root:
                for (var i = 0; i < destination.Length; i++)
                    destination[i] = Math.Pow(left, 1.0 / right[i]);
                break;
            case Operation.AnalyticQuotient:
                for (var i = 0; i < destination.Length; i++)
                    destination[i] = left / Math.Sqrt(1.0 + (right[i] * right[i]));
                break;
            default:
                throw new InvalidOperationException($"Operation {operation} is not binary.");
        }
    }

    private void EvaluateUnary(Instruction instruction, BatchRange batch, Span<double> destination)
    {
        var operand = GetVectorPrimal(instruction.LeftOperand, batch);
        switch (instruction.Operation)
        {
            case Operation.Negate:
                TensorPrimitives.Negate(operand, destination);
                break;
            case Operation.Exp:
                TensorPrimitives.Exp(operand, destination);
                break;
            case Operation.Log:
                TensorPrimitives.Log(operand, destination);
                break;
            case Operation.Sqrt:
                TensorPrimitives.Sqrt(operand, destination);
                break;
            case Operation.Abs:
                TensorPrimitives.Abs(operand, destination);
                break;
            case Operation.Square:
                TensorPrimitives.Multiply(operand, operand, destination);
                break;
            case Operation.Cube:
                TensorPrimitives.Multiply(operand, operand, destination);
                TensorPrimitives.Multiply(destination, operand, destination);
                break;
            case Operation.CubeRoot:
                TensorPrimitives.Cbrt(operand, destination);
                break;
            case Operation.Sin:
                TensorPrimitives.Sin(operand, destination);
                break;
            case Operation.Cos:
                TensorPrimitives.Cos(operand, destination);
                break;
            case Operation.Tan:
                TensorPrimitives.Tan(operand, destination);
                break;
            case Operation.Tanh:
                TensorPrimitives.Tanh(operand, destination);
                break;
            default:
                throw new InvalidOperationException($"Operation {instruction.Operation} is not unary.");
        }
    }

    private ReadOnlySpan<double> GetVectorPrimal(int instructionIndex, BatchRange batch)
    {
        var instruction = program.Instructions[instructionIndex];
        return instruction.Operation == Operation.Input
            ? inputColumns[instruction.PayloadIndex].Span.Slice(batch.Offset, batch.Count)
            : GetVectorPrimalSlot(instruction.VectorPrimalSlot, batch.Count);
    }

    private Span<double> GetVectorPrimalSlot(int slot, int count) => vectorPrimals.AsSpan(slot * batchCapacity, count);
}
