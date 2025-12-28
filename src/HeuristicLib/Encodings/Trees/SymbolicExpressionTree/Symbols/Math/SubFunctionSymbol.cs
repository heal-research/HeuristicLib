using HEAL.HeuristicLib.Genotypes.Trees;

namespace HEAL.HeuristicLib.Encodings.Trees.SymbolicExpressionTree.Symbols.Math;

public sealed class SubFunctionSymbol() : Symbol(0, 1, 1) {
  public override SymbolicExpressionTreeNode CreateTreeNode() => new SubFunctionTreeNode(this);
}
