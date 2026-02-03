namespace HEAL.HeuristicLib.Encodings.SymbolicExpressionTree.Symbols.Math;

public sealed class Constant() : Symbol(0, 0, 0) {
  public double Value { get; set; }

  public override SymbolicExpressionTreeNode CreateTreeNode() => new ConstantTreeNode(Value);
}

public sealed class ConstantTreeNode : SymbolicExpressionTreeNode {
  public new Constant Symbol => (Constant)base.Symbol;

  public double Value { get; set; }

  public ConstantTreeNode(Constant numberSymbol) : base(numberSymbol) { }

  public ConstantTreeNode(ConstantTreeNode original) : base(original) => Value = original.Value;

  public ConstantTreeNode(double value) : this(new Constant()) => Value = value;

  public override bool HasLocalParameters => false;

  public override SymbolicExpressionTreeNode Clone() => new ConstantTreeNode(this);

  public override string ToString() => $"{Value:E4}";
}
