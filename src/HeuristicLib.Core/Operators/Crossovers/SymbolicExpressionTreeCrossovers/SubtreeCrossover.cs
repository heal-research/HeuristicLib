using HEAL.HeuristicLib.Genotypes.Trees;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces.Trees;

namespace HEAL.HeuristicLib.Operators.Crossovers.SymbolicExpressionTreeCrossovers;

/// <summary>
///   Takes two parent individuals P0 and P1 each. Selects a random node N0 of P0 and a random node N1 of P1.
///   And replaces the branch with root0 N0 in P0 with N1 from P1 if the tree-size limits are not violated.
///   When recombination with N0 and N1 would create a tree that is too large or invalid the operator randomly selects new
///   N0 and N1
///   until a valid configuration is found.
/// </summary>
public class SubtreeCrossover : SymbolicExpressionTreeCrossover
{
  public double InternalCrossoverPointProbability { get; set; } = 0.9;

  public static SymbolicExpressionTree Cross(IRandomNumberGenerator random,
    SymbolicExpressionTree parent0, SymbolicExpressionTree parent1,
    double internalCrossoverPointProbability, SymbolicExpressionTreeSearchSpace searchSpace)
  {
    // select a random crossover point in the first parent 
    parent0 = new SymbolicExpressionTree(parent0.Root.Clone());
    SelectCrossoverPoint(random, parent0, internalCrossoverPointProbability, searchSpace, out var crossoverPoint0);

    var childLength = crossoverPoint0.Child?.GetLength() ?? 0;
    // calculate the max length and depth that the inserted branch can have 
    var maxInsertedBranchLength = Math.Max(0, searchSpace.TreeLength - (parent0.Length - childLength));
    var maxInsertedBranchDepth = Math.Max(0, searchSpace.TreeDepth - parent0.Root.GetBranchLevel(crossoverPoint0.Parent));

    var allowedBranches = new List<SymbolicExpressionTreeNode?>();
    parent1.Root.ForEachNodePostfix(n => {
      if (n.GetLength() <= maxInsertedBranchLength &&
          n.GetDepth() <= maxInsertedBranchDepth && crossoverPoint0.IsMatchingPointType(n)) {
        allowedBranches.Add(n);
      }
    });
    // empty branch
    if (crossoverPoint0.IsMatchingPointType(null)) {
      allowedBranches.Add(null);
    }

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

    Extensions.CheckDebug(searchSpace.Contains(parent0), "Generated Invalid Child");
    return parent0;
  }

  private static void SelectCrossoverPoint(IRandomNumberGenerator random, SymbolicExpressionTree parent0, double internalNodeProbability, SymbolicExpressionTreeSearchSpace searchSpace, out CutPoint crossoverPoint)
  {
    ArgumentOutOfRangeException.ThrowIfLessThan(internalNodeProbability, 0);
    ArgumentOutOfRangeException.ThrowIfGreaterThan(internalNodeProbability, 1);
    var maxBranchLength = searchSpace.TreeLength;
    var maxBranchDepth = searchSpace.TreeDepth;
    var internalCrossoverPoints = new List<CutPoint>();
    var leafCrossoverPoints = new List<CutPoint>();
    parent0.Root.ForEachNodePostfix(n => {
        if (n.SubtreeCount <= 0 || n == parent0.Root) {
          return;
        }

        //avoid linq to reduce memory pressure
        for (var i = 0; i < n.SubtreeCount; i++) {
          var child = n[i];
          if (child.GetLength() > maxBranchLength ||
              child.GetDepth() > maxBranchDepth) {
            continue;
          }

          if (child.SubtreeCount > 0) {
            internalCrossoverPoints.Add(new CutPoint(n, child, searchSpace));
          } else {
            leafCrossoverPoints.Add(new CutPoint(n, child, searchSpace));
          }
        }

        // add one additional extension point if the number of subtrees for the symbol is not full
        if (n.SubtreeCount < searchSpace.Grammar.GetMaximumSubtreeCount(n.Symbol)) {
          // empty extension point
          internalCrossoverPoints.Add(new CutPoint(n, n.SubtreeCount, searchSpace));
        }
      }
    );

    if (random.NextDouble() < internalNodeProbability) {
      // select from internal node if possible
      // select internal crossover point or leaf
      crossoverPoint = internalCrossoverPoints.Count > 0
        ? internalCrossoverPoints[random.NextInt(internalCrossoverPoints.Count)]
        :
        // otherwise select external node
        leafCrossoverPoints[random.NextInt(leafCrossoverPoints.Count)];
    } else if (leafCrossoverPoints.Count > 0) {
      // select from leaf crossover point if possible
      crossoverPoint = leafCrossoverPoints[random.NextInt(leafCrossoverPoints.Count)];
    } else {
      // otherwise select internal crossover point
      crossoverPoint = internalCrossoverPoints[random.NextInt(internalCrossoverPoints.Count)];
    }
  }

  private static SymbolicExpressionTreeNode? SelectRandomBranch(IRandomNumberGenerator random, List<SymbolicExpressionTreeNode?> branches, double internalNodeProbability)
  {
    if (internalNodeProbability is < 0.0 or > 1.0) {
      throw new ArgumentException("internalNodeProbability");
    }

    List<SymbolicExpressionTreeNode> allowedInternalBranches;
    List<SymbolicExpressionTreeNode> allowedLeafBranches;
    if (random.NextDouble() < internalNodeProbability) {
      // select internal node if possible
      allowedInternalBranches = (from branch in branches
        where branch is { SubtreeCount: > 0 }
        select branch).ToList();
      if (allowedInternalBranches.Count > 0) {
        return allowedInternalBranches.SampleRandom(random);
      }

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
    if (allowedLeafBranches.Count > 0) {
      return allowedLeafBranches.SampleRandom(random);
    }

    allowedInternalBranches = (from branch in branches
      where branch is { SubtreeCount: > 0 }
      select branch).ToList();
    return allowedInternalBranches.Count == 0 ? null : allowedInternalBranches.SampleRandom(random);
  }

  public override SymbolicExpressionTree Cross(IParents<SymbolicExpressionTree> parents, IRandomNumberGenerator random, SymbolicExpressionTreeSearchSpace searchSpace) => Cross(random, parents.Item1, parents.Item2, InternalCrossoverPointProbability, searchSpace);
}
