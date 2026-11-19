using HEAL.HeuristicLib.Encodings.RealVectors;
using HEAL.HeuristicLib.Inspection;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Creators;
using HEAL.HeuristicLib.Operators.Mutators;
using HEAL.HeuristicLib.Problems;

namespace HEAL.HeuristicLib.Tests.Operators.Mutators;

public class AdaptableMutationStrengthTests
{
    [Fact]
    public void GaussianMutator_KeepsConfiguredBaseAndIndependentRuntimeAdaptations()
    {
        var adaptation = new ScaleAdaptation();
        var mutator = new GaussianMutator(1.0, 2.0) { MutationStrengthAdaptation = adaptation };
        var problem = CreateProblem(new BoundedRealVectorSearchSpace(1, -10.0, 10.0));
        var first = ResolutionScope.Create();
        var second = ResolutionScope.Create();
        var outer = first.For<RealVector, BoundedRealVectorSearchSpace, FuncProblem<RealVector, BoundedRealVectorSearchSpace>>(problem).Resolve(mutator);
        var other = second.For<RealVector, BoundedRealVectorSearchSpace, FuncProblem<RealVector, BoundedRealVectorSearchSpace>>(problem).Resolve(mutator);
        first.Resolve(adaptation).Factor *= 2.0;

        outer.Mutate([new RealVector(0.0)], new SequenceRandom(0.0, 1.0)).ShouldBe([new RealVector(2.0)]);
        other.Mutate([new RealVector(0.0)], new SequenceRandom(0.0, 1.0)).ShouldBe([new RealVector(1.0)]);
        var child = first.CreateChildScope();
        var calls = new Counter();
        var inner = child.For<RealVector, BoundedRealVectorSearchSpace, FuncProblem<RealVector, BoundedRealVectorSearchSpace>>(problem).Resolve(mutator.Counted(calls));
        inner.Mutate([new RealVector(0.0)], new SequenceRandom(0.0, 1.0)).ShouldBe([new RealVector(1.0)]);
        first.Results.Result(calls).CurrentCount.ShouldBe(1);
        mutator.MutationStrength.ShouldBe(2.0);
    }

