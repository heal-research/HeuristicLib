using HEAL.HeuristicLib.Encodings.SymbolicExpressionTree.Creators;
using HEAL.HeuristicLib.Encodings.SymbolicExpressionTree.Symbols;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Encodings.SymbolicExpressionTree.Mutators;

public sealed class ReplaceBranchManipulation : SymbolicExpressionTreeManipulator {
  private const int MaxTries = 100;

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
      var child = parent[childIndex];
      maxLength = encoding.TreeLength - childTree.Length + child.GetLength();
      maxDepth = encoding.TreeDepth - childTree.Depth + child.GetDepth();

      allowedSymbols.Clear();
      allowedSymbols.AddRange(encoding.Grammar.GetAllowedChildSymbols(parent.Symbol, childIndex)
                                      .Where(symbol => symbol != child.Symbol
                                                       && symbol.InitialFrequency > 0
                                                       && encoding.Grammar.GetMinimumExpressionDepth(symbol) + 1 <= maxDepth
                                                       && encoding.Grammar.GetMinimumExpressionLength(symbol) <= maxLength));

      tries++;
    } while (tries < MaxTries && allowedSymbols.Count == 0);

    if (tries < MaxTries) {
      var weights = allowedSymbols.Select(s => s.InitialFrequency).ToList();
      var seedSymbol = allowedSymbols.SampleProportional(random, 1, weights).First();

      // replace the old node with the new node
      var seedNode = seedSymbol.CreateTreeNode();
      if (seedNode.HasLocalParameters)
        seedNode.ResetLocalParameters(random);

      parent.RemoveSubtree(childIndex);
      parent.InsertSubtree(childIndex, seedNode);
      ProbabilisticTreeCreator.Ptc2(random, seedNode, maxDepth, maxLength, encoding);
      return childTree;
    }

    return symbolicExpressionTree;
  }

  public override SymbolicExpressionTree Mutate(SymbolicExpressionTree parent, IRandomNumberGenerator random, SymbolicExpressionTreeEncoding encoding) {
    var t = ReplaceRandomBranch(random, parent, encoding);
    Extensions.CheckDebug(encoding.Contains(t), "Upps destroyed tree");
    return t;
  }
}
