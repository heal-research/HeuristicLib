using HEAL.HeuristicLib.Numerics;

namespace HEAL.HeuristicLib.Encodings.SymbolicExpressions;

internal sealed record ExpressionCompilationFailure(ExpressionPoint Point, Operation UnsupportedOperation)
{
    internal Symbol Symbol => Point.Node.Symbol;
}
