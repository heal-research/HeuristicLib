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

namespace HEAL.HeuristicLib.Operators.SymbolicExpression.Formatters {
  public sealed class SymbolicExpressionTreeHierarchicalFormatter : ISymbolicExpressionTreeStringFormatter {
    public string Format(SymbolicExpressionTree symbolicExpressionTree) {
      var sw = new StringWriter();
      RenderTree(sw, symbolicExpressionTree);
      return sw.ToString();
    }

    private static void RenderTree(TextWriter writer, SymbolicExpressionTree tree) {
      RenderNode(writer, tree.Root, string.Empty);
    }

    public static void RenderNode(TextWriter writer, SymbolicExpressionTreeNode node, string prefix) {
      var label = node.ToString();
      writer.Write(label);
      if (node.SubtreeCount > 0) {
        var padding = prefix + new string(' ', label.Length);
        for (var i = 0; i != node.SubtreeCount; ++i) {
          char connector;
          char extender;
          if (i == 0) {
            if (node.SubtreeCount > 1) {
              connector = RenderChars.JunctionDown;
              extender = RenderChars.VerticalLine;
            } else {
              connector = RenderChars.HorizontalLine;
              extender = ' ';
            }
          } else {
            writer.Write(padding);
            if (i == node.SubtreeCount - 1) {
              connector = RenderChars.CornerRight;
              extender = ' ';
            } else {
              connector = RenderChars.JunctionRight;
              extender = RenderChars.VerticalLine;
            }
          }

          writer.Write(string.Concat(connector, RenderChars.HorizontalLine));
          var newPrefix = string.Concat(padding, extender, ' ');
          RenderNode(writer, node.GetSubtree(i), newPrefix);
        }
      } else
        writer.WriteLine();
    }

    // helper class providing characters for displaying a tree in the console
    public static class RenderChars {
      public const char JunctionDown = '┬';
      public const char HorizontalLine = '─';
      public const char VerticalLine = '│';
      public const char JunctionRight = '├';
      public const char CornerRight = '└';
    }
  }
}
