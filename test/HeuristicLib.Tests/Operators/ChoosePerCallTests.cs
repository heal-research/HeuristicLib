using HEAL.HeuristicLib.Encodings.RealVectors;
using HEAL.HeuristicLib.Operators.Evaluators;
using HEAL.HeuristicLib.Operators.Mutators;
using HEAL.HeuristicLib.Operators.Selectors;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Problems.TestFunctions;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.Tests.ExecutionInfrastructure.OperatorFoundation;
using HEAL.HeuristicLib.Tests.TestSupport.Mocks;

namespace HEAL.HeuristicLib.Tests.Operators;

public class ChoosePerCallTests
{
    private static readonly FuncProblem<int, DummySearchSpace<int>> Problem =
        FuncProblem.Create((int x) => x, DummySearchSpace<int>.Instance, SingleObjective.Minimize);

    [Fact]
    public void SnapshotsChildrenAndWeightsWithStructuralEquality()
    {
        var first = new IndexMutator(0, []);
        var second = new IndexMutator(1, []);
        List<IOperator<Mutation, int>> children = [first, second];
        List<double> weights = [1.0, 2.0];

        var choice = ChoosePerCall.Create(children, weights);
        children.Add(new IndexMutator(2, []));
        weights.Add(3.0);

        choice.ChildOperators.ShouldBe([first, second]);
        choice.Weights.ShouldBe([1.0, 2.0]);
        choice.ShouldBe(new ChoosePerCall<Mutation, int>([first, second]) { Weights = [1.0, 2.0] });
        ChoosePerCall.Create(first, second).Weights.ShouldBeEmpty();
        ChoosePerCall.Create(first, second).ShouldNotBe(choice);
    }

    [Fact]
    public void PreparationRejectsMissingChildrenAndMismatchedWeights()
    {
        var child = new IndexMutator(0, []);
        var scope = CreateScope();

        Should.Throw<InvalidOperationException>(() => scope.Resolve(ChoosePerCall.Create<Mutation, int>()));
        Should.Throw<InvalidOperationException>(() => scope.Resolve(ChoosePerCall.Create(child) with { ChildOperators = [] }));
        Should.Throw<InvalidOperationException>(() => scope.Resolve(ChoosePerCall.Create([child], [1.0, 1.0])));
        Should.Throw<InvalidOperationException>(() => scope.Resolve(ChoosePerCall.Create([child, child], [1.0])));
    }

    [Theory]
    [InlineData(new double[0])]
    [InlineData(new[] { 1.0, 3.0, 0.5 })]
    public void ChoosesTheSameChildrenAndLeavesTheSameDrawsAsChooseOneSelector(double[] weights)
    {
        var mutations = new List<(int Index, double NextDraw)>();
        var selections = new List<(int Index, double NextDraw)>();
        var choice = ChoosePerCall.Create(
            Enumerable.Range(0, 3).Select(index => (IOperator<Mutation, int>)new IndexMutator(index, mutations)).ToArray(), weights);
        var selector = ChooseOneSelector.Create(
            Enumerable.Range(0, 3).Select(index => (ISelector<int>)new IndexSelector(index, selections)).ToArray(), weights);
        var mutation = CreateScope().Resolve(choice);
        var selection = CreateScope().Resolve(selector);
        IReadOnlyList<EvaluatedCandidate<int>> population = [EvaluatedCandidate.From(0, new ObjectiveVector(0.0))];

        for (var seed = 0; seed < 64; seed++)
        {
            mutation.Mutate([0], RandomNumberGenerator.Create(seed), Problem.SearchSpace, Problem);
            selection.Select(population, Problem.Objective, 1, RandomNumberGenerator.Create(seed), Problem.SearchSpace, Problem);
        }

        mutations.ShouldBe(selections);
        mutations.Select(call => call.Index).Distinct().Count().ShouldBe(3);
    }

