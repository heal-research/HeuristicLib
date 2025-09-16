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

namespace HEAL.HeuristicLib.Problems.DataAnalysis.Symbolic {
  public static class SymbolicExpressionTreeLinearCompiler {
    public static LinearInstruction[] Compile(SymbolicExpressionTree tree, Func<SymbolicExpressionTreeNode, byte> opCodeMapper) {
      var root = tree.Root.GetSubtree(0).GetSubtree(0);
      return Compile(root, opCodeMapper);
    }

    public static LinearInstruction[] Compile(SymbolicExpressionTreeNode root, Func<SymbolicExpressionTreeNode, byte> opCodeMapper) {
      var code = new LinearInstruction[root.GetLength()];
      if (root.SubtreeCount > ushort.MaxValue) throw new ArgumentException("Number of subtrees is too big (>65.535)");
      code[0] = new LinearInstruction(root, (ushort)root.SubtreeCount, opCodeMapper(root), 0);
      int c = 1, i = 0;
      foreach (var node in root.IterateNodesBreadth()) {
        for (var j = 0; j < node.SubtreeCount; ++j) {
          var s = node.GetSubtree(j);
          if (s.SubtreeCount > ushort.MaxValue) throw new ArgumentException("Number of subtrees is too big (>65.535)");
          code[c + j] = new LinearInstruction(s, (ushort)s.SubtreeCount, opCodeMapper(s), 0);
        }

        code[i].childIndex = c;
        c += node.SubtreeCount;
        ++i;
      }

      return code;
    }
  }
}
