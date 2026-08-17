namespace HEAL.HeuristicLib.Numerics.AutomaticDifferentiation;

internal sealed class Builder
{
    private const int NoIndex = -1;

    private readonly List<BuilderInstruction> instructions = [];
    private int inputCount;
    private int parameterCount;
    private bool isBuilt;

    internal Value Input()
    {
        EnsureMutable();
        return Append(Operation.Input, payloadIndex: inputCount++);
    }

    internal Value Parameter()
    {
        EnsureMutable();
        return Append(Operation.Parameter, payloadIndex: parameterCount++);
    }

    internal Value Constant(double value)
    {
        EnsureMutable();
        return Append(Operation.Constant, constantValue: value);
    }

    internal Value Add(Value left, Value right) => Binary(Operation.Add, left, right);
    internal Value Subtract(Value left, Value right) => Binary(Operation.Subtract, left, right);
    internal Value Multiply(Value left, Value right) => Binary(Operation.Multiply, left, right);
    internal Value Divide(Value left, Value right) => Binary(Operation.Divide, left, right);
    internal Value Negate(Value value) => Unary(Operation.Negate, value);
    internal Value Exp(Value value) => Unary(Operation.Exp, value);
    internal Value Log(Value value) => Unary(Operation.Log, value);
    internal Value Sqrt(Value value) => Unary(Operation.Sqrt, value);
    internal Value Abs(Value value) => Unary(Operation.Abs, value);
    internal Value Square(Value value) => Unary(Operation.Square, value);
    internal Value Cube(Value value) => Unary(Operation.Cube, value);
    internal Value CubeRoot(Value value) => Unary(Operation.CubeRoot, value);
    internal Value Power(Value left, Value right) => Binary(Operation.Power, left, right);
    internal Value Root(Value left, Value right) => Binary(Operation.Root, left, right);
    internal Value AnalyticQuotient(Value left, Value right) => Binary(Operation.AnalyticQuotient, left, right);
    internal Value Sin(Value value) => Unary(Operation.Sin, value);
    internal Value Cos(Value value) => Unary(Operation.Cos, value);
    internal Value Tan(Value value) => Unary(Operation.Tan, value);
    internal Value Tanh(Value value) => Unary(Operation.Tanh, value);

    internal Program Build(Value root)
    {
        EnsureMutable();
        var rootInstructionIndex = GetInstructionIndex(root, nameof(root));
        var reachable = FindReachableInstructions(rootInstructionIndex);

        ValidateTerminalsAreReachable(reachable);

        var program = Compile(reachable, rootInstructionIndex);
        isBuilt = true;
        return program;
    }

    private Value Unary(Operation operation, Value value)
    {
        EnsureMutable();
        var operand = GetInstructionIndex(value, nameof(value));
        return Append(operation, leftOperand: operand);
    }

    private Value Binary(Operation operation, Value left, Value right)
    {
        EnsureMutable();
        var leftOperand = GetInstructionIndex(left, nameof(left));
        var rightOperand = GetInstructionIndex(right, nameof(right));
        return Append(operation, leftOperand, rightOperand);
    }

    private Value Append(Operation operation, int leftOperand = NoIndex, int rightOperand = NoIndex, int payloadIndex = NoIndex, double constantValue = default)
    {
        var instructionIndex = instructions.Count;
        instructions.Add(new BuilderInstruction(operation, leftOperand, rightOperand, payloadIndex, constantValue));
        return new Value(this, instructionIndex);
    }

    private int GetInstructionIndex(Value value, string parameterName)
    {
        if (!ReferenceEquals(value.Owner, this))
            throw new ArgumentException("The value belongs to a different automatic-differentiation builder.", parameterName);

        if (value.InstructionIndex < 0 || value.InstructionIndex >= instructions.Count)
            throw new ArgumentException("The value is not a valid instruction handle.", parameterName);

        return value.InstructionIndex;
    }

    private bool[] FindReachableInstructions(int rootInstructionIndex)
    {
        var reachable = new bool[instructions.Count];
        var pending = new Stack<int>();
        pending.Push(rootInstructionIndex);

        while (pending.TryPop(out var instructionIndex))
        {
            if (reachable[instructionIndex])
            {
                continue;
            }

            reachable[instructionIndex] = true;
            var instruction = instructions[instructionIndex];
            if (instruction.LeftOperand != NoIndex)
            {
                pending.Push(instruction.LeftOperand);
            }

            if (instruction.RightOperand != NoIndex)
            {
                pending.Push(instruction.RightOperand);
            }
        }

        return reachable;
    }

