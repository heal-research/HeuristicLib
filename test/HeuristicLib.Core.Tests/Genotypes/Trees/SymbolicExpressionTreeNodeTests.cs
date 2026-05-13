using HEAL.HeuristicLib.Genotypes.Trees;
using HEAL.HeuristicLib.SearchSpaces.Trees.SymbolicExpressionTree.Symbols;

namespace HEAL.HeuristicLib.Tests.Genotypes.Trees;

public sealed class SymbolicExpressionTreeNodeTests
{
  [Fact]
  public void Constructor_InitializesNodeWithSymbol()
  {
    var symbol = new TestSymbol();
    var node = new SymbolicExpressionTreeNode(symbol);

    Assert.Same(symbol, node.Symbol);
    Assert.Null(node.Parent);
    Assert.Equal(0, node.SubtreeCount);
    Assert.Empty(node.Subtrees);
    Assert.False(node.HasLocalParameters);
  }

  [Fact]
  public void GetLength_Leaf_ReturnsOne()
  {
    var node = CreateNode();

    Assert.Equal(1, node.GetLength());
  }

  [Fact]
  public void GetDepth_Leaf_ReturnsOne()
  {
    var node = CreateNode();

    Assert.Equal(1, node.GetDepth());
  }

  [Fact]
  public void AddSubtree_AddsChild_SetsParent_AndUpdatesCaches()
  {
    var root = CreateNode();
    var child = CreateNode();

    Assert.Equal(1, root.GetLength());
    Assert.Equal(1, root.GetDepth());

    root.AddSubtree(child);

    Assert.Equal(1, root.SubtreeCount);
    Assert.Same(child, root.GetSubtree(0));
    Assert.Same(root, child.Parent);
    Assert.Equal(2, root.GetLength());
    Assert.Equal(2, root.GetDepth());
  }

  [Fact]
  public void InsertSubtree_InsertsAtIndex_SetsParent_AndUpdatesCaches()
  {
    var root = CreateNode();
    var a = CreateNode();
    var b = CreateNode();

    root.AddSubtree(a);
    root.InsertSubtree(0, b);

    Assert.Equal(2, root.SubtreeCount);
    Assert.Same(b, root.GetSubtree(0));
    Assert.Same(a, root.GetSubtree(1));
    Assert.Same(root, a.Parent);
    Assert.Same(root, b.Parent);
    Assert.Equal(3, root.GetLength());
    Assert.Equal(2, root.GetDepth());
  }

  [Fact]
  public void RemoveSubtree_RemovesChild_ClearsParent_AndUpdatesCaches()
  {
    var root = CreateNode();
    var a = CreateNode();
    var b = CreateNode();

    root.AddSubtree(a);
    root.AddSubtree(b);

    Assert.Equal(3, root.GetLength());

    root.RemoveSubtree(0);

    Assert.Equal(1, root.SubtreeCount);
    Assert.Same(b, root.GetSubtree(0));
    Assert.Null(a.Parent);
    Assert.Same(root, b.Parent);
    Assert.Equal(2, root.GetLength());
    Assert.Equal(2, root.GetDepth());
  }

  [Fact]
  public void ReplaceSubtree_ByIndex_ReplacesChild_UpdatesParents_AndCaches()
  {
    var root = CreateNode();
    var oldChild = CreateNode();
    var grandChild = CreateNode();
    oldChild.AddSubtree(grandChild);
    root.AddSubtree(oldChild);

    Assert.Equal(3, root.GetLength());
    Assert.Equal(3, root.GetDepth());

    var replacement = CreateNode();
    root.ReplaceSubtree(0, replacement);

    Assert.Same(replacement, root.GetSubtree(0));
    Assert.Same(root, replacement.Parent);
    Assert.Null(oldChild.Parent);
    Assert.Equal(2, root.GetLength());
    Assert.Equal(2, root.GetDepth());
  }

  [Fact]
  public void ReplaceSubtree_ByReference_ReplacesChild_UpdatesParents_AndCaches()
  {
    var root = CreateNode();
    var oldChild = CreateNode();
    var grandChild = CreateNode();
    oldChild.AddSubtree(grandChild);
    root.AddSubtree(oldChild);

    var replacement = CreateNode();

    root.ReplaceSubtree(oldChild, replacement);

    Assert.Same(replacement, root.GetSubtree(0));
    Assert.Same(root, replacement.Parent);
    Assert.Null(oldChild.Parent);
    Assert.Equal(2, root.GetLength());
    Assert.Equal(2, root.GetDepth());
  }

