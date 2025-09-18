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

public class GrowTreeCreator : SymbolicExpressionTreeCreator {
  /// <summary>
  /// Create a symbolic expression tree using the 'Grow' method.
  /// All symbols are allowed for nodes, so the resulting trees can be of any shape and size. 
  /// </summary>
  /// <param name="random">Random generator</param>
  /// <param name="encoding"></param>
  /// <returns></returns>
  public override SymbolicExpressionTree Create(IRandomNumberGenerator random, SymbolicExpressionTreeEncoding encoding) => CreateTree(random, encoding);

  private static SymbolicExpressionTree CreateTree(IRandomNumberGenerator random, SymbolicExpressionTreeEncoding encoding) {
    var tree = encoding.Grammar.MakeStump(random);

    Create(random, tree.Root.GetSubtree(0), encoding.TreeDepth - 1, encoding);
    return tree;
  }

  public static void Create(IRandomNumberGenerator random, SymbolicExpressionTreeNode seedNode, int maxDepth, SymbolicExpressionTreeEncoding encoding) {
    // make sure it is possible to create a trees smaller than maxDepth
    if (encoding.Grammar.GetMinimumExpressionDepth(seedNode.Symbol) > maxDepth)
      throw new ArgumentException("Cannot create trees of depth " + maxDepth + " or smaller because of grammar constraints.", nameof(maxDepth));

    var arity = SampleArity(random, seedNode, encoding);
    // throw an exception if the seedNode happens to be a terminal, since in this case we cannot grow a tree
    if (arity <= 0)
      throw new ArgumentException("Cannot grow tree. Seed node shouldn't have arity zero.");

    var allowedSymbols = encoding.Grammar.AllowedSymbols.Where(s => s.InitialFrequency > 0.0).ToList();

    for (var i = 0; i < arity; i++) {
      var possibleSymbols = allowedSymbols.Where(s => encoding.Grammar.IsAllowedChildSymbol(seedNode.Symbol, s, i)).ToList();
      var weights = possibleSymbols.Select(s => s.InitialFrequency).ToList();

      var selectedSymbol = possibleSymbols.SampleProportional(random, weights);
      var tree = selectedSymbol.CreateTreeNode();
      if (tree.HasLocalParameters) tree.ResetLocalParameters(random);
      seedNode.AddSubtree(tree);
    }

    // Only iterate over the non-terminal nodes (those which have arity > 0)
    // Start from depth 2 since the first two levels are formed by the rootNode and the seedNode
    foreach (var subTree in seedNode.Subtrees)
      if (encoding.Grammar.GetMaximumSubtreeCount(subTree.Symbol) > 0)
        RecursiveCreate(random, subTree, 2, maxDepth, encoding);
  }

  private static void RecursiveCreate(IRandomNumberGenerator random, SymbolicExpressionTreeNode root, int currentDepth, int maxDepth, SymbolicExpressionTreeEncoding encoding) {
    var arity = SampleArity(random, root, encoding);
    if (arity == 0)
      return;

    var allowedSymbols = encoding.Grammar.AllowedSymbols.Where(s => s.InitialFrequency > 0.0).ToList();

    for (var i = 0; i < arity; i++) {
      var possibleSymbols = allowedSymbols.Where(s => encoding.Grammar.IsAllowedChildSymbol(root.Symbol, s, i) &&
                                                      encoding.Grammar.GetMinimumExpressionDepth(s) - 1 <= maxDepth - currentDepth).ToList();

      if (possibleSymbols.Count == 0)
        throw new InvalidOperationException("No symbols are available for the tree.");

      var weights = possibleSymbols.Select(s => s.InitialFrequency).ToList();

      var selectedSymbol = possibleSymbols.SampleProportional(random, weights);

      var tree = selectedSymbol.CreateTreeNode();
      if (tree.HasLocalParameters) tree.ResetLocalParameters(random);
      root.AddSubtree(tree);
    }

    if (maxDepth <= currentDepth)
      return;

    foreach (var subTree in root.Subtrees)
      if (encoding.Grammar.GetMaximumSubtreeCount(subTree.Symbol) != 0)
        RecursiveCreate(random, subTree, currentDepth + 1, maxDepth, encoding);
  }

  private static int SampleArity(IRandomNumberGenerator random, SymbolicExpressionTreeNode node, SymbolicExpressionTreeEncoding encoding) {
    var minArity = encoding.Grammar.GetMinimumSubtreeCount(node.Symbol);
    var maxArity = encoding.Grammar.GetMaximumSubtreeCount(node.Symbol);

    return random.Next(minArity, maxArity + 1);
  }
}
