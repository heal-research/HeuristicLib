using HEAL.HeuristicLib.Encodings.SymbolicExpressionTree.Symbols;

namespace HEAL.HeuristicLib.Encodings.SymbolicExpressionTree.Grammars;

public sealed class SimpleSymbolicExpressionGrammar() : SymbolicExpressionGrammar() {
  public void AddSymbol(SimpleSymbol symbol) {
    SetSubtreeCount(symbol, symbol.MinimumArity, symbol.MaximumArity);

    foreach (var s in Symbols) {
      if (s == ProgramRootSymbol)
        continue;
      if (s.MaximumArity > 0)
        AddAllowedChildSymbol(s, symbol);
      if (s == DefunSymbol)
        continue;
      if (s == StartSymbol)
        continue;
      if (symbol.MaximumArity > 0)
        AddAllowedChildSymbol(symbol, s);
    }
  }
}
