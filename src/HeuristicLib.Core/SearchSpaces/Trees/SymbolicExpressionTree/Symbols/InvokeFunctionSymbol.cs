namespace HEAL.HeuristicLib.SearchSpaces.Trees.SymbolicExpressionTree.Symbols;

/// <summary>
///   Symbol for invoking automatically defined functions
/// </summary>
public sealed class InvokeFunctionSymbol(string functionName) : Symbol(0, 1, byte.MaxValue)
{
  public string FunctionName => functionName;
}