    [Theory]
    [InlineData(new[] { 0.0, 1.0 }, new[] { 1 })]
    [InlineData(new[] { -1.0, double.NaN, 2.0, double.NegativeInfinity }, new[] { 2 })]
    [InlineData(new[] { double.PositiveInfinity, 1.0, double.PositiveInfinity }, new[] { 0, 2 })]
    [InlineData(new[] { 0.0, double.NegativeInfinity }, new[] { 0, 1 })]
    public void WeightsFollowTheDocumentedUnusualValueSemantics(double[] weights, int[] chosen)
    {
        var calls = new List<(int Index, double NextDraw)>();
        var choice = ChoosePerCall.Create(
            Enumerable.Range(0, weights.Length).Select(index => (IOperator<Mutation, int>)new IndexMutator(index, calls)).ToArray(), weights);
        var executor = CreateScope().Resolve(choice);
        var random = RandomNumberGenerator.Create(5);

        for (var call = 0; call < 200; call++)
            executor.Mutate([0], random, Problem.SearchSpace, Problem);

        calls.Select(call => call.Index).Distinct().Order().ShouldBe(chosen);
    }

    [Fact]
    public void TheChosenChildReceivesTheCallsArgumentsAndRandom()
    {
        var observed = new List<(IReadOnlyList<int> Parents, IRandomNumberGenerator Random, DummySearchSpace<int> SearchSpace, FuncProblem<int, DummySearchSpace<int>> Problem)>();
        var executor = CreateScope().Resolve(ChoosePerCall.Create([new IndexMutator(0, []), new ObservingMutator(observed)], [0.0, 1.0]));
        IReadOnlyList<int> parents = [1, 2];
        var random = RandomNumberGenerator.Create(3);

        executor.Mutate(parents, random, Problem.SearchSpace, Problem).ShouldBeSameAs(parents);

        observed.ShouldHaveSingleItem().ShouldSatisfyAllConditions(
            call => call.Parents.ShouldBeSameAs(parents),
            call => call.Random.ShouldBeSameAs(random),
            call => call.SearchSpace.ShouldBeSameAs(Problem.SearchSpace),
            call => call.Problem.ShouldBeSameAs(Problem));
    }

    [Fact]
    public void ChoosesOneEvaluatorForTheWholeEvaluationCall()
    {
        var choice = ChoosePerCall.Create([new ConstantEvaluator(10.0), new ConstantEvaluator(20.0)], [0.0, 1.0]);
        var executor = CreateScope().Resolve(choice);

        var objectives = executor.Evaluate([1, 2, 3], RandomNumberGenerator.Create(1), Problem.SearchSpace, Problem);

        objectives.Select(objective => objective[0]).ShouldBe([20.0, 20.0, 20.0]);
    }

    [Fact]
    public void AdviceCountsTheChoiceOncePerCallAndOnlyTheChosenChild()
    {
        var first = new IndexMutator(0, []);
        var second = new IndexMutator(1, []);
        var choice = ChoosePerCall.Create([first, second], [1.0, 0.0]);
        CountAccumulator firstCalls = new(), secondCalls = new(), choiceCalls = new(), choiceCandidates = new();
        var executor = ResolutionScope.Create(
                Aspect.CountCalls(first, firstCalls), Aspect.CountCalls(second, secondCalls),
                Aspect.CountCalls(choice, choiceCalls), Aspect.CountCandidates(choice, choiceCandidates))
            .For<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>().Resolve(choice);
        var random = RandomNumberGenerator.Create(1);

        for (var call = 0; call < 3; call++)
            executor.Mutate([1, 2], random, Problem.SearchSpace, Problem);

        firstCalls.CurrentCount.ShouldBe(3);
        secondCalls.CurrentCount.ShouldBe(0);
        choiceCalls.CurrentCount.ShouldBe(3);
        choiceCandidates.CurrentCount.ShouldBe(6);
    }

    [Fact]
    public void EachConstructionResolvesItsChildrenInTheRequestingContext()
    {
        var child = new IndexMutator(0, []);
        var choice = ChoosePerCall.Create(child);
        var calls = new CountAccumulator();
        var root = ResolutionScope.Create();
        var plain = root.For<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>().Resolve(choice);
        var counted = root.With(Aspect.CountCalls(child, calls)).For<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>().Resolve(choice);
        var random = RandomNumberGenerator.Create(1);

        plain.Mutate([1], random, Problem.SearchSpace, Problem);
        counted.Mutate([1], random, Problem.SearchSpace, Problem);

        calls.CurrentCount.ShouldBe(1);
    }

