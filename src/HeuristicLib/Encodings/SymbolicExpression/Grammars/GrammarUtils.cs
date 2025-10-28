using HEAL.HeuristicLib.Encodings.SymbolicExpression.Symbols;

namespace HEAL.HeuristicLib.Encodings.SymbolicExpression.Grammars;

public static class GrammarUtils {
  private static IEnumerable<Symbol> GetTopmostSymbols(this SymbolicExpressionGrammarBase grammar) {
    // build parents list so we can find out the topmost symbol(s)
    var parents = new Dictionary<Symbol, List<Symbol>>();
    foreach (var symbol in grammar.Symbols.Where(x => grammar.GetMinimumSubtreeCount(x) > 0)) {
      var minSubtreeCount = grammar.GetMinimumSubtreeCount(symbol);
      for (var argIndex = 0; argIndex < minSubtreeCount; ++argIndex) {
        foreach (var childSymbol in grammar.GetAllowedActiveSymbols(symbol, argIndex)) {
          if (!parents.ContainsKey(childSymbol))
            parents[childSymbol] = [];
          parents[childSymbol].Add(symbol);
        }
      }
    }

    // the topmost symbols have no parents
    return parents.Values.SelectMany(x => x).Distinct().Where(x => !parents.ContainsKey(x));
  }

  private static IReadOnlyCollection<Symbol> IterateBreadthReverse(this SymbolicExpressionGrammarBase grammar, Symbol topSymbol) {
    // sort symbols in reverse breadth order (starting from the topSymbol)
    // each symbol is visited only once (this avoids infinite recursion)
    var symbols = new List<Symbol> { topSymbol };
    var visited = new HashSet<Symbol> { topSymbol };
    var i = 0;
    while (i < symbols.Count) {
      var symbol = symbols[i];
      var minSubtreeCount = grammar.GetMinimumSubtreeCount(symbol);

      for (var argIndex = 0; argIndex < minSubtreeCount; ++argIndex) {
        foreach (var childSymbol in grammar.GetAllowedActiveSymbols(symbol, argIndex)) {
          if (grammar.GetMinimumSubtreeCount(childSymbol) == 0)
            continue;

          if (visited.Add(childSymbol))
            symbols.Add(childSymbol);
        }
      }

      ++i;
    }

    symbols.Reverse();
    return symbols;
  }

  private static IEnumerable<Symbol> GetAllowedActiveSymbols(this SymbolicExpressionGrammarBase grammar, Symbol symbol, int argIndex) {
    return grammar.GetAllowedChildSymbols(symbol, argIndex).Where(s => s.InitialFrequency > 0);
  }

  public static void CalculateMinimumExpressionLengths(SymbolicExpressionGrammarBase grammar,
                                                       Dictionary<Symbol, int> minimumExpressionLengths) {
    minimumExpressionLengths.Clear();
    //terminal symbols => minimum expression length = 1
    foreach (var s in grammar.Symbols) {
      if (grammar.GetMinimumSubtreeCount(s) == 0)
        minimumExpressionLengths[s] = 1;
      else
        minimumExpressionLengths[s] = int.MaxValue;
    }

    foreach (var topSymbol in grammar.GetTopmostSymbols()) {
      // get all symbols below in reverse breadth order
      // this way we ensure lengths are calculated bottom-up
      var symbols = grammar.IterateBreadthReverse(topSymbol);
      foreach (var symbol in symbols) {
        long minLength = 1;
        for (var argIndex = 0; argIndex < grammar.GetMinimumSubtreeCount(symbol); ++argIndex) {
          long length = grammar.GetAllowedActiveSymbols(symbol, argIndex)
                               .Where(minimumExpressionLengths.ContainsKey)
                               .Select(x => minimumExpressionLengths[x]).DefaultIfEmpty(int.MaxValue).Min();
          minLength += length;
        }

        if (minimumExpressionLengths.TryGetValue(symbol, out var oldLength))
          minLength = Math.Min(minLength, oldLength);
        minimumExpressionLengths[symbol] = (int)Math.Min(int.MaxValue, minLength);
      }

      // correction step for cycles
      var changed = true;
      while (changed) {
        changed = false;
        foreach (var symbol in symbols) {
          var minLength = Enumerable.Range(0, grammar.GetMinimumSubtreeCount(symbol))
                                    .Sum(x => grammar.GetAllowedActiveSymbols(symbol, x)
                                                     .Select(s => (long)minimumExpressionLengths[s]).DefaultIfEmpty(int.MaxValue).Min()) + 1;
          if (minLength < minimumExpressionLengths[symbol]) {
            minimumExpressionLengths[symbol] = (int)Math.Min(minLength, int.MaxValue);
            changed = true;
          }
        }
      }
    }
  }

  public static void CalculateMinimumExpressionDepth(SymbolicExpressionGrammarBase grammar,
                                                     Dictionary<Symbol, int> minimumExpressionDepths) {
    minimumExpressionDepths.Clear();
    //terminal symbols => minimum expression depth = 1
    foreach (var s in grammar.Symbols) {
      if (grammar.GetMinimumSubtreeCount(s) == 0)
        minimumExpressionDepths[s] = 1;
      else
        minimumExpressionDepths[s] = int.MaxValue;
    }

    foreach (var topSymbol in grammar.GetTopmostSymbols()) {
      // get all symbols below in reverse breadth order
      // this way we ensure lengths are calculated bottom-up
      var symbols = grammar.IterateBreadthReverse(topSymbol);
      foreach (var symbol in symbols) {
        long minDepth = -1;
        for (var argIndex = 0; argIndex < grammar.GetMinimumSubtreeCount(symbol); ++argIndex) {
          var depth = grammar.GetAllowedActiveSymbols(symbol, argIndex)
                             .Where(minimumExpressionDepths.ContainsKey)
                             .Select(x => (long)minimumExpressionDepths[x]).DefaultIfEmpty(int.MaxValue).Min() + 1;
          minDepth = Math.Max(minDepth, depth);
        }

        if (minimumExpressionDepths.TryGetValue(symbol, out var oldDepth))
          minDepth = Math.Min(minDepth, oldDepth);
        minimumExpressionDepths[symbol] = (int)Math.Min(int.MaxValue, minDepth);
      }

      // correction step for cycles
      var changed = true;
      while (changed) {
        changed = false;
        foreach (var symbol in symbols) {
          var minDepth = Enumerable.Range(0, grammar.GetMinimumSubtreeCount(symbol))
                                   .Max(x => grammar.GetAllowedActiveSymbols(symbol, x)
                                                    .Select(s => (long)minimumExpressionDepths[s]).DefaultIfEmpty(int.MaxValue).Min()) + 1;
          if (minDepth < minimumExpressionDepths[symbol]) {
            minimumExpressionDepths[symbol] = (int)Math.Min(minDepth, int.MaxValue);
            changed = true;
          }
        }
      }
    }
  }
}
