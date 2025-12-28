using HEAL.HeuristicLib.Genotypes.Trees;

namespace HEAL.HeuristicLib.SearchSpaces.Trees.SymbolicExpressionTree.Symbols.Math;

public sealed class SubFunctionTreeNode : SymbolicExpressionTreeNode {
  #region Properties
  public new SubFunctionSymbol Symbol => (SubFunctionSymbol)base.Symbol;

  public IEnumerable<string> Arguments { get; set; } = [];

  public string Name { get; set; } = string.Empty;
  #endregion

  #region Constructors
  public SubFunctionTreeNode(SubFunctionSymbol symbol) : base(symbol) { }

  private SubFunctionTreeNode(SubFunctionTreeNode original) : base(original) {
    Arguments = original.Arguments;
    Name = original.Name;
  }
  #endregion

  #region Cloning
  public override SymbolicExpressionTreeNode Clone() => new SubFunctionTreeNode(this);
  #endregion

  public override string? ToString() {
    return string.IsNullOrEmpty(Name) ? base.ToString() : $"{Name}({string.Join(",", Arguments)})";
  }
}
