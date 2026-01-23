using HEAL.HeuristicLib.SearchSpaces.Trees.SymbolicExpressionTree.Symbols;

namespace HEAL.HeuristicLib.SearchSpaces.Trees.SymbolicExpressionTree.Grammars;

internal sealed class EmptySymbolicExpressionTreeGrammar : ISymbolicExpressionGrammar {
  private readonly ISymbolicExpressionGrammar grammar;

  internal EmptySymbolicExpressionTreeGrammar(ISymbolicExpressionGrammar grammar) {
    ArgumentNullException.ThrowIfNull(grammar);
    this.grammar = grammar;
  }

  public IEnumerable<Symbol> Symbols => grammar.Symbols;
  public IEnumerable<Symbol> AllowedSymbols => grammar.AllowedSymbols;

  public bool ContainsSymbol(Symbol symbol) => grammar.ContainsSymbol(symbol);

  public bool IsAllowedChildSymbol(Symbol parent, Symbol child) => grammar.IsAllowedChildSymbol(parent, child);

  public bool IsAllowedChildSymbol(Symbol parent, Symbol child, int argumentIndex) => grammar.IsAllowedChildSymbol(parent, child, argumentIndex);

  public IEnumerable<Symbol> GetAllowedChildSymbols(Symbol parent) => grammar.GetAllowedChildSymbols(parent);

  public IEnumerable<Symbol> GetAllowedChildSymbols(Symbol parent, int argumentIndex) => grammar.GetAllowedChildSymbols(parent, argumentIndex);

  public int GetMinimumSubtreeCount(Symbol symbol) => grammar.GetMinimumSubtreeCount(symbol);

  public int GetMaximumSubtreeCount(Symbol symbol) => grammar.GetMaximumSubtreeCount(symbol);

  public int GetMinimumExpressionDepth(Symbol start) => grammar.GetMinimumExpressionDepth(start);

  public int GetMaximumExpressionDepth(Symbol start) => grammar.GetMaximumExpressionDepth(start);

  public int GetMinimumExpressionLength(Symbol start) => grammar.GetMinimumExpressionLength(start);

  public int GetMaximumExpressionLength(Symbol start, int maxDepth) => grammar.GetMaximumExpressionLength(start, maxDepth);

  public void AddSymbol(Symbol symbol) { throw new NotSupportedException(); }
  public void RemoveSymbol(Symbol symbol) { throw new NotSupportedException(); }
  public void AddAllowedChildSymbol(Symbol parent, Symbol child) { throw new NotSupportedException(); }
  public void AddAllowedChildSymbol(Symbol parent, Symbol child, int argumentIndex) { throw new NotSupportedException(); }
  public void RemoveAllowedChildSymbol(Symbol parent, Symbol child) { throw new NotSupportedException(); }
  public void RemoveAllowedChildSymbol(Symbol parent, Symbol child, int argumentIndex) { throw new NotSupportedException(); }
  public void ClearAllowedChildSymbols(Symbol parent) { throw new NotSupportedException(); }
  public void ClearAllAllowedChildSymbols() { throw new NotSupportedException(); }
  public void SetSubtreeCount(Symbol symbol, int minimumSubtreeCount, int maximumSubtreeCount) { throw new NotSupportedException(); }

  public ProgramRootSymbol ProgramRootSymbol => grammar.ProgramRootSymbol;
  public StartSymbol StartSymbol => grammar.StartSymbol;
  public int MinimumFunctionDefinitions { get => grammar.MinimumFunctionDefinitions; set => grammar.MinimumFunctionDefinitions = value; }
  public int MaximumFunctionDefinitions { get => grammar.MaximumFunctionDefinitions; set => grammar.MaximumFunctionDefinitions = value; }
  public int MinimumFunctionArguments { get => grammar.MinimumFunctionArguments; set => grammar.MinimumFunctionArguments = value; }
  public int MaximumFunctionArguments { get => grammar.MaximumFunctionArguments; set => grammar.MaximumFunctionArguments = value; }
  public bool ReadOnly { get; set; }
  public bool Conforms(Genotypes.Trees.SymbolicExpressionTree symbolicExpressionTree) => symbolicExpressionTree.Root.Symbol == grammar.ProgramRootSymbol;
}
