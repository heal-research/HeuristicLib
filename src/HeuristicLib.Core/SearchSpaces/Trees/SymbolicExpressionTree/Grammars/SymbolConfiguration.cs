using HEAL.HeuristicLib.SearchSpaces.Trees.SymbolicExpressionTree.Symbols;

namespace HEAL.HeuristicLib.SearchSpaces.Trees.SymbolicExpressionTree.Grammars;

public record SymbolConfiguration(
  (int minSubTreeCount, int maxSubTreeCount) SymbolSubtreeCount,
  List<Symbol> AllowedChildSymbols,
  Dictionary<int, List<Symbol>> AllowedChildSymbolsPerIndex) {
  public (int minSubTreeCount, int maxSubTreeCount) SymbolSubtreeCount { get; set; } = SymbolSubtreeCount;
}
