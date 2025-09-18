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
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Operators.SymbolicExpression.Creators;

public class FullTreeCreator : SymbolicExpressionTreeCreator {
  /// <summary>
  /// Create a symbolic expression tree using the 'Full' method.
  /// Function symbols are used for all nodes situated on a level above the maximum tree depth. 
  /// Nodes on the last tree level will have Terminal symbols.
  /// </summary>
  /// <param name="random">Random generator</param>
  /// <param name="grammar">Available tree grammar</param>
  /// <param name="maxTreeDepth">Maximum tree depth</param>
  /// <param name="maxTreeLength">Maximum tree length. This parameter is not used.</param>
  /// <returns></returns>
  public override SymbolicExpressionTree Create(IRandomNumberGenerator random, SymbolicExpressionTreeEncoding encoding) {
    return CreateTree(random, encoding);
  }

  public static SymbolicExpressionTree CreateTree(IRandomNumberGenerator random, SymbolicExpressionTreeEncoding encoding) {
    var tree = encoding.Grammar.MakeStump(random);
    Create(random, tree.Root.GetSubtree(0), encoding, encoding.TreeDepth - 1);
    return tree;
  }

  public static void Create(IRandomNumberGenerator random, SymbolicExpressionTreeNode seedNode, SymbolicExpressionTreeEncoding encoding, int maxDepth) {
    // make sure it is possible to create a trees smaller than maxDepth
    if (encoding.Grammar.GetMinimumExpressionDepth(seedNode.Symbol) > maxDepth)
      throw new ArgumentException("Cannot create trees of depth " + maxDepth + " or smaller because of grammar constraints.", nameof(maxDepth));

    var arity = encoding.Grammar.GetMaximumSubtreeCount(seedNode.Symbol);
    // Throw an exception if the seedNode happens to be a terminal, since in this case we cannot grow a tree.
    if (arity <= 0)
      throw new ArgumentException("Cannot grow tree. Seed node shouldn't have arity zero.");

    var allowedSymbols = encoding.Grammar.AllowedSymbols
                                 .Where(s => s.InitialFrequency > 0.0 && encoding.Grammar.GetMaximumSubtreeCount(s) > 0)
                                 .ToList();

    for (var i = 0; i < arity; i++) {
      var possibleSymbols = allowedSymbols
                            .Where(s => encoding.Grammar.IsAllowedChildSymbol(seedNode.Symbol, s, i)
                                        && encoding.Grammar.GetMinimumExpressionDepth(s) <= maxDepth
                                        && encoding.Grammar.GetMaximumExpressionDepth(s) >= maxDepth)
                            .ToList();
      var weights = possibleSymbols.Select(s => s.InitialFrequency).ToList();

      var selectedSymbol = possibleSymbols.SampleProportional(random, weights);

      var tree = selectedSymbol.CreateTreeNode();
      if (tree.HasLocalParameters)
        tree.ResetLocalParameters(random);
      seedNode.AddSubtree(tree);
    }

    // Only iterate over the non-terminal nodes (those which have arity > 0)
    // Start from depth 2 since the first two levels are formed by the rootNode and the seedNode
    foreach (var subTree in seedNode.Subtrees)
      if (encoding.Grammar.GetMaximumSubtreeCount(subTree.Symbol) > 0)
        RecursiveCreate(random, subTree, encoding, 2, maxDepth);
  }

  private static void RecursiveCreate(IRandomNumberGenerator random, SymbolicExpressionTreeNode root, SymbolicExpressionTreeEncoding encoding, int currentDepth, int maxDepth) {
    var arity = encoding.Grammar.GetMaximumSubtreeCount(root.Symbol);
    // In the 'Full' grow method, we cannot have terminals on the intermediate tree levels.
    if (arity <= 0)
      throw new ArgumentException("Cannot grow node of arity zero. Expected a function node.");

    var allowedSymbols = encoding.Grammar.AllowedSymbols
                                 .Where(s => s.InitialFrequency > 0.0)
                                 .ToList();

    for (var i = 0; i < arity; i++) {
      var possibleSymbols = allowedSymbols
                            .Where(s => encoding.Grammar.IsAllowedChildSymbol(root.Symbol, s, i) &&
                                        encoding.Grammar.GetMinimumExpressionDepth(s) - 1 <= maxDepth - currentDepth &&
                                        encoding.Grammar.GetMaximumExpressionDepth(s) > maxDepth - currentDepth)
                            .ToList();
      if (!possibleSymbols.Any())
        throw new InvalidOperationException("No symbols are available for the tree.");
      var weights = possibleSymbols.Select(s => s.InitialFrequency).ToList();

      var selectedSymbol = possibleSymbols.SampleProportional(random, weights);

      var tree = selectedSymbol.CreateTreeNode();
      if (tree.HasLocalParameters)
        tree.ResetLocalParameters(random);
      root.AddSubtree(tree);
    }

    //additional levels should only be added if the maximum depth is not reached yet
    if (maxDepth <= currentDepth) {
      return;
    }

    foreach (var subTree in root.Subtrees)
      if (encoding.Grammar.GetMaximumSubtreeCount(subTree.Symbol) > 0)
        RecursiveCreate(random, subTree, encoding, currentDepth + 1, maxDepth);
  }
}
