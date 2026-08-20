using HEAL.HeuristicLib.Numerics;

namespace HEAL.HeuristicLib.Genotypes.SymbolicExpressions.AutomaticDifferentiation;

internal sealed record ExpressionCompilationFailure(ExpressionPoint Point, Operation UnsupportedOperation)
{
    internal Symbol Symbol => Point.Node.Symbol;
}
