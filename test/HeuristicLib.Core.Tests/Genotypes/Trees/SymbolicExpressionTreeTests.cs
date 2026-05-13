using HEAL.HeuristicLib.Genotypes.Trees;
using HEAL.HeuristicLib.SearchSpaces.Trees.SymbolicExpressionTree.Symbols;

namespace HEAL.HeuristicLib.Tests.Genotypes.Trees;

public sealed class SymbolicExpressionTreeTests
{
  [Fact]
  public void Constructor_SetsRoot()
  {
    var root = CreateNode();

    var tree = new SymbolicExpressionTree(root);

    Assert.Same(root, tree.Root);
  }

  [Fact]
  public void Length_DelegatesToRootLength()
  {
    var root = CreateNode();
    var child1 = CreateNode();
    var child2 = CreateNode();
    root.AddSubtree(child1);
    root.AddSubtree(child2);

    var tree = new SymbolicExpressionTree(root);

    Assert.Equal(3, tree.Length);
  }

  [Fact]
  public void Depth_DelegatesToRootDepth()
  {
    var root = CreateNode();
    var child = CreateNode();
    var grandChild = CreateNode();

    root.AddSubtree(child);
    child.AddSubtree(grandChild);

    var tree = new SymbolicExpressionTree(root);

    Assert.Equal(3, tree.Depth);
  }

  [Fact]
  public void CopyConstructor_CreatesDeepCopy()
  {
    var root = CreateNode(1.25);

    var child1 = CreateNode(2.5);
    var child2 = CreateNode();
    var grandChild = CreateNode();

    root.AddSubtree(child1);
    root.AddSubtree(child2);
    child1.AddSubtree(grandChild);

    var original = new SymbolicExpressionTree(root);

    var copy = new SymbolicExpressionTree(original);

    Assert.NotSame(original, copy);
    Assert.NotSame(original.Root, copy.Root);

    Assert.Equal(original.Length, copy.Length);
    Assert.Equal(original.Depth, copy.Depth);

    Assert.Equal(2, copy.Root.SubtreeCount);
    Assert.NotSame(original.Root.GetSubtree(0), copy.Root.GetSubtree(0));
    Assert.NotSame(original.Root.GetSubtree(1), copy.Root.GetSubtree(1));

    Assert.Same(copy.Root, copy.Root.GetSubtree(0).Parent);
    Assert.Same(copy.Root, copy.Root.GetSubtree(1).Parent);
    Assert.Same(copy.Root.GetSubtree(0), copy.Root.GetSubtree(0).GetSubtree(0).Parent);

    Assert.Equal(original.Root.NodeWeight, copy.Root.NodeWeight);
    Assert.Equal(original.Root.GetSubtree(0).NodeWeight, copy.Root.GetSubtree(0).NodeWeight);
  }

  [Fact]
  public void CopyConstructor_ModifyingCopy_DoesNotAffectOriginal()
  {
    var root = CreateNode();
    root.AddSubtree(CreateNode());

    var original = new SymbolicExpressionTree(root);
    var copy = new SymbolicExpressionTree(original);

    copy.Root.AddSubtree(CreateNode());

    Assert.Equal(2, original.Length);
    Assert.Equal(3, copy.Length);
    Assert.Equal(1, original.Root.SubtreeCount);
    Assert.Equal(2, copy.Root.SubtreeCount);
  }

  [Fact]
  public void IterateNodesBreadth_DelegatesToRoot()
  {
    var root = CreateNode();
    var a = CreateNode();
    var b = CreateNode();
    var a1 = CreateNode();

    root.AddSubtree(a);
    root.AddSubtree(b);
    a.AddSubtree(a1);

    var tree = new SymbolicExpressionTree(root);

    var result = tree.IterateNodesBreadth().ToList();

    Assert.Equal(new[] { root, a, b, a1 }, result);
  }

  [Fact]
  public void IterateNodesPrefix_DelegatesToRoot()
  {
    var root = CreateNode();
    var a = CreateNode();
    var b = CreateNode();
    var a1 = CreateNode();

    root.AddSubtree(a);
    root.AddSubtree(b);
    a.AddSubtree(a1);

    var tree = new SymbolicExpressionTree(root);

    var result = tree.IterateNodesPrefix().ToList();

    Assert.Equal(new[] { root, a, a1, b }, result);
  }

  [Fact]
  public void IterateNodesPostfix_DelegatesToRoot()
  {
    var root = CreateNode();
    var a = CreateNode();
    var b = CreateNode();
    var a1 = CreateNode();

    root.AddSubtree(a);
    root.AddSubtree(b);
    a.AddSubtree(a1);

    var tree = new SymbolicExpressionTree(root);

    var result = tree.IterateNodesPostfix().ToList();

    Assert.Equal(new[] { a1, a, b, root }, result);
  }

  [Fact]
  public void Length_And_Depth_WorkForSingleNodeTree()
  {
    var root = CreateNode();
    var tree = new SymbolicExpressionTree(root);

    Assert.Equal(1, tree.Length);
    Assert.Equal(1, tree.Depth);
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
