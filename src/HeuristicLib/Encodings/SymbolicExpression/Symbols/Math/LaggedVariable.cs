namespace HEAL.HeuristicLib.Encodings.SymbolicExpression.Symbols.Math;

public class LaggedVariable : VariableBase {
  public int MinLag { get; set; }

  public int MaxLag { get; set; }

  public override SymbolicExpressionTreeNode CreateTreeNode() => new LaggedVariableTreeNode(this);
}
