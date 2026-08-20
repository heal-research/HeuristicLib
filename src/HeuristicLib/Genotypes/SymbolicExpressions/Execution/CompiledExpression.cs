using System.Globalization;
using HEAL.HeuristicLib.Numerics;

namespace HEAL.HeuristicLib.Genotypes.SymbolicExpressions;

public sealed class CompiledExpression : IEquatable<CompiledExpression>
{
    private readonly Instruction[] instructions;
    private readonly double[] constants;
    private readonly VariableReference[] variableReferences;
    private readonly int hashCode;

    private CompiledExpression(Instruction[] instructions, double[] constants, VariableReference[] variableReferences, bool takeOwnership)
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

    public CompiledSubExpression Root => CreateSubExpression(instructions.Length - 1);

    public IEnumerable<CompiledSubExpression> TraversePreOrder() => Root.TraversePreOrder();
    public IEnumerable<CompiledSubExpression> TraversePostOrder() => Root.TraversePostOrder();
    public IEnumerable<CompiledSubExpression> TraverseBreadthFirst() => Root.TraverseBreadthFirst();

    internal static CompiledExpression Create(IEnumerable<Instruction> instructions, IEnumerable<double> constants, IEnumerable<VariableReference> variableReferences)
    {
        return new CompiledExpression(instructions.ToArray(), constants.ToArray(), variableReferences.ToArray(), takeOwnership: true);
    }

    internal static CompiledExpression FromOwnedArrays(Instruction[] instructions, double[] constants, VariableReference[] variableReferences)
    {
        return new CompiledExpression(instructions, constants, variableReferences, takeOwnership: true);
    }

    internal CompiledSubExpression CreateSubExpression(int rootInstructionIndex)
    {
        if (rootInstructionIndex < 0 || rootInstructionIndex >= instructions.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(rootInstructionIndex));
        }

        return new CompiledSubExpression(this, rootInstructionIndex);
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
            ref readonly var info = ref OperationCatalog.GetInfo(instruction.Operation);
            switch (info)
            {
                case { PayloadKind: PayloadKind.VariableReference }:
                    stack.Push(variableReferences[instruction.PayloadIndex].Name);
                    break;
                case { PayloadKind: PayloadKind.Constant }:
                    stack.Push(constants[instruction.PayloadIndex].ToString("G", CultureInfo.InvariantCulture));
                    break;
                case { Arity: 1 }:
                    stack.Push($"{info.Name}({stack.Pop()})");
                    break;
                case { Arity: 2 }:
                    // The operands come off the stack in reverse.
                    var right = stack.Pop();
                    var left = stack.Pop();
                    stack.Push(info.Notation == OperationNotation.Infix
                        ? $"({left} {info.Name} {right})"
                        : $"{info.Name}({left}, {right})");
                    break;
                default:
                    throw new NotSupportedException($"Operation {info.Operation} has unsupported arity {info.Arity}.");
            }
        }

        return stack.Single();
    }

    public override string ToString() => ToInfixString();

    public bool Equals(CompiledExpression? other)
    {
        if (other is null)
            return false;
        if (ReferenceEquals(this, other))
            return true;
        return hashCode == other.hashCode
               && instructions.SequenceEqual(other.instructions)
               && constants.SequenceEqual(other.constants)
               && variableReferences.SequenceEqual(other.variableReferences);
    }

    public override bool Equals(object? obj) => obj is CompiledExpression other && Equals(other);

    public override int GetHashCode() => hashCode;

    public static bool operator ==(CompiledExpression? left, CompiledExpression? right) => Equals(left, right);

    public static bool operator !=(CompiledExpression? left, CompiledExpression? right) => !Equals(left, right);


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
                throw new ArgumentException($"Variable reference '{variableReferences[i].Name}' declares index {variableReferences[i].Index} but is stored at index {i}.", nameof(variableReferences));
            }
        }
    }

    private static void ValidateInstructionShape(Instruction instruction, double[] constants, VariableReference[] variableReferences)
    {
        if (instruction.Operation == Operation.Invalid)
            throw new ArgumentException("An invalid operation is not allowed.");

        if (!OperationCatalog.TryGetInfo(instruction.Operation, out var arityDefinition) || arityDefinition.Arity != instruction.Arity)
            throw new ArgumentException($"Unsupported symbol operation {instruction.Operation} with arity {instruction.Arity}.");

        if (instruction.SubtreeLength <= 0)
            throw new ArgumentException("SubtreeLength must be positive.");

        switch (OperationCatalog.GetInfo(instruction.Operation).PayloadKind)
        {
            case PayloadKind.VariableReference:
                ValidatePayloadIndex(instruction.PayloadIndex, variableReferences.Length, "variable reference");
                break;
            case PayloadKind.Constant:
                ValidatePayloadIndex(instruction.PayloadIndex, constants.Length, "constant");
                break;
            case PayloadKind.None:
                if (instruction.PayloadIndex != -1)
                    throw new ArgumentException($"Operation {instruction.Operation} must not have a payload index.");
                break;
        }
    }

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
