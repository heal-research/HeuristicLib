using HEAL.HeuristicLib.Encodings.SymbolicExpression.Symbols;

namespace HEAL.HeuristicLib.Encodings.SymbolicExpression.Grammars;

/// <summary>
/// The default symbolic expression grammar stores symbols and syntactic constraints for symbols.
/// Symbols are treated as equivalent if they have the same name.
/// Syntactic constraints limit the number of allowed subtrees for a node with a symbol and which symbols are allowed 
/// in the subtrees of a symbol (can be specified for each subtree index separately).
/// </summary>
public abstract class SymbolicExpressionGrammarBase {
  protected readonly Dictionary<Symbol, SymbolConfiguration> SymbolConfigurations = [];

  #region protected grammar manipulation methods
  public virtual void AddSymbol(Symbol symbol) {
    if (ContainsSymbol(symbol))
      throw new ArgumentException("Symbol " + symbol + " is already defined.");
    foreach (var s in symbol.Flatten()) {
      SymbolConfigurations.Add(s, new SymbolConfiguration((s.MinimumArity, Math.Min(s.MinimumArity + 1, s.MaximumArity)), [], []));
    }

    ClearCaches();
  }

  public virtual void RemoveSymbol(Symbol symbol) {
    foreach (var s in symbol.Flatten()) {
      SymbolConfigurations.Remove(s);

      foreach (var (parent, config) in SymbolConfigurations) {
        config.AllowedChildSymbols.Remove(s);
        for (var i = 0; i < GetMaximumSubtreeCount(parent); i++) {
          if (config.AllowedChildSymbolsPerIndex.TryGetValue(i, out var l))
            l.Remove(s);
        }
      }

      foreach (var groupSymbol in Symbols.OfType<GroupSymbol>())
        groupSymbol.SymbolsCollection.Remove(symbol);
    }

    ClearCaches();
  }

  public virtual void AddAllowedChildSymbol(Symbol parent, Symbol child) {
    var changed = false;

    foreach (var p in parent.Flatten().Where(p => p is not GroupSymbol))
      changed |= AddAllowedChildSymbolToDictionaries(p, child);

    if (changed)
      ClearCaches();
  }

  public virtual void AddAllowedChildSymbol(Symbol parent, Symbol child, int argumentIndex) {
    var changed = false;

    foreach (var p in parent.Flatten().Where(p => p is not GroupSymbol))
      changed |= AddAllowedChildSymbolToDictionaries(p, child, argumentIndex);

    if (changed)
      ClearCaches();
  }

  private bool AddAllowedChildSymbolToDictionaries(Symbol parent, Symbol child) {
    var pconfig = GetConfig(parent);
    if (pconfig.AllowedChildSymbols.Contains(child))
      return false;

    for (var argumentIndex = 0; argumentIndex < GetMaximumSubtreeCount(parent); argumentIndex++)
      RemoveAllowedChildSymbol(parent, child, argumentIndex);

    pconfig.AllowedChildSymbols.Add(child);
    return true;
  }

  private bool AddAllowedChildSymbolToDictionaries(Symbol parent, Symbol child, int argumentIndex) {
    var pconfig = GetConfig(parent);
    var childSymbols = pconfig.AllowedChildSymbols;

    if (childSymbols.Contains(child))
      return false;
    childSymbols = pconfig.AllowedChildSymbolsPerIndex.GetOrInitialize(argumentIndex, []);

    if (childSymbols.Contains(child))
      return false;
    childSymbols.Add(child);
    return true;
  }

  public virtual void RemoveAllowedChildSymbol(Symbol parent, Symbol child) {
    var pconfig = GetConfig(parent);
    var childSymbols = pconfig.AllowedChildSymbols;
    var changed = childSymbols.Remove(child);

    for (var argumentIndex = 0; argumentIndex < GetMaximumSubtreeCount(parent); argumentIndex++) {
      if (pconfig.AllowedChildSymbolsPerIndex.TryGetValue(argumentIndex, out childSymbols))
        changed |= childSymbols.Remove(child);
    }

    if (changed)
      ClearCaches();
  }

