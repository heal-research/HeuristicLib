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
using HEAL.HeuristicLib.Encodings.SymbolicExpression.Grammars;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Operators.SymbolicExpression.Creators;

public class ProbabilisticTreeCreator : SymbolicExpressionTreeCreator {
  private const int MAX_TRIES = 100;

  public override SymbolicExpressionTree Create(IRandom random, SymbolicExpressionTreeEncoding encoding) {
    var rootNode = encoding.Grammar.ProgramRootSymbol.CreateTreeNode();
    if (rootNode.HasLocalParameters) rootNode.ResetLocalParameters(random);

    var startNode = encoding.Grammar.StartSymbol.CreateTreeNode();
    if (startNode.HasLocalParameters) startNode.ResetLocalParameters(random);

    rootNode.AddSubtree(startNode);
    PTC2(random, startNode, encoding);
    return new(rootNode);
  }

  public static SymbolicExpressionTree CreateExpressionTree(IRandom random, ISymbolicExpressionGrammar grammar, int targetLength,
                                                            int maxTreeDepth, SymbolicExpressionTreeEncoding encoding) {
    var rootNode = grammar.ProgramRootSymbol.CreateTreeNode();
    if (rootNode.HasLocalParameters) rootNode.ResetLocalParameters(random);
    var startNode = grammar.StartSymbol.CreateTreeNode();
    if (startNode.HasLocalParameters) startNode.ResetLocalParameters(random);

    rootNode.AddSubtree(startNode);
    var success = TryCreateFullTreeFromSeed(random, startNode, targetLength - 2, maxTreeDepth - 1, encoding);
    if (!success) throw new InvalidOperationException($"Could not create a tree with target length {targetLength} and max depth {maxTreeDepth}");

    return new(rootNode);
  }

  private class TreeExtensionPoint {
    public SymbolicExpressionTreeNode Parent { get; init; }
    public int ChildIndex { get; init; }
    public int ExtensionPointDepth { get; init; }
    public int MaximumExtensionLength { get; set; }
    public int MinimumExtensionLength { get; set; }
  }

  public static void PTC2(IRandom random, SymbolicExpressionTreeNode seedNode,
                          SymbolicExpressionTreeEncoding encoding) {
    // make sure it is possible to create a trees smaller than maxLength and maxDepth
    var maxDepth = encoding.TreeDepth;
    var maxLength = encoding.TreeLength;

    if (encoding.Grammar.GetMinimumExpressionLength(seedNode.Symbol) > maxLength)
      throw new ArgumentException("Cannot create trees of length " + maxLength + " or shorter because of grammar constraints.", "maxLength");
    if (encoding.Grammar.GetMinimumExpressionDepth(seedNode.Symbol) > maxDepth)
      throw new ArgumentException("Cannot create trees of depth " + maxDepth + " or smaller because of grammar constraints.", "maxDepth");

    // tree length is limited by the grammar and by the explicit size constraints
    var allowedMinLength = encoding.Grammar.GetMinimumExpressionLength(seedNode.Symbol);
    var allowedMaxLength = Math.Min(maxLength, encoding.Grammar.GetMaximumExpressionLength(seedNode.Symbol, maxDepth));
    var tries = 0;
    while (tries++ < MAX_TRIES) {
      // select a target tree length uniformly in the possible range (as determined by explicit limits and limits of the grammar)
      int targetTreeLength;
      targetTreeLength = random.Integer(allowedMinLength, allowedMaxLength + 1);
      if (targetTreeLength <= 1 || maxDepth <= 1) return;

      var success = TryCreateFullTreeFromSeed(random, seedNode, targetTreeLength - 1, maxDepth - 1, encoding);

      // if successful => check constraints and return the tree if everything looks ok        
      if (success && seedNode.GetLength() <= maxLength && seedNode.GetDepth() <= maxDepth) {
        return;
      } else {
        // clean seedNode
        while (seedNode.Subtrees.Any()) seedNode.RemoveSubtree(0);
      }
      // try a different length MAX_TRIES times
    }

    throw new ArgumentException("Couldn't create a random valid tree.");
  }