    [Fact]
    public void ExposesNoControlsOfItsChildren()
    {
        var child = new ControlledMutator();
        var scope = CreateScope();

        scope.Resolve(child, out ILevelControl? direct).ShouldNotBeNull();
        scope.Resolve(ChoosePerCall.Create(child), out ILevelControl? throughChoice);
        scope.Resolve(ChoosePerCall.Create(child).Named("choice").Retry(2), out ILevelControl? throughWrappers);

        direct.ShouldNotBeNull();
        throughChoice.ShouldBeNull();
        throughWrappers.ShouldBeNull();
    }

    [Fact]
    public void ChoosesAmongExternalOperationsWithoutAdapterChanges()
    {
        var choice = ChoosePerCall.Create([new ChecksumValue(10), new ChecksumValue(20)], [0.0, 1.0]);
        var problem = new TestFunctionProblem(new SphereFunction(1));
        var executor = ResolutionScope.Create().For<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>().Resolve(choice);

        executor.Read(RandomNumberGenerator.Create(1), problem.SearchSpace, problem).ShouldBe(20);
    }

    private static ResolutionScope<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>> CreateScope() =>
        ResolutionScope.Create().For<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>();

    private sealed record IndexMutator(int Index, List<(int Index, double NextDraw)> Calls) : StatelessMutator<int>
    {
        public override IReadOnlyList<int> Mutate(IReadOnlyList<int> parents, IRandomNumberGenerator random)
        {
            Calls.Add((Index, random.NextDouble()));
            return parents.Select(_ => Index).ToArray();
        }
    }

    private sealed record IndexSelector(int Index, List<(int Index, double NextDraw)> Calls) : StatelessSelector<int>
    {
        public override IReadOnlyList<EvaluatedCandidate<int>> Select(IReadOnlyList<EvaluatedCandidate<int>> population, ObjectiveDirections objective, int count, IRandomNumberGenerator random)
        {
            Calls.Add((Index, random.NextDouble()));
            return population.Take(count).ToArray();
        }
    }

    private sealed record ObservingMutator(List<(IReadOnlyList<int> Parents, IRandomNumberGenerator Random, DummySearchSpace<int> SearchSpace, FuncProblem<int, DummySearchSpace<int>> Problem)> Calls)
        : StatelessMutator<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>
    {
        public override IReadOnlyList<int> Mutate(IReadOnlyList<int> parents, IRandomNumberGenerator random, DummySearchSpace<int> searchSpace, FuncProblem<int, DummySearchSpace<int>> problem)
        {
            Calls.Add((parents, random, searchSpace, problem));
            return parents;
        }
    }

    private sealed record ConstantEvaluator(double Value) : StatelessEvaluator<int>
    {
        public override IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<int> candidates, IRandomNumberGenerator random) =>
            candidates.Select(_ => new ObjectiveVector(Value)).ToArray();
    }

    private interface ILevelControl
    {
        int Level { get; set; }
    }

    private sealed record ControlledMutator : IOperator<Mutation, int>
    {
        public OperatorExecutorFactory CreateExecutorFactory() => OperatorExecutorFactory.FromFactory<Executor>(_ => new Executor());

        private sealed class Executor : IMutationExecutor<int, ISearchSpace<int>, IProblem<int, ISearchSpace<int>>>, ILevelControl
        {
            public int Level { get; set; }

            public IReadOnlyList<int> Mutate(IReadOnlyList<int> parents, IRandomNumberGenerator random, ISearchSpace<int> searchSpace, IProblem<int, ISearchSpace<int>> problem) =>
                parents;
        }
    }

    private sealed record ChecksumValue(int Value) : IOperator<Checksum, RealVector>
    {
        public OperatorExecutorFactory CreateExecutorFactory() => OperatorExecutorFactory.FromFactory<Executor>(_ => new Executor(Value));

        private sealed class Executor(int value) : IChecksumExecutor<RealVector, ISearchSpace<RealVector>, IProblem<RealVector, ISearchSpace<RealVector>>>
        {
            public int Read(IRandomNumberGenerator random, ISearchSpace<RealVector> searchSpace, IProblem<RealVector, ISearchSpace<RealVector>> problem) => value;
        }
    }
}
