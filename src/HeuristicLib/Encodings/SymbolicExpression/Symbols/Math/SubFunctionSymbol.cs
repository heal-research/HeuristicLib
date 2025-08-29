namespace HEAL.HeuristicLib.Encodings.SymbolicExpression.Symbols.Math;

public sealed class SubFunctionSymbol : Symbol {
  public override int MinimumArity => 0;
  public override int MaximumArity => 1;

  public override SymbolicExpressionTreeNode CreateTreeNode() => new SubFunctionTreeNode(this);
}
