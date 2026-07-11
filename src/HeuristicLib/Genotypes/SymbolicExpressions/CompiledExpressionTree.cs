namespace HEAL.HeuristicLib.Genotypes.SymbolicExpressions;

public sealed class CompiledExpressionTree : IEquatable<CompiledExpressionTree>
{
    private readonly Instruction[] instructions;
    private readonly double[] constants;
    private readonly VariableReference[] variableReferences;
    private readonly int hashCode;

    private CompiledExpressionTree(Instruction[] instructions, double[] constants, VariableReference[] variableReferences, bool takeOwnership)
    {
        this.instructions = takeOwnership ? instructions : instructions.ToArray();
        this.constants = takeOwnership ? constants : constants.ToArray();
        this.variableReferences = takeOwnership ? variableReferences : variableReferences.ToArray();

        Depth = ValidateAndCalculateDepth(this.instructions, this.constants, this.variableReferences);
        hashCode = CalculateHashCode();
    }

    internal int InstructionCount => instructions.Length;
    internal int ConstantCount => constants.Length;
    internal int VariableReferenceCount => variableReferences.Length;

    public int Length => instructions.Length;
    public int Depth { get; }

    public CompiledExpressionSubtree Root => CreateSubtree(instructions.Length - 1);
    public ExpressionLocation RootLocation => new(instructions.Length - 1);

    public IEnumerable<CompiledExpressionSubtree> TraversePreOrder() => Root.TraversePreOrder();
    public IEnumerable<CompiledExpressionSubtree> TraversePostOrder() => Root.TraversePostOrder();
    public IEnumerable<CompiledExpressionSubtree> TraverseBreadthFirst() => Root.TraverseBreadthFirst();

    internal static CompiledExpressionTree Create(IEnumerable<Instruction> instructions, IEnumerable<double> constants, IEnumerable<VariableReference> variableReferences)
    {
        return new CompiledExpressionTree(instructions.ToArray(), constants.ToArray(), variableReferences.ToArray(), takeOwnership: true);
    }

    internal static CompiledExpressionTree FromOwnedArrays(Instruction[] instructions, double[] constants, VariableReference[] variableReferences)
    {
        return new CompiledExpressionTree(instructions, constants, variableReferences, takeOwnership: true);
    }

    public CompiledExpressionSubtree GetSubtree(ExpressionLocation location)
    {
        return CreateSubtree(location.InstructionIndex);
    }

    public CompiledExpressionTree WithOpCode(ExpressionLocation location, OpCode opCode)
    {
        return WithOpCode(location.InstructionIndex, opCode);
    }

    internal CompiledExpressionTree WithOpCode(int instructionIndex, OpCode opCode)
    {
        ValidateInstructionIndex(instructionIndex);
        var instruction = instructions[instructionIndex];
        if (instruction.PayloadIndex != -1)
            throw new ArgumentException("Opcode edits are only supported for operation instructions.", nameof(instructionIndex));

        var arity = GetArity(opCode);
        if (arity != instruction.Arity)
            throw new ArgumentException($"Opcode {opCode} has arity {arity} but the selected instruction has arity {instruction.Arity}.", nameof(opCode));

        if (opCode is OpCode.Variable or OpCode.Constant)
            throw new ArgumentException("Opcode edits cannot change an operation into a payload instruction.", nameof(opCode));

        var newInstructions = instructions.ToArray();
        newInstructions[instructionIndex] = new Instruction(opCode, instruction.Arity, instruction.SubtreeLength);
        return FromOwnedArrays(newInstructions, constants, variableReferences);
    }

    public CompiledExpressionTree WithConstant(ExpressionLocation location, double value)
    {
        return WithConstantInstruction(location.InstructionIndex, value);
    }

    internal CompiledExpressionTree WithConstantEntry(int constantIndex, double value)
    {
        if ((uint)constantIndex >= (uint)constants.Length)
            throw new ArgumentOutOfRangeException(nameof(constantIndex));

        var newConstants = constants.ToArray();
        newConstants[constantIndex] = value;
        return FromOwnedArrays(instructions, newConstants, variableReferences);
    }

    public CompiledExpressionTree WithVariable(ExpressionLocation location, string variableName)
    {
        return WithVariableInstruction(location.InstructionIndex, variableName);
    }

    internal CompiledExpressionTree WithVariableReferenceEntry(int variableIndex, VariableReference reference)
    {
        if ((uint)variableIndex >= (uint)variableReferences.Length)
            throw new ArgumentOutOfRangeException(nameof(variableIndex));

        if (reference.Index != variableIndex)
            throw new ArgumentException($"Variable reference index must be {variableIndex}.", nameof(reference));

        var newVariableReferences = variableReferences.ToArray();
        newVariableReferences[variableIndex] = reference;
        return FromOwnedArrays(instructions, constants, newVariableReferences);
    }

