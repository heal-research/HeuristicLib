namespace HEAL.HeuristicLib.SearchSpaces.Trees.SymbolicExpressionTree.Symbols;

/// <summary>
/// Symbol for function arguments
/// </summary>
public sealed class ArgumentSymbol(int argumentIndex) : Symbol(0, 0, 0) {
  public int ArgumentIndex => argumentIndex;
}
