namespace HEAL.HeuristicLib.Genotypes.SymbolicExpressions;

public sealed class SymbolicExpression : IEquatable<SymbolicExpression>
{
    private readonly ExpressionInstruction[] instructions;
    private readonly NumericLiteral[] numericLiterals;
    private readonly VariableReference[] variableReferences;
    private readonly int hashCode;

    private SymbolicExpression(
      ExpressionInstruction[] instructions,
      NumericLiteral[] numericLiterals,
      VariableReference[] variableReferences,
      bool takeOwnership)
    {
        this.instructions = takeOwnership ? instructions : instructions.ToArray();
        this.numericLiterals = takeOwnership ? numericLiterals : numericLiterals.ToArray();
        this.variableReferences = takeOwnership ? variableReferences : variableReferences.ToArray();

        Depth = ValidateAndCalculateDepth(this.instructions, this.numericLiterals, this.variableReferences);
        hashCode = CalculateHashCode();
    }

    public IReadOnlyList<ExpressionInstruction> Instructions => instructions;
    public IReadOnlyList<NumericLiteral> NumericLiterals => numericLiterals;
    public IReadOnlyList<VariableReference> VariableReferences => variableReferences;
    public int Length => instructions.Length;
    public int Complexity => Length;
    public int Depth { get; }

    public static SymbolicExpression Create(
      IEnumerable<ExpressionInstruction> instructions,
      IEnumerable<NumericLiteral> numericLiterals,
      IEnumerable<VariableReference> variableReferences)
    {
        return new SymbolicExpression(
          instructions.ToArray(),
          numericLiterals.ToArray(),
          variableReferences.ToArray(),
          takeOwnership: true);
    }

    /// <summary>
    /// Creates an expression backed by the supplied arrays. The caller transfers ownership and must not mutate them afterwards.
    /// </summary>
    public static SymbolicExpression FromOwnedArrays(
      ExpressionInstruction[] instructions,
      NumericLiteral[] numericLiterals,
      VariableReference[] variableReferences)
    {
        return new SymbolicExpression(instructions, numericLiterals, variableReferences, takeOwnership: true);
    }

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
                   || instructions.SequenceEqual(other.instructions)
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

    private static int ValidateAndCalculateDepth(
      IReadOnlyList<ExpressionInstruction> instructions,
      IReadOnlyList<NumericLiteral> numericLiterals,
      IReadOnlyList<VariableReference> variableReferences)
    {
        if (instructions.Count == 0)
            throw new ArgumentException("Expression must contain at least one instruction.", nameof(instructions));

        ValidateVariableReferences(variableReferences);

        var stack = new Stack<SubtreeState>();
        for (var i = 0; i < instructions.Count; i++)
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

    private static void ValidateVariableReferences(IReadOnlyList<VariableReference> variableReferences)
    {
        for (var i = 0; i < variableReferences.Count; i++)
        {
            if (variableReferences[i].Index != i)
            {
                throw new ArgumentException(
                  $"Variable reference '{variableReferences[i].Name}' declares index {variableReferences[i].Index} but is stored at index {i}.",
                  nameof(variableReferences));
            }
        }
    }

    private static void ValidateInstructionShape(
      ExpressionInstruction instruction,
      IReadOnlyList<NumericLiteral> numericLiterals,
      IReadOnlyList<VariableReference> variableReferences)
    {
        if (instruction.OpCode == SymbolicExpressionOpCode.Invalid)
            throw new ArgumentException("Invalid opcode is not allowed.");

        var expectedArity = GetArity(instruction.OpCode);
        if (instruction.Arity != expectedArity)
            throw new ArgumentException($"Opcode {instruction.OpCode} requires arity {expectedArity}.");

        if (instruction.SubtreeLength <= 0)
            throw new ArgumentException("SubtreeLength must be positive.");

        switch (instruction.OpCode)
        {
            case SymbolicExpressionOpCode.Variable:
                ValidatePayloadIndex(instruction.PayloadIndex, variableReferences.Count, "variable reference");
                break;
            case SymbolicExpressionOpCode.NumericLiteral:
                ValidatePayloadIndex(instruction.PayloadIndex, numericLiterals.Count, "numeric literal");
                break;
            default:
                if (instruction.PayloadIndex != -1)
                    throw new ArgumentException($"Opcode {instruction.OpCode} must not have a payload index.");
                break;
        }
    }

    private static int GetArity(SymbolicExpressionOpCode opCode) => opCode switch
    {
        SymbolicExpressionOpCode.Variable or SymbolicExpressionOpCode.NumericLiteral => 0,
        SymbolicExpressionOpCode.Log or SymbolicExpressionOpCode.Sqrt => 1,
        SymbolicExpressionOpCode.Add or SymbolicExpressionOpCode.Subtract or SymbolicExpressionOpCode.Multiply or SymbolicExpressionOpCode.Divide => 2,
        _ => throw new ArgumentException($"Unsupported opcode {opCode}.")
    };

    private static void ValidatePayloadIndex(int payloadIndex, int payloadCount, string payloadName)
    {
        if (payloadIndex < 0 || payloadIndex >= payloadCount)
            throw new ArgumentException($"Payload index {payloadIndex} is out of range for {payloadName} table of length {payloadCount}.");
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