    internal CompiledExpressionTree WithVariableInstruction(ExpressionLocation location, string variableName)
    {
        return WithVariableInstruction(location.InstructionIndex, variableName);
    }

    internal CompiledExpressionTree WithVariableInstruction(int instructionIndex, string variableName)
    {
        ValidateInstructionIndex(instructionIndex);
        if (string.IsNullOrWhiteSpace(variableName))
            throw new ArgumentException("Variable name must not be empty.", nameof(variableName));

        if (instructions[instructionIndex].Arity != 0)
            throw new ArgumentException("Only leaf instructions can be changed to variable instructions.", nameof(instructionIndex));

        var combinedVariables = new VariableReference[variableReferences.Length + 1];
        variableReferences.AsSpan().CopyTo(combinedVariables);
        combinedVariables[^1] = new VariableReference(variableName, combinedVariables.Length - 1);

        var newInstructions = instructions.ToArray();
        newInstructions[instructionIndex] = Instruction.Variable(combinedVariables.Length - 1);
        return CreateWithCompactedPayloadTables(newInstructions, constants, combinedVariables);
    }

    internal CompiledExpressionTree WithConstantInstruction(ExpressionLocation location, double value)
    {
        return WithConstantInstruction(location.InstructionIndex, value);
    }

    internal CompiledExpressionTree WithConstantInstruction(int instructionIndex, double value)
    {
        ValidateInstructionIndex(instructionIndex);
        if (instructions[instructionIndex].Arity != 0)
            throw new ArgumentException("Only leaf instructions can be changed to constant instructions.", nameof(instructionIndex));

        var combinedConstants = new double[constants.Length + 1];
        constants.AsSpan().CopyTo(combinedConstants);
        combinedConstants[^1] = value;

        var newInstructions = instructions.ToArray();
        newInstructions[instructionIndex] = Instruction.Constant(combinedConstants.Length - 1);
        return CreateWithCompactedPayloadTables(newInstructions, combinedConstants, variableReferences);
    }

    public CompiledExpressionTree ReplaceSubtree(ExpressionLocation location, CompiledExpressionTree replacement)
    {
        return ReplaceSubtree(location.InstructionIndex, replacement);
    }

    internal CompiledExpressionTree ReplaceSubtree(int rootInstructionIndex, CompiledExpressionTree replacement)
    {
        ArgumentNullException.ThrowIfNull(replacement);
        ValidateInstructionIndex(rootInstructionIndex);

        var replacedRoot = instructions[rootInstructionIndex];
        var replacedStart = rootInstructionIndex - replacedRoot.SubtreeLength + 1;
        var newLength = instructions.Length - replacedRoot.SubtreeLength + replacement.instructions.Length;
        var splicedInstructions = new Instruction[newLength];

        instructions.AsSpan(0, replacedStart).CopyTo(splicedInstructions);

        var constantOffset = constants.Length;
        var variableReferenceOffset = variableReferences.Length;
        for (var i = 0; i < replacement.instructions.Length; i++)
        {
            var instruction = replacement.instructions[i];
            var payloadKind = OpCodes.GetPayloadKind(instruction.OpCode);
            var payloadIndex = payloadKind switch
            {
                PayloadKind.Constant => instruction.PayloadIndex + constantOffset,
                PayloadKind.VariableReference => instruction.PayloadIndex + variableReferenceOffset,
                _ => instruction.PayloadIndex
            };

            splicedInstructions[replacedStart + i] = instruction with { PayloadIndex = payloadIndex };
        }

        var suffixStart = rootInstructionIndex + 1;
        instructions.AsSpan(suffixStart).CopyTo(splicedInstructions.AsSpan(replacedStart + replacement.instructions.Length));

        var combinedConstants = new double[constants.Length + replacement.constants.Length];
        constants.AsSpan().CopyTo(combinedConstants);
        replacement.constants.AsSpan().CopyTo(combinedConstants.AsSpan(constants.Length));

        var combinedVariableReferences = new VariableReference[variableReferences.Length + replacement.variableReferences.Length];
        variableReferences.AsSpan().CopyTo(combinedVariableReferences);
        for (var i = 0; i < replacement.variableReferences.Length; i++)
        {
            var reference = replacement.variableReferences[i];
            combinedVariableReferences[variableReferences.Length + i] = reference with { Index = variableReferences.Length + i };
        }

        RecalculateSubtreeLengths(splicedInstructions);
        return CreateWithCompactedPayloadTables(splicedInstructions, combinedConstants, combinedVariableReferences);
    }

    internal CompiledExpressionSubtree CreateSubtree(int rootInstructionIndex)
    {
        if ((uint)rootInstructionIndex >= (uint)instructions.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(rootInstructionIndex));
        }

