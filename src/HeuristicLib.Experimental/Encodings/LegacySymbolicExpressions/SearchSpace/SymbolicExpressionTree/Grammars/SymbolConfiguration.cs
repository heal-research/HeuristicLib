namespace HEAL.HeuristicLib.Encodings.LegacySymbolicExpressions;

public sealed class SymbolConfiguration(
  (int minSubTreeCount, int maxSubTreeCount) SymbolSubtreeCount,
  List<Symbol> AllowedChildSymbols,
  Dictionary<int, List<Symbol>> AllowedChildSymbolsPerIndex)
{
    public (int minSubTreeCount, int maxSubTreeCount) SymbolSubtreeCount { get; set; } = SymbolSubtreeCount;
    public List<Symbol> AllowedChildSymbols { get; init; } = AllowedChildSymbols;
    public Dictionary<int, List<Symbol>> AllowedChildSymbolsPerIndex { get; init; } = AllowedChildSymbolsPerIndex;
}
