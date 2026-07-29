using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Creators;
using HEAL.HeuristicLib.Operators.Crossovers;
using HEAL.HeuristicLib.Operators.Interceptors;
using HEAL.HeuristicLib.Operators.Mutators;
using HEAL.HeuristicLib.Operators.Replacers;
using HEAL.HeuristicLib.Operators.Selectors;
using HEAL.HeuristicLib.Operators.Terminators;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.States;
using HEAL.HeuristicLib.Tests.TestSupport.Mocks;
using HEAL.HeuristicLib.Tests.TestSupport.Random;

namespace HEAL.HeuristicLib.Tests.Operators;

public class ChooseOneOperatorTests
{
    [Fact]
    public void ChooseOneMutator_SnapshotsMutatorsAndWeights()
    {
        var first = new AddOffsetMutator(1);
        var second = new AddOffsetMutator(2);
        var mutators = new List<IMutator<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>>> { first, second };
        var weights = new List<double> { 1.0, 2.0 };
        var configuration = new ChooseOneMutator<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>>(mutators, weights);

        mutators.Clear();
        weights[0] = 100.0;

        configuration.Mutators.ShouldBe([first, second]);
        configuration.Weights.ShouldBe([1.0, 2.0]);
    }

    [Fact]
    public void ChooseOneCreator_ShouldPreserveAssignmentOrder()
    {
        var creator = ChooseOneCreator.Create(
          [new ConstantCreator(100), new ConstantCreator(200)],
          [1.0, 1.0]);
        var problem = FuncProblem.Create((int x) => x, DummySearchSpace<int>.Instance, SingleObjective.Minimize);
        var instance = new Execution.ExecutionInstanceRegistry().Resolve(creator);

        var result = instance.Create(3, new SequenceRandomNumberGenerator(0.2, 0.8, 0.3), DummySearchSpace<int>.Instance, problem);

        result.ShouldBe([100, 200, 100]);
    }

    [Fact]
    public void TransformedCreator_ShouldApplyMutatorToCreatedCandidates()
    {
        var creator = new ConstantCreator(5).TransformWith(new AddOffsetMutator(10));
        var problem = FuncProblem.Create((int x) => x, DummySearchSpace<int>.Instance, SingleObjective.Minimize);
        var instance = new Execution.ExecutionInstanceRegistry().Resolve(creator);

        var result = instance.Create(3, RandomNumberGenerator.Create(0), DummySearchSpace<int>.Instance, problem);

        result.ShouldBe([15, 15, 15]);
    }

    [Fact]
    public void TransformedCrossover_ShouldAlwaysApplyMutatorToCrossedCandidates()
    {
        var crossover = new FirstParentCrossover(100).TransformWith(new AddOffsetMutator(10));
        var problem = FuncProblem.Create((int x) => x, DummySearchSpace<int>.Instance, SingleObjective.Minimize);
        var instance = new Execution.ExecutionInstanceRegistry().Resolve(crossover);

        var result = instance.Cross([Parents.From(1, 10), Parents.From(2, 20)], RandomNumberGenerator.Create(0), DummySearchSpace<int>.Instance, problem);

        result.ShouldBe([111, 112]);
    }

    [Fact]
    public void ChooseOneMutator_ShouldPreserveOriginalOrder()
    {
        var mutator = ChooseOneMutator.Create(
          [new AddOffsetMutator(100), new AddOffsetMutator(200)],
          [1.0, 1.0]);

        var problem = FuncProblem.Create(
          evaluateFunc: (int x) => x,
          encoding: DummySearchSpace<int>.Instance,
          objective: SingleObjective.Minimize);
        var rng = new SequenceRandomNumberGenerator(0.2, 0.8, 0.3);
        var instance = new Execution.ExecutionInstanceRegistry().Resolve(mutator);

        var result = instance.Mutate([1, 2, 3], rng, DummySearchSpace<int>.Instance, problem);

        result.ShouldBe([101, 202, 103]);
    }

