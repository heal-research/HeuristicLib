namespace HEAL.HeuristicLib.Genotypes.Trees;

public class SymbolicExpressionTree(SymbolicExpressionTreeNode root)
{
  public SymbolicExpressionTreeNode Root { get; set; } = root;

  public int Length => Root.GetLength();

  public int Depth => Root.GetDepth();

  public SymbolicExpressionTree(SymbolicExpressionTree symbolicExpressionTree) : this(symbolicExpressionTree.Root.Clone()) { }

  public IEnumerable<SymbolicExpressionTreeNode> IterateNodesBreadth() => Root.IterateNodesBreadth();

  public IEnumerable<SymbolicExpressionTreeNode> IterateNodesPrefix() => Root.IterateNodesPrefix();

  public IEnumerable<SymbolicExpressionTreeNode> IterateNodesPostfix() => Root.IterateNodesPostfix();
}
