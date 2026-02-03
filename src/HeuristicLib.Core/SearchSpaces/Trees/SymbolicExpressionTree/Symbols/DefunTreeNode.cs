using HEAL.HeuristicLib.Genotypes.Trees;

namespace HEAL.HeuristicLib.SearchSpaces.Trees.SymbolicExpressionTree.Symbols;

public sealed class DefunTreeNode : SymbolicExpressionTreeNode
{
  public DefunTreeNode(DefunSymbol defunSymbol, string functionName) : base(defunSymbol) => FunctionName = functionName;

  private DefunTreeNode(DefunTreeNode original)
    : base(original)
  {
    NumberOfArguments = original.NumberOfArguments;
    FunctionName = original.FunctionName;
  }

  public int NumberOfArguments { get; set; }
  public string FunctionName { get; set; }

  public override SymbolicExpressionTreeNode Clone() => new DefunTreeNode(this);
}
