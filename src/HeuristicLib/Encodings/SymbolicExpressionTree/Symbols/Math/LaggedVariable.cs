namespace HEAL.HeuristicLib.Encodings.SymbolicExpressionTree.Symbols.Math;

public class LaggedVariable : VariableBase {
  public int MinLag { get; set; }

  public int MaxLag { get; set; }

  public override SymbolicExpressionTreeNode CreateTreeNode() => new LaggedVariableTreeNode(this);
}