    [Fact]
    public void ChooseOneSelector_ShouldChooseOneSelectorForCompleteCall()
    {
        var selector = ChooseOneSelector.Create(
            [new FirstCandidatesSelector(), new LastCandidatesSelector()],
            [1.0, 1.0]);
        var problem = FuncProblem.Create((int x) => x, DummySearchSpace<int>.Instance, SingleObjective.Minimize);
        var population = CreateEvaluatedCandidates(1, 2, 3);
        var instance = new Execution.ExecutionInstanceRegistry().Resolve(selector);

        var selected = instance.Select(population, problem.Objective, 2, new SequenceRandomNumberGenerator(0.8), problem.SearchSpace, problem);

        selected.Select(candidate => candidate.Candidate).ShouldBe([2, 3]);
    }

    [Fact]
    public void ChooseOneReplacer_ShouldChooseOneReplacerForCompleteCall()
    {
        var replacer = ChooseOneReplacer.Create(
            [new PreviousCandidatesReplacer(), new OffspringCandidatesReplacer()],
            [1.0, 1.0]);
        var problem = FuncProblem.Create((int x) => x, DummySearchSpace<int>.Instance, SingleObjective.Minimize);
        var instance = new Execution.ExecutionInstanceRegistry().Resolve(replacer);

        var replaced = instance.Replace(CreateEvaluatedCandidates(1, 2), CreateEvaluatedCandidates(3, 4), problem.Objective, 2, new SequenceRandomNumberGenerator(0.8), problem.SearchSpace, problem);

        replaced.Select(candidate => candidate.Candidate).ShouldBe([3, 4]);
    }

