using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces.Trees.SymbolicExpressionTree.Symbols;

// ReSharper disable ForCanBeConvertedToForeach
// ReSharper disable LoopCanBeConvertedToQuery

namespace HEAL.HeuristicLib.Genotypes.Trees;

public class SymbolicExpressionTreeNode
{
  private readonly List<SymbolicExpressionTreeNode>? subtrees;

  // cached values to prevent unnecessary tree iterations
  private ushort length;
  private ushort depth;

  public Symbol Symbol { get; } = null!;

  public SymbolicExpressionTreeNode? Parent { get; set; }

  public double NodeWeight { get; set; }

  internal SymbolicExpressionTreeNode()
  {
    // don't allocate subtrees list here!
    // because we don't want to allocate it in terminal nodes
  }

  public SymbolicExpressionTreeNode(Symbol symbol)
  {
    subtrees = new List<SymbolicExpressionTreeNode>(3);
    Symbol = symbol;
  }

  protected SymbolicExpressionTreeNode(SymbolicExpressionTreeNode node)
  {
    subtrees = node.subtrees?.Select(x => {
      var p = x.Clone();
      p.Parent = this;
      return p;
    }).ToList();
    length = node.length;
    depth = node.depth;
    Symbol = node.Symbol;
    NodeWeight = node.NodeWeight;
  }

  public virtual SymbolicExpressionTreeNode Clone() => new(this);

  public virtual bool HasLocalParameters => false;

  public IEnumerable<SymbolicExpressionTreeNode> Subtrees => subtrees ?? [];

  public int GetLength()
  {
    if (length > 0) {
      return length;
    }

    ushort l = 1;
    if (subtrees != null) {
      for (var i = 0; i < subtrees.Count; i++) {
        checked { l += (ushort)subtrees[i].GetLength(); }
      }
    }

    length = l;
    return length;
  }

  public int GetDepth()
  {
    if (depth > 0) {
      return depth;
    }

    ushort d = 0;
    if (subtrees != null) {
      for (var i = 0; i < subtrees.Count; i++) {
        d = Math.Max(d, (ushort)subtrees[i].GetDepth());
      }
    }

    d++;
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

  public virtual void ResetLocalParameters(IRandomNumberGenerator random) { }
  public virtual void ShakeLocalParameters(IRandomNumberGenerator random, double shakingFactor) { }

  public int SubtreeCount => subtrees?.Count ?? 0;

  public SymbolicExpressionTreeNode GetSubtree(int index) => subtrees![index];

  public SymbolicExpressionTreeNode this[int key]
  {
    get => subtrees![key];
    set => ReplaceSubtree(key, value);
  }

  public int IndexOfSubtree(SymbolicExpressionTreeNode tree) => subtrees!.IndexOf(tree);

  public void AddSubtree(SymbolicExpressionTreeNode tree)
  {
    subtrees!.Add(tree);
    tree.Parent = this;
    ResetCachedValues();
  }

  public void InsertSubtree(int index, SymbolicExpressionTreeNode tree)
  {
    subtrees!.Insert(index, tree);
    tree.Parent = this;
    ResetCachedValues();
  }

  public void RemoveSubtree(int index)
  {
    subtrees![index].Parent = null;
    subtrees.RemoveAt(index);
    ResetCachedValues();
  }

  public void ReplaceSubtree(int index, SymbolicExpressionTreeNode repl)
  {
    subtrees![index].Parent = null;
    subtrees[index] = repl;
    repl.Parent = this;
    ResetCachedValues();
  }

  public void ReplaceSubtree(SymbolicExpressionTreeNode old, SymbolicExpressionTreeNode repl)
  {
    var index = IndexOfSubtree(old);
    subtrees![index].Parent = null;
    subtrees[index] = repl;
    repl.Parent = this;
    ResetCachedValues();
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
    if (subtrees == null) {
      return;
    }

    //avoid linq to reduce memory pressure
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
    if (subtrees != null)
      //avoid linq to reduce memory pressure
      // ReSharper disable once ForCanBeConvertedToForeach
    {
      for (var i = 0; i < subtrees.Count; i++) {
        subtrees[i].ForEachNodePostfix(a);
      }
    }

    a(this);
  }

  private void ResetCachedValues()
  {
    length = 0;
    depth = 0;
    Parent?.ResetCachedValues();
  }
}