  public virtual void RemoveAllowedChildSymbol(Symbol parent, Symbol child, int argumentIndex) {
    var changed = false;
    var pconfig = GetConfig(parent);

    if (pconfig.AllowedChildSymbols.Remove(child)) {
      for (var i = 0; i < GetMaximumSubtreeCount(parent); i++) {
        if (i != argumentIndex)
          AddAllowedChildSymbol(parent, child, i);
      }

      changed = true;
    }

    changed |= pconfig.AllowedChildSymbols.Remove(child);

    if (changed)
      ClearCaches();
  }

  public virtual void ClearAllowedChildSymbols(Symbol parent) {
    var changed = ClearAllowedChildSymbolsDictionaries(parent);
    if (changed) {
      ClearCaches();
    }
  }

  public virtual void ClearAllAllowedChildSymbols() {
    var changed = false;
    foreach (var symbol in Symbols)
      changed |= ClearAllowedChildSymbolsDictionaries(symbol);

    if (changed) {
      ClearCaches();
    }
  }

  private bool ClearAllowedChildSymbolsDictionaries(Symbol parent) {
    var pconfig = GetConfig(parent);
    var childSymbols = pconfig.AllowedChildSymbols;
    var changed = false;
    changed |= childSymbols.Count > 0;
    childSymbols.Clear();

    for (var argumentIndex = 0; argumentIndex < GetMaximumSubtreeCount(parent); argumentIndex++) {
      if (!pconfig.AllowedChildSymbolsPerIndex.TryGetValue(argumentIndex, out childSymbols))
        continue;
      changed |= childSymbols.Count > 0;
      childSymbols.Clear();
    }

    return changed;
  }

  public virtual void SetSubtreeCount(Symbol symbol, int minimumSubtreeCount, int maximumSubtreeCount) {
    var symbols = symbol.Flatten().Where(s => s is not GroupSymbol).ToArray();
    if (symbols.Any(s => s.MinimumArity > minimumSubtreeCount))
      throw new ArgumentException("Invalid minimum subtree count " + minimumSubtreeCount + " for " + symbol);
    if (symbols.Any(s => s.MaximumArity < maximumSubtreeCount))
      throw new ArgumentException("Invalid maximum subtree count " + maximumSubtreeCount + " for " + symbol);

    foreach (var s in symbols)
      SetSubTreeCountInDictionaries(s, minimumSubtreeCount, maximumSubtreeCount);
    ClearCaches();
  }

  private void SetSubTreeCountInDictionaries(Symbol symbol, int minimumSubtreeCount, int maximumSubtreeCount) {
    var config = GetConfig(symbol);
    for (var i = maximumSubtreeCount; i < GetMaximumSubtreeCount(symbol); i++) {
      if (config.AllowedChildSymbolsPerIndex.TryGetValue(i, out var childSymbols))
        childSymbols.Clear();
    }

    config.SymbolSubtreeCount = (minimumSubtreeCount, maximumSubtreeCount);
  }
  #endregion

  public virtual IEnumerable<Symbol> Symbols => SymbolConfigurations.Keys;
  public virtual IEnumerable<Symbol> AllowedSymbols => Symbols.Where(s => s.Enabled);
  public virtual bool ContainsSymbol(Symbol symbol) => SymbolConfigurations.ContainsKey(symbol);

  private Dictionary<Tuple<Symbol, Symbol>, bool> cachedIsAllowedChildSymbol = new();
  private readonly Lock cachedIsAllowedChildSymbolLock = new();
  private Dictionary<Tuple<Symbol, Symbol, int>, bool> cachedIsAllowedChildSymbolIndex = new();
  private readonly Lock cachedIsAllowedChildSymbolIndexLock = new();

  public virtual bool IsAllowedChildSymbol(Symbol parent, Symbol child) {
    if (SymbolConfigurations.Count == 0)
      return false;
    if (!child.Enabled)
      return false;

    var key = Tuple.Create(parent, child);
    if (cachedIsAllowedChildSymbol.TryGetValue(key, out var result))
      return result;

    // value has to be calculated and cached make sure this is done in only one thread
    lock (cachedIsAllowedChildSymbolLock) {
      // in case the value has been calculated on another thread in the meanwhile
      if (cachedIsAllowedChildSymbol.TryGetValue(key, out result))
        return result;

      var state = SymbolConfigurations.TryGetValue(parent, out var config) && config.AllowedChildSymbols.SelectMany(x => x.Flatten()).Contains(child);
      cachedIsAllowedChildSymbol.Add(key, state);
      return state;
    }
  }

