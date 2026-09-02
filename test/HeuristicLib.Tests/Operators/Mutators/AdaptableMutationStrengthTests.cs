using HEAL.HeuristicLib.Encodings.RealVectors;
using HEAL.HeuristicLib.Operators.Creators;
using HEAL.HeuristicLib.Problems;

namespace HEAL.HeuristicLib.Tests.Operators.Mutators;

public class AdaptableMutationStrengthTests
{
    [Fact]
    public void GaussianMutator_KeepsConfiguredStrengthOnConfigurationAndCurrentStrengthOnInstance()
    {
        var mutator = new GaussianMutator(1.0, 2.0);
        var instance = Resolve(mutator);

        instance.CurrentMutationStrength = 4.0;

        mutator.MutationStrength.ShouldBe(2.0);
        instance.CurrentMutationStrength.ShouldBe(4.0);
    }

    [Fact]
    public void GaussianMutator_CreatesIndependentStrengthsForIndependentRuns()
    {
        var mutator = new GaussianMutator(1.0, 2.0);
        var first = Resolve(mutator);
        var second = Resolve(mutator);

        first.CurrentMutationStrength = 4.0;

        first.CurrentMutationStrength.ShouldBe(4.0);
        second.CurrentMutationStrength.ShouldBe(2.0);
    }

    [Fact]
    public void GaussianMutator_UsesCurrentInstanceStrength()
    {
        var mutator = new GaussianMutator(1.0, 2.0);
        var instance = Resolve(mutator);
        instance.CurrentMutationStrength = 4.0;
        var searchSpace = new BoundedRealVectorSearchSpace(1, -10.0, 10.0);

        var offspring = instance.Mutate([new RealVector(0.0)], new SequenceRandom(0.0, 1.0), searchSpace, CreateProblem(searchSpace));

        offspring.Single().ShouldBe(new RealVector(2.0));
    }

    [Fact]
    public void EvolutionStrategy_AdaptsGaussianMutatorStrength()
    {
        var searchSpace = new BoundedRealVectorSearchSpace(1, -10.0, 10.0);
        var problem = CreateProblem(searchSpace);
        var gaussian = new GaussianMutator(1.0, 3.0);
        var algorithm = new EvolutionStrategy<RealVector, BoundedRealVectorSearchSpace, IProblem<RealVector, BoundedRealVectorSearchSpace>>
        {
            PopulationSize = 1,
            NumberOfChildren = 1,
            Strategy = EvolutionStrategyType.Comma,
            Creator = new ZeroCreator(),
            Mutator = gaussian,
            Crossover = null,
            Selector = BestSelector.For(problem),
            MaximumGenerations = 3
        };

        var states = algorithm.Stream(problem, new SequenceRandom(), ct: TestContext.Current.CancellationToken).ToList();

        states.Select(state => state.Population.EvaluatedCandidates.Single().Candidate[0]).ShouldBe([0.0, -1.5, -2.5]);
        gaussian.MutationStrength.ShouldBe(3.0);
    }

    /// <summary>
    /// Resolved as an ordinary mutator and then tested for the adaptable strength, which is exactly what
    /// <c>EvolutionStrategy</c> does: the configuration says nothing about adaptation, so the instance is where it is
    /// discovered.
    /// </summary>
    private static IAdaptableMutationStrengthInstance<RealVector, BoundedRealVectorSearchSpace, IProblem<RealVector, BoundedRealVectorSearchSpace>> Resolve(GaussianMutator mutator) =>
        new ExecutionInstanceRegistry()
            .For<RealVector, BoundedRealVectorSearchSpace, IProblem<RealVector, BoundedRealVectorSearchSpace>>()
            .Resolve(mutator)
            .ShouldBeAssignableTo<IAdaptableMutationStrengthInstance<RealVector, BoundedRealVectorSearchSpace, IProblem<RealVector, BoundedRealVectorSearchSpace>>>()!;

    private static FuncProblem<RealVector, BoundedRealVectorSearchSpace> CreateProblem(BoundedRealVectorSearchSpace searchSpace) =>
        FuncProblem.Create((RealVector candidate) => candidate[0] * candidate[0], searchSpace, SingleObjective.Minimize);

    private sealed record ZeroCreator : SingleCandidateCreator<RealVector, BoundedRealVectorSearchSpace>
    {
        public override RealVector CreateCandidate(IRandomNumberGenerator random, BoundedRealVectorSearchSpace searchSpace) => new(0.0);
    }

    private sealed class SequenceRandom(params double[] values) : IRandomNumberGenerator
    {
        private readonly Queue<double> values = new(values);

        public double NextDouble() => values.Count == 0 ? 0.0 : values.Dequeue();

        public int NextInt() => 0;

        public IRandomNumberGenerator Fork(ulong forkKey) => this;
    }
}
