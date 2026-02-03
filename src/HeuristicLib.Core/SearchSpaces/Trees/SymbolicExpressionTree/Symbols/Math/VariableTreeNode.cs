namespace HEAL.HeuristicLib.Encodings.SymbolicExpressionTree.Symbols.Math;

public sealed class VariableTreeNode : VariableTreeNodeBase {
  public new Variable Symbol => (Variable)base.Symbol;

  private VariableTreeNode(VariableTreeNode original) : base(original) { }

  public VariableTreeNode(Variable variableSymbol) : base(variableSymbol) { }

  public override SymbolicExpressionTreeNode Clone() => new VariableTreeNode(this);
}