  public virtual bool IsAllowedChildSymbol(Symbol parent, Symbol child, int argumentIndex) {
    if (!child.Enabled)
      return false;
    if (IsAllowedChildSymbol(parent, child))
      return true;
    if (SymbolConfigurations.Count == 0)
      return false;

    var key = Tuple.Create(parent, child, argumentIndex);
    if (cachedIsAllowedChildSymbolIndex.TryGetValue(key, out var result))
      return result;

    // value has to be calculated and cached make sure this is done in only one thread
    lock (cachedIsAllowedChildSymbolIndexLock) {
      // in case the value has been calculated on another thread in the meanwhile
      if (cachedIsAllowedChildSymbolIndex.TryGetValue(key, out result))
        return result;
      var state = SymbolConfigurations.TryGetValue(parent, out var config)
                  && config.AllowedChildSymbolsPerIndex.TryGetValue(argumentIndex, out var l)
                  && l.SelectMany(x => x.Flatten()).Contains(child);

      cachedIsAllowedChildSymbolIndex.Add(key, state);
      return state;
    }
  }

  public IEnumerable<Symbol> GetAllowedChildSymbols(Symbol parent)
    => AllowedSymbols.Where(child => IsAllowedChildSymbol(parent, child));

  public IEnumerable<Symbol> GetAllowedChildSymbols(Symbol parent, int argumentIndex)
    => AllowedSymbols.Where(child => IsAllowedChildSymbol(parent, child, argumentIndex));

  public virtual int GetMinimumSubtreeCount(Symbol symbol) => SymbolConfigurations[symbol].SymbolSubtreeCount.Item1;

  public virtual int GetMaximumSubtreeCount(Symbol symbol) => SymbolConfigurations[symbol].SymbolSubtreeCount.Item2;

  protected void ClearCaches() {
    cachedMinExpressionLength = [];
    cachedMaxExpressionLength = [];
    cachedMinExpressionDepth = [];
    cachedMaxExpressionDepth = [];
    cachedIsAllowedChildSymbol = [];
    cachedIsAllowedChildSymbolIndex = [];
  }

  private Dictionary<Symbol, int> cachedMinExpressionLength = new();
  private readonly Lock cachedMinExpressionLengthLock = new();

  public int GetMinimumExpressionLength(Symbol symbol) {
    if (cachedMinExpressionLength.TryGetValue(symbol, out var res))
      return res;

    // value has to be calculated and cached make sure this is done in only one thread
    lock (cachedMinExpressionLengthLock) {
      // in case the value has been calculated on another thread in the meanwhile
      if (cachedMinExpressionLength.TryGetValue(symbol, out res))
        return res;

      var cMin = new Dictionary<Symbol, int>(cachedMinExpressionLength);
      GrammarUtils.CalculateMinimumExpressionLengths(this, cMin);
      cachedMinExpressionLength = cMin;

      return cachedMinExpressionLength[symbol];
    }
  }

  private Dictionary<Tuple<Symbol, int>, int> cachedMaxExpressionLength = new();
  private readonly Lock cachedMaxExpressionLengthLock = new();

  public int GetMaximumExpressionLength(Symbol symbol, int maxDepth) {
    var key = Tuple.Create(symbol, maxDepth);
    if (cachedMaxExpressionLength.TryGetValue(key, out var temp))
      return temp;
    // value has to be calculated and cached make sure this is done in only one thread
    lock (cachedMaxExpressionLengthLock) {
      // in case the value has been calculated on another thread in the meanwhile
      if (cachedMaxExpressionLength.TryGetValue(key, out temp))
        return temp;

      var cMin = new Dictionary<Tuple<Symbol, int>, int>(cachedMaxExpressionLength);
      var res = GetMaximumExpressionLength(symbol, maxDepth, cMin);
      cachedMaxExpressionLength = cMin;
      return res;
    }
  }

