namespace HEAL.HeuristicLib.Genotypes.SymbolicExpressions;

public readonly record struct Instruction(OpCode OpCode, int Arity, int SubtreeLength, int PayloadIndex = -1)
{
    public static Instruction Variable(int payloadIndex) =>
        new(OpCode.Variable, Arity: 0, SubtreeLength: 1, payloadIndex);

    public static Instruction Constant(int payloadIndex) =>
        new(OpCode.Constant, Arity: 0, SubtreeLength: 1, payloadIndex);

    public static Instruction Unary(OpCode opCode, int childSubtreeLength) =>
        new(opCode, Arity: 1, SubtreeLength: checked(childSubtreeLength + 1));

    public static Instruction Binary(OpCode opCode, int leftSubtreeLength, int rightSubtreeLength) =>
        new(opCode, Arity: 2, SubtreeLength: checked(leftSubtreeLength + rightSubtreeLength + 1));
}
