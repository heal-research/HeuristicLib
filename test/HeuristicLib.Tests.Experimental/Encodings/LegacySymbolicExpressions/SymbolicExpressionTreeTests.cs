using HEAL.HeuristicLib.Encodings.LegacySymbolicExpressions;

namespace HEAL.HeuristicLib.Tests.Encodings.LegacySymbolicExpressions;

public sealed class SymbolicExpressionTreeTests
{
    [Fact]
    public void Constructor_SetsRoot()
    {
        var root = CreateNode();

        var tree = new SymbolicExpressionTree(root);

        tree.Root.ShouldBeSameAs(root);
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

        tree.Length.ShouldBe(3);
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

        tree.Depth.ShouldBe(3);
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

        copy.ShouldNotBeSameAs(original);
        copy.Root.ShouldNotBeSameAs(original.Root);

        copy.Length.ShouldBe(original.Length);
        copy.Depth.ShouldBe(original.Depth);

        copy.Root.SubtreeCount.ShouldBe(2);
        copy.Root.GetSubtree(0).ShouldNotBeSameAs(original.Root.GetSubtree(0));
        copy.Root.GetSubtree(1).ShouldNotBeSameAs(original.Root.GetSubtree(1));

        copy.Root.GetSubtree(0).Parent.ShouldBeSameAs(copy.Root);
        copy.Root.GetSubtree(1).Parent.ShouldBeSameAs(copy.Root);
        copy.Root.GetSubtree(0).GetSubtree(0).Parent.ShouldBeSameAs(copy.Root.GetSubtree(0));

        copy.Root.NodeWeight.ShouldBe(original.Root.NodeWeight);
        copy.Root.GetSubtree(0).NodeWeight.ShouldBe(original.Root.GetSubtree(0).NodeWeight);
    }

    [Fact]
    public void CopyConstructor_ModifyingCopy_DoesNotAffectOriginal()
    {
        var root = CreateNode();
        root.AddSubtree(CreateNode());

        var original = new SymbolicExpressionTree(root);
        var copy = new SymbolicExpressionTree(original);

        copy.Root.AddSubtree(CreateNode());

        original.Length.ShouldBe(2);
        copy.Length.ShouldBe(3);
        original.Root.SubtreeCount.ShouldBe(1);
        copy.Root.SubtreeCount.ShouldBe(2);
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

        result.ShouldBe(new[] { root, a, b, a1 });
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

        result.ShouldBe(new[] { root, a, a1, b });
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

        result.ShouldBe(new[] { a1, a, b, root });
    }

    [Fact]
    public void Length_And_Depth_WorkForSingleNodeTree()
    {
        var root = CreateNode();
        var tree = new SymbolicExpressionTree(root);

        tree.Length.ShouldBe(1);
        tree.Depth.ShouldBe(1);
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
