using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Encodings.LegacySymbolicExpressions;

public sealed record FullTreeShaker : SymbolicExpressionTreeManipulator
{
    public double ShakingFactor { get; init; } = 1.0;

    public override SymbolicExpressionTree Mutate(SymbolicExpressionTree parent, IRandomNumberGenerator random, SymbolicExpressionTreeSearchSpace searchSpace) =>
        Mutate(random, parent, ShakingFactor);

    public static SymbolicExpressionTree Mutate(IRandomNumberGenerator random, SymbolicExpressionTree tree, double shakingFactor)
    {
        var clone = new SymbolicExpressionTree(tree);
        clone.Root.ForEachNodePostfix(node =>
        {
            if (node.HasLocalParameters)
            {
                node.ShakeLocalParameters(random, shakingFactor);
            }
        });
        return clone;
    }
}
