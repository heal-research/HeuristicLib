using HEAL.HeuristicLib.Genotypes.Trees;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces.Trees;
using HEAL.HeuristicLib.SearchSpaces.Trees.SymbolicExpressionTree.Grammars;

namespace HEAL.HeuristicLib.Operators.Creators.SymbolicExpressionTreeCreators;

public class RampedHalfAndHalfTreeCreator : SymbolicExpressionTreeCreator
{
  /// <summary>
  ///   GetEvaluator a symbolic expression tree using 'RampedHalfAndHalf' strategy.
  ///   Half the trees are created with the 'Grow' method, and the other half are created with the 'Full' method.
  /// </summary>
  /// <param name="random">Random generator</param>
  /// <param name="searchSpace"></param>
  /// <returns></returns>
  public static SymbolicExpressionTree CreateTree(IRandomNumberGenerator random, SymbolicExpressionTreeSearchSpace searchSpace)
  {
    var tree = searchSpace.Grammar.MakeStump(random);
    var startNode = tree.Root[0];

    if (random.NextDouble() < 0.5) {
      GrowTreeCreator.Create(random, startNode, searchSpace.TreeDepth - 2, searchSpace);
    } else {
      FullTreeCreator.Create(random, startNode, searchSpace, searchSpace.TreeDepth - 2);
    }

    return tree;
  }

  public override SymbolicExpressionTree Create(IRandomNumberGenerator random, SymbolicExpressionTreeSearchSpace searchSpace) => CreateTree(random, searchSpace);
}