  [Fact]
  public void Indexer_Get_DelegatesToSubtreeAccess()
  {
    var root = CreateNode();
    var original = CreateNode();
    root.AddSubtree(original);
    Assert.Same(original, root[0]);
  }

  [Fact]
  public void IndexOfSubtree_ReturnsCorrectIndex()
  {
    var root = CreateNode();
    var a = CreateNode();
    var b = CreateNode();
    var c = CreateNode();

    root.AddSubtree(a);
    root.AddSubtree(b);
    root.AddSubtree(c);

    Assert.Equal(0, root.IndexOfSubtree(a));
    Assert.Equal(1, root.IndexOfSubtree(b));
    Assert.Equal(2, root.IndexOfSubtree(c));
  }

  [Fact]
  public void GetBranchLevel_ReturnsZeroForSelf()
  {
    var root = CreateNode();

    Assert.Equal(0, root.GetBranchLevel(root));
  }

  [Fact]
  public void GetBranchLevel_ReturnsDistanceToDescendant()
  {
    var root = CreateNode();
    var child = CreateNode();
    var grandChild = CreateNode();
    var greatGrandChild = CreateNode();

    root.AddSubtree(child);
    child.AddSubtree(grandChild);
    grandChild.AddSubtree(greatGrandChild);

    Assert.Equal(1, root.GetBranchLevel(child));
    Assert.Equal(2, root.GetBranchLevel(grandChild));
    Assert.Equal(3, root.GetBranchLevel(greatGrandChild));
  }

  [Fact]
  public void GetBranchLevel_ReturnsIntMaxValueForNodeOutsideTree()
  {
    var root = CreateNode();
    var child = CreateNode();
    var outsider = CreateNode();

    root.AddSubtree(child);

    Assert.Equal(int.MaxValue, root.GetBranchLevel(outsider));
    Assert.Equal(int.MaxValue, root.GetBranchLevel(null));
  }

  [Fact]
  public void IterateNodesBreadth_ReturnsNodesInBreadthFirstOrder()
  {
    var root = CreateNode();
    var a = CreateNode();
    var b = CreateNode();
    var a1 = CreateNode();
    var a2 = CreateNode();
    var b1 = CreateNode();

    root.AddSubtree(a);
    root.AddSubtree(b);
    a.AddSubtree(a1);
    a.AddSubtree(a2);
    b.AddSubtree(b1);

    var result = root.IterateNodesBreadth().ToList();

    Assert.Equal(new[] { root, a, b, a1, a2, b1 }, result);
  }

  [Fact]
  public void IterateNodesPrefix_ReturnsNodesInPreorder()
  {
    var root = CreateNode();
    var a = CreateNode();
    var b = CreateNode();
    var a1 = CreateNode();
    var a2 = CreateNode();
    var b1 = CreateNode();

    root.AddSubtree(a);
    root.AddSubtree(b);
    a.AddSubtree(a1);
    a.AddSubtree(a2);
    b.AddSubtree(b1);

    var result = root.IterateNodesPrefix().ToList();

    Assert.Equal(new[] { root, a, a1, a2, b, b1 }, result);
  }

  [Fact]
  public void IterateNodesPostfix_ReturnsNodesInPostorder()
  {
    var root = CreateNode();
    var a = CreateNode();
    var b = CreateNode();
    var a1 = CreateNode();
    var a2 = CreateNode();
    var b1 = CreateNode();

    root.AddSubtree(a);
    root.AddSubtree(b);
    a.AddSubtree(a1);
    a.AddSubtree(a2);
    b.AddSubtree(b1);

    var result = root.IterateNodesPostfix().ToList();

    Assert.Equal(new[] { a1, a2, a, b1, b, root }, result);
  }

  [Fact]
  public void ForEachNodePrefix_VisitsNodesInPreorder()
  {
    var root = CreateNode();
    var a = CreateNode();
    var b = CreateNode();
    var a1 = CreateNode();

    root.AddSubtree(a);
    root.AddSubtree(b);
    a.AddSubtree(a1);

    var visited = new List<SymbolicExpressionTreeNode>();
    root.ForEachNodePrefix(visited.Add);

    Assert.Equal(new[] { root, a, a1, b }, visited);
  }

