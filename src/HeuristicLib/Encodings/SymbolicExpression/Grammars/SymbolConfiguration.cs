using HEAL.HeuristicLib.Encodings.SymbolicExpression.Symbols;

namespace HEAL.HeuristicLib.Encodings.SymbolicExpression.Grammars;

public record SymbolConfiguration(
  (int minSubTreeCount, int maxSubTreeCount) SymbolSubtreeCount,
  List<Symbol> AllowedChildSymbols,
  Dictionary<int, List<Symbol>> AllowedChildSymbolsPerIndex) {
  public (int minSubTreeCount, int maxSubTreeCount) SymbolSubtreeCount { get; set; } = SymbolSubtreeCount;
}