  private static bool TryCreateFullTreeFromSeed(IRandom random, SymbolicExpressionTreeNode root,
                                                int targetLength, int maxDepth, SymbolicExpressionTreeEncoding encoding) {
    var extensionPoints = new List<TreeExtensionPoint>();
    var currentLength = 0;
    var actualArity = SampleArity(random, root, targetLength, maxDepth, encoding);
    if (actualArity < 0) return false;

    for (var i = 0; i < actualArity; i++) {
      // insert a dummy sub-tree and add the pending extension to the list
      var dummy = new SymbolicExpressionTreeNode();
      root.AddSubtree(dummy);
      var x = new TreeExtensionPoint { Parent = root, ChildIndex = i, ExtensionPointDepth = 0 };
      FillExtensionLengths(x, maxDepth, encoding);
      extensionPoints.Add(x);
    }

    //necessary to use long data type as the extension point length could be int.MaxValue
    var minExtensionPointsLength = extensionPoints.Select(x => (long)x.MinimumExtensionLength).Sum();
    var maxExtensionPointsLength = extensionPoints.Select(x => (long)x.MaximumExtensionLength).Sum();

    // while there are pending extension points and we have not reached the limit of adding new extension points
    while (extensionPoints.Count > 0 && minExtensionPointsLength + currentLength <= targetLength) {
      var randomIndex = random.Integer(extensionPoints.Count);
      var nextExtension = extensionPoints[randomIndex];
      extensionPoints.RemoveAt(randomIndex);
      var parent = nextExtension.Parent;
      var argumentIndex = nextExtension.ChildIndex;
      var extensionDepth = nextExtension.ExtensionPointDepth;

      if (encoding.Grammar.GetMinimumExpressionDepth(parent.Symbol) > maxDepth - extensionDepth) {
        ReplaceWithMinimalTree(random, parent, argumentIndex, encoding);
        var insertedTreeLength = parent.GetSubtree(argumentIndex).GetLength();
        currentLength += insertedTreeLength;
        minExtensionPointsLength -= insertedTreeLength;
        maxExtensionPointsLength -= insertedTreeLength;
      } else {
        //remove currently chosen extension point from calculation
        minExtensionPointsLength -= nextExtension.MinimumExtensionLength;
        maxExtensionPointsLength -= nextExtension.MaximumExtensionLength;

        var length = currentLength;
        var pointsLength = minExtensionPointsLength;
        var symbols = from s in encoding.Grammar.GetAllowedChildSymbols(parent.Symbol, argumentIndex)
                      where s.InitialFrequency > 0.0
                      where encoding.Grammar.GetMinimumExpressionDepth(s) <= maxDepth - extensionDepth
                      where encoding.Grammar.GetMinimumExpressionLength(s) <= targetLength - length - pointsLength
                      select s;
        if (maxExtensionPointsLength < targetLength - currentLength) {
          var length1 = currentLength;
          var extensionPointsLength = maxExtensionPointsLength;
          symbols = from s in symbols
                    where encoding.Grammar.GetMaximumExpressionLength(s, maxDepth - extensionDepth) >= targetLength - length1 - extensionPointsLength
                    select s;
        }

        var allowedSymbols = symbols.ToList();

        if (allowedSymbols.Count == 0) return false;
        var weights = allowedSymbols.Select(x => x.InitialFrequency).ToList();

        var selectedSymbol = allowedSymbols.SampleProportional(weights, random);

        var newTree = selectedSymbol.CreateTreeNode();
        if (newTree.HasLocalParameters) newTree.ResetLocalParameters(random);
        parent.RemoveSubtree(argumentIndex);
        parent.InsertSubtree(argumentIndex, newTree);

        currentLength++;
        actualArity = SampleArity(random, newTree, targetLength - currentLength, maxDepth - extensionDepth, encoding);
        if (actualArity < 0) return false;
        for (var i = 0; i < actualArity; i++) {
          // insert a dummy sub-tree and add the pending extension to the list
          var dummy = new SymbolicExpressionTreeNode();
          newTree.AddSubtree(dummy);
          var x = new TreeExtensionPoint { Parent = newTree, ChildIndex = i, ExtensionPointDepth = extensionDepth + 1 };
          FillExtensionLengths(x, maxDepth, encoding);
          extensionPoints.Add(x);
          maxExtensionPointsLength += x.MaximumExtensionLength;
          minExtensionPointsLength += x.MinimumExtensionLength;
        }
      }
    }

    // fill all pending extension points
    while (extensionPoints.Count > 0) {
      var randomIndex = random.Integer(extensionPoints.Count);
      var nextExtension = extensionPoints[randomIndex];
      extensionPoints.RemoveAt(randomIndex);
      var parent = nextExtension.Parent;
      var a = nextExtension.ChildIndex;
      ReplaceWithMinimalTree(random, parent, a, encoding);
    }

    return true;
  }

