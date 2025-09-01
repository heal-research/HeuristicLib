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

using System.Collections.Generic;
using System.Text;
using HEAL.HeuristicLib.Encodings.SymbolicExpression;
using HEAL.HeuristicLib.Encodings.SymbolicExpression.Symbols;
using HEAL.HeuristicLib.Encodings.SymbolicExpression.Symbols.Math;
using HEAL.HeuristicLib.Operators.Formatters;

namespace HeuristicLab.Encodings.SymbolicExpressionTreeEncoding {
  public sealed class SymbolicExpressionTreeGraphvizFormatter : ISymbolicExpressionTreeStringFormatter {
    public bool Indent {
      get => indent;
      set => indent = value;
    }

    private bool indent;

    public SymbolicExpressionTreeGraphvizFormatter() {
      Indent = true;
    }

    public string Format(SymbolicExpressionTree symbolicExpressionTree) {
      var nodeCounter = 1;
      var strBuilder = new StringBuilder();
      strBuilder.AppendLine("graph {");
      strBuilder.AppendLine(FormatRecursively(symbolicExpressionTree.Root, 0, ref nodeCounter));
      strBuilder.AppendLine("}");
      return strBuilder.ToString();
    }

    private string FormatRecursively(SymbolicExpressionTreeNode node, int indentLength, ref int nodeId) {
      // save id of current node
      var currentNodeId = nodeId;
      // increment id for next node
      nodeId++;

      var strBuilder = new StringBuilder();
      if (Indent) strBuilder.Append(' ', indentLength);

      // get label for node and map if necessary

      var nodeLabel = node.GetType().ToString();
      var sym = node.Symbol;
      nodeLabel = sym switch {
        ProgramRootSymbol => "PRog",
        StartSymbol => "RPB",
        Addition => "+",
        Subtraction => "-",
        Multiplication => "*",
        Division => "/",
        Absolute => "abs",
        AnalyticQuotient => "AQ",
        Sine => "sin",
        Cosine => "cos",
        Tangent => "tan",
        HyperbolicTangent => "tanh",
        Exponential => "exp",
        Logarithm => "log",
        SquareRoot => "sqrt",
        Square => "sqr",
        CubeRoot => "cbrt",
        Cube => "cube",
        GreaterThan => ">",
        LessThan => "<",
        _ => nodeLabel
      };

      //switch (node) {
      // match Koza style
      //  case(ProgramRootSymbol): return "Prog";
      ////  {nameof(StartSymbol), "RPB"},

      ////  // short form 
      ////  {"Subtraction", "-" },
      ////  {"Addition", "+" },

      ////}; 
      //  default: return "unknown";

      strBuilder.Append(nameof(node) + currentNodeId + "[label=\"" + nodeLabel + "\"");
      // leaf nodes should have box shape
      strBuilder.AppendLine(node.SubtreeCount == 0 ? ", shape=\"box\"];" : "];");

      // internal nodes or leaf nodes?
      foreach (var subTree in node.Subtrees) {
        // add an edge 
        if (Indent) strBuilder.Append(' ', indentLength);
        strBuilder.AppendLine(nameof(node) + currentNodeId + " -- node" + nodeId + ";");
        // format the whole subtree
        strBuilder.Append(FormatRecursively(subTree, indentLength + 2, ref nodeId));
      }

      return strBuilder.ToString();
    }
  }
}
