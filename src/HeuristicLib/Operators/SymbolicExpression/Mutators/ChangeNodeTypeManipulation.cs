using HEAL.HeuristicLib.Encodings.SymbolicExpression;
using HEAL.HeuristicLib.Encodings.SymbolicExpression.Symbols;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Operators.SymbolicExpression.Mutators;

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

      child = parent[childIndex];
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
          var existingSubtree = child[existingSubtreeIndex];
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
    var t = ChangeNodeType(parent, random, encoding);
    Extensions.CheckDebug(encoding.Contains(t), "Upps destroyed tree");
    return t;
  }
}
