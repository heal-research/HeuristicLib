using HEAL.HeuristicLib.Encodings.RealVectors;
using HEAL.HeuristicLib.Operators.Creators;
using HEAL.HeuristicLib.Operators.Mutators;
using HEAL.HeuristicLib.Problems;

namespace HEAL.HeuristicLib.Tests.Operators.Mutators;

public class AdaptableMutationStrengthTests
{
    [Fact]
    public void GaussianMutator_KeepsConfiguredStrengthOnConfigurationAndCurrentStrengthOnExecution()
    {
        var mutator = new GaussianMutator(1.0, 2.0);
        var execution = Resolve(mutator);

        execution.CurrentMutationStrength = 4.0;

        mutator.MutationStrength.ShouldBe(2.0);
        execution.CurrentMutationStrength.ShouldBe(4.0);
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
    public void GaussianMutator_UsesCurrentExecutionStrength()
    {
        var mutator = new GaussianMutator(1.0, 2.0);
        var searchSpace = new BoundedRealVectorSearchSpace(1, -10.0, 10.0);
        var execution = ResolutionScope.Create().Resolve<RealVector, BoundedRealVectorSearchSpace, IProblem<RealVector, BoundedRealVectorSearchSpace>>(mutator);
        execution.ShouldBeAssignableTo<IMutationStrengthControl>().CurrentMutationStrength = 4.0;

        var offspring = execution.Mutate([new RealVector(0.0)], new SequenceRandom(0.0, 1.0), searchSpace, CreateProblem(searchSpace));

        offspring.Single().ShouldBe(new RealVector(2.0));
    }

    [Fact]
    public void GaussianMutator_RebindingPreservesStrengthAndObservesOnlyContextualCalls()
    {
        var mutator = new GaussianMutator(1.0, 2.0);
        var searchSpace = new BoundedRealVectorSearchSpace(1, -10.0, 10.0);
        var problem = CreateProblem(searchSpace);
        var parent = ResolutionScope.Create();
        var outer = parent.For<RealVector, BoundedRealVectorSearchSpace, IProblem<RealVector, BoundedRealVectorSearchSpace>>().Resolve(mutator, out IMutationStrengthControl? strength);
        strength.ShouldNotBeNull();
        strength.CurrentMutationStrength = 4.0;
        var observedCalls = new CountAccumulator();
        var child = parent.CreateChildScope(builder => builder.Wrap<IMutator<RealVector>>(mutator, original => original.CountCalls(observedCalls)));
        var typed = child.For<RealVector, BoundedRealVectorSearchSpace, IProblem<RealVector, BoundedRealVectorSearchSpace>>();
        var inner = typed.Resolve(mutator, out IMutationStrengthControl? innerStrength);
        innerStrength.ShouldBeSameAs(strength);
        inner.ShouldNotBeAssignableTo<IMutationStrengthControl>();
        typed.Resolve(mutator, out IMutationStrengthControl? repeatedStrength).ShouldBeSameAs(inner);
        repeatedStrength.ShouldBeSameAs(strength);

        inner.Mutate([new RealVector(0.0)], new SequenceRandom(0.0, 1.0), searchSpace, problem).ShouldBe([new RealVector(2.0)]);
        observedCalls.CurrentCount.ShouldBe(1);
        strength.CurrentMutationStrength = 6.0;
        outer.Mutate([new RealVector(0.0)], new SequenceRandom(0.0, 1.0), searchSpace, problem).ShouldBe([new RealVector(3.0)]);
        observedCalls.CurrentCount.ShouldBe(1);
        inner.Mutate([new RealVector(0.0)], new SequenceRandom(0.0, 1.0), searchSpace, problem).ShouldBe([new RealVector(3.0)]);
        observedCalls.CurrentCount.ShouldBe(2);
        Resolve(mutator).CurrentMutationStrength.ShouldBe(2.0);
        mutator.MutationStrength.ShouldBe(2.0);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void EvolutionStrategy_AdaptsGaussianMutatorStrength(bool observed)
    {
        var searchSpace = new BoundedRealVectorSearchSpace(1, -10.0, 10.0);
        var problem = CreateProblem(searchSpace);
        var gaussian = new GaussianMutator(1.0, 3.0);
        var algorithm = CreateAlgorithm(gaussian, problem);
        var calls = new CountAccumulator();
        var scope = observed ? ResolutionScope.Create(builder => builder.Wrap<IMutator<RealVector>>(gaussian, source => source.CountCalls(calls))) : ResolutionScope.Create();
        var execution = scope.Resolve<RealVector, BoundedRealVectorSearchSpace, FuncProblem<RealVector, BoundedRealVectorSearchSpace>, PopulationState<RealVector>>(algorithm);
        var states = execution.Stream(problem, new SequenceRandom(), ct: TestContext.Current.CancellationToken).ToList();

        states.Select(state => state.Population.EvaluatedCandidates.Single().Candidate[0]).ShouldBe([0.0, -1.5, -2.5]);
        gaussian.MutationStrength.ShouldBe(3.0);
        calls.CurrentCount.ShouldBe(observed ? 2 : 0);
    }

    [Fact]
    public async Task EvolutionStrategy_RebindingUsesItsPinnedStrengthWithoutAdaptingAChildLocalMutator()
    {
        var searchSpace = new BoundedRealVectorSearchSpace(1, -10.0, 10.0);
        var problem = CreateProblem(searchSpace);
        var gaussian = new GaussianMutator(1.0, 3.0);
        var algorithm = CreateAlgorithm(gaussian, problem);
        var calls = new CountAccumulator();
        var root = ResolutionScope.Create();
        var child = root.CreateChildScope(builder => builder.Wrap<IMutator<RealVector>>(gaussian, source => source.CountCalls(calls)));
        child.For<RealVector, BoundedRealVectorSearchSpace, FuncProblem<RealVector, BoundedRealVectorSearchSpace>>().Resolve(gaussian, out IMutationStrengthControl? localStrength);
        localStrength.ShouldNotBeNull();
        localStrength.CurrentMutationStrength = 9.0;
        var original = root.Resolve<RealVector, BoundedRealVectorSearchSpace, FuncProblem<RealVector, BoundedRealVectorSearchSpace>, PopulationState<RealVector>>(algorithm);
        var ancestorStrength = root.Resolve<RealVector, BoundedRealVectorSearchSpace, FuncProblem<RealVector, BoundedRealVectorSearchSpace>>(gaussian).ShouldBeAssignableTo<IMutationStrengthControl>();
        await using var paused = original.RunStreamingAsync(problem, new SequenceRandom(), ct: TestContext.Current.CancellationToken).GetAsyncEnumerator(TestContext.Current.CancellationToken);
        (await paused.MoveNextAsync()).ShouldBeTrue();
        (await paused.MoveNextAsync()).ShouldBeTrue();
        paused.Current.Population.Single().Candidate.ShouldBe(new RealVector(-1.5));
        ancestorStrength.CurrentMutationStrength.ShouldBe(2.0);

        var rebound = child.Resolve<RealVector, BoundedRealVectorSearchSpace, FuncProblem<RealVector, BoundedRealVectorSearchSpace>, PopulationState<RealVector>>(algorithm);
        var observed = rebound.Stream(problem, new SequenceRandom(), ct: TestContext.Current.CancellationToken).ToList();
        observed[1].Population.Single().Candidate.ShouldBe(new RealVector(-1.0));
        calls.CurrentCount.ShouldBe(2);
        localStrength.CurrentMutationStrength.ShouldBe(9.0);
        ancestorStrength.CurrentMutationStrength.ShouldBe(3.0 / Math.Pow(1.5, 3), 1e-12);
        (await paused.MoveNextAsync()).ShouldBeTrue();
        calls.CurrentCount.ShouldBe(2);
        ancestorStrength.CurrentMutationStrength.ShouldBe(3.0 / Math.Pow(1.5, 4), 1e-12);
        localStrength.CurrentMutationStrength.ShouldBe(9.0);
    }

    [Fact]
    public void EvolutionStrategy_ConfiguredWrapperDoesNotInheritItsChildStrengthControl()
    {
        var searchSpace = new BoundedRealVectorSearchSpace(1, -10.0, 10.0);
        var problem = CreateProblem(searchSpace);
        var gaussian = new GaussianMutator(1.0, 3.0);
        var calls = new CountAccumulator();
        var mutator = gaussian.CountCalls(calls);
        var algorithm = CreateAlgorithm(mutator, problem);
        var scope = ResolutionScope.Create();
        var typed = scope.For<RealVector, BoundedRealVectorSearchSpace, FuncProblem<RealVector, BoundedRealVectorSearchSpace>, PopulationState<RealVector>>();
        typed.Resolve(mutator, out IMutationStrengthControl? strength);
        strength.ShouldBeNull();

        var states = typed.Resolve(algorithm).Stream(problem, new SequenceRandom(), ct: TestContext.Current.CancellationToken).ToList();

        states.Select(state => state.Population.Single().Candidate[0]).ShouldBe([0.0, -1.5, -3.0]);
        calls.CurrentCount.ShouldBe(2);
    }

    private static EvolutionStrategy<RealVector> CreateAlgorithm(IMutator<RealVector> mutator, FuncProblem<RealVector, BoundedRealVectorSearchSpace> problem) => new()
    {
        PopulationSize = 1,
        NumberOfChildren = 1,
        Strategy = EvolutionStrategyType.Comma,
        Creator = new ZeroCreator(),
        Mutator = mutator,
        Crossover = null,
        Selector = BestSelector.For(problem),
        MaximumGenerations = 3
    };

    private static IMutationStrengthControl Resolve(GaussianMutator mutator) =>
        ResolutionScope.Create()
            .For<RealVector, BoundedRealVectorSearchSpace, IProblem<RealVector, BoundedRealVectorSearchSpace>>()
            .Resolve(mutator)
            .ShouldBeAssignableTo<IMutationStrengthControl>();

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
