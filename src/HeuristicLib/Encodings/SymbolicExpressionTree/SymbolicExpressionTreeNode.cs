#region License Information
/* HeuristicLab
 * Copyright (C) Heuristic and Evolutionary Algorithms Laboratory (HEAL)
 *
 * This file is part of HeuristicLab.
 *
 * HeuristicLab is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * HeuristicLab is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with HeuristicLab. If not, see <http://www.gnu.org/licenses/>.
 */
#endregion

using System;
using System.Collections.Generic;
using HEAL.HeuristicLib.Random;
using HeuristicLab.Encodings.SymbolicExpressionTreeEncoding;

namespace HEAL.HeuristicLib.Encodings.SymbolicExpressionTree {
  public class SymbolicExpressionTreeNode {
    private readonly IList<SymbolicExpressionTreeNode> subtrees;

    // cached values to prevent unnecessary tree iterations
    private ushort length;
    private ushort depth;

    public Symbol Symbol { get; protected set; }

    public SymbolicExpressionTreeNode Parent { get; set; }

    public double NodeWeight { get; set; }

    internal SymbolicExpressionTreeNode() {
      // don't allocate subtrees list here!
      // because we don't want to allocate it in terminal nodes
    }

    public SymbolicExpressionTreeNode(Symbol symbol) {
      subtrees = new List<SymbolicExpressionTreeNode>(3);
      Symbol = symbol;
    }

    public virtual bool HasLocalParameters => false;

    public virtual IEnumerable<SymbolicExpressionTreeNode> Subtrees => subtrees;

    public virtual ISymbolicExpressionTreeGrammar Grammar => Parent.Grammar;

    public int GetLength() {
      if (length > 0)
        return length;
      ushort l = 1;
      if (subtrees != null) {
        for (var i = 0; i < subtrees.Count; i++) {
          checked { l += (ushort)subtrees[i].GetLength(); }
        }
      }

      length = l;
      return length;
    }

    public int GetDepth() {
      if (depth > 0)
        return depth;
      ushort d = 0;
      if (subtrees != null) {
        for (var i = 0; i < subtrees.Count; i++)
          d = Math.Max(d, (ushort)subtrees[i].GetDepth());
      }

      d++;
      depth = d;
      return depth;
    }

    public int GetBranchLevel(SymbolicExpressionTreeNode child) {
      return GetBranchLevel(this, child);
    }

    private static int GetBranchLevel(SymbolicExpressionTreeNode root, SymbolicExpressionTreeNode point) {
      if (root == point)
        return 0;
      foreach (var subtree in root.Subtrees) {
        var branchLevel = GetBranchLevel(subtree, point);
        if (branchLevel < int.MaxValue)
          return 1 + branchLevel;
      }

      return int.MaxValue;
    }

    public virtual void ResetLocalParameters(IRandomNumberGenerator random) { }
    public virtual void ShakeLocalParameters(IRandomNumberGenerator random, double shakingFactor) { }

    public int SubtreeCount {
      get {
        if (subtrees == null)
          return 0;
        return subtrees.Count;
      }
    }

    public virtual SymbolicExpressionTreeNode GetSubtree(int index) {
      return subtrees[index];
    }

    public virtual int IndexOfSubtree(SymbolicExpressionTreeNode tree) {
      return subtrees.IndexOf(tree);
    }

    public virtual void AddSubtree(SymbolicExpressionTreeNode tree) {
      subtrees.Add(tree);
      tree.Parent = this;
      ResetCachedValues();
    }

    public virtual void InsertSubtree(int index, SymbolicExpressionTreeNode tree) {
      subtrees.Insert(index, tree);
      tree.Parent = this;
      ResetCachedValues();
    }

    public virtual void RemoveSubtree(int index) {
      subtrees[index].Parent = null;
      subtrees.RemoveAt(index);
      ResetCachedValues();
    }

    public virtual void ReplaceSubtree(int index, SymbolicExpressionTreeNode repl) {
      subtrees[index].Parent = null;
      subtrees[index] = repl;
      repl.Parent = this;
      ResetCachedValues();
    }

    public virtual void ReplaceSubtree(SymbolicExpressionTreeNode old, SymbolicExpressionTreeNode repl) {
      var index = IndexOfSubtree(old);
      subtrees[index].Parent = null;
      subtrees[index] = repl;
      repl.Parent = this;
      ResetCachedValues();
    }

    public IEnumerable<SymbolicExpressionTreeNode> IterateNodesBreadth() {
      var list = new List<SymbolicExpressionTreeNode>(GetLength()) { this };
      var i = 0;
      while (i != list.Count) {
        for (var j = 0; j != list[i].SubtreeCount; ++j)
          list.Add(list[i].GetSubtree(j));
        ++i;
      }

      return list;
    }

    public IEnumerable<SymbolicExpressionTreeNode> IterateNodesPrefix() {
      var list = new List<SymbolicExpressionTreeNode>();
      ForEachNodePrefix((n) => list.Add(n));
      return list;
    }

    public void ForEachNodePrefix(Action<SymbolicExpressionTreeNode> a) {
      a(this);
      if (subtrees != null) {
        //avoid linq to reduce memory pressure
        for (var i = 0; i < subtrees.Count; i++) {
          subtrees[i].ForEachNodePrefix(a);
        }
      }
    }

    public IEnumerable<SymbolicExpressionTreeNode> IterateNodesPostfix() {
      var list = new List<SymbolicExpressionTreeNode>();
      ForEachNodePostfix((n) => list.Add(n));
      return list;
    }

    public void ForEachNodePostfix(Action<SymbolicExpressionTreeNode> a) {
      if (subtrees != null) {
        //avoid linq to reduce memory pressure
        for (var i = 0; i < subtrees.Count; i++) {
          subtrees[i].ForEachNodePostfix(a);
        }
      }

      a(this);
    }

    private void ResetCachedValues() {
      length = 0;
      depth = 0;
      Parent?.ResetCachedValues();
    }
  }
}
