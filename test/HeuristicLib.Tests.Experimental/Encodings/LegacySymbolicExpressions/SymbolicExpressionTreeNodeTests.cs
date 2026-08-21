using HEAL.HeuristicLib.Encodings.LegacySymbolicExpressions;

namespace HEAL.HeuristicLib.Tests.Encodings.LegacySymbolicExpressions;

public sealed class SymbolicExpressionTreeNodeTests
{
    [Fact]
    public void Constructor_InitializesNodeWithSymbol()
    {
        var symbol = new TestSymbol();
        var node = new SymbolicExpressionTreeNode(symbol);

        node.Symbol.ShouldBeSameAs(symbol);
        node.Parent.ShouldBeNull();
        node.SubtreeCount.ShouldBe(0);
        node.Subtrees.ShouldBeEmpty();
        node.HasLocalParameters.ShouldBeFalse();
    }

    [Fact]
    public void GetLength_Leaf_ReturnsOne()
    {
        var node = CreateNode();

        node.GetLength().ShouldBe(1);
    }

    [Fact]
    public void GetDepth_Leaf_ReturnsOne()
    {
        var node = CreateNode();

        node.GetDepth().ShouldBe(1);
    }

    [Fact]
    public void AddSubtree_AddsChild_SetsParent_AndUpdatesCaches()
    {
        var root = CreateNode();
        var child = CreateNode();

        root.GetLength().ShouldBe(1);
        root.GetDepth().ShouldBe(1);

        root.AddSubtree(child);

        root.SubtreeCount.ShouldBe(1);
        root.GetSubtree(0).ShouldBeSameAs(child);
        child.Parent.ShouldBeSameAs(root);
        root.GetLength().ShouldBe(2);
        root.GetDepth().ShouldBe(2);
    }

    [Fact]
    public void InsertSubtree_InsertsAtIndex_SetsParent_AndUpdatesCaches()
    {
        var root = CreateNode();
        var a = CreateNode();
        var b = CreateNode();

        root.AddSubtree(a);
        root.InsertSubtree(0, b);

        root.SubtreeCount.ShouldBe(2);
        root.GetSubtree(0).ShouldBeSameAs(b);
        root.GetSubtree(1).ShouldBeSameAs(a);
        a.Parent.ShouldBeSameAs(root);
        b.Parent.ShouldBeSameAs(root);
        root.GetLength().ShouldBe(3);
        root.GetDepth().ShouldBe(2);
    }

    [Fact]
    public void RemoveSubtree_RemovesChild_ClearsParent_AndUpdatesCaches()
    {
        var root = CreateNode();
        var a = CreateNode();
        var b = CreateNode();

        root.AddSubtree(a);
        root.AddSubtree(b);

        root.GetLength().ShouldBe(3);

        root.RemoveSubtree(0);

        root.SubtreeCount.ShouldBe(1);
        root.GetSubtree(0).ShouldBeSameAs(b);
        a.Parent.ShouldBeNull();
        b.Parent.ShouldBeSameAs(root);
        root.GetLength().ShouldBe(2);
        root.GetDepth().ShouldBe(2);
    }

    [Fact]
    public void ReplaceSubtree_ByIndex_ReplacesChild_UpdatesParents_AndCaches()
    {
        var root = CreateNode();
        var oldChild = CreateNode();
        var grandChild = CreateNode();
        oldChild.AddSubtree(grandChild);
        root.AddSubtree(oldChild);

        root.GetLength().ShouldBe(3);
        root.GetDepth().ShouldBe(3);

        var replacement = CreateNode();
        root.ReplaceSubtree(0, replacement);

        root.GetSubtree(0).ShouldBeSameAs(replacement);
        replacement.Parent.ShouldBeSameAs(root);
        oldChild.Parent.ShouldBeNull();
        root.GetLength().ShouldBe(2);
        root.GetDepth().ShouldBe(2);
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

        root.GetSubtree(0).ShouldBeSameAs(replacement);
        replacement.Parent.ShouldBeSameAs(root);
        oldChild.Parent.ShouldBeNull();
        root.GetLength().ShouldBe(2);
        root.GetDepth().ShouldBe(2);
    }

