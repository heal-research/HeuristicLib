using HEAL.HeuristicLib.Genotypes.Trees;

namespace HEAL.HeuristicLib.Problems.DataAnalysis.Symbolic;

public static class SymbolicExpressionTreeLinearCompiler
{
  public static LinearInstruction[] Compile(SymbolicExpressionTree tree, Func<SymbolicExpressionTreeNode, byte> opCodeMapper) => Compile(tree.Root[0][0], opCodeMapper);

  public static LinearInstruction[] Compile(SymbolicExpressionTreeNode root, Func<SymbolicExpressionTreeNode, byte> opCodeMapper)
  {
    var code = new LinearInstruction[root.GetLength()];
    if (root.SubtreeCount > ushort.MaxValue) {
      throw new ArgumentException("Number of subtrees is too big (>65.535)");
    }
    code[0] = new LinearInstruction(root, (ushort)root.SubtreeCount, opCodeMapper(root), 0);
    int c = 1, i = 0;
    foreach (var node in root.IterateNodesBreadth()) {
      for (var j = 0; j < node.SubtreeCount; ++j) {
        var s = node[j];
        if (s.SubtreeCount > ushort.MaxValue) {
          throw new ArgumentException("Number of subtrees is too big (>65.535)");
        }
        code[c + j] = new LinearInstruction(s, (ushort)s.SubtreeCount, opCodeMapper(s), 0);
      }

      code[i].ChildIndex = c;
      c += node.SubtreeCount;
      ++i;
    }

    return code;
  }
}
