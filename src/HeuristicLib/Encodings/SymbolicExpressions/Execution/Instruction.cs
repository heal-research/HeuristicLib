using HEAL.HeuristicLib.Numerics;

namespace HEAL.HeuristicLib.Encodings.SymbolicExpressions;

public readonly record struct Instruction(Operation Operation, int Arity, int SubtreeLength, int PayloadIndex = -1)
{
    public static Instruction Variable(int payloadIndex) =>
        new(Operation.Variable, Arity: 0, SubtreeLength: 1, payloadIndex);

    public static Instruction Constant(int payloadIndex) =>
        new(Operation.Constant, Arity: 0, SubtreeLength: 1, payloadIndex);

    public static Instruction Unary(Operation operation, int childSubtreeLength) =>
        new(operation, Arity: 1, SubtreeLength: checked(childSubtreeLength + 1));

    public static Instruction Binary(Operation operation, int leftSubtreeLength, int rightSubtreeLength) =>
        new(operation, Arity: 2, SubtreeLength: checked(leftSubtreeLength + rightSubtreeLength + 1));
}
