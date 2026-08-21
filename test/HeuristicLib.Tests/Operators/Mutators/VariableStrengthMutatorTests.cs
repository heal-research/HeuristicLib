using HEAL.HeuristicLib.Encodings.RealVectors;
using HEAL.HeuristicLib.Operators.Creators;
using HEAL.HeuristicLib.Problems;

namespace HEAL.HeuristicLib.Tests.Operators.Mutators;

public class VariableStrengthMutatorTests
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
        var searchSpace = new RealVectorSearchSpace(1, -10.0, 10.0);

        var offspring = instance.Mutate([new RealVector(0.0)], new SequenceRandom(0.0, 1.0), searchSpace, CreateProblem(searchSpace));

        offspring.Single().ShouldBe(new RealVector(2.0));
    }

    [Fact]
    public void EvolutionStrategy_AdaptsGaussianMutatorStrength()
    {
        var searchSpace = new RealVectorSearchSpace(1, -10.0, 10.0);
        var problem = CreateProblem(searchSpace);
        var gaussian = new GaussianMutator(1.0, 3.0);
        var algorithm = new EvolutionStrategy<RealVector, RealVectorSearchSpace, IProblem<RealVector, RealVectorSearchSpace>>
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

    private static IVariableStrengthMutatorInstance<RealVector, RealVectorSearchSpace, IProblem<RealVector, RealVectorSearchSpace>> Resolve(GaussianMutator mutator)
    {
        var registry = new ExecutionInstanceRegistry();
        return registry.Resolve<IVariableStrengthMutatorInstance<RealVector, RealVectorSearchSpace, IProblem<RealVector, RealVectorSearchSpace>>>(mutator);
    }

    private static FuncProblem<RealVector, RealVectorSearchSpace> CreateProblem(RealVectorSearchSpace searchSpace) =>
        FuncProblem.Create((RealVector candidate) => candidate[0] * candidate[0], searchSpace, SingleObjective.Minimize);

    private sealed record ZeroCreator : SingleCandidateCreator<RealVector, RealVectorSearchSpace>
    {
        public override RealVector CreateCandidate(IRandomNumberGenerator random, RealVectorSearchSpace searchSpace) => new(0.0);
    }

    private sealed class SequenceRandom(params double[] values) : IRandomNumberGenerator
    {
        private readonly Queue<double> values = new(values);

        public double NextDouble() => values.Count == 0 ? 0.0 : values.Dequeue();

        public int NextInt() => 0;

        public IRandomNumberGenerator Fork(ulong forkKey) => this;
    }
}
