using HEAL.HeuristicLib.Encodings.SymbolicExpression.Symbols;

namespace HEAL.HeuristicLib.Encodings.SymbolicExpression.Grammars;

public interface ISymbolicExpressionGrammarBase {
  IEnumerable<Symbol> Symbols { get; }
  IEnumerable<Symbol> AllowedSymbols { get; }

  bool ContainsSymbol(Symbol symbol);

  void AddSymbol(Symbol symbol);
  void RemoveSymbol(Symbol symbol);

  bool IsAllowedChildSymbol(Symbol parent, Symbol child);
  bool IsAllowedChildSymbol(Symbol parent, Symbol child, int argumentIndex);
  IEnumerable<Symbol> GetAllowedChildSymbols(Symbol parent);
  IEnumerable<Symbol> GetAllowedChildSymbols(Symbol parent, int argumentIndex);

  void AddAllowedChildSymbol(Symbol parent, Symbol child);
  void AddAllowedChildSymbol(Symbol parent, Symbol child, int argumentIndex);
  void RemoveAllowedChildSymbol(Symbol parent, Symbol child);
  void RemoveAllowedChildSymbol(Symbol parent, Symbol child, int argumentIndex);
  void ClearAllowedChildSymbols(Symbol parent);
  void ClearAllAllowedChildSymbols();

  int GetMinimumSubtreeCount(Symbol symbol);
  int GetMaximumSubtreeCount(Symbol symbol);
  void SetSubtreeCount(Symbol symbol, int minimumSubtreeCount, int maximumSubtreeCount);

  int GetMinimumExpressionDepth(Symbol start);
  int GetMaximumExpressionDepth(Symbol start);
  int GetMinimumExpressionLength(Symbol start);
  int GetMaximumExpressionLength(Symbol start, int maxDepth);
}
