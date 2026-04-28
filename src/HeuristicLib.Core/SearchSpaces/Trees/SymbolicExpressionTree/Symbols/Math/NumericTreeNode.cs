using HEAL.HeuristicLib.Genotypes.Trees;

namespace HEAL.HeuristicLib.SearchSpaces.Trees.SymbolicExpressionTree.Symbols.Math;

public abstract class NumericTreeNode : SymbolicExpressionTreeNode
{
  protected NumericTreeNode(Symbol symbol) : base(symbol) { }

  protected NumericTreeNode(NumericTreeNode original) : base(original) => Value = original.Value;

  public double Value { get; set; }

  public override string ToString() => $"{Value:E4}";
}
