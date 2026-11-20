using HEAL.HeuristicLib.Encodings.Vectors;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Mutators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Encodings.RealVectors;

public record GaussianMutator
    : Mutator<RealVector, BoundedRealVectorSearchSpace>, IOperatorContract<RealVector>
{
    /// <summary>
    /// The result is clamped to the search space bounds, so length and bounds both survive at any mutation rate or
    /// strength.
    /// </summary>
    public bool? Ensures(ICandidateInvariant<RealVector> invariant) => invariant switch
    {
        RealVectorLength or RealVectorBounds => true,
        _ => null
    };

    public GaussianMutator(double mutationRate, double mutationStrength)
    {
        MutationRate = mutationRate;
        MutationStrength = mutationStrength;
    }

    public double MutationRate { get; init; }

    public double MutationStrength { get; init; }

    public IConfigurationNode<IAdaptation>? MutationStrengthAdaptation { get; init; }

    public static Parameter<GaussianMutator, double> MutationRateParameter { get; } =
        new(nameof(MutationRate), static mutator => mutator.MutationRate, static (mutator, value) => mutator with { MutationRate = value });

    public static Parameter<GaussianMutator, double> MutationStrengthParameter { get; } =
        new(nameof(MutationStrength), static mutator => mutator.MutationStrength, static (mutator, value) => mutator with { MutationStrength = value });

    public static Parameter<GaussianMutator, IConfigurationNode<IAdaptation>?> MutationStrengthAdaptationParameter { get; } =
        new(nameof(MutationStrengthAdaptation), static mutator => mutator.MutationStrengthAdaptation, static (mutator, value) => mutator with { MutationStrengthAdaptation = value });

    public static IReadOnlyList<IParameter> Parameters { get; } = [MutationRateParameter, MutationStrengthParameter, MutationStrengthAdaptationParameter];

    public override IReadOnlyList<IParameter> GetParameters() => [.. base.GetParameters(), .. Parameters];

    protected override IMutationExecutor<RealVector> CreateExecutor<TRunSearchState>(ResolutionScope<RealVector, BoundedRealVectorSearchSpace, IProblem<RealVector, BoundedRealVectorSearchSpace>, TRunSearchState> scope) =>
        new Executor(MutationRate, MutationStrength, scope.ResolveOptional(MutationStrengthAdaptation), searchSpace: scope.SearchSpace);

    private sealed class Executor(double mutationRate, double mutationStrength, IAdaptation? adaptation, BoundedRealVectorSearchSpace searchSpace)
        : MutationExecutor<RealVector>
    {
        public override IReadOnlyList<RealVector> Mutate(IReadOnlyList<RealVector> parents, IRandomNumberGenerator random) =>
            BatchExecution.Sequential(
                parents,
                (searchSpace, mutationRate, mutationStrength: adaptation?.Apply(mutationStrength) ?? mutationStrength),
                static (parent, itemRandom, parameters) => GaussianMutator.Mutate(parent, itemRandom, parameters.searchSpace, parameters.mutationRate, parameters.mutationStrength),
                random);
    }

    public static RealVector Mutate(RealVector candidate, IRandomNumberGenerator random, BoundedRealVectorSearchSpace searchSpace, double mutationRate, double mutationStrength)
    {
        return Mutate(candidate, random, searchSpace.Minimum, searchSpace.Maximum, mutationRate, mutationStrength);
    }

    public static RealVector Mutate(RealVector candidate, IRandomNumberGenerator random, RealVector minimum, RealVector maximum, double mutationRate, double mutationStrength)
    {
        if (!Vector.AreBroadcastableTo(candidate.Count, minimum, maximum))
            throw new ArgumentException("Minimum and maximum must each have length 1 or match the candidate length.");

        if (candidate.Count == 0)
            return candidate;

        var newElements = candidate.ToArray();
        for (var i = 0; i < newElements.Length; i++)
        {
            if (random.NextDouble() < mutationRate)
            {
                newElements[i] += mutationStrength * (random.NextDouble() - 0.5);
            }
        }

        return RealVector.Clamp(RealVector.FromOwnedArray(newElements), minimum, maximum);
    }
}
