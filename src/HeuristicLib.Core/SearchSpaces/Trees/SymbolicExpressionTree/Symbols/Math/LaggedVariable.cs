using HEAL.HeuristicLib.Genotypes.Trees;

namespace HEAL.HeuristicLib.SearchSpaces.Trees.SymbolicExpressionTree.Symbols.Math;

public class LaggedVariable : VariableBase {
  public int MinLag { get; set; }

  public int MaxLag { get; set; }

  public override SymbolicExpressionTreeNode CreateTreeNode() => new LaggedVariableTreeNode(this);
}
