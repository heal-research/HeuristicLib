namespace HEAL.HeuristicLib.Genotypes.SymbolicExpressions;

public abstract record ExpressionDraft
{
    public static ExpressionDraft Variable(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Variable name must not be empty.", nameof(name));

        return new VariableDraft(name);
    }

    public static ExpressionDraft Fixed(double value) => new NumericLiteralDraft(value, NumericLiteralKind.Fixed);

    public static ExpressionDraft Parameter(double value) => new NumericLiteralDraft(value, NumericLiteralKind.Optimizable);

    public static ExpressionDraft Add(ExpressionDraft left, ExpressionDraft right) =>
      new BinaryDraft(SymbolicExpressionOpCode.Add, left, right);

    public static ExpressionDraft Subtract(ExpressionDraft left, ExpressionDraft right) =>
      new BinaryDraft(SymbolicExpressionOpCode.Subtract, left, right);

    public static ExpressionDraft Multiply(ExpressionDraft left, ExpressionDraft right) =>
      new BinaryDraft(SymbolicExpressionOpCode.Multiply, left, right);

    public static ExpressionDraft Divide(ExpressionDraft left, ExpressionDraft right) =>
      new BinaryDraft(SymbolicExpressionOpCode.Divide, left, right);

    public static ExpressionDraft Log(ExpressionDraft child) =>
      new UnaryDraft(SymbolicExpressionOpCode.Log, child);

    public static ExpressionDraft Sqrt(ExpressionDraft child) =>
      new UnaryDraft(SymbolicExpressionOpCode.Sqrt, child);

    public SymbolicExpression Compile()
    {
        var instructions = new List<ExpressionInstruction>();
        var numericLiterals = new List<NumericLiteral>();
        var variableReferences = new List<VariableReference>();
        var variableIndexByName = new Dictionary<string, int>(StringComparer.Ordinal);

        Emit(this, instructions, numericLiterals, variableReferences, variableIndexByName);

        return SymbolicExpression.FromOwnedArrays(
          instructions.ToArray(),
          numericLiterals.ToArray(),
          variableReferences.ToArray());
    }

    private static int Emit(
      ExpressionDraft draft,
      List<ExpressionInstruction> instructions,
      List<NumericLiteral> numericLiterals,
      List<VariableReference> variableReferences,
      Dictionary<string, int> variableIndexByName)
    {
        switch (draft)
        {
            case VariableDraft variable:
                if (!variableIndexByName.TryGetValue(variable.Name, out var variableIndex))
                {
                    variableIndex = variableReferences.Count;
                    variableIndexByName.Add(variable.Name, variableIndex);
                    variableReferences.Add(new VariableReference(variable.Name, variableIndex));
                }

                instructions.Add(ExpressionInstruction.Variable(variableIndex));
                return 1;
            case NumericLiteralDraft literal:
                var literalIndex = numericLiterals.Count;
                numericLiterals.Add(new NumericLiteral(literal.Value, literal.Kind));
                instructions.Add(ExpressionInstruction.NumericLiteral(literalIndex));
                return 1;
            case UnaryDraft unary:
                var childLength = Emit(unary.Child, instructions, numericLiterals, variableReferences, variableIndexByName);
                instructions.Add(ExpressionInstruction.Unary(unary.OpCode, childLength));
                return childLength + 1;
            case BinaryDraft binary:
                var leftLength = Emit(binary.Left, instructions, numericLiterals, variableReferences, variableIndexByName);
                var rightLength = Emit(binary.Right, instructions, numericLiterals, variableReferences, variableIndexByName);
                instructions.Add(ExpressionInstruction.Binary(binary.OpCode, leftLength, rightLength));
                return leftLength + rightLength + 1;
            default:
                throw new InvalidOperationException($"Unsupported expression draft node {draft.GetType()}.");
        }
    }

    private sealed record VariableDraft(string Name) : ExpressionDraft;

    private sealed record NumericLiteralDraft(double Value, NumericLiteralKind Kind) : ExpressionDraft;

    private sealed record UnaryDraft(SymbolicExpressionOpCode OpCode, ExpressionDraft Child) : ExpressionDraft;

    private sealed record BinaryDraft(SymbolicExpressionOpCode OpCode, ExpressionDraft Left, ExpressionDraft Right) : ExpressionDraft;
}
