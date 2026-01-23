using HEAL.HeuristicLib.Genotypes.Trees;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces.Trees;
using HEAL.HeuristicLib.SearchSpaces.Trees.SymbolicExpressionTree.Symbols;

namespace HEAL.HeuristicLib.Operators.Mutators.SymbolicExpressionTreeMutators;

public sealed class RemoveBranchManipulation : SymbolicExpressionTreeManipulator
{
  private const int MaxTries = 100;

  public static SymbolicExpressionTree RemoveRandomBranch(IRandomNumberGenerator random, SymbolicExpressionTree symbolicExpressionTree, SymbolicExpressionTreeSearchSpace searchSpace)
  {
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

      childIndex = random.NextInt(parent.SubtreeCount);
      var child = parent[childIndex];
      maxLength = searchSpace.TreeLength - childTree.Length + child.GetLength();
      maxDepth = searchSpace.TreeDepth - childTree.Depth + child.GetDepth();

      allowedSymbols.Clear();
      foreach (var symbol in searchSpace.Grammar.GetAllowedChildSymbols(parent.Symbol, childIndex)) {
        // check basic properties that the new symbol must have
        if (symbol != child.Symbol &&
            symbol.InitialFrequency > 0 &&
            searchSpace.Grammar.GetMinimumExpressionDepth(symbol) + 1 <= maxDepth &&
            searchSpace.Grammar.GetMinimumExpressionLength(symbol) <= maxLength) {
          allowedSymbols.Add(symbol);
        }
      }

      tries++;
    } while (tries < MaxTries && allowedSymbols.Count == 0);

    if (tries >= MaxTries) {
      return symbolicExpressionTree;
    }

    ReplaceWithMinimalTree(random, childTree.Root, parent, childIndex, searchSpace);
    return childTree;
  }

  private static void ReplaceWithMinimalTree(IRandomNumberGenerator random, SymbolicExpressionTreeNode root, SymbolicExpressionTreeNode parent, int childIndex, SymbolicExpressionTreeSearchSpace searchSpace)
  {
    // determine possible symbols that will lead to the smallest possible tree
    var possibleSymbols = (from s in searchSpace.Grammar.GetAllowedChildSymbols(parent.Symbol, childIndex)
      where s.InitialFrequency > 0.0
      group s by searchSpace.Grammar.GetMinimumExpressionLength(s)
      into g
      orderby g.Key
      select g).First().ToList();
    var weights = possibleSymbols.Select(x => x.InitialFrequency).ToList();

    var selectedSymbol = possibleSymbols.SampleProportional(random, 1, weights).First();

    var newTreeNode = selectedSymbol.CreateTreeNode();
    if (newTreeNode.HasLocalParameters) {
      newTreeNode.ResetLocalParameters(random);
    }

    parent.RemoveSubtree(childIndex);
    parent.InsertSubtree(childIndex, newTreeNode);

    for (var i = 0; i < searchSpace.Grammar.GetMinimumSubtreeCount(newTreeNode.Symbol); i++) {
      // insert a dummy sub-tree and add the pending extension to the list
      var dummy = new SymbolicExpressionTreeNode();
      newTreeNode.AddSubtree(dummy);
      // replace the just inserted dummy by recursive application
      ReplaceWithMinimalTree(random, root, newTreeNode, i, searchSpace);
    }
  }

  public override SymbolicExpressionTree Mutate(SymbolicExpressionTree parent, IRandomNumberGenerator random, SymbolicExpressionTreeSearchSpace searchSpace)
  {
    var t = RemoveRandomBranch(random, parent, searchSpace);
    Extensions.CheckDebug(searchSpace.Contains(t), "Upps destroyed tree");
    return t;
  }
}
