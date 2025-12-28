using HEAL.HeuristicLib.Encodings.Trees.SymbolicExpressionTree.Symbols;
using HEAL.HeuristicLib.Genotypes.Trees;

namespace HEAL.HeuristicLib.Problems.DataAnalysis.Symbolic;

public static class SymbolicExpressionTreeCompiler {
  public static Instruction[] Compile(SymbolicExpressionTree tree, Func<SymbolicExpressionTreeNode, byte> opCodeMapper) {
    return Compile(tree, opCodeMapper, Array.Empty<Func<Instruction, Instruction>>());
  }

  public static Instruction[] Compile(SymbolicExpressionTree tree, Func<SymbolicExpressionTreeNode, byte> opCodeMapper, ICollection<Func<Instruction, Instruction>> postInstructionCompiledHooks) {
    var entryPoint = new Dictionary<string, ushort>();
    var code = new List<Instruction>();
    // compile main body branches
    foreach (var branch in tree.Root[0].Subtrees) {
      code.AddRange(Compile(branch, opCodeMapper, postInstructionCompiledHooks));
    }

    // compile function branches
    var functionBranches = tree.IterateNodesPrefix().OfType<DefunTreeNode>();
    foreach (var branch in functionBranches) {
      if (code.Count > ushort.MaxValue) throw new ArgumentException("Code for the tree is too long (> ushort.MaxValue).");
      entryPoint[branch.FunctionName] = (ushort)code.Count;
      code.AddRange(Compile(branch[0], opCodeMapper, postInstructionCompiledHooks));
    }

    // address of all functions is fixed now
    // iterate through code again and fill in the jump locations
    foreach (var instr in code) {
      if (instr.DynamicNode.Symbol is not InvokeFunction)
        continue;
      var invokeNode = instr.DynamicNode;
      var functionName = ((InvokeFunctionSymbol)invokeNode.Symbol).FunctionName;
      instr.Data = entryPoint[functionName];
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
