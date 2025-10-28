namespace HEAL.HeuristicLib.Encodings.SymbolicExpression.Symbols.Math;

public abstract class LaggedSymbol(int minArity, int defaultArity, int maximumArity) : Symbol(minArity, defaultArity, maximumArity) {
  public virtual int MinLag { get; set; }
  public virtual int MaxLag { get; set; }
  public override SymbolicExpressionTreeNode CreateTreeNode() => new LaggedTreeNode(this);
}
