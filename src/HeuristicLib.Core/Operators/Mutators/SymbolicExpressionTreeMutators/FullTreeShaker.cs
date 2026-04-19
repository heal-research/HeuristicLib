using System.Diagnostics;
using HEAL.HeuristicLib.Genotypes.Trees;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces.Trees;

namespace HEAL.HeuristicLib.Operators.Mutators.SymbolicExpressionTreeMutators;

public sealed record FullTreeShaker : SymbolicExpressionTreeManipulator
{
  public double ShakingFactor
  {
    get;
    set;
  } = 1.0;

  public override SymbolicExpressionTree Mutate(
    SymbolicExpressionTree parent, IRandomNumberGenerator random, SymbolicExpressionTreeSearchSpace searchSpace)
  {
    var t = Mutate(random, parent, ShakingFactor);
    Debug.Assert(searchSpace.Contains(t), "Upps destroyed tree");
    return t;
  }

  public static SymbolicExpressionTree Mutate(
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
}
