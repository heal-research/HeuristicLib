#region License Information
/* HeuristicLab
 * Copyright (C) Heuristic and Evolutionary Algorithms Laboratory (HEAL)
 *
 * This file is part of HeuristicLab.
 *
 * HeuristicLab is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * HeuristicLab is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with HeuristicLab. If not, see <http://www.gnu.org/licenses/>.
 */
#endregion

using HEAL.HeuristicLib.Encodings.SymbolicExpression.Symbols;
using HEAL.HeuristicLib.Operators.SymbolicExpression.Grammars;

namespace HEAL.HeuristicLib.Encodings.Grammars {
  public abstract class SymbolicExpressionGrammar : SymbolicExpressionGrammarBase, IComparerSymbolicExpressionGrammar {
    #region fields & properties
    public bool ReadOnly { get; set; }

    public int MinimumFunctionDefinitions { get; set; }
    public int MaximumFunctionDefinitions { get; set; }
    public int MinimumFunctionArguments { get; set; }
    public int MaximumFunctionArguments { get; set; }

    public ProgramRootSymbol ProgramRootSymbol { get; private set; }
    public StartSymbol StartSymbol { get; private set; }
    protected DefunSymbol DefunSymbol { get; }

    private readonly ISymbolicExpressionTreeGrammar emptyGrammar;
    #endregion

    protected SymbolicExpressionGrammar(string name, string description) {
      emptyGrammar = new EmptySymbolicExpressionTreeGrammar(this);

      ProgramRootSymbol = new();
      AddSymbol(ProgramRootSymbol);
      SetSubtreeCount(ProgramRootSymbol, 1, 1);

      StartSymbol = new();
      AddSymbol(StartSymbol);
      SetSubtreeCount(StartSymbol, 1, 1);

      DefunSymbol = new();
      AddSymbol(DefunSymbol);
      SetSubtreeCount(DefunSymbol, 1, 1);

      AddAllowedChildSymbol(ProgramRootSymbol, StartSymbol, 0);
      UpdateAdfConstraints();
    }

    private void UpdateAdfConstraints() {
      SetSubtreeCount(ProgramRootSymbol, MinimumFunctionDefinitions + 1, MaximumFunctionDefinitions + 1);

      // ADF branches maxFunctionDefinitions 
      for (var argumentIndex = 1; argumentIndex < MaximumFunctionDefinitions + 1; argumentIndex++) {
        RemoveAllowedChildSymbol(ProgramRootSymbol, DefunSymbol, argumentIndex);
        AddAllowedChildSymbol(ProgramRootSymbol, DefunSymbol, argumentIndex);
      }
    }

    public ISymbolicExpressionTreeGrammar CreateExpressionTreeGrammar() {
      return MaximumFunctionDefinitions == 0 ? emptyGrammar : new SymbolicExpressionTreeGrammar(this);
    }

    public sealed override void AddSymbol(Symbol symbol) {
      if (ReadOnly)
        throw new InvalidOperationException();
      base.AddSymbol(symbol);
      RegisterSymbolEvents(symbol);
      OnChanged();
    }

    public sealed override void RemoveSymbol(Symbol symbol) {
      if (ReadOnly)
        throw new InvalidOperationException();
      DeregisterSymbolEvents(symbol);
      base.RemoveSymbol(symbol);
      OnChanged();
    }

    public event EventHandler ReadOnlyChanged;

    protected virtual void OnReadOnlyChanged() {
      ReadOnlyChanged?.Invoke(this, EventArgs.Empty);
    }

    public sealed override void AddAllowedChildSymbol(Symbol parent, Symbol child) {
      if (ReadOnly)
        throw new InvalidOperationException();
      base.AddAllowedChildSymbol(parent, child);
    }

    public sealed override void AddAllowedChildSymbol(Symbol parent, Symbol child, int argumentIndex) {
      if (ReadOnly)
        throw new InvalidOperationException();
      base.AddAllowedChildSymbol(parent, child, argumentIndex);
    }

    public sealed override void RemoveAllowedChildSymbol(Symbol parent, Symbol child) {
      if (ReadOnly)
        throw new InvalidOperationException();
      base.RemoveAllowedChildSymbol(parent, child);
    }

    public sealed override void RemoveAllowedChildSymbol(Symbol parent, Symbol child, int argumentIndex) {
      if (ReadOnly)
        throw new InvalidOperationException();
      base.RemoveAllowedChildSymbol(parent, child, argumentIndex);
    }

    public sealed override void ClearAllowedChildSymbols(Symbol parent) {
      if (ReadOnly)
        throw new InvalidOperationException();
      base.ClearAllowedChildSymbols(parent);
    }

    public sealed override void ClearAllAllowedChildSymbols() {
      if (ReadOnly)
        throw new InvalidOperationException();
      base.ClearAllAllowedChildSymbols();
    }

    public sealed override void SetSubtreeCount(Symbol symbol, int minimumSubtreeCount, int maximumSubtreeCount) {
      if (ReadOnly)
        throw new InvalidOperationException();
      base.SetSubtreeCount(symbol, minimumSubtreeCount, maximumSubtreeCount);
    }

    private bool suppressEvents = false;

    public void StartGrammarManipulation() {
      suppressEvents = true;
    }

