namespace HEAL.HeuristicLib.Genotypes.SymbolicExpressions;

public sealed class SymbolicExpression : IEquatable<SymbolicExpression>
{
    private readonly ExpressionInstruction[] instructions;
    private readonly NumericLiteral[] numericLiterals;
    private readonly VariableReference[] variableReferences;
    private readonly int hashCode;

    private SymbolicExpression(ExpressionInstruction[] instructions, NumericLiteral[] numericLiterals, VariableReference[] variableReferences, bool takeOwnership)
    {
        this.instructions = takeOwnership ? instructions : instructions.ToArray();
        this.numericLiterals = takeOwnership ? numericLiterals : numericLiterals.ToArray();
        this.variableReferences = takeOwnership ? variableReferences : variableReferences.ToArray();

        Depth = ValidateAndCalculateDepth(this.instructions, this.numericLiterals, this.variableReferences);
        hashCode = CalculateHashCode();
    }

    internal int InstructionCount => instructions.Length;
    internal int NumericLiteralCount => numericLiterals.Length;
    internal int VariableReferenceCount => variableReferences.Length;
    public int Length => instructions.Length;
    public int Complexity => Length;
    public int Depth { get; }
    public SymbolicSubExpression Root => CreateSubExpression(instructions.Length - 1);
    public SymbolicExpressionLocation RootLocation => new(instructions.Length - 1);
    public IEnumerable<SymbolicSubExpression> TraversePreOrder() => Root.TraversePreOrder();
    public IEnumerable<SymbolicSubExpression> TraversePostOrder() => Root.TraversePostOrder();
    public IEnumerable<SymbolicSubExpression> TraverseBreadthFirst() => Root.TraverseBreadthFirst();

    internal static SymbolicExpression Create(IEnumerable<ExpressionInstruction> instructions, IEnumerable<NumericLiteral> numericLiterals, IEnumerable<VariableReference> variableReferences)
    {
        return new SymbolicExpression(instructions.ToArray(), numericLiterals.ToArray(), variableReferences.ToArray(), takeOwnership: true);
    }

    internal static SymbolicExpression FromOwnedArrays(ExpressionInstruction[] instructions, NumericLiteral[] numericLiterals, VariableReference[] variableReferences)
    {
        return new SymbolicExpression(instructions, numericLiterals, variableReferences, takeOwnership: true);
    }

    public SymbolicSubExpression GetSubExpression(SymbolicExpressionLocation location)
    {
        return CreateSubExpression(location.InstructionIndex);
    }

    public SymbolicExpression WithOpCode(SymbolicExpressionLocation location, SymbolicExpressionOpCode opCode)
    {
        return WithOpCode(location.InstructionIndex, opCode);
    }

    internal SymbolicExpression WithOpCode(int instructionIndex, SymbolicExpressionOpCode opCode)
    {
        ValidateInstructionIndex(instructionIndex);
        var instruction = instructions[instructionIndex];
        if (instruction.PayloadIndex != -1)
            throw new ArgumentException("Opcode edits are only supported for operation instructions.", nameof(instructionIndex));

        var arity = GetArity(opCode);
        if (arity != instruction.Arity)
            throw new ArgumentException($"Opcode {opCode} has arity {arity} but the selected instruction has arity {instruction.Arity}.", nameof(opCode));

        if (opCode is SymbolicExpressionOpCode.Variable or SymbolicExpressionOpCode.NumericLiteral)
            throw new ArgumentException("Opcode edits cannot change an operation into a payload instruction.", nameof(opCode));

        var newInstructions = instructions.ToArray();
        newInstructions[instructionIndex] = new ExpressionInstruction(opCode, instruction.Arity, instruction.SubtreeLength);
        return FromOwnedArrays(newInstructions, numericLiterals, variableReferences);
    }

    public SymbolicExpression WithNumericLiteral(SymbolicExpressionLocation location, NumericLiteral literal)
    {
        return WithNumericLiteralInstruction(location.InstructionIndex, literal);
    }

    public SymbolicExpression WithNumericLiteral(SymbolicExpressionLocation location, double value)
    {
        return WithNumericLiteral(location, new NumericLiteral(value, NumericLiteralKind.Optimizable));
    }

