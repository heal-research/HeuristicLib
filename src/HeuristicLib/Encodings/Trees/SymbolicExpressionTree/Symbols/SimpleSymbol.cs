namespace HEAL.HeuristicLib.Encodings.Trees.SymbolicExpressionTree.Symbols;

public sealed class SimpleSymbol(int minimumArity, int maximumArity) : Symbol(minimumArity, minimumArity, maximumArity) {
  public SimpleSymbol(int arity) : this(arity, arity) { }
}
