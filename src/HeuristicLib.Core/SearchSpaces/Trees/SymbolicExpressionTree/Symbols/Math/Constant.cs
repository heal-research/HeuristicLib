using HEAL.HeuristicLib.Genotypes.Trees;

namespace HEAL.HeuristicLib.SearchSpaces.Trees.SymbolicExpressionTree.Symbols.Math;

public sealed class Constant() : Symbol(0, 0, 0)
{
  public double Value { get; set; }

  public override SymbolicExpressionTreeNode CreateTreeNode() => new ConstantTreeNode(Value);
}

public sealed class ConstantTreeNode : NumericTreeNode
{

  public ConstantTreeNode(Constant numberSymbol) : base(numberSymbol) { }

  public ConstantTreeNode(ConstantTreeNode original) : base(original) => Value = original.Value;

  public ConstantTreeNode(double value) : this(new Constant()) => Value = value;
  public new Constant Symbol => (Constant)base.Symbol;

  public override bool HasLocalParameters => false;

  public override SymbolicExpressionTreeNode Clone() => new ConstantTreeNode(this);
}
