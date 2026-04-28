using HEAL.HeuristicLib.Genotypes.Trees;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces.Trees;

namespace HEAL.HeuristicLib.Operators.Mutators.SymbolicExpressionTreeMutators;

public sealed record OnePointShaker : SymbolicExpressionTreeManipulator
{

  #region properties
  public double ShakingFactor
  {
    get;
    set;
  } = 1.0;
  #endregion

  public override SymbolicExpressionTree Mutate(SymbolicExpressionTree parent, IRandomNumberGenerator random, SymbolicExpressionTreeSearchSpace searchSpace) => Mutate(random, parent, ShakingFactor);

  public static SymbolicExpressionTree Mutate(IRandomNumberGenerator random, SymbolicExpressionTree tree, double shakingFactor)
  {
    tree = new SymbolicExpressionTree(tree);
    var parametricNodes = new List<SymbolicExpressionTreeNode?>();
    tree.Root.ForEachNodePostfix(n => {
      if (n!.HasLocalParameters) {
        parametricNodes.Add(n);
      }
    });
    if (parametricNodes.Count <= 0) {
      return tree;
    }

    parametricNodes.SampleRandom(random)!.ShakeLocalParameters(random, shakingFactor);
    return tree;
  }
}
