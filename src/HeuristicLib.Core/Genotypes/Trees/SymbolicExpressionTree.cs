namespace HEAL.HeuristicLib.Genotypes.Trees;

public sealed class SymbolicExpressionTree : IEquatable<SymbolicExpressionTree>
{
  public SymbolicExpressionTreeNode Root { get; }
  public int Length => Root.GetLength();

  public int Depth => Root.GetDepth();

  public SymbolicExpressionTree(SymbolicExpressionTree symbolicExpressionTree)
  {
    Root = symbolicExpressionTree.Root.Clone();
  }

  public SymbolicExpressionTree(SymbolicExpressionTreeNode root)
  {
    Root = root;
  }

  public IEnumerable<SymbolicExpressionTreeNode> IterateNodesBreadth() => Root.IterateNodesBreadth();

  public IEnumerable<SymbolicExpressionTreeNode> IterateNodesPrefix() => Root.IterateNodesPrefix();

  public IEnumerable<SymbolicExpressionTreeNode> IterateNodesPostfix() => Root.IterateNodesPostfix();

  public bool Equals(SymbolicExpressionTree? other)
  {
    if (other is null)
      return false;

    return ReferenceEquals(this, other) || Root.Equals(other.Root);
  }

  public override bool Equals(object? obj) => ReferenceEquals(this, obj) || obj is SymbolicExpressionTree other && Equals(other);

  public override int GetHashCode() => Root.GetHashCode();
}
