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

namespace HEAL.HeuristicLib.Operators.SymbolicExpression.Crossovers;

/// <summary>
/// Takes two parent individuals P0 and P1 each. Selects a random node N0 of P0 and a random node N1 of P1.
/// And replaces the branch with root0 N0 in P0 with N1 from P1 if the tree-size limits are not violated.
/// When recombination with N0 and N1 would create a tree that is too large or invalid the operator randomly selects new N0 and N1 
/// until a valid configuration is found.
/// </summary>  
public class SubtreeCrossover : SymbolicExpressionTreeCrossover {
  public double InternalCrossoverPointProbability { get; set; } = 0.9;

  public static SymbolicExpressionTree Cross(IRandomNumberGenerator random,
                                             SymbolicExpressionTree parent0, SymbolicExpressionTree parent1,
                                             double internalCrossoverPointProbability, SymbolicExpressionTreeEncoding encoding) {
    var op0 = parent0;
    // select a random crossover point in the first parent 
    parent0 = new SymbolicExpressionTree(parent0.Root.Clone());
    SelectCrossoverPoint(random, parent0, internalCrossoverPointProbability, encoding, out var crossoverPoint0);

    var childLength = crossoverPoint0.Child?.GetLength() ?? 0;
    // calculate the max length and depth that the inserted branch can have 
    var maxInsertedBranchLength = Math.Max(0, encoding.TreeLength - (parent0.Length - childLength));
    var maxInsertedBranchDepth = Math.Max(0, encoding.TreeDepth - parent0.Root.GetBranchLevel(crossoverPoint0.Parent));

    var allowedBranches = new List<SymbolicExpressionTreeNode?>();
    parent1.Root.ForEachNodePostfix((n) => {
      if (n.GetLength() <= maxInsertedBranchLength &&
          n.GetDepth() <= maxInsertedBranchDepth && crossoverPoint0.IsMatchingPointType(n))
        allowedBranches.Add(n);
    });
    // empty branch
    if (crossoverPoint0.IsMatchingPointType(null)) allowedBranches.Add(null);

    if (allowedBranches.Count == 0) {
      return parent0;
    }

    var selectedBranch = SelectRandomBranch(random, allowedBranches, internalCrossoverPointProbability);
    selectedBranch = selectedBranch?.Clone();

    if (crossoverPoint0.Child != null) {
      // manipulate the tree of parent0 in place
      // replace the branch in tree0 with the selected branch from tree1

      if (selectedBranch != null) {
        crossoverPoint0.Parent.RemoveSubtree(crossoverPoint0.ChildIndex);
        crossoverPoint0.Parent.InsertSubtree(crossoverPoint0.ChildIndex, selectedBranch);
      }
    } else {
      // child is null (additional child should be added under the parent)
      if (selectedBranch != null) {
        crossoverPoint0.Parent.AddSubtree(selectedBranch);
      }
    }

    Extensions.CheckDebug(encoding.Contains(parent0), "Generated Invalid Child");
    return parent0;
  }

  private static void SelectCrossoverPoint(IRandomNumberGenerator random, SymbolicExpressionTree parent0, double internalNodeProbability, SymbolicExpressionTreeEncoding encoding, out CutPoint crossoverPoint) {
    if (internalNodeProbability is < 0.0 or > 1.0) throw new ArgumentException("internalNodeProbability");
    var maxBranchLength = encoding.TreeLength;
    var maxBranchDepth = encoding.TreeDepth;
    var internalCrossoverPoints = new List<CutPoint>();
    var leafCrossoverPoints = new List<CutPoint>();
    parent0.Root.ForEachNodePostfix((n) => {
        if (n.SubtreeCount <= 0 || n == parent0.Root)
          return;

        //avoid linq to reduce memory pressure
        for (var i = 0; i < n.SubtreeCount; i++) {
          var child = n.GetSubtree(i);
          if (child.GetLength() > maxBranchLength ||
              child.GetDepth() > maxBranchDepth) {
            continue;
          }

          if (child.SubtreeCount > 0)
            internalCrossoverPoints.Add(new CutPoint(n, child, encoding));
          else
            leafCrossoverPoints.Add(new CutPoint(n, child, encoding));
        }

        // add one additional extension point if the number of subtrees for the symbol is not full
        if (n.SubtreeCount < encoding.Grammar.GetMaximumSubtreeCount(n.Symbol)) {
          // empty extension point
          internalCrossoverPoints.Add(new CutPoint(n, n.SubtreeCount, encoding));
        }
      }
    );

    if (random.Random() < internalNodeProbability) {
      // select from internal node if possible
      // select internal crossover point or leaf
      crossoverPoint = internalCrossoverPoints.Count > 0
        ? internalCrossoverPoints[random.Next(internalCrossoverPoints.Count)]
        :
        // otherwise select external node
        leafCrossoverPoints[random.Next(leafCrossoverPoints.Count)];
    } else if (leafCrossoverPoints.Count > 0) {
      // select from leaf crossover point if possible
      crossoverPoint = leafCrossoverPoints[random.Next(leafCrossoverPoints.Count)];
    } else {
      // otherwise select internal crossover point
      crossoverPoint = internalCrossoverPoints[random.Next(internalCrossoverPoints.Count)];
    }
  }

  private static SymbolicExpressionTreeNode? SelectRandomBranch(IRandomNumberGenerator random, List<SymbolicExpressionTreeNode?> branches, double internalNodeProbability) {
    if (internalNodeProbability is < 0.0 or > 1.0) throw new ArgumentException("internalNodeProbability");
    List<SymbolicExpressionTreeNode> allowedInternalBranches;
    List<SymbolicExpressionTreeNode> allowedLeafBranches;
    if (random.Random() < internalNodeProbability) {
      // select internal node if possible
      allowedInternalBranches = (from branch in branches
                                 where branch is { SubtreeCount: > 0 }
                                 select branch).ToList();
      if (allowedInternalBranches.Count > 0)
        return allowedInternalBranches.SampleRandom(random);

      // no internal nodes allowed => select leaf nodes
      allowedLeafBranches = (from branch in branches
                             where branch == null || branch.SubtreeCount == 0
                             select branch).ToList();
      return allowedLeafBranches.Count == 0 ? null : allowedLeafBranches.SampleRandom(random);
    }

    // select leaf node if possible
    allowedLeafBranches = (from branch in branches
                           where branch == null || branch.SubtreeCount == 0
                           select branch).ToList();
    if (allowedLeafBranches.Count > 0)
      return allowedLeafBranches.SampleRandom(random);

    allowedInternalBranches = (from branch in branches
                               where branch is { SubtreeCount: > 0 }
                               select branch).ToList();
    return allowedInternalBranches.Count == 0 ? null : allowedLeafBranches.SampleRandom(random);
  }

  public override SymbolicExpressionTree Cross((SymbolicExpressionTree, SymbolicExpressionTree) parents, IRandomNumberGenerator random, SymbolicExpressionTreeEncoding encoding) => Cross(random, parents.Item1, parents.Item2, InternalCrossoverPointProbability, encoding);
}
