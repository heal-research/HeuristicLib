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

    protected override IMutatorInstance<RealVector, RealVectorSearchSpace, IProblem<RealVector, RealVectorSearchSpace>> CreateMutatorInstance(ExecutionInstanceRegistry registry) =>
        CreateVariableStrengthMutatorInstance();

    IVariableStrengthMutatorInstance<RealVector, RealVectorSearchSpace, IProblem<RealVector, RealVectorSearchSpace>> IExecutionInstanceResolvable<IVariableStrengthMutatorInstance<RealVector, RealVectorSearchSpace, IProblem<RealVector, RealVectorSearchSpace>>>.CreateExecutionInstance(
        ExecutionInstanceRegistry instanceRegistry) => CreateVariableStrengthMutatorInstance();

    private IVariableStrengthMutatorInstance<RealVector, RealVectorSearchSpace, IProblem<RealVector, RealVectorSearchSpace>> CreateVariableStrengthMutatorInstance() =>
        new Instance(MutationRate, MutationStrength);

    private sealed class Instance(double mutationRate, double mutationStrength)
        : MutatorInstance<RealVector, RealVectorSearchSpace>, IVariableStrengthMutatorInstance<RealVector, RealVectorSearchSpace, IProblem<RealVector, RealVectorSearchSpace>>
    {
        public double CurrentMutationStrength { get; set; } = mutationStrength;

        public override IReadOnlyList<RealVector> Mutate(IReadOnlyList<RealVector> parents, IRandomNumberGenerator random, RealVectorSearchSpace searchSpace) =>
            BatchExecution.Sequential(parents, (parent, itemRandom) => GaussianMutator.Mutate(parent, itemRandom, searchSpace, mutationRate, CurrentMutationStrength), random);
    }

    public static RealVector Mutate(RealVector solution, IRandomNumberGenerator random, RealVectorSearchSpace searchSpace, double mutationRate, double mutationStrength) =>
        Mutate(solution, random, mutationRate, mutationStrength, searchSpace.Minimum, searchSpace.Maximum);

    public static RealVector Mutate(RealVector solution, IRandomNumberGenerator random, double mutationRate, double mutationStrength, RealVector minimum, RealVector maximum)
    {
        var newElements = solution.ToArray();
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
