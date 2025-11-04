namespace HEAL.HeuristicLib.Encodings.SymbolicExpressionTree.Symbols;

public sealed class DefunTreeNode : SymbolicExpressionTreeNode {
  public DefunTreeNode(DefunSymbol defunSymbol, string functionName) : base(defunSymbol) {
    FunctionName = functionName;
  }

  private DefunTreeNode(DefunTreeNode original)
    : base(original) {
    NumberOfArguments = original.NumberOfArguments;
    FunctionName = original.FunctionName;
  }

  public override SymbolicExpressionTreeNode Clone() {
    return new DefunTreeNode(this);
  }

  public int NumberOfArguments { get; set; }
  public string FunctionName { get; set; }
}
