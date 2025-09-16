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
using HEAL.HeuristicLib.Encodings.SymbolicExpression.Symbols;

namespace HEAL.HeuristicLib.Problems.DataAnalysis.Symbolic {
  public static class SymbolicExpressionTreeCompiler {
    public static Instruction[] Compile(SymbolicExpressionTree tree, Func<SymbolicExpressionTreeNode, byte> opCodeMapper) {
      return Compile(tree, opCodeMapper, Array.Empty<Func<Instruction, Instruction>>());
    }

    public static Instruction[] Compile(SymbolicExpressionTree tree, Func<SymbolicExpressionTreeNode, byte> opCodeMapper, ICollection<Func<Instruction, Instruction>> postInstructionCompiledHooks) {
      var entryPoint = new Dictionary<string, ushort>();
      var code = new List<Instruction>();
      // compile main body branches
      foreach (var branch in tree.Root.GetSubtree(0).Subtrees) {
        code.AddRange(Compile(branch, opCodeMapper, postInstructionCompiledHooks));
      }

      // compile function branches
      var functionBranches = tree.IterateNodesPrefix().OfType<DefunTreeNode>();
      foreach (var branch in functionBranches) {
        if (code.Count > ushort.MaxValue) throw new ArgumentException("Code for the tree is too long (> ushort.MaxValue).");
        entryPoint[branch.FunctionName] = (ushort)code.Count;
        code.AddRange(Compile(branch.GetSubtree(0), opCodeMapper, postInstructionCompiledHooks));
      }

      // address of all functions is fixed now
      // iterate through code again and fill in the jump locations
      foreach (var instr in code) {
        if (instr.dynamicNode.Symbol is not InvokeFunction)
          continue;
        var invokeNode = instr.dynamicNode;
        var functionName = ((InvokeFunctionSymbol)invokeNode.Symbol).FunctionName;
        instr.data = entryPoint[functionName];
      }

      return code.ToArray();
    }

    private static IEnumerable<Instruction> Compile(SymbolicExpressionTreeNode branch, Func<SymbolicExpressionTreeNode, byte> opCodeMapper, ICollection<Func<Instruction, Instruction>> postInstructionCompiledHooks) {
      foreach (var node in branch.IterateNodesPrefix()) {
        var subtreesCount = node.SubtreeCount;
        if (subtreesCount > ushort.MaxValue) throw new ArgumentException("Number of subtrees is too big (> 65.535)");
        ushort data = 0;
        if (node.Symbol is Argument) {
          var argNode = (ArgumentSymbol)node.Symbol;
          data = (ushort)argNode.ArgumentIndex;
        }

        var instr = new Instruction(node, (ushort)subtreesCount, opCodeMapper(node), data);
        foreach (var hook in postInstructionCompiledHooks) {
          instr = hook(instr);
        }

        yield return instr;
      }
    }
  }
}