        var root = instructions[rootInstructionIndex];
        var start = rootInstructionIndex - root.SubtreeLength + 1;
        return new CompiledExpressionSubtree(this, start, root.SubtreeLength, rootInstructionIndex);
    }

    internal Instruction GetInstruction(int instructionIndex) => instructions[instructionIndex];

    internal double GetConstant(int constantIndex) => constants[constantIndex];

    internal VariableReference GetVariableReference(int variableIndex) => variableReferences[variableIndex];

    internal ReadOnlySpan<Instruction> InstructionsInPostOrder => instructions;

    public string ToInfixString()
    {
        var stack = new Stack<string>();
        foreach (var instruction in instructions)
        {
            switch (instruction.OpCode)
            {
                case OpCode.Variable:
                    stack.Push(variableReferences[instruction.PayloadIndex].Name);
                    break;
                case OpCode.Constant:
                    stack.Push(constants[instruction.PayloadIndex].ToString("G", System.Globalization.CultureInfo.InvariantCulture));
                    break;
                case OpCode.Add:
                    PushBinary(stack, "+");
                    break;
                case OpCode.Subtract:
                    PushBinary(stack, "-");
                    break;
                case OpCode.Multiply:
                    PushBinary(stack, "*");
                    break;
                case OpCode.Divide:
                    PushBinary(stack, "/");
                    break;
                case OpCode.Negate:
                    PushUnary(stack, "negate");
                    break;
                case OpCode.Exp:
                    PushUnary(stack, "exp");
                    break;
                case OpCode.Log:
                    PushUnary(stack, "log");
                    break;
                case OpCode.Sqrt:
                    PushUnary(stack, "sqrt");
                    break;
                default:
                    throw new InvalidOperationException($"Unsupported opcode {instruction.OpCode}.");
            }
        }

        return stack.Single();
    }

    public override string ToString() => ToInfixString();

    public bool Equals(CompiledExpressionTree? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return hashCode == other.hashCode
               && instructions.SequenceEqual(other.instructions)
               && constants.SequenceEqual(other.constants)
               && variableReferences.SequenceEqual(other.variableReferences);
    }

    public override bool Equals(object? obj) => obj is CompiledExpressionTree other && Equals(other);

    public override int GetHashCode() => hashCode;

    public static bool operator ==(CompiledExpressionTree? left, CompiledExpressionTree? right) => Equals(left, right);

    public static bool operator !=(CompiledExpressionTree? left, CompiledExpressionTree? right) => !Equals(left, right);

    private static void PushBinary(Stack<string> stack, string op)
    {
        var right = stack.Pop();
        var left = stack.Pop();
        stack.Push($"({left} {op} {right})");
    }

    private static void PushUnary(Stack<string> stack, string functionName)
    {
        var child = stack.Pop();
        stack.Push($"{functionName}({child})");
    }

    private static int ValidateAndCalculateDepth(Instruction[] instructions, double[] constants, VariableReference[] variableReferences)
    {
        if (instructions.Length == 0)
            throw new ArgumentException("Expression must contain at least one instruction.", nameof(instructions));

        ValidateVariableReferences(variableReferences);

        var stack = new Stack<SubtreeState>();
        for (var i = 0; i < instructions.Length; i++)
        {
            var instruction = instructions[i];
            ValidateInstructionShape(instruction, constants, variableReferences);

            if (stack.Count < instruction.Arity)
                throw new ArgumentException($"Instruction {i} requires {instruction.Arity} operands but only {stack.Count} are available.", nameof(instructions));

            var subtreeLength = 1;
            var depth = 1;
            for (var j = 0; j < instruction.Arity; j++)
            {
                var child = stack.Pop();
                subtreeLength += child.Length;
                depth = Math.Max(depth, child.Depth + 1);
            }

            if (instruction.SubtreeLength != subtreeLength)
            {
                throw new ArgumentException(
                  $"Instruction {i} declares subtree length {instruction.SubtreeLength} but calculated length is {subtreeLength}.",
                  nameof(instructions));
            }

            stack.Push(new SubtreeState(subtreeLength, depth));
        }

        if (stack.Count != 1)
            throw new ArgumentException("Expression instructions must contain exactly one root expression.", nameof(instructions));

        return stack.Pop().Depth;
    }

    private static void ValidateVariableReferences(VariableReference[] variableReferences)
    {
        for (var i = 0; i < variableReferences.Length; i++)
        {
            if (variableReferences[i].Index != i)
            {
                throw new ArgumentException(
                  $"Variable reference '{variableReferences[i].Name}' declares index {variableReferences[i].Index} but is stored at index {i}.",
                  nameof(variableReferences));
            }
        }
    }

    private static void ValidateInstructionShape(Instruction instruction, double[] constants, VariableReference[] variableReferences)
    {
        if (instruction.OpCode == OpCode.Invalid)
            throw new ArgumentException("Invalid opcode is not allowed.");

        if (!OpCodes.MatchesArity(instruction.OpCode, instruction.Arity))
            throw new ArgumentException($"Unsupported symbol opcode {instruction.OpCode} with arity {instruction.Arity}.");

        if (instruction.SubtreeLength <= 0)
            throw new ArgumentException("SubtreeLength must be positive.");

        switch (OpCodes.GetPayloadKind(instruction.OpCode))
        {
            case PayloadKind.VariableReference:
                ValidatePayloadIndex(instruction.PayloadIndex, variableReferences.Length, "variable reference");
                break;
            case PayloadKind.Constant:
                ValidatePayloadIndex(instruction.PayloadIndex, constants.Length, "constant");
                break;
            case PayloadKind.None:
                if (instruction.PayloadIndex != -1)
                    throw new ArgumentException($"Opcode {instruction.OpCode} must not have a payload index.");
                break;
        }
    }

    private static int GetArity(OpCode opCode) => OpCodes.GetArity(opCode);

    private static void ValidatePayloadIndex(int payloadIndex, int payloadCount, string payloadName)
    {
        if (payloadIndex < 0 || payloadIndex >= payloadCount)
            throw new ArgumentException($"Payload index {payloadIndex} is out of range for {payloadName} table of length {payloadCount}.");
    }

    private void ValidateInstructionIndex(int instructionIndex)
    {
        if ((uint)instructionIndex >= (uint)instructions.Length)
            throw new ArgumentOutOfRangeException(nameof(instructionIndex));
    }

    private static CompiledExpressionTree CreateWithCompactedPayloadTables(Instruction[] sourceInstructions, double[] sourceConstants, VariableReference[] sourceVariableReferences)
    {
        var constants = new List<double>();
        var variableReferences = new List<VariableReference>();
        var variableIndexByName = new Dictionary<string, int>(StringComparer.Ordinal);
        var compactedInstructions = new Instruction[sourceInstructions.Length];

        for (var i = 0; i < sourceInstructions.Length; i++)
        {
            var instruction = sourceInstructions[i];
            switch (OpCodes.GetPayloadKind(instruction.OpCode))
            {
                case PayloadKind.Constant:
                    var constant = sourceConstants[instruction.PayloadIndex];
                    var constantIndex = constants.IndexOf(constant);
                    if (constantIndex < 0)
                    {
                        constantIndex = constants.Count;
                        constants.Add(constant);
                    }

                    compactedInstructions[i] = instruction with { PayloadIndex = constantIndex };
                    break;
                case PayloadKind.VariableReference:
                    var variableName = sourceVariableReferences[instruction.PayloadIndex].Name;
                    if (!variableIndexByName.TryGetValue(variableName, out var variableIndex))
                    {
                        variableIndex = variableReferences.Count;
                        variableIndexByName.Add(variableName, variableIndex);
                        variableReferences.Add(new VariableReference(variableName, variableIndex));
                    }

                    compactedInstructions[i] = instruction with { PayloadIndex = variableIndex };
                    break;
                default:
                    compactedInstructions[i] = instruction;
                    break;
            }
        }

        return FromOwnedArrays(compactedInstructions, constants.ToArray(), variableReferences.ToArray());
    }

    private static void RecalculateSubtreeLengths(Instruction[] targetInstructions)
    {
        var stack = new Stack<int>();
        for (var i = 0; i < targetInstructions.Length; i++)
        {
            var instruction = targetInstructions[i];
            var arity = GetArity(instruction.OpCode);
            if (stack.Count < arity)
                throw new ArgumentException($"Instruction {i} requires {arity} operands but only {stack.Count} are available.", nameof(targetInstructions));

            var subtreeLength = 1;
            for (var j = 0; j < arity; j++)
            {
                subtreeLength += stack.Pop();
            }

            targetInstructions[i] = instruction with { Arity = arity, SubtreeLength = subtreeLength };
            stack.Push(subtreeLength);
        }

        if (stack.Count != 1)
            throw new ArgumentException("Expression instructions must contain exactly one root expression.", nameof(targetInstructions));
    }

    private int CalculateHashCode()
    {
        var hash = new HashCode();
        foreach (var instruction in instructions)
        {
            hash.Add(instruction);
        }

        foreach (var constant in constants)
        {
            hash.Add(constant);
        }

        foreach (var variableReference in variableReferences)
        {
            hash.Add(variableReference);
        }

        return hash.ToHashCode();
    }

    private readonly record struct SubtreeState(int Length, int Depth);
}
