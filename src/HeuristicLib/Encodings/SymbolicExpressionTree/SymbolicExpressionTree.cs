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
using System.Drawing;
using System.Linq;
using HeuristicLab.Common;
using HeuristicLab.Core;
using HEAL.Attic;

namespace HEAL.HeuristicLib.Encodings.SymbolicExpressionTree {
  [StorableType("2B92C9B9-1816-4BDA-9F21-79D1E163EFF6")]
  public class IdEqualityComparer : EqualityComparer<SymbolicExpressionTree>, IDeepCloneable {
    public override bool Equals(SymbolicExpressionTree x, SymbolicExpressionTree y) => x?.Id == y?.Id;

    [StorableConstructor]
    protected IdEqualityComparer(StorableConstructorFlag _) { }

    protected IdEqualityComparer() { }

    public override int GetHashCode(SymbolicExpressionTree obj) => obj.Id;
    public object Clone() => throw new NotImplementedException();

    public IDeepCloneable Clone(Cloner cloner) {
      var c = new IdEqualityComparer();
      cloner.RegisterClonedObject(this, c);
      return c;
    }
  }

  [StorableType("E98BB36B-FBB5-4A6C-A2E5-D47E0BD0687B")]
  [Item(nameof(SymbolicExpressionTree), "Represents a symbolic expression tree.")]
  public class SymbolicExpressionTree : Item, ISymbolicExpressionTree {
    [Storable]
    public bool Pruned {
      get => pruned;
      set => pruned = value;
    }
    private static int staticId;
    private static readonly object StaticIdLock = new object();

    private static int GetNextId() {
      lock (StaticIdLock) {
        return staticId++;
      }
    }

    public int Id => id;
    public new static Image StaticItemImage => Common.Resources.VSImageLibrary.Function;
    [Storable] private ISymbolicExpressionTreeNode root;
    private bool pruned = false;
    private readonly int id = GetNextId();
    public ISymbolicExpressionTreeNode Root {
      get => root;
      set {
        if (value == null)
          throw new ArgumentNullException();
        if (value == root)
          return;
        root = value;
        OnToStringChanged();
      }
    }

    public int Length => root?.GetLength() ?? 0;

    public int Depth => root?.GetDepth() ?? 0;

    [StorableConstructor]
    protected SymbolicExpressionTree(StorableConstructorFlag _) : base(_) { }

    protected SymbolicExpressionTree(SymbolicExpressionTree original, Cloner cloner) : base(original, cloner) {
      root = cloner.Clone(original.Root);
    }

    public SymbolicExpressionTree() { }

    public SymbolicExpressionTree(ISymbolicExpressionTreeNode root, int id) {
      this.id = id;
      Root = root;
    }

    public SymbolicExpressionTree(ISymbolicExpressionTreeNode root) {
      Root = root;
    }

    public IEnumerable<ISymbolicExpressionTreeNode> IterateNodesBreadth() {
      return root?.IterateNodesBreadth() ?? Enumerable.Empty<SymbolicExpressionTreeNode>();
    }

    public IEnumerable<ISymbolicExpressionTreeNode> IterateNodesPrefix() {
      return root?.IterateNodesPrefix() ?? Enumerable.Empty<SymbolicExpressionTreeNode>();
    }

    public IEnumerable<ISymbolicExpressionTreeNode> IterateNodesPostfix() {
      return root?.IterateNodesPostfix() ?? Enumerable.Empty<SymbolicExpressionTreeNode>();
    }

    public override IDeepCloneable Clone(Cloner cloner) {
      return new SymbolicExpressionTree(this, cloner);
    }

    public override int GetHashCode() => Id.GetHashCode();
    public override bool Equals(object obj) => obj is SymbolicExpressionTree tree && Id.Equals(tree.Id);
  }
}
