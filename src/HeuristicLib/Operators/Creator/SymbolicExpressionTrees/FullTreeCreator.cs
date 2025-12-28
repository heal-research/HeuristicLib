using HEAL.HeuristicLib.Genotypes.Trees;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces.Trees;
using HEAL.HeuristicLib.SearchSpaces.Trees.SymbolicExpressionTree.Grammars;
using HEAL.HeuristicLib.SearchSpaces.Trees.SymbolicExpressionTree.Symbols;

namespace HEAL.HeuristicLib.Operators.Creator.SymbolicExpressionTrees;

public class FullTreeCreator : SymbolicExpressionTreeCreator {
  /// <summary>
  /// GetEvaluator a symbolic expression tree using the 'Full' method.
  /// Function symbols are used for all nodes situated on a level above the maximum tree depth. 
  /// Nodes on the last tree level will have Terminal symbols.
  /// </summary>
  /// <param name="random">Random generator</param>
  /// <param name="grammar">Available tree grammar</param>
  /// <param name="maxTreeDepth">Maximum tree depth</param>
  /// <param name="maxTreeLength">Maximum tree length. This parameter is not used.</param>
  /// <returns></returns>
  public override SymbolicExpressionTree Create(IRandomNumberGenerator random, SymbolicExpressionTreeSearchSpace searchSpace) {
    return CreateTree(random, searchSpace);
  }

  public static SymbolicExpressionTree CreateTree(IRandomNumberGenerator random, SymbolicExpressionTreeSearchSpace searchSpace) {
    var tree = searchSpace.Grammar.MakeStump(random);
    Create(random, tree.Root[0], searchSpace, searchSpace.TreeDepth - tree.Depth);
    return tree;
  }

  public static void Create(IRandomNumberGenerator random, SymbolicExpressionTreeNode seedNode, SymbolicExpressionTreeSearchSpace searchSpace, int maxDepth) {
    // make sure it is possible to create a trees smaller than maxDepth
    if (searchSpace.Grammar.GetMinimumExpressionDepth(seedNode.Symbol) > maxDepth)
      throw new ArgumentException("Cannot create trees of depth " + maxDepth + " or smaller because of grammar constraints.", nameof(maxDepth));

    var arity = searchSpace.Grammar.GetMaximumSubtreeCount(seedNode.Symbol);
    // Throw an exception if the seedNode happens to be a terminal, since in this case we cannot grow a tree.
    if (arity <= 0) throw new ArgumentException("Cannot grow tree. Seed node shouldn't have arity zero.");

    var allowedSymbols = searchSpace.Grammar.AllowedSymbols
                                 .Where(s => s.InitialFrequency > 0.0 && searchSpace.Grammar.GetMaximumSubtreeCount(s) > 0)
                                 .ToList();

    for (var i = 0; i < arity; i++) {
      var selectedSymbol = SelectSymbol(random, seedNode, maxDepth, allowedSymbols, searchSpace.Grammar, i);
      var tree = selectedSymbol.CreateTreeNode();
      if (tree.HasLocalParameters)
        tree.ResetLocalParameters(random);
      seedNode.AddSubtree(tree);
    }

    var allowedSymbols1 = searchSpace.Grammar.AllowedSymbols.Where(s => s.InitialFrequency > 0.0).ToList();
    // Only iterate over the non-terminal nodes (those which have arity > 0)
    // Start from depth 2 since the first two levels are formed by the rootNode and the seedNode
    foreach (var subTree in seedNode.Subtrees)
      if (searchSpace.Grammar.GetMaximumSubtreeCount(subTree.Symbol) > 0)
        RecursiveCreate(random, subTree, searchSpace, 2, maxDepth, allowedSymbols1);
  }

  private static void RecursiveCreate(IRandomNumberGenerator random, SymbolicExpressionTreeNode root, SymbolicExpressionTreeSearchSpace searchSpace, int currentDepth, int maxDepth, List<Symbol> allowedSymbols) {
    var grammar = searchSpace.Grammar;
    var arity = grammar.GetMaximumSubtreeCount(root.Symbol);
    // In the 'Full' grow method, we cannot have terminals on the intermediate tree levels.
    if (arity <= 0)
      throw new ArgumentException("Cannot grow node of arity zero. Expected a function node.");

    for (var i = 0; i < arity; i++) {
      var selectedSymbol = SelectSymbol(random, root, maxDepth - currentDepth, allowedSymbols, grammar, i);
      var tree = selectedSymbol.CreateTreeNode();
      tree.ResetLocalParameters(random);
      root.AddSubtree(tree);
    }

    //additional levels should only be added if the maximum depth is not reached yet
    if (maxDepth <= currentDepth) {
      return;
    }

    foreach (var subTree in root.Subtrees)
      if (grammar.GetMaximumSubtreeCount(subTree.Symbol) > 0)
        RecursiveCreate(random, subTree, searchSpace, currentDepth + 1, maxDepth, allowedSymbols);
  }

  private static Symbol SelectSymbol(IRandomNumberGenerator random, SymbolicExpressionTreeNode root, int remainingDepth, List<Symbol> allowedSymbols, ISymbolicExpressionGrammar grammar, int position) {
    var suballowed = allowedSymbols.Where(s => grammar.IsAllowedChildSymbol(root.Symbol, s, position));
    var possibleSymbols = suballowed.Where(s => grammar.GetMinimumExpressionDepth(s) - 1 <= remainingDepth);
    //prefer symbols that can fill the remaining depth
    var preferredSymbols = possibleSymbols.GroupBy(x => grammar.GetMaximumExpressionDepth(x) > remainingDepth ? 0 : 1).OrderBy(g => g.Key).First().ToList();
    switch (preferredSymbols.Count) {
      case 0:
        throw new InvalidOperationException("No symbols are available for the tree.");
      case 1:
        return preferredSymbols[0];
      default:
        var weights = preferredSymbols.Select(s => s.InitialFrequency).ToList();
        var selectedSymbol = preferredSymbols.SampleProportional(random, weights);
        return selectedSymbol;
    }
  }
}
