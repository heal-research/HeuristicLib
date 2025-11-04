namespace HEAL.HeuristicLib.Encodings.SymbolicExpressionTree.Symbols.Math;

public sealed class SubFunctionSymbol() : Symbol(0, 1, 1) {
  public override SymbolicExpressionTreeNode CreateTreeNode() => new SubFunctionTreeNode(this);
}