    internal SymbolicExpression WithNumericLiteralEntry(int literalIndex, NumericLiteral literal)
    {
        if ((uint)literalIndex >= (uint)numericLiterals.Length)
            throw new ArgumentOutOfRangeException(nameof(literalIndex));

        var newNumericLiterals = numericLiterals.ToArray();
        newNumericLiterals[literalIndex] = literal;
        return FromOwnedArrays(instructions, newNumericLiterals, variableReferences);
    }

    public SymbolicExpression WithVariable(SymbolicExpressionLocation location, string variableName)
    {
        return WithVariableInstruction(location.InstructionIndex, variableName);
    }

    internal SymbolicExpression WithVariableReferenceEntry(int variableIndex, VariableReference reference)
    {
        if ((uint)variableIndex >= (uint)variableReferences.Length)
            throw new ArgumentOutOfRangeException(nameof(variableIndex));

        if (reference.Index != variableIndex)
            throw new ArgumentException($"Variable reference index must be {variableIndex}.", nameof(reference));

        var newVariableReferences = variableReferences.ToArray();
        newVariableReferences[variableIndex] = reference;
        return FromOwnedArrays(instructions, numericLiterals, newVariableReferences);
    }

    internal SymbolicExpression WithVariableInstruction(SymbolicExpressionLocation location, string variableName)
    {
        return WithVariableInstruction(location.InstructionIndex, variableName);
    }

