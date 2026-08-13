using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Genotypes.Vectors;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces.Vectors;

namespace HEAL.HeuristicLib.Operators.Mutators.RealVectorMutators;

public record GaussianMutator
    : Mutator<RealVector, RealVectorSearchSpace>, IVariableStrengthMutator<RealVector, RealVectorSearchSpace, IProblem<RealVector, RealVectorSearchSpace>>
{
    public GaussianMutator(double mutationRate, double mutationStrength)
    {
        MutationRate = mutationRate;
        MutationStrength = mutationStrength;
    }

    public double MutationRate { get; init; }

    public double MutationStrength { get; init; }

    public override IMutatorInstance<RealVector, RealVectorSearchSpace, IProblem<RealVector, RealVectorSearchSpace>> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) =>
        CreateVariableStrengthMutatorInstance();

    IVariableStrengthMutatorInstance<RealVector, RealVectorSearchSpace, IProblem<RealVector, RealVectorSearchSpace>> IExecutionInstanceResolvable<IVariableStrengthMutatorInstance<RealVector, RealVectorSearchSpace, IProblem<RealVector, RealVectorSearchSpace>>>.CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) =>
        CreateVariableStrengthMutatorInstance();

    private IVariableStrengthMutatorInstance<RealVector, RealVectorSearchSpace, IProblem<RealVector, RealVectorSearchSpace>> CreateVariableStrengthMutatorInstance() =>
        new Instance(MutationRate, MutationStrength);

    private sealed class Instance(double mutationRate, double mutationStrength)
        : MutatorInstance<RealVector, RealVectorSearchSpace>, IVariableStrengthMutatorInstance<RealVector, RealVectorSearchSpace, IProblem<RealVector, RealVectorSearchSpace>>
    {
        public double CurrentMutationStrength { get; set; } = mutationStrength;

        public override IReadOnlyList<RealVector> Mutate(IReadOnlyList<RealVector> parents, IRandomNumberGenerator random, RealVectorSearchSpace searchSpace) =>
            BatchExecution.Sequential(
                parents,
                (searchSpace, mutationRate, mutationStrength: CurrentMutationStrength),
                static (parent, itemRandom, state) => GaussianMutator.Mutate(parent, itemRandom, state.searchSpace, state.mutationRate, state.mutationStrength),
                random);
    }

    public static RealVector Mutate(RealVector candidate, IRandomNumberGenerator random, RealVectorSearchSpace searchSpace, double mutationRate, double mutationStrength)
    {
        if (candidate.Count != searchSpace.Length)
            throw new ArgumentException("Candidate length must match the search space length.", nameof(candidate));

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
