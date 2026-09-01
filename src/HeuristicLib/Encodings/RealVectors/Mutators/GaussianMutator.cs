using HEAL.HeuristicLib.Encodings.Vectors;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Mutators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;

using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Encodings.RealVectors;

public record GaussianMutator
    : Mutator<RealVector, BoundedRealVectorSearchSpace>, IVariableStrengthMutator<RealVector, BoundedRealVectorSearchSpace, IProblem<RealVector, BoundedRealVectorSearchSpace>>, IInvariantContract<RealVector>
{
    /// <summary>
    /// The result is clamped to the search space bounds, so length and bounds both survive at any mutation rate or
    /// strength.
    /// </summary>
    public bool? Ensures(ISearchInvariant<RealVector> invariant) => invariant switch
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

    public override IMutatorInstance<RealVector, BoundedRealVectorSearchSpace, IProblem<RealVector, BoundedRealVectorSearchSpace>> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) =>
        CreateVariableStrengthMutatorInstance();

    IVariableStrengthMutatorInstance<RealVector, BoundedRealVectorSearchSpace, IProblem<RealVector, BoundedRealVectorSearchSpace>> IExecutionInstanceResolvable<IVariableStrengthMutatorInstance<RealVector, BoundedRealVectorSearchSpace, IProblem<RealVector, BoundedRealVectorSearchSpace>>>.CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) =>
        CreateVariableStrengthMutatorInstance();

    private IVariableStrengthMutatorInstance<RealVector, BoundedRealVectorSearchSpace, IProblem<RealVector, BoundedRealVectorSearchSpace>> CreateVariableStrengthMutatorInstance() =>
        new Instance(MutationRate, MutationStrength);

    private sealed class Instance(double mutationRate, double mutationStrength)
        : MutatorInstance<RealVector, BoundedRealVectorSearchSpace>, IVariableStrengthMutatorInstance<RealVector, BoundedRealVectorSearchSpace, IProblem<RealVector, BoundedRealVectorSearchSpace>>
    {
        public double CurrentMutationStrength { get; set; } = mutationStrength;

        public override IReadOnlyList<RealVector> Mutate(IReadOnlyList<RealVector> parents, IRandomNumberGenerator random, BoundedRealVectorSearchSpace searchSpace) =>
            BatchExecution.Sequential(
                parents,
                (searchSpace, mutationRate, mutationStrength: CurrentMutationStrength),
                static (parent, itemRandom, state) => GaussianMutator.Mutate(parent, itemRandom, state.searchSpace, state.mutationRate, state.mutationStrength),
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