    [Fact]
    public void Indexer_Get_DelegatesToSubtreeAccess()
    {
        var root = CreateNode();
        var original = CreateNode();
        root.AddSubtree(original);
        root[0].ShouldBeSameAs(original);
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

        root.IndexOfSubtree(a).ShouldBe(0);
        root.IndexOfSubtree(b).ShouldBe(1);
        root.IndexOfSubtree(c).ShouldBe(2);
    }

    [Fact]
    public void GetBranchLevel_ReturnsZeroForSelf()
    {
        var root = CreateNode();

        root.GetBranchLevel(root).ShouldBe(0);
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

        root.GetBranchLevel(child).ShouldBe(1);
        root.GetBranchLevel(grandChild).ShouldBe(2);
        root.GetBranchLevel(greatGrandChild).ShouldBe(3);
    }

    [Fact]
    public void GetBranchLevel_ReturnsIntMaxValueForNodeOutsideTree()
    {
        var root = CreateNode();
        var child = CreateNode();
        var outsider = CreateNode();

        root.AddSubtree(child);

        root.GetBranchLevel(outsider).ShouldBe(int.MaxValue);
        root.GetBranchLevel(null).ShouldBe(int.MaxValue);
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

        result.ShouldBe(new[] { root, a, b, a1, a2, b1 });
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

        result.ShouldBe(new[] { root, a, a1, a2, b, b1 });
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

        result.ShouldBe(new[] { a1, a2, a, b1, b, root });
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

        visited.ShouldBe(new[] { root, a, a1, b });
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

        visited.ShouldBe(new[] { a1, a, b, root });
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

        clone.ShouldNotBeSameAs(root);
        clone.Symbol.ShouldBeSameAs(root.Symbol);
        clone.NodeWeight.ShouldBe(root.NodeWeight);

        clone.GetLength().ShouldBe(root.GetLength());
        clone.GetDepth().ShouldBe(root.GetDepth());

        clone.SubtreeCount.ShouldBe(2);
        clone.GetSubtree(0).ShouldNotBeSameAs(root.GetSubtree(0));
        clone.GetSubtree(1).ShouldNotBeSameAs(root.GetSubtree(1));

        clone.GetSubtree(0).Parent.ShouldBeSameAs(clone);
        clone.GetSubtree(1).Parent.ShouldBeSameAs(clone);
        clone.GetSubtree(0).GetSubtree(0).Parent.ShouldBeSameAs(clone.GetSubtree(0));
    }

    [Fact]
    public void Clone_ModifyingClone_DoesNotAffectOriginal()
    {
        var root = CreateNode();
        var child = CreateNode();
        root.AddSubtree(child);

        var clone = root.Clone();
        clone.AddSubtree(CreateNode());

        root.GetLength().ShouldBe(2);
        clone.GetLength().ShouldBe(3);
        root.SubtreeCount.ShouldBe(1);
        clone.SubtreeCount.ShouldBe(2);
    }

    [Fact]
    public void Cache_IsInvalidatedUpTheParentChain_WhenDeepNodeIsAdded()
    {
        var root = CreateNode();
        var child = CreateNode();
        var grandChild = CreateNode();

        root.AddSubtree(child);
        child.AddSubtree(grandChild);

        root.GetLength().ShouldBe(3);
        root.GetDepth().ShouldBe(3);

        var newLeaf = CreateNode();
        grandChild.AddSubtree(newLeaf);

        root.GetLength().ShouldBe(4);
        root.GetDepth().ShouldBe(4);
        child.GetDepth().ShouldBe(3);
        grandChild.GetDepth().ShouldBe(2);
    }

    [Fact]
    public void Subtrees_OfLeaf_IsEmpty()
    {
        var node = CreateNode();

        node.Subtrees.ShouldBeEmpty();
        node.SubtreeCount.ShouldBe(0);
    }

    [Fact]
    public void NodeWeight_CanBeReadAndSet()
    {
        var node = CreateNode(42.25);
        node.NodeWeight.ShouldBe(42.25);
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
        clone.Root.GetSubtree(0).NodeWeight.ShouldNotBe(original.Root.GetSubtree(0).NodeWeight);
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