  private static void ReplaceWithMinimalTree(IRandom random, SymbolicExpressionTreeNode parent,
                                             int childIndex, SymbolicExpressionTreeEncoding encoding) {
    // determine possible symbols that will lead to the smallest possible tree
    var possibleSymbols = (from s in encoding.Grammar.GetAllowedChildSymbols(parent.Symbol, childIndex)
                           where s.InitialFrequency > 0.0
                           group s by encoding.Grammar.GetMinimumExpressionLength(s)
                           into g
                           orderby g.Key
                           select g).First().ToList();
    var weights = possibleSymbols.Select(x => x.InitialFrequency).ToList();

    var selectedSymbol = possibleSymbols.SampleProportional(weights, random);

    var tree = selectedSymbol.CreateTreeNode();
    if (tree.HasLocalParameters) tree.ResetLocalParameters(random);
    parent.RemoveSubtree(childIndex);
    parent.InsertSubtree(childIndex, tree);

    for (var i = 0; i < encoding.Grammar.GetMinimumSubtreeCount(tree.Symbol); i++) {
      // insert a dummy sub-tree and add the pending extension to the list
      var dummy = new SymbolicExpressionTreeNode();
      tree.AddSubtree(dummy);
      // replace the just inserted dummy by recursive application
      ReplaceWithMinimalTree(random, tree, i, encoding);
    }
  }

  private static void FillExtensionLengths(TreeExtensionPoint extension, int maxDepth, SymbolicExpressionTreeEncoding encoding) {
    var grammar = encoding.Grammar;
    var maxLength = int.MinValue;
    var minLength = int.MaxValue;
    foreach (var s in grammar.GetAllowedChildSymbols(extension.Parent.Symbol, extension.ChildIndex)) {
      if (s.InitialFrequency <= 0.0) continue;
      var max = grammar.GetMaximumExpressionLength(s, maxDepth - extension.ExtensionPointDepth);
      maxLength = Math.Max(maxLength, max);
      var min = grammar.GetMinimumExpressionLength(s);
      minLength = Math.Min(minLength, min);
    }

    extension.MaximumExtensionLength = maxLength;
    extension.MinimumExtensionLength = minLength;
  }

  private static int SampleArity(IRandom random, SymbolicExpressionTreeNode node, int targetLength, int maxDepth, SymbolicExpressionTreeEncoding encoding) {
    // select actualArity randomly with the constraint that the sub-trees in the minimal arity can become large enough
    var minArity = encoding.Grammar.GetMinimumSubtreeCount(node.Symbol);
    var maxArity = encoding.Grammar.GetMaximumSubtreeCount(node.Symbol);
    if (maxArity > targetLength) {
      maxArity = targetLength;
    }

    if (minArity == maxArity) return minArity;

    // the min number of sub-trees has to be set to a value that is large enough so that the largest possible tree is at least tree length
    // if 1..3 trees are possible and the largest possible first sub-tree is smaller larger than the target length then minArity should be at least 2
    long aggregatedLongestExpressionLength = 0;
    for (var i = 0; i < maxArity; i++) {
      aggregatedLongestExpressionLength += (from s in encoding.Grammar.GetAllowedChildSymbols(node.Symbol, i)
                                            where s.InitialFrequency > 0.0
                                            select encoding.Grammar.GetMaximumExpressionLength(s, maxDepth)).Max();
      if (i > minArity && aggregatedLongestExpressionLength < targetLength) minArity = i + 1;
      else break;
    }

    // the max number of sub-trees has to be set to a value that is small enough so that the smallest possible tree is at most tree length 
    // if 1..3 trees are possible and the smallest possible first sub-tree is already larger than the target length then maxArity should be at most 0
    long aggregatedShortestExpressionLength = 0;
    for (var i = 0; i < maxArity; i++) {
      aggregatedShortestExpressionLength += (from s in encoding.Grammar.GetAllowedChildSymbols(node.Symbol, i)
                                             where s.InitialFrequency > 0.0
                                             select encoding.Grammar.GetMinimumExpressionLength(s)).Min();
      if (aggregatedShortestExpressionLength > targetLength) {
        maxArity = i;
        break;
      }
    }

    if (minArity > maxArity) return -1;
    return random.Next(minArity, maxArity + 1);
  }
}
