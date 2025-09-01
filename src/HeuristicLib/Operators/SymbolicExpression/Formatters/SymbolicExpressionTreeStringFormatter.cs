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

using System.Text;
using HEAL.HeuristicLib.Encodings.SymbolicExpression;

namespace HEAL.HeuristicLib.Operators.SymbolicExpression.Formatters {
  public class SymbolicExpressionTreeStringFormatter : ISymbolicExpressionTreeStringFormatter {
    public bool Indent {
      get;
      set;
    } = true;

    public string Format(SymbolicExpressionTree symbolicExpressionTree) {
      return FormatRecursively(symbolicExpressionTree.Root, 0);
    }

    private string FormatRecursively(SymbolicExpressionTreeNode node, int indentLength) {
      var strBuilder = new StringBuilder();
      if (Indent)
        strBuilder.Append(' ', indentLength);
      strBuilder.Append('(');
      // internal nodes or leaf nodes?
      if (node.Subtrees.Any()) {
        // symbol on same line as '('
        strBuilder.AppendLine(node.Symbol.ToString());
        // each subtree expression on a new line
        // and closing ')' also on new line
        foreach (var subtree in node.Subtrees) {
          strBuilder.AppendLine(FormatRecursively(subtree, indentLength + 2));
        }

        if (Indent)
          strBuilder.Append(' ', indentLength);
      } else {
        // symbol in the same line with as '(' and ')'
        strBuilder.Append(node);
      }

      strBuilder.Append(')');
      return strBuilder.ToString();
    }
  }
}
