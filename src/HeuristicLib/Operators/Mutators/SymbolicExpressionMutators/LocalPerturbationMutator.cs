using HEAL.HeuristicLib.Genotypes.SymbolicExpressions;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces.SymbolicExpressions;

namespace HEAL.HeuristicLib.Operators.Mutators.SymbolicExpressionMutators;

public abstract record LocalPerturbationTargetSelection
{
    internal abstract IReadOnlyList<ExpressionPoint> Select(
        IReadOnlyList<ExpressionPoint> eligiblePoints,
        IRandomNumberGenerator random);
}

public sealed record OneLocalPerturbationTarget : LocalPerturbationTargetSelection
{
    internal override IReadOnlyList<ExpressionPoint> Select(IReadOnlyList<ExpressionPoint> eligiblePoints, IRandomNumberGenerator random)
    {
        return [eligiblePoints[random.NextInt(eligiblePoints.Count)]];
    }
}

public sealed record AllLocalPerturbationTargets : LocalPerturbationTargetSelection
{
    internal override IReadOnlyList<ExpressionPoint> Select(IReadOnlyList<ExpressionPoint> eligiblePoints, IRandomNumberGenerator random)
    {
        return eligiblePoints;
    }
}

public sealed record EachLocalPerturbationTarget : LocalPerturbationTargetSelection
{
    public EachLocalPerturbationTarget(double probability)
    {
        Probability = probability;
    }

    /// <summary>Gets the probability of selecting each eligible point independently.</summary>
    /// <remarks>
    /// The value is used as a threshold against a random draw in <c>[0, 1)</c> and is retained as configured. A value
    /// at or below zero, negative infinity and <see cref="double.NaN"/> select no point; a value at or above one and
    /// positive infinity select every point.
    /// </remarks>
    public double Probability { get; init; }

    internal override IReadOnlyList<ExpressionPoint> Select(IReadOnlyList<ExpressionPoint> eligiblePoints, IRandomNumberGenerator random)
    {
        return eligiblePoints.Where(_ => random.NextDouble() < Probability).ToArray();
    }
}

public static class LocalPerturbationTargets
{
    public static LocalPerturbationTargetSelection One { get; } = new OneLocalPerturbationTarget();
    public static LocalPerturbationTargetSelection All { get; } = new AllLocalPerturbationTargets();
    public static LocalPerturbationTargetSelection Each(double probability) => new EachLocalPerturbationTarget(probability);
}

public sealed record LocalPerturbationMutator
    : SingleCandidateMutator<ExpressionTree, ExpressionTreeSearchSpace>
{
    /// <summary>Gets the policy that selects which eligible points are perturbed.</summary>
    public LocalPerturbationTargetSelection TargetSelection { get; init; } = LocalPerturbationTargets.One;

    public LocalPerturbationMutator()
    {
    }

    public LocalPerturbationMutator(LocalPerturbationTargetSelection targetSelection)
    {
        TargetSelection = targetSelection;
    }

    public LocalPerturbationMutator(double eachProbability)
        : this(LocalPerturbationTargets.Each(eachProbability))
    {
    }

    public override ExpressionTree MutateCandidate(ExpressionTree parent, IRandomNumberGenerator random, ExpressionTreeSearchSpace searchSpace)
    {
        return LocalPerturbationMutation.Mutate(parent, random, TargetSelection);
    }
}

public static class LocalPerturbationMutation
{
    public static ExpressionTree Mutate(ExpressionTree parent, IRandomNumberGenerator random, LocalPerturbationTargetSelection targetSelection)
    {
        var eligiblePoints = parent.FindLocallyPerturbablePoints().ToArray();
        if (eligiblePoints.Length == 0)
            return parent;

        var selectedPoints = targetSelection.Select(eligiblePoints, random);
        if (selectedPoints.Count == 0)
            return parent;

        var replacements = new List<(ExpressionPoint Point, ExpressionNode Replacement)>(selectedPoints.Count);
        foreach (var selectedPoint in selectedPoints)
        {
            if (selectedPoint.Node.Symbol.TryPerturb(selectedPoint.Node, random, out var perturbed))
                replacements.Add((selectedPoint, perturbed));
        }

        return parent.ReplaceMany(replacements);
    }
}
