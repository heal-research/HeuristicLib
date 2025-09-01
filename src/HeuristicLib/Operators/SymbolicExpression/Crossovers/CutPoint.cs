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

using HEAL.HeuristicLib.Encodings.SymbolicExpression;
using HEAL.HeuristicLib.Encodings.SymbolicExpression.Grammars;

namespace HEAL.HeuristicLib.Operators.SymbolicExpression.Crossovers {
  public class CutPoint {
    public SymbolicExpressionTreeNode Parent { get; }
    public SymbolicExpressionTreeNode? Child { get; }
    private readonly ISymbolicExpressionGrammar grammar;

    public int ChildIndex { get; }

    public CutPoint(SymbolicExpressionTreeNode parent, SymbolicExpressionTreeNode child, SymbolicExpressionTreeEncoding encoding) {
      Parent = parent;
      Child = child;
      ChildIndex = parent.IndexOfSubtree(child);
      grammar = encoding.Grammar;
    }

    public CutPoint(SymbolicExpressionTreeNode parent, int childIndex, SymbolicExpressionTreeEncoding encoding) {
      Parent = parent;
      ChildIndex = childIndex;
      Child = null;
      grammar = encoding.Grammar;
    }

    public bool IsMatchingPointType(SymbolicExpressionTreeNode? newChild) {
      if (newChild == null) {
        // make sure that one subtree can be removed and that only the last subtree is removed 
        return grammar.GetMinimumSubtreeCount(Parent.Symbol) < Parent.SubtreeCount &&
               ChildIndex == Parent.SubtreeCount - 1;
      }

      // check syntax constraints of direct parent - child relation
      if (!grammar.ContainsSymbol(newChild.Symbol) ||
          !grammar.IsAllowedChildSymbol(Parent.Symbol, newChild.Symbol, ChildIndex))
        return false;

      var result = true;
      // check point type for the whole branch
      newChild.ForEachNodePostfix(n => {
        result =
          result &&
          grammar.ContainsSymbol(n.Symbol) &&
          n.SubtreeCount >= grammar.GetMinimumSubtreeCount(n.Symbol) &&
          n.SubtreeCount <= grammar.GetMaximumSubtreeCount(n.Symbol);
      });
      return result;
    }
  }
}
