using HEAL.HeuristicLib.Encodings.SymbolicExpression;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Operators.SymbolicExpression.Mutators;

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
