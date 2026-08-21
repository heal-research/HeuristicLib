namespace HEAL.HeuristicLib.Encodings.LegacySymbolicExpressions;

public sealed class SubFunctionSymbol() : Symbol(0, 1, 1)
{
    public override SymbolicExpressionTreeNode CreateTreeNode() => new SubFunctionTreeNode(this);
}
