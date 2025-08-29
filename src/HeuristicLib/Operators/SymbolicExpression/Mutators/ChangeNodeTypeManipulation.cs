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
  public class ChangeNodeTypeManipulation : SymbolicExpressionTreeManipulator {
    private const int MaxTries = 100;

    public static SymbolicExpressionTree ChangeNodeType(SymbolicExpressionTree symbolicExpressionTree, IRandomNumberGenerator random, SymbolicExpressionTreeEncoding encoding) {
      var allowedSymbols = new List<Symbol>();
      var mutant = new SymbolicExpressionTree(symbolicExpressionTree);

      SymbolicExpressionTreeNode parent;
      int childIndex;
      SymbolicExpressionTreeNode child;
      // repeat until a fitting parent and child are found (MAX_TRIES times)
      var tries = 0;
      var grammar = encoding.Grammar;
      do {
        parent = mutant.Root.IterateNodesPrefix().Skip(1).Where(n => n.SubtreeCount > 0).SampleRandom(random, 1).First();

        childIndex = random.Integer(parent.SubtreeCount);

        child = parent.GetSubtree(childIndex);
        var existingSubtreeCount = child.SubtreeCount;
        allowedSymbols.Clear();
        foreach (var symbol in grammar.GetAllowedChildSymbols(parent.Symbol, childIndex)) {
          // check basic properties that the new symbol must have
          if (symbol == child.Symbol ||
              symbol.InitialFrequency <= 0 ||
              existingSubtreeCount > grammar.GetMinimumSubtreeCount(symbol) ||
              existingSubtreeCount < grammar.GetMaximumSubtreeCount(symbol)) {
            continue;
          }

          // check that all existing subtrees are also allowed for the new symbol
          var allExistingSubtreesAllowed = true;
          for (var existingSubtreeIndex = 0; existingSubtreeIndex < existingSubtreeCount && allExistingSubtreesAllowed; existingSubtreeIndex++) {
            var existingSubtree = child.GetSubtree(existingSubtreeIndex);
            allExistingSubtreesAllowed &= grammar.IsAllowedChildSymbol(symbol, existingSubtree.Symbol, existingSubtreeIndex);
          }

          if (allExistingSubtreesAllowed) {
            allowedSymbols.Add(symbol);
          }
        }

        tries++;
      } while (tries < MaxTries && allowedSymbols.Count == 0);

      if (tries >= MaxTries)
        return symbolicExpressionTree;

      var weights = allowedSymbols.Select(s => s.InitialFrequency).ToList();
      var newSymbol = allowedSymbols.SampleProportional(random, 1, weights).First();

      // replace the old node with the new node
      var newNode = newSymbol.CreateTreeNode();
      if (newNode.HasLocalParameters)
        newNode.ResetLocalParameters(random);
      foreach (var subtree in child.Subtrees)
        newNode.AddSubtree(subtree);

      parent.RemoveSubtree(childIndex);
      parent.InsertSubtree(childIndex, newNode);
      return mutant;
    }

    public override SymbolicExpressionTree Mutate(SymbolicExpressionTree parent, IRandomNumberGenerator random, SymbolicExpressionTreeEncoding encoding) {
      return ChangeNodeType(parent, random, encoding);
    }
  }
}
