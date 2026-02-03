namespace HEAL.HeuristicLib.Encodings.SymbolicExpressionTree.Symbols;

/// <summary>
/// Symbol for function defining branches
/// </summary>
public sealed class DefunSymbol() : Symbol(1, 1, 1) {
  public override SymbolicExpressionTreeNode CreateTreeNode() {
    return new DefunTreeNode(this, "function");
  }
}
