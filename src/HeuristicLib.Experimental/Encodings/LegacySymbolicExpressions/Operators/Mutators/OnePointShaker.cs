using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Encodings.LegacySymbolicExpressions;

public sealed record OnePointShaker : SymbolicExpressionTreeManipulator
{
    public double ShakingFactor { get; init; } = 1.0;

    public static Parameter<OnePointShaker, double> ShakingFactorParameter { get; } =
        new(nameof(ShakingFactor), static shaker => shaker.ShakingFactor, static (shaker, value) => shaker with { ShakingFactor = value });

    public new static IReadOnlyList<IParameter> Parameters { get; } = [ShakingFactorParameter];

    public override IReadOnlyList<IParameter> GetParameters() => [.. base.GetParameters(), .. Parameters];

    public override SymbolicExpressionTree Mutate(SymbolicExpressionTree parent, IRandomNumberGenerator random, SymbolicExpressionTreeSearchSpace searchSpace) => Mutate(random, parent, ShakingFactor);

    public static SymbolicExpressionTree Mutate(IRandomNumberGenerator random, SymbolicExpressionTree tree, double shakingFactor)
    {
        tree = new SymbolicExpressionTree(tree);
        var parametricNodes = new List<SymbolicExpressionTreeNode?>();
        tree.Root.ForEachNodePostfix(n =>
        {
            if (n.HasLocalParameters)
            {
                parametricNodes.Add(n);
            }
        });
        if (parametricNodes.Count <= 0)
        {
            return tree;
        }

        parametricNodes.SampleRandom(random)!.ShakeLocalParameters(random, shakingFactor);
        return tree;
    }
}
