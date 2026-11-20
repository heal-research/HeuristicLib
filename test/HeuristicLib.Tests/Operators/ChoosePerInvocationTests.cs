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

public class ChoosePerInvocationTests
{
    private static readonly FuncProblem<int, DummySearchSpace<int>> Problem =
        FuncProblem.Create((int x) => x, DummySearchSpace<int>.Instance, SingleObjective.Minimize);

    [Fact]
    public void SnapshotsChildrenAndWeightsWithStructuralEquality()
    {
        var first = new IndexMutator(0, _ => { });
        var second = new IndexMutator(1, _ => { });
        List<IOperator<Mutation, int>> children = [first, second];
        List<double> weights = [1.0, 2.0];

        var choice = ChoosePerInvocation.Create(children, weights);
        children.Add(new IndexMutator(2, _ => { }));
        weights.Add(3.0);

        choice.ChildOperators.ShouldBe([first, second]);
        choice.Weights.ShouldBe([1.0, 2.0]);
        choice.ShouldBe(new ChoosePerInvocation<Mutation, int>([first, second]) { Weights = [1.0, 2.0] });
        ChoosePerInvocation.Create(first, second).Weights.ShouldBeEmpty();
        ChoosePerInvocation.Create(first, second).ShouldNotBe(choice);
    }

    [Fact]
    public void ResolutionRejectsMissingChildrenAndMismatchedWeights()
    {
        var child = new IndexMutator(0, _ => { });
        var scope = CreateScope();

        Should.Throw<InvalidOperationException>(() => scope.Resolve(ChoosePerInvocation.Create<Mutation, int>()));
        Should.Throw<InvalidOperationException>(() => scope.Resolve(ChoosePerInvocation.Create(child) with { ChildOperators = [] }));
        Should.Throw<InvalidOperationException>(() => scope.Resolve(ChoosePerInvocation.Create([child], [1.0, 1.0])));
        Should.Throw<InvalidOperationException>(() => scope.Resolve(ChoosePerInvocation.Create([child, child], [1.0])));
    }

