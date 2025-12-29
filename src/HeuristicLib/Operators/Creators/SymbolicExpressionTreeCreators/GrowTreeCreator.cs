using HEAL.HeuristicLib.Genotypes.Trees;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces.Trees;
using HEAL.HeuristicLib.SearchSpaces.Trees.SymbolicExpressionTree.Symbols;

namespace HEAL.HeuristicLib.Operators.Creators.SymbolicExpressionTreeCreators;

public class GrowTreeCreator : SymbolicExpressionTreeCreator {
  /// <summary>
  /// GetEvaluator a symbolic expression tree using the 'Grow' method.
  /// All symbols are allowed for nodes, so the resulting trees can be of any shape and size. 
  /// </summary>
  /// <param name="random">Random generator</param>
  /// <param name="searchSpace"></param>
  /// <returns></returns>
  public override SymbolicExpressionTree Create(IRandomNumberGenerator random, SymbolicExpressionTreeSearchSpace searchSpace) => CreateTree(random, searchSpace);

  private static SymbolicExpressionTree CreateTree(IRandomNumberGenerator random, SymbolicExpressionTreeSearchSpace searchSpace) {
    var tree = searchSpace.Grammar.MakeStump(random);

    Create(random, tree.Root[0], searchSpace.TreeDepth - tree.Length, searchSpace);
    return tree;
  }

  public static void Create(IRandomNumberGenerator random, SymbolicExpressionTreeNode seedNode, int maxDepth, SymbolicExpressionTreeSearchSpace searchSpace) {
    // make sure it is possible to create a trees smaller than maxDepth
    if (searchSpace.Grammar.GetMinimumExpressionDepth(seedNode.Symbol) > maxDepth)
      throw new ArgumentException("Cannot create trees of depth " + maxDepth + " or smaller because of grammar constraints.", nameof(maxDepth));
    var allowedSymbols = searchSpace.Grammar.AllowedSymbols.Where(s => s.InitialFrequency > 0.0).ToList();
    RecursiveCreate(random, seedNode, 0, maxDepth - 1, searchSpace, allowedSymbols);
  }

  private static void RecursiveCreate(IRandomNumberGenerator random, SymbolicExpressionTreeNode root, int currentDepth, int maxDepth, SymbolicExpressionTreeSearchSpace searchSpace, IReadOnlyCollection<Symbol> allowedSymbols) {
    var arity = SampleArity(random, root, searchSpace);
    if (arity == 0)
      return;

    for (var i = 0; i < arity; i++) {
      var possibleSymbols = allowedSymbols.Where(s => searchSpace.Grammar.IsAllowedChildSymbol(root.Symbol, s, i) &&
                                                      searchSpace.Grammar.GetMinimumExpressionDepth(s) - 1 <= maxDepth - currentDepth).ToList();

      if (possibleSymbols.Count == 0) throw new InvalidOperationException("No symbols are available for the tree.");

      var selectedSymbol = possibleSymbols.SampleProportional(random, possibleSymbols.Select(s => s.InitialFrequency));

      var tree = selectedSymbol.CreateTreeNode();
      tree.ResetLocalParameters(random);
      root.AddSubtree(tree);
    }

    if (maxDepth <= currentDepth)
      return;

    foreach (var subTree in root.Subtrees)
      if (searchSpace.Grammar.GetMaximumSubtreeCount(subTree.Symbol) != 0)
        RecursiveCreate(random, subTree, currentDepth + 1, maxDepth, searchSpace, allowedSymbols);
  }

  private static int SampleArity(IRandomNumberGenerator random, SymbolicExpressionTreeNode node, SymbolicExpressionTreeSearchSpace searchSpace) {
    var minArity = searchSpace.Grammar.GetMinimumSubtreeCount(node.Symbol);
    var maxArity = searchSpace.Grammar.GetMaximumSubtreeCount(node.Symbol);
    return random.Integer(minArity, maxArity, true);
  }
}