    private void ValidateTerminalsAreReachable(bool[] reachable)
    {
        for (var instructionIndex = 0; instructionIndex < instructions.Count; instructionIndex++)
        {
            if (reachable[instructionIndex])
            {
                continue;
            }

            var instruction = instructions[instructionIndex];
            if (instruction.Operation == Operation.Input)
                throw new InvalidOperationException($"Input {instruction.PayloadIndex} is not reachable from the program root.");

            if (instruction.Operation == Operation.Parameter)
                throw new InvalidOperationException($"Parameter {instruction.PayloadIndex} is not reachable from the program root.");
        }
    }

    private Program Compile(bool[] reachable, int rootInstructionIndex)
    {
        var oldToNewInstructionIndex = new int[instructions.Count];
        Array.Fill(oldToNewInstructionIndex, NoIndex);

        var compiledInstructions = new List<Instruction>();
        var constants = new List<double>();
        var parameterInstructionIndices = new int[parameterCount];
        var vectorPrimalSlotCount = 0;
        var adjointSlotCount = 0;

        for (var oldInstructionIndex = 0; oldInstructionIndex < instructions.Count; oldInstructionIndex++)
        {
            if (!reachable[oldInstructionIndex])
            {
                continue;
            }

            var source = instructions[oldInstructionIndex];
            var leftOperand = MapOperand(source.LeftOperand, oldToNewInstructionIndex);
            var rightOperand = MapOperand(source.RightOperand, oldToNewInstructionIndex);
            var dependsOnInput = DependsOnInput(source.Operation, leftOperand, rightOperand, compiledInstructions);
            var dependsOnParameter = DependsOnParameter(source.Operation, leftOperand, rightOperand, compiledInstructions);
            var payloadIndex = source.PayloadIndex;

            if (source.Operation == Operation.Constant)
            {
                payloadIndex = constants.Count;
                constants.Add(source.ConstantValue);
            }

            var vectorPrimalSlot = source.Operation != Operation.Input && dependsOnInput ? vectorPrimalSlotCount++ : NoIndex;
            var adjointSlot = dependsOnParameter ? adjointSlotCount++ : NoIndex;
            var compiledInstructionIndex = compiledInstructions.Count;

            compiledInstructions.Add(new Instruction(source.Operation, leftOperand, rightOperand, payloadIndex, vectorPrimalSlot, adjointSlot));
            oldToNewInstructionIndex[oldInstructionIndex] = compiledInstructionIndex;

            if (source.Operation == Operation.Parameter)
            {
                parameterInstructionIndices[source.PayloadIndex] = compiledInstructionIndex;
            }
        }

        if (oldToNewInstructionIndex[rootInstructionIndex] != compiledInstructions.Count - 1)
            throw new InvalidOperationException("The compiled program root is not the final instruction.");

        return new Program([.. compiledInstructions], [.. constants], parameterInstructionIndices, inputCount, vectorPrimalSlotCount, adjointSlotCount);
    }

    private static int MapOperand(int oldOperand, int[] oldToNewInstructionIndex)
    {
        if (oldOperand == NoIndex)
            return NoIndex;

        var newOperand = oldToNewInstructionIndex[oldOperand];
        if (newOperand == NoIndex)
            throw new InvalidOperationException("An instruction operand is not reachable from the program root.");

        return newOperand;
    }

    private static bool DependsOnInput(Operation operation, int leftOperand, int rightOperand, List<Instruction> compiledInstructions) =>
        operation == Operation.Input || OperandDependsOnInput(leftOperand, compiledInstructions) || OperandDependsOnInput(rightOperand, compiledInstructions);

    private static bool DependsOnParameter(Operation operation, int leftOperand, int rightOperand, List<Instruction> compiledInstructions) =>
        operation == Operation.Parameter || OperandDependsOnParameter(leftOperand, compiledInstructions) || OperandDependsOnParameter(rightOperand, compiledInstructions);

    private static bool OperandDependsOnInput(int operand, List<Instruction> compiledInstructions) =>
        operand != NoIndex && compiledInstructions[operand].DependsOnInput;

    private static bool OperandDependsOnParameter(int operand, List<Instruction> compiledInstructions) =>
        operand != NoIndex && compiledInstructions[operand].DependsOnParameter;

    private void EnsureMutable()
    {
        if (isBuilt)
            throw new InvalidOperationException("The automatic-differentiation builder has already been built.");
    }

    private readonly record struct BuilderInstruction(Operation Operation, int LeftOperand, int RightOperand, int PayloadIndex, double ConstantValue);
}