  private int GetMaximumExpressionLength(Symbol symbol, int maxDepth, Dictionary<Tuple<Symbol, int>, int> tempMaxExpressionLength) {
    var key = Tuple.Create(symbol, maxDepth);
    // in case the value has been calculated on another thread in the meanwhile
    if (tempMaxExpressionLength.TryGetValue(key, out var temp))
      return temp;

    tempMaxExpressionLength[key] = int.MaxValue; // prevent infinite recursion
    var sumOfMaxTrees = 1 + (from argIndex in Enumerable.Range(0, GetMaximumSubtreeCount(symbol))
                             let maxForSlot = (long)(from s in GetAllowedChildSymbols(symbol, argIndex)
                                                     where s.InitialFrequency > 0.0
                                                     where GetMinimumExpressionDepth(s) < maxDepth
                                                     select GetMaximumExpressionLength(s, maxDepth - 1, tempMaxExpressionLength)).DefaultIfEmpty(0).Max()
                             select maxForSlot).DefaultIfEmpty(0).Sum();

    return tempMaxExpressionLength[key] = (int)Math.Min(sumOfMaxTrees, int.MaxValue);
  }

  private Dictionary<Symbol, int> cachedMinExpressionDepth = new();
  private readonly Lock cachedMinExpressionDepthLock = new();

  public int GetMinimumExpressionDepth(Symbol symbol) {
    if (cachedMinExpressionDepth.TryGetValue(symbol, out var res))
      return res;

    // value has to be calculated and cached make sure this is done in only one thread
    lock (cachedMinExpressionDepthLock) {
      // in case the value has been calculated on another thread in the meanwhile
      if (cachedMinExpressionDepth.TryGetValue(symbol, out res))
        return res;

      var cMin = new Dictionary<Symbol, int>(cachedMinExpressionDepth);
      GrammarUtils.CalculateMinimumExpressionDepth(this, cMin);
      cachedMinExpressionDepth = cMin;
      return cachedMinExpressionDepth[symbol];
    }
  }

  private Dictionary<Symbol, int> cachedMaxExpressionDepth = new();
  private readonly Lock cachedMaxExpressionDepthLock = new();

  public int GetMaximumExpressionDepth(Symbol symbol) {
    if (cachedMaxExpressionDepth.TryGetValue(symbol, out var temp))
      return temp;
    // value has to be calculated and cached make sure this is done in only one thread
    lock (cachedMaxExpressionDepthLock) {
      // in case the value has been calculated on another thread in the meanwhile
      if (cachedMaxExpressionDepth.TryGetValue(symbol, out temp))
        return temp;
      var cMin = new Dictionary<Symbol, int>(cachedMaxExpressionDepth);
      var res = GetMaximumExpressionDepth(symbol, cMin);
      cachedMaxExpressionDepth = cMin;
      return res;
    }
  }

  private int GetMaximumExpressionDepth(Symbol symbol, Dictionary<Symbol, int> tempCachedMaxExpressionDepth) {
    // in case the value has been calculated on another thread in the meanwhile
    if (tempCachedMaxExpressionDepth.TryGetValue(symbol, out var temp))
      return temp;

    tempCachedMaxExpressionDepth[symbol] = int.MaxValue; // prevent infinite recursion
    var maxDepth = 1 + (from argIndex in Enumerable.Range(0, GetMaximumSubtreeCount(symbol))
                        let maxForSlot = (long)(from s in GetAllowedChildSymbols(symbol, argIndex)
                                                where s.InitialFrequency > 0.0
                                                select GetMaximumExpressionDepth(s, tempCachedMaxExpressionDepth)).DefaultIfEmpty(0).Max()
                        select maxForSlot).DefaultIfEmpty(0).Max();
    return tempCachedMaxExpressionDepth[symbol] = (int)Math.Min(maxDepth, int.MaxValue);
  }

  private SymbolConfiguration GetConfig(Symbol parent) {
    if (!SymbolConfigurations.TryGetValue(parent, out var pconfig))
      throw new ArgumentException("Symbol not in Grammar", nameof(parent));
    return pconfig;
  }
}