    [Theory]
    [InlineData("plain")]
    [InlineData("timed")]
    [InlineData("counted")]
    [InlineData("pipeline")]
    [InlineData("named-retry")]
    public void EvolutionStrategy_ExplicitAdaptationWorksThroughWrappers(string wrapping)
    {
        var adaptation = new ScaleAdaptation();
        var gaussian = new GaussianMutator(1.0, 3.0) { MutationStrengthAdaptation = adaptation };
        var calls = new Counter();
        var duration = new Duration();
        IOperator<Mutation, RealVector> mutator = wrapping switch
        {
            "timed" => gaussian.Timed(duration),
            "counted" => gaussian.Counted(calls),
            "pipeline" => PipelineMutator.Create(gaussian),
            "named-retry" => gaussian.Retry(3).Named("inner").Named("outer"),
            _ => gaussian
        };
        var problem = CreateProblem(new BoundedRealVectorSearchSpace(1, -10.0, 10.0));
        var algorithm = CreateAlgorithm(mutator, problem) with { MutationStrengthAdaptation = adaptation };
        var scope = ResolutionScope.Create();
        var executor = scope.Resolve<RealVector, BoundedRealVectorSearchSpace, FuncProblem<RealVector, BoundedRealVectorSearchSpace>, PopulationState<RealVector>>(algorithm, problem);

        executor.RunStreaming(new SequenceRandom(), ct: TestContext.Current.CancellationToken)
            .Select(state => state.Population.Single().Candidate[0]).ShouldBe([0.0, -1.5, -2.5]);
        scope.Resolve(adaptation).Factor.ShouldBe(1 / 2.25);
        scope.Results.Result(calls).CurrentCount.ShouldBe(wrapping == "counted" ? 2 : 0);
        gaussian.MutationStrength.ShouldBe(3.0);
        algorithm.MutationStrengthAdaptation.ShouldBeSameAs(adaptation);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void EvolutionStrategy_UnsetSlotDoesNotAdapt(bool linkMutator)
    {
        var adaptation = new ScaleAdaptation();
        var gaussian = new GaussianMutator(1.0, 3.0) { MutationStrengthAdaptation = linkMutator ? adaptation : null };
        var problem = CreateProblem(new BoundedRealVectorSearchSpace(1, -10.0, 10.0));
        var algorithm = CreateAlgorithm(gaussian, problem);
        var scope = ResolutionScope.Create();
        var executor = scope.Resolve<RealVector, BoundedRealVectorSearchSpace, FuncProblem<RealVector, BoundedRealVectorSearchSpace>, PopulationState<RealVector>>(algorithm, problem);

        executor.RunStreaming(new SequenceRandom(), ct: TestContext.Current.CancellationToken)
            .Select(state => state.Population.Single().Candidate[0]).ShouldBe([0.0, -1.5, -3.0]);
        scope.Resolve(adaptation).Factor.ShouldBe(1.0);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    [InlineData(false, true)]
    public void EvolutionStrategy_CycleSharingFollowsExplicitLinks(bool shareAdaptation, bool shareMutator)
    {
        var adaptation = new ScaleAdaptation();
        var gaussian = new GaussianMutator(1.0, 3.0) { MutationStrengthAdaptation = adaptation };
        var problem = CreateProblem(new BoundedRealVectorSearchSpace(1, -10.0, 10.0));
        var algorithm = CreateAlgorithm(gaussian, problem) with { MutationStrengthAdaptation = adaptation, MaximumGenerations = 2 };
        var cycle = CycleAlgorithm.Create(algorithm) with { MaximumCycles = 2 };
        IAlgorithm<RealVector, PopulationState<RealVector>> configured = cycle;
        if (shareAdaptation && shareMutator)
            configured = cycle.Sharing(adaptation, gaussian);
        else if (shareAdaptation)
            configured = cycle.Sharing(adaptation);
        else if (shareMutator)
            configured = cycle.Sharing(gaussian);
        var scope = ResolutionScope.Create();
        var executor = scope.Resolve<RealVector, BoundedRealVectorSearchSpace, FuncProblem<RealVector, BoundedRealVectorSearchSpace>, PopulationState<RealVector>>(configured, problem);

        var states = executor.RunStreaming(new SequenceRandom(), ct: TestContext.Current.CancellationToken).ToList();
        // Each turn has its own strategy. Sharing only the mutator leaves it reading its first, unwritten adaptation.
        double[] expected = (shareAdaptation, shareMutator) switch
        {
            (true, _) => [0.0, -1.5, -2.5, -2.5 - 2.0 / 3.0],
            (false, true) => [0.0, -1.5, -3.0, -4.5],
            _ => [0.0, -1.5, -3.0, -4.0]
        };
        states.Select(state => state.Population.Single().Candidate[0]).ShouldBe(expected);
    }

    [Fact]
    public void EvolutionStrategy_ChoiceChildrenBothReadTheSharedAdaptation()
    {
        var adaptation = new ScaleAdaptation();
        var first = new GaussianMutator(1.0, 3.0) { MutationStrengthAdaptation = adaptation };
        var second = new GaussianMutator(1.0, 3.0) { MutationStrengthAdaptation = adaptation };
        var calls = new[] { new Counter(), new Counter() };
        var choice = ChoosePerInvocation.Create<Mutation, RealVector>(first.Counted(calls[0]), second.Counted(calls[1]));
        var problem = CreateProblem(new BoundedRealVectorSearchSpace(1, -10.0, 10.0));
        var algorithm = CreateAlgorithm(choice, problem) with { MutationStrengthAdaptation = adaptation };
        var scope = ResolutionScope.Create();
        var executor = scope.Resolve<RealVector, BoundedRealVectorSearchSpace, FuncProblem<RealVector, BoundedRealVectorSearchSpace>, PopulationState<RealVector>>(algorithm, problem);

        executor.RunStreaming(new SequenceRandom(0.0, 0.0, 0.0, 0.75, 0.0, 0.0), ct: TestContext.Current.CancellationToken)
            .Select(state => state.Population.Single().Candidate[0]).ShouldBe([0.0, -1.5, -2.5]);
        calls.Select(counter => scope.Results.Result(counter).CurrentCount).ShouldBe([1, 1]);
    }

    [Fact]
    public void ExternalOffsetRuleAndRecordNodeCanAdaptAGaussianBase()
    {
        IConfigurationNode<IAdaptation> adaptation = new ExternalOffset();
        var mutator = new GaussianMutator(1.0, 2.0) { MutationStrengthAdaptation = adaptation };
        var scope = ResolutionScope.Create();
        var problem = CreateProblem(new BoundedRealVectorSearchSpace(1, -10.0, 10.0));
        var executor = scope.For<RealVector, BoundedRealVectorSearchSpace, FuncProblem<RealVector, BoundedRealVectorSearchSpace>>(problem).Resolve(mutator);
        executor.Mutate([new RealVector(0.0)], new SequenceRandom(0.0, 1.0)).ShouldBe([new RealVector(2.0)]);
    }

    [Fact]
    public void ExternalRuleWritesAnOffsetReadByTheMutator()
    {
        var adaptation = new OffsetAdaptation();
        var mutator = new GaussianMutator(1.0, 2.0) { MutationStrengthAdaptation = adaptation };
        var problem = CreateProblem(new BoundedRealVectorSearchSpace(1, -10.0, 10.0));
        var scope = ResolutionScope.Create();
        var executor = scope.For<RealVector, BoundedRealVectorSearchSpace, FuncProblem<RealVector, BoundedRealVectorSearchSpace>>(problem).Resolve(mutator);
        ApplyExternalRule(scope.Resolve(adaptation));

        executor.Mutate([new RealVector(0.0)], new SequenceRandom(0.0, 1.0)).ShouldBe([new RealVector(2.0)]);
        mutator.MutationStrength.ShouldBe(2.0);

        static void ApplyExternalRule(AdaptedOffset offset) => offset.Offset += 2.0;
    }

    [Fact]
    public void IndependentStrategyRunsStartWithIndependentAdaptations()
    {
        var adaptation = new ScaleAdaptation();
        var mutator = new GaussianMutator(1.0, 3.0) { MutationStrengthAdaptation = adaptation };
        var problem = CreateProblem(new BoundedRealVectorSearchSpace(1, -10.0, 10.0));
        var algorithm = CreateAlgorithm(mutator, problem) with { MutationStrengthAdaptation = adaptation };
        for (var i = 0; i < 2; i++)
        {
            var scope = ResolutionScope.Create();
            var executor = scope.Resolve<RealVector, BoundedRealVectorSearchSpace, FuncProblem<RealVector, BoundedRealVectorSearchSpace>, PopulationState<RealVector>>(algorithm, problem);
            executor.RunStreaming(new SequenceRandom(), ct: TestContext.Current.CancellationToken)
                .Select(state => state.Population.Single().Candidate[0]).ShouldBe([0.0, -1.5, -2.5]);
        }
    }

    [Fact]
    public void GaussianMutatorReadsAdaptationOncePerBatchInvocation()
    {
        var adaptation = new CountingAdaptation();
        var mutator = new GaussianMutator(1.0, 2.0) { MutationStrengthAdaptation = adaptation };
        var problem = CreateProblem(new BoundedRealVectorSearchSpace(1, -10.0, 10.0));
        var scope = ResolutionScope.Create();
        var executor = scope.For<RealVector, BoundedRealVectorSearchSpace, FuncProblem<RealVector, BoundedRealVectorSearchSpace>>(problem).Resolve(mutator);

        executor.Mutate([new RealVector(0.0), new RealVector(0.0)], new SequenceRandom(0.0, 1.0, 0.0, 1.0))
            .ShouldBe([new RealVector(1.0), new RealVector(1.0)]);
        scope.Resolve(adaptation).Reads.ShouldBe(1);
        executor.Mutate([new RealVector(0.0)], new SequenceRandom(0.0, 1.0)).ShouldBe([new RealVector(2.0)]);
        scope.Resolve(adaptation).Reads.ShouldBe(2);
    }

    private sealed record CountingAdaptation : IConfigurationNode<CountingAdaptation.Value>
    {
        public Value CreateExecutionNode(ResolutionScope scope) => new();

        public sealed class Value : IAdaptation
        {
            public int Reads { get; private set; }
            public double Apply(double baseValue) => baseValue * ++Reads;
        }
    }

    private sealed record ExternalOffset : IConfigurationNode<IAdaptation>
    {
        public IAdaptation CreateExecutionNode(ResolutionScope scope) => new AdaptedOffset { Offset = 2.0 };
    }

    private static EvolutionStrategy<RealVector> CreateAlgorithm(IOperator<Mutation, RealVector> mutator, FuncProblem<RealVector, BoundedRealVectorSearchSpace> problem) => new()
    {
        PopulationSize = 1,
        NumberOfChildren = 1,
        Strategy = EvolutionStrategyType.Comma,
        Creator = new ZeroCreator(),
        Mutator = mutator,
        Selector = BestSelector.For(problem),
        MaximumGenerations = 3
    };

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
