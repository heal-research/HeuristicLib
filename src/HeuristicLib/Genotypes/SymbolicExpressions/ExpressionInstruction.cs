namespace HEAL.HeuristicLib.Genotypes.SymbolicExpressions;

public readonly record struct ExpressionInstruction(
  SymbolicExpressionOpCode OpCode,
  int Arity,
  int SubtreeLength,
  int PayloadIndex = -1)
{
    public static ExpressionInstruction Variable(int payloadIndex) =>
      new(SymbolicExpressionOpCode.Variable, Arity: 0, SubtreeLength: 1, payloadIndex);

    public static ExpressionInstruction NumericLiteral(int payloadIndex) =>
      new(SymbolicExpressionOpCode.NumericLiteral, Arity: 0, SubtreeLength: 1, payloadIndex);

    public static ExpressionInstruction Unary(SymbolicExpressionOpCode opCode, int childSubtreeLength) =>
      new(opCode, Arity: 1, SubtreeLength: checked(childSubtreeLength + 1));

    public static ExpressionInstruction Binary(SymbolicExpressionOpCode opCode, int leftSubtreeLength, int rightSubtreeLength) =>
      new(opCode, Arity: 2, SubtreeLength: checked(leftSubtreeLength + rightSubtreeLength + 1));
}
