namespace HEAL.HeuristicLib.Encodings.LegacySymbolicExpressions;

/// <summary>
///   Symbol for function defining branches
/// </summary>
public sealed class DefunSymbol() : Symbol(1, 1, 1)
{
    public override SymbolicExpressionTreeNode CreateTreeNode() => new DefunTreeNode(this, "function");
}
