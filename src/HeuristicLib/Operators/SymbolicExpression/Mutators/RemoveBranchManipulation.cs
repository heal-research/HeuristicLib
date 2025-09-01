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

using HEAL.HeuristicLib.Encodings.SymbolicExpression;
using HEAL.HeuristicLib.Encodings.SymbolicExpression.Symbols;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Operators.SymbolicExpression.Mutators;

public sealed class RemoveBranchManipulation : SymbolicExpressionTreeManipulator {
  private const int MAX_TRIES = 100;

  public static SymbolicExpressionTree RemoveRandomBranch(IRandomNumberGenerator random, SymbolicExpressionTree symbolicExpressionTree, SymbolicExpressionTreeEncoding encoding) {
    SymbolicExpressionTree childTree = new(symbolicExpressionTree);
    var allowedSymbols = new List<Symbol>();
    SymbolicExpressionTreeNode parent;
    int childIndex;
    int maxLength;
    int maxDepth;
    // repeat until a fitting parent and child are found (MAX_TRIES times)
    var tries = 0;

    var nodes = childTree.Root.IterateNodesPrefix().Skip(1).Where(n => n.SubtreeCount > 0).ToList();
    do {
      parent = nodes.SampleRandom(random);

      childIndex = random.Integer(parent.SubtreeCount);
      var child = parent.GetSubtree(childIndex);
      maxLength = encoding.TreeLength - childTree.Length + child.GetLength();
      maxDepth = encoding.TreeDepth - childTree.Depth + child.GetDepth();

      allowedSymbols.Clear();
      foreach (var symbol in encoding.Grammar.GetAllowedChildSymbols(parent.Symbol, childIndex)) {
        // check basic properties that the new symbol must have
        if (symbol != child.Symbol &&
            symbol.InitialFrequency > 0 &&
            encoding.Grammar.GetMinimumExpressionDepth(symbol) + 1 <= maxDepth &&
            encoding.Grammar.GetMinimumExpressionLength(symbol) <= maxLength) {
          allowedSymbols.Add(symbol);
        }
      }

      tries++;
    } while (tries < MAX_TRIES && allowedSymbols.Count == 0);

    if (tries >= MAX_TRIES) return symbolicExpressionTree;
    ReplaceWithMinimalTree(random, childTree.Root, parent, childIndex, encoding);
    return childTree;
  }

  private static void ReplaceWithMinimalTree(IRandomNumberGenerator random, SymbolicExpressionTreeNode root, SymbolicExpressionTreeNode parent, int childIndex, SymbolicExpressionTreeEncoding encoding) {
    // determine possible symbols that will lead to the smallest possible tree
    var possibleSymbols = (from s in encoding.Grammar.GetAllowedChildSymbols(parent.Symbol, childIndex)
                           where s.InitialFrequency > 0.0
                           group s by encoding.Grammar.GetMinimumExpressionLength(s)
                           into g
                           orderby g.Key
                           select g).First().ToList();
    var weights = possibleSymbols.Select(x => x.InitialFrequency).ToList();

    var selectedSymbol = possibleSymbols.SampleProportional(random, 1, weights).First();

    var newTreeNode = selectedSymbol.CreateTreeNode();
    if (newTreeNode.HasLocalParameters) newTreeNode.ResetLocalParameters(random);
    parent.RemoveSubtree(childIndex);
    parent.InsertSubtree(childIndex, newTreeNode);

    for (var i = 0; i < encoding.Grammar.GetMinimumSubtreeCount(newTreeNode.Symbol); i++) {
      // insert a dummy sub-tree and add the pending extension to the list
      var dummy = new SymbolicExpressionTreeNode();
      newTreeNode.AddSubtree(dummy);
      // replace the just inserted dummy by recursive application
      ReplaceWithMinimalTree(random, root, newTreeNode, i, encoding);
    }
  }

  public override SymbolicExpressionTree Mutate(SymbolicExpressionTree parent, IRandomNumberGenerator random, SymbolicExpressionTreeEncoding encoding) {
    return RemoveRandomBranch(random, parent, encoding);
  }
}
