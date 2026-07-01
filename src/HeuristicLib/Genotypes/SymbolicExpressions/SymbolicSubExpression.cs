namespace HEAL.HeuristicLib.Genotypes.SymbolicExpressions;

public readonly ref struct SymbolicSubExpression
{
    private readonly ReadOnlySpan<ExpressionInstruction> instructions;
    private readonly ReadOnlySpan<NumericLiteral> numericLiterals;
    private readonly ReadOnlySpan<VariableReference> variableReferences;

    internal SymbolicSubExpression(ReadOnlySpan<ExpressionInstruction> instructions, ReadOnlySpan<NumericLiteral> numericLiterals, ReadOnlySpan<VariableReference> variableReferences)
    {
        this.instructions = instructions;
        this.numericLiterals = numericLiterals;
        this.variableReferences = variableReferences;
    }

    public int Length => instructions.Length;
    public ExpressionInstruction Instruction => instructions[^1];
    public SymbolicExpressionOpCode OpCode => Instruction.OpCode;
    public int Arity => Instruction.Arity;
    public int SubtreeLength => Instruction.SubtreeLength;

    public SymbolicSubExpression Child(int index)
    {
        if ((uint)index >= (uint)Arity)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        var childRootIndex = instructions.Length - 2;
        for (var i = Arity - 1; i > index; i--)
        {
            childRootIndex -= instructions[childRootIndex].SubtreeLength;
        }

        var childRoot = instructions[childRootIndex];
        var childStartIndex = childRootIndex - childRoot.SubtreeLength + 1;
        return new SymbolicSubExpression(instructions.Slice(childStartIndex, childRoot.SubtreeLength), numericLiterals, variableReferences);
    }

    public bool TryGetNumericLiteral(out NumericLiteral numericLiteral)
    {
        if (Instruction.OpCode != SymbolicExpressionOpCode.NumericLiteral)
        {
            numericLiteral = default;
            return false;
        }

        numericLiteral = numericLiterals[Instruction.PayloadIndex];
        return true;
    }

    public bool TryGetVariableReference(out VariableReference variableReference)
    {
        if (Instruction.OpCode != SymbolicExpressionOpCode.Variable)
        {
            variableReference = default;
            return false;
        }

        variableReference = variableReferences[Instruction.PayloadIndex];
        return true;
    }
}
