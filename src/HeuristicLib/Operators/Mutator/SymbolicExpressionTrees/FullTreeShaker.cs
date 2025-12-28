using HEAL.HeuristicLib.Encodings.Trees;
using HEAL.HeuristicLib.Genotypes.Trees;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Operators.Mutator.SymbolicExpressionTrees;

public sealed class FullTreeShaker : SymbolicExpressionTreeManipulator {
  public double ShakingFactor {
    get;
    set;
  } = 1.0;

  public static SymbolicExpressionTree Apply(
    IRandomNumberGenerator random, SymbolicExpressionTree tree, double shakingFactor) {
    var clone = new SymbolicExpressionTree(tree);
    clone.Root.ForEachNodePostfix(node => {
      if (node.HasLocalParameters) {
        node.ShakeLocalParameters(random, shakingFactor);
      }
    });
    return clone;
  }

  public override SymbolicExpressionTree Mutate(
    SymbolicExpressionTree parent, IRandomNumberGenerator random, SymbolicExpressionTreeEncoding encoding) {
    var t = Apply(random, parent, ShakingFactor);
    Extensions.CheckDebug(encoding.Contains(t), "Upps destroyed tree");
    return t;
  }
}
