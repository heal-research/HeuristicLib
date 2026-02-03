using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Encodings.SymbolicExpressionTree.Mutators;

public sealed class OnePointShaker : SymbolicExpressionTreeManipulator {
  #region properties
  public double ShakingFactor {
    get;
    set;
  } = 1.0;
  #endregion

  public static SymbolicExpressionTree Shake(IRandomNumberGenerator random, SymbolicExpressionTree tree, double shakingFactor) {
    tree = new SymbolicExpressionTree(tree);
    var parametricNodes = new List<SymbolicExpressionTreeNode?>();
    tree.Root.ForEachNodePostfix(n => {
      if (n!.HasLocalParameters) parametricNodes.Add(n);
    });
    if (parametricNodes.Count <= 0)
      return tree;
    parametricNodes.SampleRandom(random)!.ShakeLocalParameters(random, shakingFactor);
    return tree;
  }

  public override SymbolicExpressionTree Mutate(SymbolicExpressionTree parent, IRandomNumberGenerator random, SymbolicExpressionSearchSpace encoding) {
    return Shake(random, parent, ShakingFactor);
  }
}