    internal SymbolicExpression WithVariableInstruction(int instructionIndex, string variableName)
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
        newInstructions[instructionIndex] = ExpressionInstruction.Variable(combinedVariables.Length - 1);
        return CreateWithCompactedPayloadTables(newInstructions, numericLiterals, combinedVariables);
    }

    internal SymbolicExpression WithNumericLiteralInstruction(SymbolicExpressionLocation location, NumericLiteral literal)
    {
        return WithNumericLiteralInstruction(location.InstructionIndex, literal);
    }

    internal SymbolicExpression WithNumericLiteralInstruction(int instructionIndex, NumericLiteral literal)
    {
        ValidateInstructionIndex(instructionIndex);
        if (instructions[instructionIndex].Arity != 0)
            throw new ArgumentException("Only leaf instructions can be changed to numeric literal instructions.", nameof(instructionIndex));

        var combinedNumericLiterals = new NumericLiteral[numericLiterals.Length + 1];
        numericLiterals.AsSpan().CopyTo(combinedNumericLiterals);
        combinedNumericLiterals[^1] = literal;

        var newInstructions = instructions.ToArray();
        newInstructions[instructionIndex] = ExpressionInstruction.NumericLiteral(combinedNumericLiterals.Length - 1);
        return CreateWithCompactedPayloadTables(newInstructions, combinedNumericLiterals, variableReferences);
    }

    public SymbolicExpression ReplaceSubExpression(SymbolicExpressionLocation location, SymbolicExpression replacement)
    {
        return ReplaceSubExpression(location.InstructionIndex, replacement);
    }

    internal SymbolicExpression ReplaceSubExpression(int rootInstructionIndex, SymbolicExpression replacement)
    {
        ArgumentNullException.ThrowIfNull(replacement);
        ValidateInstructionIndex(rootInstructionIndex);

        var replacedRoot = instructions[rootInstructionIndex];
        var replacedStart = rootInstructionIndex - replacedRoot.SubtreeLength + 1;
        var newLength = instructions.Length - replacedRoot.SubtreeLength + replacement.instructions.Length;
        var splicedInstructions = new ExpressionInstruction[newLength];

        instructions.AsSpan(0, replacedStart).CopyTo(splicedInstructions);

        var numericLiteralOffset = numericLiterals.Length;
        var variableReferenceOffset = variableReferences.Length;
        for (var i = 0; i < replacement.instructions.Length; i++)
        {
            var instruction = replacement.instructions[i];
            var payloadKind = SymbolicExpressionOpCodes.GetPayloadKind(instruction.OpCode);
            var payloadIndex = payloadKind switch
            {
                SymbolicExpressionPayloadKind.NumericLiteral => instruction.PayloadIndex + numericLiteralOffset,
                SymbolicExpressionPayloadKind.VariableReference => instruction.PayloadIndex + variableReferenceOffset,
                _ => instruction.PayloadIndex
            };

            splicedInstructions[replacedStart + i] = instruction with { PayloadIndex = payloadIndex };
        }

        var suffixStart = rootInstructionIndex + 1;
        instructions.AsSpan(suffixStart).CopyTo(splicedInstructions.AsSpan(replacedStart + replacement.instructions.Length));

        var combinedNumericLiterals = new NumericLiteral[numericLiterals.Length + replacement.numericLiterals.Length];
        numericLiterals.AsSpan().CopyTo(combinedNumericLiterals);
        replacement.numericLiterals.AsSpan().CopyTo(combinedNumericLiterals.AsSpan(numericLiterals.Length));

        var combinedVariableReferences = new VariableReference[variableReferences.Length + replacement.variableReferences.Length];
        variableReferences.AsSpan().CopyTo(combinedVariableReferences);
        for (var i = 0; i < replacement.variableReferences.Length; i++)
        {
            var reference = replacement.variableReferences[i];
            combinedVariableReferences[variableReferences.Length + i] = reference with { Index = variableReferences.Length + i };
        }

        RecalculateSubtreeLengths(splicedInstructions);
        return CreateWithCompactedPayloadTables(splicedInstructions, combinedNumericLiterals, combinedVariableReferences);
    }

    internal SymbolicSubExpression CreateSubExpression(int rootInstructionIndex)
    {
        if ((uint)rootInstructionIndex >= (uint)instructions.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(rootInstructionIndex));
        }

        var root = instructions[rootInstructionIndex];
        var start = rootInstructionIndex - root.SubtreeLength + 1;
        return new SymbolicSubExpression(this, start, root.SubtreeLength, rootInstructionIndex);
    }

    internal ExpressionInstruction GetInstruction(int instructionIndex) => instructions[instructionIndex];

    internal NumericLiteral GetNumericLiteral(int literalIndex) => numericLiterals[literalIndex];

    internal VariableReference GetVariableReference(int variableIndex) => variableReferences[variableIndex];

    internal ReadOnlySpan<ExpressionInstruction> InstructionsInPostOrder => instructions;

    public string ToInfixString()
    {
        var stack = new Stack<string>();
        foreach (var instruction in instructions)
        {
            switch (instruction.OpCode)
            {
                case SymbolicExpressionOpCode.Variable:
                    stack.Push(variableReferences[instruction.PayloadIndex].Name);
                    break;
                case SymbolicExpressionOpCode.NumericLiteral:
                    stack.Push(numericLiterals[instruction.PayloadIndex].Value.ToString("G", System.Globalization.CultureInfo.InvariantCulture));
                    break;
                case SymbolicExpressionOpCode.Add:
                    PushBinary(stack, "+");
                    break;
                case SymbolicExpressionOpCode.Subtract:
                    PushBinary(stack, "-");
                    break;
                case SymbolicExpressionOpCode.Multiply:
                    PushBinary(stack, "*");
                    break;
                case SymbolicExpressionOpCode.Divide:
                    PushBinary(stack, "/");
                    break;
                case SymbolicExpressionOpCode.Log:
                    PushUnary(stack, "log");
                    break;
                case SymbolicExpressionOpCode.Sqrt:
                    PushUnary(stack, "sqrt");
                    break;
                default:
                    throw new InvalidOperationException($"Unsupported opcode {instruction.OpCode}.");
            }
        }

        return stack.Single();
    }

    public override string ToString() => ToInfixString();

    public bool Equals(SymbolicExpression? other)
    {
        return other is not null
               && (ReferenceEquals(this, other)
                   || hashCode == other.hashCode
                   && instructions.SequenceEqual(other.instructions)
                   && numericLiterals.SequenceEqual(other.numericLiterals)
                   && variableReferences.SequenceEqual(other.variableReferences));
    }

    public override bool Equals(object? obj) => obj is SymbolicExpression other && Equals(other);

    public override int GetHashCode() => hashCode;

    public static bool operator ==(SymbolicExpression? left, SymbolicExpression? right) => Equals(left, right);

    public static bool operator !=(SymbolicExpression? left, SymbolicExpression? right) => !Equals(left, right);

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

    private static int ValidateAndCalculateDepth(ExpressionInstruction[] instructions, NumericLiteral[] numericLiterals, VariableReference[] variableReferences)
    {
        if (instructions.Length == 0)
            throw new ArgumentException("Expression must contain at least one instruction.", nameof(instructions));

        ValidateVariableReferences(variableReferences);

        var stack = new Stack<SubtreeState>();
        for (var i = 0; i < instructions.Length; i++)
        {
            var instruction = instructions[i];
            ValidateInstructionShape(instruction, numericLiterals, variableReferences);

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

    private static void ValidateInstructionShape(ExpressionInstruction instruction, NumericLiteral[] numericLiterals, VariableReference[] variableReferences)
    {
        if (instruction.OpCode == SymbolicExpressionOpCode.Invalid)
            throw new ArgumentException("Invalid opcode is not allowed.");

        if (!SymbolicExpressionOpCodes.MatchesArity(instruction.OpCode, instruction.Arity))
            throw new ArgumentException($"Unsupported symbol opcode {instruction.OpCode} with arity {instruction.Arity}.");

        if (instruction.SubtreeLength <= 0)
            throw new ArgumentException("SubtreeLength must be positive.");

        switch (SymbolicExpressionOpCodes.GetPayloadKind(instruction.OpCode))
        {
            case SymbolicExpressionPayloadKind.VariableReference:
                ValidatePayloadIndex(instruction.PayloadIndex, variableReferences.Length, "variable reference");
                break;
            case SymbolicExpressionPayloadKind.NumericLiteral:
                ValidatePayloadIndex(instruction.PayloadIndex, numericLiterals.Length, "numeric literal");
                break;
            case SymbolicExpressionPayloadKind.None:
                if (instruction.PayloadIndex != -1)
                    throw new ArgumentException($"Opcode {instruction.OpCode} must not have a payload index.");
                break;
        }
    }

    private static int GetArity(SymbolicExpressionOpCode opCode) => SymbolicExpressionOpCodes.GetArity(opCode);

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

    private static SymbolicExpression CreateWithCompactedPayloadTables(ExpressionInstruction[] sourceInstructions, NumericLiteral[] sourceNumericLiterals, VariableReference[] sourceVariableReferences)
    {
        var numericLiterals = new List<NumericLiteral>();
        var variableReferences = new List<VariableReference>();
        var variableIndexByName = new Dictionary<string, int>(StringComparer.Ordinal);
        var compactedInstructions = new ExpressionInstruction[sourceInstructions.Length];

        for (var i = 0; i < sourceInstructions.Length; i++)
        {
            var instruction = sourceInstructions[i];
            switch (SymbolicExpressionOpCodes.GetPayloadKind(instruction.OpCode))
            {
                case SymbolicExpressionPayloadKind.NumericLiteral:
                    var numericLiteral = sourceNumericLiterals[instruction.PayloadIndex];
                    var numericLiteralIndex = numericLiterals.IndexOf(numericLiteral);
                    if (numericLiteralIndex < 0)
                    {
                        numericLiteralIndex = numericLiterals.Count;
                        numericLiterals.Add(numericLiteral);
                    }

                    compactedInstructions[i] = instruction with { PayloadIndex = numericLiteralIndex };
                    break;
                case SymbolicExpressionPayloadKind.VariableReference:
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

        return FromOwnedArrays(compactedInstructions, numericLiterals.ToArray(), variableReferences.ToArray());
    }

    private static void RecalculateSubtreeLengths(ExpressionInstruction[] targetInstructions)
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

            targetInstructions[i] = new ExpressionInstruction(instruction.OpCode, arity, subtreeLength, instruction.PayloadIndex);
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

        foreach (var numericLiteral in numericLiterals)
        {
            hash.Add(numericLiteral);
        }

        foreach (var variableReference in variableReferences)
        {
            hash.Add(variableReference);
        }

        return hash.ToHashCode();
    }

    private readonly record struct SubtreeState(int Length, int Depth);
}
