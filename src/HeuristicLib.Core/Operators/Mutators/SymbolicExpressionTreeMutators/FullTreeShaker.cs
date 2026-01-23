using HEAL.HeuristicLib.Genotypes.Trees;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces.Trees;

namespace HEAL.HeuristicLib.Operators.Mutators.SymbolicExpressionTreeMutators;

public sealed class FullTreeShaker : SymbolicExpressionTreeManipulator
{
  public double ShakingFactor
  {
    get;
    set;
  } = 1.0;

  public static SymbolicExpressionTree Apply(
    IRandomNumberGenerator random, SymbolicExpressionTree tree, double shakingFactor)
  {
    var clone = new SymbolicExpressionTree(tree);
    clone.Root.ForEachNodePostfix(node => {
      if (node.HasLocalParameters) {
        node.ShakeLocalParameters(random, shakingFactor);
      }
    });
    return clone;
  }

  public override SymbolicExpressionTree Mutate(
    SymbolicExpressionTree parent, IRandomNumberGenerator random, SymbolicExpressionTreeSearchSpace searchSpace)
  {
    var t = Apply(random, parent, ShakingFactor);
    Extensions.CheckDebug(searchSpace.Contains(t), "Upps destroyed tree");
    return t;
  }
}
