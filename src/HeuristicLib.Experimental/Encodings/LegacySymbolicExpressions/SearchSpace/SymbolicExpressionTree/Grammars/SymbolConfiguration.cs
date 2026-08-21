namespace HEAL.HeuristicLib.Encodings.LegacySymbolicExpressions;

public record SymbolConfiguration(
  (int minSubTreeCount, int maxSubTreeCount) SymbolSubtreeCount,
  List<Symbol> AllowedChildSymbols,
  Dictionary<int, List<Symbol>> AllowedChildSymbolsPerIndex)
{
    public (int minSubTreeCount, int maxSubTreeCount) SymbolSubtreeCount { get; set; } = SymbolSubtreeCount;
}