    public void FinishedGrammarManipulation() {
      suppressEvents = false;
      OnChanged();
    }

    protected sealed override void OnChanged() {
      if (suppressEvents)
        return;
      base.OnChanged();
    }

    #region symbol events
    private void RegisterSymbolEvents(Symbol symbol) {
      foreach (var s in symbol.Flatten()) {
        s.NameChanging += Symbol_NameChanging;
        s.NameChanged += Symbol_NameChanged;

        var groupSymbol = s as GroupSymbol;
        if (groupSymbol != null)
          RegisterGroupSymbolEvents(groupSymbol);
        else
          s.Changed += Symbol_Changed;
      }
    }

    private void DeregisterSymbolEvents(Symbol symbol) {
      foreach (var s in symbol.Flatten()) {
        s.NameChanging -= Symbol_NameChanging;
        s.NameChanged -= Symbol_NameChanged;

        var groupSymbol = s as GroupSymbol;
        if (groupSymbol != null)
          DeregisterGroupSymbolEvents(groupSymbol);
        else
          s.Changed -= Symbol_Changed;
      }
    }

    private void RegisterGroupSymbolEvents(GroupSymbol groupSymbol) {
      groupSymbol.Changed += GroupSymbol_Changed;
      groupSymbol.SymbolsCollection.ItemsAdded += GroupSymbol_ItemsAdded;
      groupSymbol.SymbolsCollection.ItemsRemoved += GroupSymbol_ItemsRemoved;
      groupSymbol.SymbolsCollection.CollectionReset += GroupSymbol_CollectionReset;
    }

    private void DeregisterGroupSymbolEvents(GroupSymbol groupSymbol) {
      groupSymbol.Changed -= GroupSymbol_Changed;
      groupSymbol.SymbolsCollection.ItemsAdded -= GroupSymbol_ItemsAdded;
      groupSymbol.SymbolsCollection.ItemsRemoved -= GroupSymbol_ItemsRemoved;
      groupSymbol.SymbolsCollection.CollectionReset -= GroupSymbol_CollectionReset;
    }

    private void Symbol_Changed(object sender, EventArgs e) {
      ClearCaches();
      OnChanged();
    }

    private void GroupSymbol_Changed(object sender, EventArgs e) {
      var groupSymbol = (GroupSymbol)sender;
      foreach (var symbol in groupSymbol.Flatten())
        symbol.Enabled = groupSymbol.Enabled;

      ClearCaches();
      OnChanged();
    }

    private void Symbol_NameChanging(object sender, CancelEventArgs<string> e) {
      if (symbols.ContainsKey(e.Value))
        e.Cancel = true;
    }

    private void Symbol_NameChanged(object sender, EventArgs e) {
      var symbol = (Symbol)sender;
      var oldName = symbols.First(x => x.Value == symbol).Key;
      var newName = symbol.Name;

      symbols.Remove(oldName);
      symbols.Add(newName, symbol);

      var subtreeCount = SymbolSubtreeCount[oldName];
      SymbolSubtreeCount.Remove(oldName);
      SymbolSubtreeCount.Add(newName, subtreeCount);

      if (allowedChildSymbols.TryGetValue(oldName, out var allowedChilds)) {
        allowedChildSymbols.Remove(oldName);
        allowedChildSymbols.Add(newName, allowedChilds);
      }

      for (var i = 0; i < GetMaximumSubtreeCount(symbol); i++) {
        if (allowedChildSymbolsPerIndex.TryGetValue(Tuple.Create(oldName, i), out allowedChilds)) {
          allowedChildSymbolsPerIndex.Remove(Tuple.Create(oldName, i));
          allowedChildSymbolsPerIndex.Add(Tuple.Create(newName, i), allowedChilds);
        }
      }

      foreach (var parent in Symbols) {
        if (allowedChildSymbols.TryGetValue(parent.Name, out allowedChilds))
          if (allowedChilds.Remove(oldName))
            allowedChilds.Add(newName);

        for (var i = 0; i < GetMaximumSubtreeCount(parent); i++) {
          if (allowedChildSymbolsPerIndex.TryGetValue(Tuple.Create(parent.Name, i), out allowedChilds))
            if (allowedChilds.Remove(oldName))
              allowedChilds.Add(newName);
        }
      }

      ClearCaches();
      OnChanged();
    }

    private void GroupSymbol_ItemsAdded(object sender, CollectionItemsChangedEventArgs<Symbol> e) {
      foreach (var symbol in e.Items)
        if (!ContainsSymbol(symbol))
          AddSymbol(symbol);
      OnChanged();
    }

    private void GroupSymbol_ItemsRemoved(object sender, CollectionItemsChangedEventArgs<Symbol> e) {
      foreach (var symbol in e.Items)
        if (ContainsSymbol(symbol))
          RemoveSymbol(symbol);
      OnChanged();
    }

    private void GroupSymbol_CollectionReset(object sender, CollectionItemsChangedEventArgs<Symbol> e) {
      foreach (var symbol in e.Items)
        if (!ContainsSymbol(symbol))
          AddSymbol(symbol);
      foreach (var symbol in e.OldItems)
        if (ContainsSymbol(symbol))
          RemoveSymbol(symbol);
      OnChanged();
    }
    #endregion
  }
}
