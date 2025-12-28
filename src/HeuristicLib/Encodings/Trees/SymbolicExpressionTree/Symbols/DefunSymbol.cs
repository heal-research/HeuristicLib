using HEAL.HeuristicLib.Genotypes.Trees;

namespace HEAL.HeuristicLib.Encodings.Trees.SymbolicExpressionTree.Symbols;

/// <summary>
/// Symbol for function defining branches
/// </summary>
public sealed class DefunSymbol() : Symbol(1, 1, 1) {
  public override SymbolicExpressionTreeNode CreateTreeNode() {
    return new DefunTreeNode(this, "function");
  }
}
