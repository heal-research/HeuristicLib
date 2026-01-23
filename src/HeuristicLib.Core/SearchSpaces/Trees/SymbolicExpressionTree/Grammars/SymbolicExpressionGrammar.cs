using HEAL.HeuristicLib.Genotypes.Trees;
using HEAL.HeuristicLib.SearchSpaces.Trees.SymbolicExpressionTree.Symbols;

namespace HEAL.HeuristicLib.SearchSpaces.Trees.SymbolicExpressionTree.Grammars;

public abstract class SymbolicExpressionGrammar : SymbolicExpressionGrammarBase, ISymbolicExpressionGrammar
{
  #region fields & properties

  public bool ReadOnly { get; set; }
  public bool Conforms(Genotypes.Trees.SymbolicExpressionTree symbolicExpressionTree) => Conforms(symbolicExpressionTree.Root);

  private bool Conforms(SymbolicExpressionTreeNode parent)
  {
    var symbol = parent.Symbol;
    if (!Symbols.Contains(symbol)) {
      return false;
    }

    if (parent.SubtreeCount > symbol.MaximumArity) {
      return false;
    }

    if (parent.SubtreeCount < symbol.MinimumArity) {
      return false;
    }

    var pos = 0;
    foreach (var child in parent.Subtrees) {
      if (!IsAllowedChildSymbol(symbol, child.Symbol, pos)) {
        return false;
      }

      if (!Conforms(child)) {
        return false;
      }

      pos++;
    }

    return true;
  }

  public int MinimumFunctionDefinitions { get; set; }
  public int MaximumFunctionDefinitions { get; set; }
  public int MinimumFunctionArguments { get; set; }
  public int MaximumFunctionArguments { get; set; }

  public ProgramRootSymbol ProgramRootSymbol { get; }
  public StartSymbol StartSymbol { get; }
  protected DefunSymbol DefunSymbol { get; }

  #endregion

  protected SymbolicExpressionGrammar()
  {
    ProgramRootSymbol = new ProgramRootSymbol();
    AddSymbol(ProgramRootSymbol);
    SetSubtreeCount(ProgramRootSymbol, 1, 1);

    StartSymbol = new StartSymbol();
    AddSymbol(StartSymbol);
    SetSubtreeCount(StartSymbol, 1, 1);

    DefunSymbol = new DefunSymbol();
    AddSymbol(DefunSymbol);
    SetSubtreeCount(DefunSymbol, 1, 1);

    AddAllowedChildSymbol(ProgramRootSymbol, StartSymbol, 0);
    UpdateAdfConstraints();
  }

  private void UpdateAdfConstraints()
  {
    SetSubtreeCount(ProgramRootSymbol, MinimumFunctionDefinitions + 1, MaximumFunctionDefinitions + 1);

    // ADF branches maxFunctionDefinitions 
    for (var argumentIndex = 1; argumentIndex < MaximumFunctionDefinitions + 1; argumentIndex++) {
      RemoveAllowedChildSymbol(ProgramRootSymbol, DefunSymbol, argumentIndex);
      AddAllowedChildSymbol(ProgramRootSymbol, DefunSymbol, argumentIndex);
    }
  }

  public sealed override void AddSymbol(Symbol symbol)
  {
    if (ReadOnly) {
      throw new InvalidOperationException();
    }

    base.AddSymbol(symbol);
  }

  public sealed override void RemoveSymbol(Symbol symbol)
  {
    if (ReadOnly) {
      throw new InvalidOperationException();
    }

    base.RemoveSymbol(symbol);
  }

  public sealed override void AddAllowedChildSymbol(Symbol parent, Symbol child)
  {
    if (ReadOnly) {
      throw new InvalidOperationException();
    }

    base.AddAllowedChildSymbol(parent, child);
  }

  public sealed override void AddAllowedChildSymbol(Symbol parent, Symbol child, int argumentIndex)
  {
    if (ReadOnly) {
      throw new InvalidOperationException();
    }

    base.AddAllowedChildSymbol(parent, child, argumentIndex);
  }

  public sealed override void RemoveAllowedChildSymbol(Symbol parent, Symbol child)
  {
    if (ReadOnly) {
      throw new InvalidOperationException();
    }

    base.RemoveAllowedChildSymbol(parent, child);
  }

  public sealed override void RemoveAllowedChildSymbol(Symbol parent, Symbol child, int argumentIndex)
  {
    if (ReadOnly) {
      throw new InvalidOperationException();
    }

    base.RemoveAllowedChildSymbol(parent, child, argumentIndex);
  }

  public sealed override void ClearAllowedChildSymbols(Symbol parent)
  {
    if (ReadOnly) {
      throw new InvalidOperationException();
    }

    base.ClearAllowedChildSymbols(parent);
  }

  public sealed override void ClearAllAllowedChildSymbols()
  {
    if (ReadOnly) {
      throw new InvalidOperationException();
    }

    base.ClearAllAllowedChildSymbols();
  }

  public sealed override void SetSubtreeCount(Symbol symbol, int minimumSubtreeCount, int maximumSubtreeCount)
  {
    if (ReadOnly) {
      throw new InvalidOperationException();
    }

    base.SetSubtreeCount(symbol, minimumSubtreeCount, maximumSubtreeCount);
  }
}
