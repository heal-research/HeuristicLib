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

namespace HEAL.HeuristicLib.Encodings.SymbolicExpression.Grammars;

public abstract class SymbolicExpressionGrammar : SymbolicExpressionGrammarBase, ISymbolicExpressionGrammar {
  #region fields & properties
  public bool ReadOnly { get; set; }

  public int MinimumFunctionDefinitions { get; set; }
  public int MaximumFunctionDefinitions { get; set; }
  public int MinimumFunctionArguments { get; set; }
  public int MaximumFunctionArguments { get; set; }

  public ProgramRootSymbol ProgramRootSymbol { get; }
  public StartSymbol StartSymbol { get; }
  protected DefunSymbol DefunSymbol { get; }
  #endregion

  protected SymbolicExpressionGrammar() {
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

  public sealed override void AddSymbol(Symbol symbol) {
    if (ReadOnly)
      throw new InvalidOperationException();
    base.AddSymbol(symbol);
  }

  public sealed override void RemoveSymbol(Symbol symbol) {
    if (ReadOnly)
      throw new InvalidOperationException();
    base.RemoveSymbol(symbol);
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
}
