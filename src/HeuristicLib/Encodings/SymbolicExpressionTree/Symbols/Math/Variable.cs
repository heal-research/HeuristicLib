namespace HEAL.HeuristicLib.Encodings.SymbolicExpressionTree.Symbols.Math;

public sealed class Variable : VariableBase {
  public override SymbolicExpressionTreeNode CreateTreeNode() => new VariableTreeNode(this);

  public VariableTreeNode CreateTreeNode(string variable, double weight) => new(this) {
    Weight = weight,
    VariableName = variable
  };
}
