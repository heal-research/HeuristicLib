using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces.Trees.SymbolicExpressionTree.Symbols;

// ReSharper disable ForCanBeConvertedToForeach
// ReSharper disable LoopCanBeConvertedToQuery

namespace HEAL.HeuristicLib.Genotypes.Trees;

public class NoSymbol : Symbol
{
  public static readonly NoSymbol Instance = new();

  private NoSymbol() : base(0, 0, 0)
  { }
}

public class SymbolicExpressionTreeNode
{
  private static readonly ImmutableList<SymbolicExpressionTreeNode> NoSubtrees = [];
  private readonly IList<SymbolicExpressionTreeNode> subtrees;

  // cached values to prevent unnecessary tree iterations
  private ushort length;
  private ushort depth;

  public Symbol Symbol { get; }

  public SymbolicExpressionTreeNode? Parent { get; private set; }

  public double NodeWeight { get; init; }

  internal SymbolicExpressionTreeNode()
  {
    subtrees = NoSubtrees;
    Symbol = NoSymbol.Instance;
  }

  public SymbolicExpressionTreeNode(Symbol symbol)
  {
    subtrees = new List<SymbolicExpressionTreeNode>(3);
    Symbol = symbol;
  }

  protected SymbolicExpressionTreeNode(SymbolicExpressionTreeNode node)
  {
    if (ReferenceEquals(node.subtrees, NoSubtrees)) {
      subtrees = NoSubtrees;
    } else {
      subtrees = node.subtrees.Select(x => {
        var p = x.Clone();
        p.Parent = this;
        return p;
      }).ToList();
    }

    length = node.length;
    depth = node.depth;
    Symbol = node.Symbol;
    NodeWeight = node.NodeWeight;
  }

  public virtual SymbolicExpressionTreeNode Clone() => new(this);

  public virtual bool HasLocalParameters => false;

  public IReadOnlyList<SymbolicExpressionTreeNode> Subtrees => subtrees.AsReadOnly();

  public int GetLength() => GetLengthInner();

  private ushort GetLengthInner()
  {
    if (length > 0) {
      return length;
    }

    ushort l = 1;
    for (var i = 0; i < subtrees.Count; i++) {
      checked { l += subtrees[i].GetLengthInner(); }
    }

    length = l;
    return length;
  }

  public int GetDepth() => GetDepthInner();

  private ushort GetDepthInner()
  {
    if (depth > 0)
      return depth;

    ushort d = 0;
    for (var i = 0; i < subtrees.Count; i++) {
      d = Math.Max(d, subtrees[i].GetDepthInner());
    }

    checked {
      d++;
    }

    depth = d;
    return depth;
  }

  public int GetBranchLevel(SymbolicExpressionTreeNode? child) => GetBranchLevel(this, child);

  private static int GetBranchLevel(SymbolicExpressionTreeNode root, SymbolicExpressionTreeNode? point)
  {
    if (root == point) {
      return 0;
    }

    foreach (var subtree in root.Subtrees) {
      var branchLevel = GetBranchLevel(subtree, point);
      if (branchLevel < int.MaxValue) {
        return 1 + branchLevel;
      }
    }

    return int.MaxValue;
  }

  public int SubtreeCount => subtrees.Count;

  public SymbolicExpressionTreeNode GetSubtree(int index) => subtrees[index];

  public SymbolicExpressionTreeNode this[int key] => subtrees[key];

  public int IndexOfSubtree(SymbolicExpressionTreeNode tree)
  {
    for (int i = 0; i < subtrees.Count; i++) {
      if (tree.Equals(subtrees[i]))
        return i;
    }

    return -1;
  }

  public IEnumerable<SymbolicExpressionTreeNode> IterateNodesBreadth()
  {
    var list = new List<SymbolicExpressionTreeNode>(GetLength()) { this };
    var i = 0;
    while (i != list.Count) {
      for (var j = 0; j != list[i].SubtreeCount; ++j) {
        list.Add(list[i][j]);
      }

      ++i;
    }

    return list;
  }

  public IEnumerable<SymbolicExpressionTreeNode> IterateNodesPrefix()
  {
    var list = new List<SymbolicExpressionTreeNode>();
    ForEachNodePrefix(n => list.Add(n));
    return list;
  }

  public void ForEachNodePrefix(Action<SymbolicExpressionTreeNode> a)
  {
    a(this);
    // avoid linq to reduce memory pressure
    // ReSharper disable once ForCanBeConvertedToForeach
    for (var i = 0; i < subtrees.Count; i++) {
      subtrees[i].ForEachNodePrefix(a);
    }
  }

  public IEnumerable<SymbolicExpressionTreeNode> IterateNodesPostfix()
  {
    var list = new List<SymbolicExpressionTreeNode>();
    ForEachNodePostfix(list.Add);
    return list;
  }

  public void ForEachNodePostfix(Action<SymbolicExpressionTreeNode> a)
  {
    for (var i = 0; i < subtrees.Count; i++) {
      subtrees[i].ForEachNodePostfix(a);
    }

    a(this);
  }

  #region mutation
  public virtual void ResetLocalParameters(IRandomNumberGenerator random) { }
  public virtual void ShakeLocalParameters(IRandomNumberGenerator random, double shakingFactor) { }

  public void AddSubtree(SymbolicExpressionTreeNode tree)
  {
    subtrees.Add(tree);
    tree.Parent = this;
    ResetCachedValues();
  }

  public void InsertSubtree(int index, SymbolicExpressionTreeNode tree)
  {
    subtrees.Insert(index, tree);
    tree.Parent = this;
    ResetCachedValues();
  }

  public void RemoveSubtree(int index)
  {
    subtrees[index].Parent = null;
    subtrees.RemoveAt(index);
    ResetCachedValues();
  }

  public void ReplaceSubtree(int index, SymbolicExpressionTreeNode repl)
  {
    subtrees[index].Parent = null;
    subtrees[index] = repl;
    repl.Parent = this;
    ResetCachedValues();
  }

  public void ReplaceSubtree(SymbolicExpressionTreeNode old, SymbolicExpressionTreeNode repl) => ReplaceSubtree(IndexOfSubtree(old), repl);
  #endregion

  private void ResetCachedValues()
  {
    length = 0;
    depth = 0;
    Parent?.ResetCachedValues();
  }
}
