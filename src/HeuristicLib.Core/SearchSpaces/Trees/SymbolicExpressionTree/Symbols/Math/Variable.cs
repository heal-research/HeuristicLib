using HEAL.HeuristicLib.Genotypes.Trees;

namespace HEAL.HeuristicLib.SearchSpaces.Trees.SymbolicExpressionTree.Symbols.Math;

public sealed class Variable : VariableBase {
  public override SymbolicExpressionTreeNode CreateTreeNode() => new VariableTreeNode(this);

  public VariableTreeNode CreateTreeNode(string variable, double weight) => new(this) {
    Weight = weight,
    VariableName = variable
  };
}
