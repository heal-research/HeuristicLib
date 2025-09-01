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

public interface ISymbolicExpressionGrammarBase {
  IEnumerable<Symbol> Symbols { get; }
  IEnumerable<Symbol> AllowedSymbols { get; }

  bool ContainsSymbol(Symbol symbol);

  void AddSymbol(Symbol symbol);
  void RemoveSymbol(Symbol symbol);

  bool IsAllowedChildSymbol(Symbol parent, Symbol child);
  bool IsAllowedChildSymbol(Symbol parent, Symbol child, int argumentIndex);
  IEnumerable<Symbol> GetAllowedChildSymbols(Symbol parent);
  IEnumerable<Symbol> GetAllowedChildSymbols(Symbol parent, int argumentIndex);

  void AddAllowedChildSymbol(Symbol parent, Symbol child);
  void AddAllowedChildSymbol(Symbol parent, Symbol child, int argumentIndex);
  void RemoveAllowedChildSymbol(Symbol parent, Symbol child);
  void RemoveAllowedChildSymbol(Symbol parent, Symbol child, int argumentIndex);
  void ClearAllowedChildSymbols(Symbol parent);
  void ClearAllAllowedChildSymbols();

  int GetMinimumSubtreeCount(Symbol symbol);
  int GetMaximumSubtreeCount(Symbol symbol);
  void SetSubtreeCount(Symbol symbol, int minimumSubtreeCount, int maximumSubtreeCount);

  int GetMinimumExpressionDepth(Symbol start);
  int GetMaximumExpressionDepth(Symbol start);
  int GetMinimumExpressionLength(Symbol start);
  int GetMaximumExpressionLength(Symbol start, int maxDepth);
}
