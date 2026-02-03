namespace HEAL.HeuristicLib.Encodings.SymbolicExpressionTree.Symbols;

/// <summary>
/// Symbol for function arguments
/// </summary>
public sealed class ArgumentSymbol(int argumentIndex) : Symbol(0, 0, 0) {
  public int ArgumentIndex => argumentIndex;
}