    [Theory]
    [InlineData(new double[0])]
    [InlineData(new[] { 1.0, 3.0, 0.5 })]
    public void WholeCallChoiceMatchesAcrossOperationsAndPreservesDraws(double[] weights)
    {
        var mutations = new List<(int Index, double NextDraw)>();
        var selections = new List<(int Index, double NextDraw)>();
        var choice = ChoosePerInvocation.Create(
            Enumerable.Range(0, 3).Select(index => (IOperator<Mutation, int>)new IndexMutator(index, mutations.Add)).ToArray(), weights);
        var selector = ChoosePerInvocation.Create(
            Enumerable.Range(0, 3).Select(index => (IOperator<Selection, int>)new IndexSelector(index, selections.Add)).ToArray(), weights);
        var mutation = CreateScope().Resolve(choice);
        var selection = CreateScope().Resolve(selector);
        IReadOnlyList<EvaluatedCandidate<int>> population = [EvaluatedCandidate.From(0, new ObjectiveVector(0.0))];

        for (var seed = 0; seed < 64; seed++)
        {
            mutation.Mutate([0], RandomNumberGenerator.Create(seed));
            selection.Select(population, 1, RandomNumberGenerator.Create(seed));
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
        var choice = ChoosePerInvocation.Create(
            Enumerable.Range(0, weights.Length).Select(index => (IOperator<Mutation, int>)new IndexMutator(index, calls.Add)).ToArray(), weights);
        var executor = CreateScope().Resolve(choice);
        var random = RandomNumberGenerator.Create(5);

        for (var call = 0; call < 200; call++)
            executor.Mutate([0], random);

        calls.Select(call => call.Index).Distinct().Order().ShouldBe(chosen);
    }

    [Fact]
    public void TheChosenChildReceivesTheCallsArgumentsAndRandom()
    {
        var observed = new List<(IReadOnlyList<int> Parents, IRandomNumberGenerator Random, DummySearchSpace<int> SearchSpace, FuncProblem<int, DummySearchSpace<int>> Problem)>();
        var executor = CreateScope().Resolve(ChoosePerInvocation.Create([new IndexMutator(0, _ => { }), new ObservingMutator(observed.Add)], [0.0, 1.0]));
        IReadOnlyList<int> parents = [1, 2];
        var random = RandomNumberGenerator.Create(3);

        executor.Mutate(parents, random).ShouldBeSameAs(parents);

        observed.ShouldHaveSingleItem().ShouldSatisfyAllConditions(
            call => call.Parents.ShouldBeSameAs(parents),
            call => call.Random.ShouldBeSameAs(random));
    }

    [Fact]
    public void ChoosesOneEvaluatorForTheWholeEvaluationCall()
    {
        var choice = ChoosePerInvocation.Create([new ConstantEvaluator(10.0), new ConstantEvaluator(20.0)], [0.0, 1.0]);
        var executor = CreateScope().Resolve(choice);

        var objectives = executor.Evaluate([1, 2, 3], RandomNumberGenerator.Create(1));

        objectives.Select(objective => objective[0]).ShouldBe([20.0, 20.0, 20.0]);
    }

    [Fact]
    public void CountersCountTheChoiceOncePerCallAndOnlyTheChosenChild()
    {
        var first = new IndexMutator(0, _ => { });
        var second = new IndexMutator(1, _ => { });
        var choice = ChoosePerInvocation.Create([first, second], [1.0, 0.0]);
        Counter firstCalls = new(), secondCalls = new(), choiceCalls = new(), choiceCandidates = new();
        var counted = choice
            .Substitute(ObservationSource.Create<Mutation, int, MutationObservation<int>>(first).Counting(firstCalls), ObservationSource.Create<Mutation, int, MutationObservation<int>>(second).Counting(secondCalls))
            .Counted(choiceCandidates, OperatorCountMetric.Candidates)
            .Counted(choiceCalls);
        var scope = ResolutionScope.Create();
        var executor = scope
            .For<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>(Problem).Resolve(counted);
        var random = RandomNumberGenerator.Create(1);

        for (var call = 0; call < 3; call++)
            executor.Mutate([1, 2], random);

        scope.Results.Result(firstCalls).CurrentCount.ShouldBe(3);
        scope.Results.Result(secondCalls).CurrentCount.ShouldBe(0);
        scope.Results.Result(choiceCalls).CurrentCount.ShouldBe(3);
        scope.Results.Result(choiceCandidates).CurrentCount.ShouldBe(6);
    }

    [Fact]
    public void EachConstructionResolvesItsChildrenInTheRequestingContext()
    {
        var child = new IndexMutator(0, _ => { });
        var choice = ChoosePerInvocation.Create(child);
        var root = ResolutionScope.Create();
        var plain = root.For<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>(Problem).Resolve(choice);
        var countedScope = root.CreateChildScope();
        var counted = countedScope.For<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>(Problem).Resolve(choice.Counting(child, out var calls));
        var random = RandomNumberGenerator.Create(1);

        plain.Mutate([1], random);
        counted.Mutate([1], random);

        root.Results.Result(calls).CurrentCount.ShouldBe(1);
    }

    [Fact]
    public void ChoosesAmongExternalOperationsWithoutAdapterChanges()
    {
        var choice = ChoosePerInvocation.Create([new ChecksumValue(10), new ChecksumValue(20)], [0.0, 1.0]);
        var problem = new TestFunctionProblem(new SphereFunction(1));
        var executor = ResolutionScope.Create().For<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>(problem).Resolve(choice);

        executor.Read(RandomNumberGenerator.Create(1)).ShouldBe(20);
    }

    private static ResolutionScope<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>> CreateScope() =>
        ResolutionScope.Create().For<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>(Problem);

    private sealed record IndexMutator(int Index, Action<(int Index, double NextDraw)> RecordInvocation) : StatelessMutator<int>
    {
        public override IReadOnlyList<int> Mutate(IReadOnlyList<int> parents, IRandomNumberGenerator random)
        {
            RecordInvocation((Index, random.NextDouble()));
            return parents.Select(_ => Index).ToArray();
        }
    }

    private sealed record IndexSelector(int Index, Action<(int Index, double NextDraw)> RecordInvocation) : StatelessSelector<int>
    {
        public override IReadOnlyList<EvaluatedCandidate<int>> Select(IReadOnlyList<EvaluatedCandidate<int>> population, ObjectiveDirections objective, int count, IRandomNumberGenerator random)
        {
            RecordInvocation((Index, random.NextDouble()));
            return population.Take(count).ToArray();
        }
    }

    private sealed record ObservingMutator(Action<(IReadOnlyList<int> Parents, IRandomNumberGenerator Random, DummySearchSpace<int> SearchSpace, FuncProblem<int, DummySearchSpace<int>> Problem)> RecordInvocation)
        : StatelessMutator<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>
    {
        public override IReadOnlyList<int> Mutate(IReadOnlyList<int> parents, IRandomNumberGenerator random, DummySearchSpace<int> searchSpace, FuncProblem<int, DummySearchSpace<int>> problem)
        {
            RecordInvocation((parents, random, searchSpace, problem));
            return parents;
        }
    }

    private sealed record ConstantEvaluator(double Value) : StatelessEvaluator<int>
    {
        public override IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<int> candidates, IRandomNumberGenerator random) =>
            candidates.Select(_ => new ObjectiveVector(Value)).ToArray();
    }

    private sealed record ChecksumValue(int Value) : IOperator<Checksum, RealVector>
    {
        IExecutor IOperator<RealVector>.CreateExecutor<TSearchSpace, TProblem, TSearchState>(
            ResolutionScope<RealVector, TSearchSpace, TProblem, TSearchState> scope) => new Executor(Value);

        private sealed class Executor(int value) : IChecksumExecutor
        {
            public int Read(IRandomNumberGenerator random) => value;
        }
    }
}