    [Fact]
    public void ChooseOneOperators_ImplicitWeights_ShouldBeNormalizedUniformly()
    {
        ImmutableArray<ICreator<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>>> creators = [new ConstantCreator(100), new ConstantCreator(200)];
        ImmutableArray<ICrossover<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>>> crossovers = [new FirstParentCrossover(100), new SecondParentCrossover(200)];
        ImmutableArray<IMutator<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>>> mutators = [new AddOffsetMutator(100), new AddOffsetMutator(200)];
        ImmutableArray<ISelector<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>>> selectors = [new FirstCandidatesSelector(), new LastCandidatesSelector()];
        ImmutableArray<IReplacer<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>>> replacers = [new PreviousCandidatesReplacer(), new OffspringCandidatesReplacer()];
        var creatorFromFactory = ChooseOneCreator.Create(new ConstantCreator(100), new ConstantCreator(200));
        var crossoverFromFactory = ChooseOneCrossover.Create(new FirstParentCrossover(100), new SecondParentCrossover(200));
        var mutatorFromFactory = ChooseOneMutator.Create(new AddOffsetMutator(100), new AddOffsetMutator(200));
        var selectorFromFactory = ChooseOneSelector.Create(new FirstCandidatesSelector(), new LastCandidatesSelector());
        var replacerFromFactory = ChooseOneReplacer.Create(new PreviousCandidatesReplacer(), new OffspringCandidatesReplacer());

        new ChooseOneCreator<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>>(creators).Weights.ShouldBe([0.5, 0.5]);
        creatorFromFactory.Weights.ShouldBe([0.5, 0.5]);
        new ChooseOneCrossover<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>>(crossovers).Weights.ShouldBe([0.5, 0.5]);
        crossoverFromFactory.Weights.ShouldBe([0.5, 0.5]);
        new ChooseOneMutator<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>>(mutators).Weights.ShouldBe([0.5, 0.5]);
        mutatorFromFactory.Weights.ShouldBe([0.5, 0.5]);
        new ChooseOneSelector<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>>(selectors).Weights.ShouldBe([0.5, 0.5]);
        selectorFromFactory.Weights.ShouldBe([0.5, 0.5]);
        new ChooseOneReplacer<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>>(replacers).Weights.ShouldBe([0.5, 0.5]);
        replacerFromFactory.Weights.ShouldBe([0.5, 0.5]);
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void WeightedBatchDispatch_ShouldRejectNonFiniteWeights(double invalidWeight)
    {
        Should.Throw<ArgumentException>(() => new WeightedBatchDispatch([1.0, invalidWeight])).ParamName.ShouldBe("weights");
    }

    [Fact]
    public void ChooseOneOperators_ShouldRejectInvalidWeights()
    {
        Should.Throw<ArgumentException>(() => ChooseOneMutator.Create(
          [new AddOffsetMutator(100)],
          []));
        Should.Throw<ArgumentException>(() => ChooseOneMutator.Create(
          [new AddOffsetMutator(100), new AddOffsetMutator(200)],
          [1.0, -1.0]));
        Should.Throw<ArgumentException>(() => ChooseOneCrossover.Create(
          [new FirstParentCrossover(100), new SecondParentCrossover(200)],
          [0.0, 0.0]));
        Should.Throw<ArgumentException>(() => ChooseOneCreator.Create(
          [new ConstantCreator(100), new ConstantCreator(200)],
          [1.0]));
        Should.Throw<ArgumentException>(() => ChooseOneSelector.Create(
            [new FirstCandidatesSelector(), new LastCandidatesSelector()],
            [double.NaN, 1.0]));
        Should.Throw<ArgumentException>(() => ChooseOneReplacer.Create<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>>([]));
    }

    [Fact]
    public void ChooseOneCrossover_ShouldPreserveOriginalOrder()
    {
        var crossover = ChooseOneCrossover.Create(
          [new FirstParentCrossover(100), new SecondParentCrossover(200)],
          [1.0, 1.0]);

        var problem = FuncProblem.Create(
          evaluateFunc: (int x) => x,
          encoding: DummySearchSpace<int>.Instance,
          objective: SingleObjective.Minimize);
        var rng = new SequenceRandomNumberGenerator(0.2, 0.8, 0.3);
        var instance = new Execution.ExecutionInstanceRegistry().Resolve(crossover);

        var result = instance.Cross(
          [Parents.From(1, 10), Parents.From(2, 20), Parents.From(3, 30)],
          rng,
          DummySearchSpace<int>.Instance,
          problem);

        result.ShouldBe([101, 220, 103]);
    }

    [Fact]
    public void PipelineMutator_ShouldApplyMutatorsInSequence()
    {
        var mutator = new AddOffsetMutator(10).Then(new AddOffsetMutator(100));

        var problem = FuncProblem.Create(
          evaluateFunc: (int x) => x,
          encoding: DummySearchSpace<int>.Instance,
          objective: SingleObjective.Minimize);
        var instance = new Execution.ExecutionInstanceRegistry().Resolve(mutator);

        var result = instance.Mutate([1, 2, 3], RandomNumberGenerator.Create(0), DummySearchSpace<int>.Instance, problem);

        result.ShouldBe([111, 112, 113]);
    }

    [Fact]
    public void PipelineMutator_ShouldResolveInnerMutatorsOncePerExecutionInstance()
    {
        var countingMutator = new CountingInstanceMutator();
        var mutator = PipelineMutator.Create(countingMutator);

        var problem = FuncProblem.Create(
          evaluateFunc: (int x) => x,
          encoding: DummySearchSpace<int>.Instance,
          objective: SingleObjective.Minimize);
        var instance = new Execution.ExecutionInstanceRegistry().Resolve(mutator);

        var first = instance.Mutate([1], RandomNumberGenerator.Create(0), DummySearchSpace<int>.Instance, problem);
        var second = instance.Mutate([1], RandomNumberGenerator.Create(1), DummySearchSpace<int>.Instance, problem);

        countingMutator.ExecutionInstancesCreated.ShouldBe(1);
        first.ShouldBe([2]);
        second.ShouldBe([3]);
    }

    [Fact]
    public void PipelineMutator_ShouldRejectEmptyPipelines()
    {
        Should.Throw<ArgumentException>(() => new PipelineMutator<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>>([]))
          .ParamName.ShouldBe("mutators");
    }

    [Fact]
    public void PipelineInterceptor_ShouldApplyInterceptorsInSequence()
    {
        var interceptor = new AddToStateInterceptor(10).Then(new AddToStateInterceptor(100));

        var problem = FuncProblem.Create(
          evaluateFunc: (int x) => x,
          encoding: DummySearchSpace<int>.Instance,
          objective: SingleObjective.Minimize);
        var instance = new Execution.ExecutionInstanceRegistry().Resolve(interceptor);

        var result = instance.Transform(new TestAlgorithmState { Value = 1 }, previousState: null, DummySearchSpace<int>.Instance, problem);

        result.Value.ShouldBe(111);
    }

    [Fact]
    public void PipelineInterceptor_EmptyPipeline_ShouldBeIdentity()
    {
        var interceptor = new PipelineInterceptor<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>, TestAlgorithmState>([]);
        var problem = FuncProblem.Create((int x) => x, DummySearchSpace<int>.Instance, SingleObjective.Minimize);
        var instance = new Execution.ExecutionInstanceRegistry().Resolve(interceptor);
        var state = new TestAlgorithmState { Value = 1 };

        instance.Transform(state, previousState: null, DummySearchSpace<int>.Instance, problem).ShouldBeSameAs(state);
    }

    [Fact]
    public void LogicalTerminators_ShouldShortCircuit()
    {
        var problem = FuncProblem.Create((int x) => x, DummySearchSpace<int>.Instance, SingleObjective.Minimize);
        var state = new TestAlgorithmState { Value = 1 };
        var registry = new Execution.ExecutionInstanceRegistry();
        var any = registry.Resolve(new ConstantTerminator(true).Or(new ThrowingTerminator()));
        var all = registry.Resolve(new ConstantTerminator(false).And(new ThrowingTerminator()));

        any.IsTerminalState(state, DummySearchSpace<int>.Instance, problem).ShouldBeTrue();
        all.IsTerminalState(state, DummySearchSpace<int>.Instance, problem).ShouldBeFalse();
    }

    [Fact]
    public void EmptyLogicalTerminators_ShouldUseBooleanIdentity()
    {
        var problem = FuncProblem.Create((int x) => x, DummySearchSpace<int>.Instance, SingleObjective.Minimize);
        var state = new TestAlgorithmState { Value = 1 };
        var registry = new Execution.ExecutionInstanceRegistry();
        var any = registry.Resolve(new AnyTerminator<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>, TestAlgorithmState>([]));
        var all = registry.Resolve(new AllTerminator<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>, TestAlgorithmState>([]));

        any.IsTerminalState(state, DummySearchSpace<int>.Instance, problem).ShouldBeFalse();
        all.IsTerminalState(state, DummySearchSpace<int>.Instance, problem).ShouldBeTrue();
    }

    private sealed record AddOffsetMutator(int Offset) : SingleSolutionMutator<int, DummySearchSpace<int>>
    {
        public override int Mutate(int parent, IRandomNumberGenerator random, DummySearchSpace<int> searchSpace) => parent + Offset;
    }

    private sealed record ConstantCreator(int Value) : SingleSolutionCreator<int, DummySearchSpace<int>>
    {
        public override int Create(IRandomNumberGenerator random, DummySearchSpace<int> searchSpace) => Value;
    }

    private sealed record FirstParentCrossover(int Offset) : SingleSolutionCrossover<int, DummySearchSpace<int>>
    {
        public override int Cross(IParents<int> parents, IRandomNumberGenerator random, DummySearchSpace<int> searchSpace) => parents.Parent1 + Offset;
    }

    private sealed record SecondParentCrossover(int Offset) : SingleSolutionCrossover<int, DummySearchSpace<int>>
    {
        public override int Cross(IParents<int> parents, IRandomNumberGenerator random, DummySearchSpace<int> searchSpace) => parents.Parent2 + Offset;
    }

    private sealed record AddToStateInterceptor(int Offset)
      : StatelessInterceptor<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>, TestAlgorithmState>
    {
        public override TestAlgorithmState Transform(TestAlgorithmState currentState, TestAlgorithmState? previousState, DummySearchSpace<int> searchSpace, IProblem<int, DummySearchSpace<int>> problem)
          => currentState with { Value = currentState.Value + Offset };
    }

    private sealed record FirstCandidatesSelector : StatelessSelector<int>
    {
        public override IReadOnlyList<EvaluatedCandidate<int>> Select(IReadOnlyList<EvaluatedCandidate<int>> population, ObjectiveDirections objective, int count, IRandomNumberGenerator random) =>
            population.Take(count).ToArray();
    }

    private sealed record LastCandidatesSelector : StatelessSelector<int>
    {
        public override IReadOnlyList<EvaluatedCandidate<int>> Select(IReadOnlyList<EvaluatedCandidate<int>> population, ObjectiveDirections objective, int count, IRandomNumberGenerator random) =>
            population.TakeLast(count).ToArray();
    }

    private sealed record PreviousCandidatesReplacer : StatelessReplacer<int>
    {
        public override IReadOnlyList<EvaluatedCandidate<int>> Replace(IReadOnlyList<EvaluatedCandidate<int>> previousPopulation, IReadOnlyList<EvaluatedCandidate<int>> offspringPopulation, ObjectiveDirections objective, int count, IRandomNumberGenerator random) =>
            previousPopulation.Take(count).ToArray();
    }

    private sealed record OffspringCandidatesReplacer : StatelessReplacer<int>
    {
        public override IReadOnlyList<EvaluatedCandidate<int>> Replace(IReadOnlyList<EvaluatedCandidate<int>> previousPopulation, IReadOnlyList<EvaluatedCandidate<int>> offspringPopulation, ObjectiveDirections objective, int count, IRandomNumberGenerator random) =>
            offspringPopulation.Take(count).ToArray();
    }

    private sealed record ConstantTerminator(bool Result)
        : StatelessTerminator<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>, TestAlgorithmState>
    {
        public override bool IsTerminalState(TestAlgorithmState state, DummySearchSpace<int> searchSpace, IProblem<int, DummySearchSpace<int>> problem) => Result;
    }

    private sealed record ThrowingTerminator
        : StatelessTerminator<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>, TestAlgorithmState>
    {
        public override bool IsTerminalState(TestAlgorithmState state, DummySearchSpace<int> searchSpace, IProblem<int, DummySearchSpace<int>> problem) =>
            throw new InvalidOperationException("This terminator should not be reached.");
    }

    private static IReadOnlyList<EvaluatedCandidate<int>> CreateEvaluatedCandidates(params int[] candidates) =>
        candidates.Select(candidate => EvaluatedCandidate.From(candidate, new ObjectiveVector(candidate))).ToArray();

    private sealed record TestAlgorithmState : SearchState
    {
        public required int Value { get; init; }
    }

    private sealed class CountingInstanceMutator : IMutator<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>>
    {
        public int ExecutionInstancesCreated { get; private set; }

        public IMutatorInstance<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>> CreateExecutionInstance(Execution.ExecutionInstanceRegistry instanceRegistry)
        {
            ExecutionInstancesCreated++;
            return new Instance();
        }

        private sealed class Instance : IMutatorInstance<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>>
        {
            private int calls;

            public IReadOnlyList<int> Mutate(IReadOnlyList<int> parents, IRandomNumberGenerator random, DummySearchSpace<int> searchSpace, IProblem<int, DummySearchSpace<int>> problem)
            {
                calls++;
                return parents.Select(parent => parent + calls).ToArray();
            }
        }
    }
}
