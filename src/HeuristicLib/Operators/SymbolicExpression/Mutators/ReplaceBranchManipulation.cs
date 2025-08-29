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

namespace HEAL.HeuristicLib.Operators.SymbolicExpression.Mutators {
  public sealed class ReplaceBranchManipulation : SymbolicExpressionTreeManipulator {
    private const int MAX_TRIES = 100;

    public static SymbolicExpressionTree ReplaceRandomBranch(IRandomNumberGenerator random, SymbolicExpressionTree symbolicExpressionTree, SymbolicExpressionTreeEncoding encoding) {
      var allowedSymbols = new List<Symbol>();
      SymbolicExpressionTreeNode parent;
      int childIndex;
      int maxLength;
      int maxDepth;
      // repeat until a fitting parent and child are found (MAX_TRIES times)
      var tries = 0;

      var childTree = new SymbolicExpressionTree(symbolicExpressionTree);
      do {
        parent = childTree.Root.IterateNodesPrefix().Skip(1).Where(n => n.SubtreeCount > 0).SampleRandom(random);

        childIndex = random.Integer(parent.SubtreeCount);
        var child = parent.GetSubtree(childIndex);
        maxLength = encoding.TreeLength - childTree.Length + child.GetLength();
        maxDepth = encoding.TreeDepth - childTree.Depth + child.GetDepth();

        allowedSymbols.Clear();
        allowedSymbols.AddRange(encoding.Grammar.GetAllowedChildSymbols(parent.Symbol, childIndex)
                                        .Where(symbol => symbol != child.Symbol
                                                         && symbol.InitialFrequency > 0
                                                         && encoding.Grammar.GetMinimumExpressionDepth(symbol) + 1 <= maxDepth
                                                         && encoding.Grammar.GetMinimumExpressionLength(symbol) <= maxLength));

        tries++;
      } while (tries < MAX_TRIES && allowedSymbols.Count == 0);

      if (tries < MAX_TRIES) {
        var weights = allowedSymbols.Select(s => s.InitialFrequency).ToList();
        var seedSymbol = allowedSymbols.SampleProportional(random, 1, weights).First();

        // replace the old node with the new node
        var seedNode = seedSymbol.CreateTreeNode();
        if (seedNode.HasLocalParameters)
          seedNode.ResetLocalParameters(random);

        parent.RemoveSubtree(childIndex);
        parent.InsertSubtree(childIndex, seedNode);
        ProbabilisticTreeCreator.PTC2(random, seedNode, maxLength, maxDepth);
        return childTree;
      }

      return symbolicExpressionTree;
    }

    public override SymbolicExpressionTree Mutate(SymbolicExpressionTree parent, IRandomNumberGenerator random, SymbolicExpressionTreeEncoding encoding) => ReplaceRandomBranch(random, parent, encoding);
  }
}
