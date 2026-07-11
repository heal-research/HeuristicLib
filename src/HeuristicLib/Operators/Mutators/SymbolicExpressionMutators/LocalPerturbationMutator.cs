using HEAL.HeuristicLib.Genotypes.SymbolicExpressions;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces.SymbolicExpressions;

namespace HEAL.HeuristicLib.Operators.Mutators.SymbolicExpressionMutators;

public abstract record LocalPerturbationTargetSelection
{
    internal abstract IReadOnlyList<ExpressionLocation> Select(
        IReadOnlyList<ExpressionLocation> eligibleLocations,
        IRandomNumberGenerator random);
}

public sealed record OneLocalPerturbationTarget : LocalPerturbationTargetSelection
{
    internal override IReadOnlyList<ExpressionLocation> Select(IReadOnlyList<ExpressionLocation> eligibleLocations, IRandomNumberGenerator random)
    {
        return [eligibleLocations[random.NextInt(eligibleLocations.Count)]];
    }
}

public sealed record AllLocalPerturbationTargets : LocalPerturbationTargetSelection
{
    internal override IReadOnlyList<ExpressionLocation> Select(IReadOnlyList<ExpressionLocation> eligibleLocations, IRandomNumberGenerator random)
    {
        return eligibleLocations;
    }
}

public sealed record EachLocalPerturbationTarget(double Probability) : LocalPerturbationTargetSelection
{
    internal override IReadOnlyList<ExpressionLocation> Select(IReadOnlyList<ExpressionLocation> eligibleLocations, IRandomNumberGenerator random)
    {
        if (Probability is < 0.0 or > 1.0)
            throw new ArgumentOutOfRangeException(nameof(Probability));

        return eligibleLocations.Where(_ => random.NextDouble() < Probability).ToArray();
    }
}

public static class LocalPerturbationTargets
{
    public static LocalPerturbationTargetSelection One { get; } = new OneLocalPerturbationTarget();
    public static LocalPerturbationTargetSelection All { get; } = new AllLocalPerturbationTargets();
    public static LocalPerturbationTargetSelection Each(double probability) => new EachLocalPerturbationTarget(probability);
}

public sealed record LocalPerturbationMutator(LocalPerturbationTargetSelection TargetSelection)
    : SingleSolutionMutator<ExpressionTree, ExpressionTreeSearchSpace>
{
    public LocalPerturbationMutator()
        : this(LocalPerturbationTargets.One)
    {
    }

    public LocalPerturbationMutator(double eachProbability)
        : this(LocalPerturbationTargets.Each(eachProbability))
    {
    }

    public override ExpressionTree Mutate(ExpressionTree parent, IRandomNumberGenerator random, ExpressionTreeSearchSpace searchSpace)
    {
        return LocalPerturbationMutation.Mutate(parent, random, TargetSelection);
    }
}

public static class LocalPerturbationMutation
{
    public static ExpressionTree Mutate(ExpressionTree parent, IRandomNumberGenerator random, LocalPerturbationTargetSelection targetSelection)
    {
        var eligibleLocations = parent.FindLocallyPerturbableLocations().ToArray();
        if (eligibleLocations.Length == 0)
            return parent;

        var selectedLocations = targetSelection.Select(eligibleLocations, random);
        var result = parent;
        foreach (var location in selectedLocations)
        {
            var node = result.GetNode(location.InstructionIndex);
            if (node.Symbol.TryPerturb(node, random, out var perturbed))
                result = result.WithNode(location, perturbed);
        }

        return result;
    }

}