  [Fact]
  public void ForEachNodePostfix_VisitsNodesInPostorder()
  {
    var root = CreateNode();
    var a = CreateNode();
    var b = CreateNode();
    var a1 = CreateNode();

    root.AddSubtree(a);
    root.AddSubtree(b);
    a.AddSubtree(a1);

    var visited = new List<SymbolicExpressionTreeNode>();
    root.ForEachNodePostfix(visited.Add);

    Assert.Equal(new[] { a1, a, b, root }, visited);
  }

  [Fact]
  public void Clone_CreatesDeepCopy_WithCopiedStructure_AndCorrectParents()
  {
    var root = CreateNode(1.5);

    var child1 = CreateNode(2.5);
    var child2 = CreateNode();
    var grandChild = CreateNode();

    root.AddSubtree(child1);
    root.AddSubtree(child2);
    child1.AddSubtree(grandChild);

    var clone = root.Clone();

    Assert.NotSame(root, clone);
    Assert.Same(root.Symbol, clone.Symbol);
    Assert.Equal(root.NodeWeight, clone.NodeWeight);

    Assert.Equal(root.GetLength(), clone.GetLength());
    Assert.Equal(root.GetDepth(), clone.GetDepth());

    Assert.Equal(2, clone.SubtreeCount);
    Assert.NotSame(root.GetSubtree(0), clone.GetSubtree(0));
    Assert.NotSame(root.GetSubtree(1), clone.GetSubtree(1));

    Assert.Same(clone, clone.GetSubtree(0).Parent);
    Assert.Same(clone, clone.GetSubtree(1).Parent);
    Assert.Same(clone.GetSubtree(0), clone.GetSubtree(0).GetSubtree(0).Parent);
  }

  [Fact]
  public void Clone_ModifyingClone_DoesNotAffectOriginal()
  {
    var root = CreateNode();
    var child = CreateNode();
    root.AddSubtree(child);

    var clone = root.Clone();
    clone.AddSubtree(CreateNode());

    Assert.Equal(2, root.GetLength());
    Assert.Equal(3, clone.GetLength());
    Assert.Equal(1, root.SubtreeCount);
    Assert.Equal(2, clone.SubtreeCount);
  }

  [Fact]
  public void Cache_IsInvalidatedUpTheParentChain_WhenDeepNodeIsAdded()
  {
    var root = CreateNode();
    var child = CreateNode();
    var grandChild = CreateNode();

    root.AddSubtree(child);
    child.AddSubtree(grandChild);

    Assert.Equal(3, root.GetLength());
    Assert.Equal(3, root.GetDepth());

    var newLeaf = CreateNode();
    grandChild.AddSubtree(newLeaf);

    Assert.Equal(4, root.GetLength());
    Assert.Equal(4, root.GetDepth());
    Assert.Equal(3, child.GetDepth());
    Assert.Equal(2, grandChild.GetDepth());
  }

  [Fact]
  public void Subtrees_OfLeaf_IsEmpty()
  {
    var node = CreateNode();

    Assert.Empty(node.Subtrees);
    Assert.Equal(0, node.SubtreeCount);
  }

  [Fact]
  public void NodeWeight_CanBeReadAndSet()
  {
    var node = CreateNode(42.25);
    Assert.Equal(42.25, node.NodeWeight);
  }

  [Fact]
  public void Clone_Modifications_DoNotLeakBetweenTrees()
  {
    var root = CreateNode();
    var child = CreateNode();
    root.AddSubtree(child);

    var original = new SymbolicExpressionTree(root);
    var clone = new SymbolicExpressionTree(original);

    // mutate clone
    clone.Root.ReplaceSubtree(0, CreateNode(999));

    // assert no leakage
    Assert.NotEqual(
      original.Root.GetSubtree(0).NodeWeight,
      clone.Root.GetSubtree(0).NodeWeight
    );
  }

  private static SymbolicExpressionTreeNode CreateNode(double? d = null)
  {
    var testSymbol = new TestSymbol();
    if (d is null)
      return new SymbolicExpressionTreeNode(testSymbol);
    return new SymbolicExpressionTreeNode(testSymbol) { NodeWeight = d.Value };
  }

  private sealed class TestSymbol() : Symbol(0, 0, 3);
}
