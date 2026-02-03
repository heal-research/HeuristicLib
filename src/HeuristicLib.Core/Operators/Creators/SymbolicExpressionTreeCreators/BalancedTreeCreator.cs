using HEAL.HeuristicLib.Genotypes.Trees;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces.Trees;
using HEAL.HeuristicLib.SearchSpaces.Trees.SymbolicExpressionTree.Grammars;
using HEAL.HeuristicLib.SearchSpaces.Trees.SymbolicExpressionTree.Symbols;

namespace HEAL.HeuristicLib.Operators.Creators.SymbolicExpressionTreeCreators;

public class BalancedTreeCreator : SymbolicExpressionTreeCreator
{
  public double IrregularityBias
  {
    get;
    set;
  }

  public static SymbolicExpressionTree Create(IRandomNumberGenerator random, SymbolicExpressionSearchSpace encoding, double irregularityBias) => CreateExpressionTree(random, encoding, irregularityBias);

  public static SymbolicExpressionTree CreateExpressionTree(IRandomNumberGenerator random, SymbolicExpressionSearchSpace encoding, double irregularityBias = 1)
  {
    // even lengths cannot be achieved without symbols of odd arity
    // therefore we randomly pick a neighbouring odd length value
    var tree = encoding.Grammar.MakeStump(random);// create a stump consisting of just a ProgramRootSymbol and a StartSymbol

    var targetLength = random.Integer(encoding.Grammar.GetMinimumExpressionLength(tree.Root[0].Symbol), encoding.TreeLength);
    CreateExpression(random, tree.Root[0], encoding, targetLength - tree.Length, encoding.TreeDepth - tree.Depth, irregularityBias);// -2 because the stump has length 2 and depth 2

    return tree;
  }

  private static SymbolicExpressionTreeNode SampleNode(IRandomNumberGenerator random, ISymbolicExpressionGrammar grammar, IEnumerable<Symbol> allowedSymbols, int minChildArity, int maxChildArity)
  {
    var candidates = new List<Symbol>();
    var weights = new List<double>();

    foreach (var s in allowedSymbols) {
      var minSubtreeCount = grammar.GetMinimumSubtreeCount(s);
      var maxSubtreeCount = grammar.GetMaximumSubtreeCount(s);

      if (maxChildArity < minSubtreeCount || minChildArity > maxSubtreeCount) {
        continue;
      }

      candidates.Add(s);
      weights.Add(s.InitialFrequency);
    }

    var symbol = candidates.Count == 1 ? candidates[0] : candidates.SampleProportional(random, 1, weights).First();
    var node = symbol.CreateTreeNode();
    if (node.HasLocalParameters) {
      node.ResetLocalParameters(random);
    }

    return node;
  }

  public static void CreateExpression(IRandomNumberGenerator random, SymbolicExpressionTreeNode root, SymbolicExpressionSearchSpace encoding, int targetLength, int maxDepth, double irregularityBias)
  {
    var grammar = encoding.Grammar;
    var minSubtreeCount = grammar.GetMinimumSubtreeCount(root.Symbol);
    var maxSubtreeCount = grammar.GetMaximumSubtreeCount(root.Symbol);
    var arity = random.Integer(minSubtreeCount, maxSubtreeCount, true);
    var openSlots = arity;

    var allowedSymbols = grammar.AllowedSymbols.Where(x => x is not (ProgramRootSymbol or GroupSymbol or DefunSymbol or StartSymbol)).ToList();
    var hasUnarySymbols = allowedSymbols.Any(x => grammar.GetMinimumSubtreeCount(x) <= 1 && grammar.GetMaximumSubtreeCount(x) >= 1);

    if (!hasUnarySymbols && targetLength % 2 == 0) {
      // without functions of arity 1 some target lengths cannot be reached
      targetLength = random.Random() < 0.5 ? targetLength - 1 : targetLength + 1;
    }

    var tuples = new List<NodeInfo>(targetLength) { new(root, 0, arity) };

    // we use tuples.Count instead of targetLength in the if condition 
    // because depth limits may prevent reaching the target length 
    for (var i = 0; i < tuples.Count; ++i) {
      var (node, depth, arity1) = tuples[i];

      for (var childIndex = 0; childIndex < arity1; childIndex++) {
        // min and max arity here refer to the required arity limits for the child node
        var minChildArity = 0;
        var maxChildArity = 0;

        var allowedChildSymbols = allowedSymbols.Where(x => grammar.IsAllowedChildSymbol(node.Symbol, x, childIndex)).ToList();

        // if we are reaching max depth we have to fill the slot with a leaf node (max arity will be zero)
        // otherwise, find the maximum value from the grammar which does not exceed the length limit 
        if (depth < maxDepth - 1 && openSlots < targetLength) {
          // we don't want to allow sampling a leaf symbol if it prevents us from reaching the target length
          // this should be allowed only when we have enough open expansion points (more than one)
          // the random check against the irregularity bias helps to increase shape variability when the conditions are met
          var minAllowedArity = allowedChildSymbols.Min(grammar.GetMaximumSubtreeCount);
          if (minAllowedArity == 0 && (openSlots - tuples.Count <= 1 || random.Random() > irregularityBias)) {
            minAllowedArity = 1;
          }

          // finally adjust min and max arity according to the expansion limits
          var maxAllowedArity = allowedChildSymbols.Max(grammar.GetMaximumSubtreeCount);
          maxChildArity = Math.Min(maxAllowedArity, targetLength - openSlots);
          minChildArity = Math.Min(minAllowedArity, maxChildArity);
        }

        // sample a random child with the arity limits
        var child = SampleNode(random, grammar, allowedChildSymbols, minChildArity, maxChildArity);

        // get actual child arity limits
        minChildArity = Math.Max(minChildArity, grammar.GetMinimumSubtreeCount(child.Symbol));
        maxChildArity = Math.Min(maxChildArity, grammar.GetMaximumSubtreeCount(child.Symbol));
        minChildArity = Math.Min(minChildArity, maxChildArity);

        // pick a random arity for the new child node
        var childArity = random.Integer(minChildArity, maxChildArity, true);
        var childDepth = depth + 1;
        node.AddSubtree(child);
        tuples.Add(new NodeInfo(child, childDepth, childArity));
        openSlots += childArity;
      }
    }
  }

  public override SymbolicExpressionTree Create(IRandomNumberGenerator random, SymbolicExpressionSearchSpace encoding)
    => Create(random, encoding, IrregularityBias);

  #region helpers

  private sealed record NodeInfo(SymbolicExpressionTreeNode Node, int Depth, int Arity);

  public void CreateExpression(IRandomNumberGenerator random, SymbolicExpressionTreeNode seedNode, SymbolicExpressionSearchSpace encoding, int maxTreeLength, int maxTreeDepth)
    => CreateExpression(random, seedNode, encoding, maxTreeLength, maxTreeDepth, IrregularityBias);

  #endregion

}
