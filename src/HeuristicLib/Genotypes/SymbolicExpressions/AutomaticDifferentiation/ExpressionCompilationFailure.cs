namespace HEAL.HeuristicLib.Genotypes.SymbolicExpressions.AutomaticDifferentiation;

internal sealed record ExpressionCompilationFailure(ExpressionPoint Point, OpCode UnsupportedOperation)
{
    internal Symbol Symbol => Point.Node.Symbol;
}
