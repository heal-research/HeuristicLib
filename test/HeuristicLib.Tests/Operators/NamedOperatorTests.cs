using HEAL.HeuristicLib.Operators.Creators;
using HEAL.HeuristicLib.Operators.Mutators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Tests.TestSupport.Mocks;

namespace HEAL.HeuristicLib.Tests.Operators;

public sealed class NamedOperatorTests
{
    [Fact]
    public void NamedMutators_ShareTheirChildStateWithIndependentNameObservations()
    {
        var leaf = new AdvancingMutator();
        var exploration = leaf.Named("exploration");
        var refinement = leaf.Named("refinement");
        var explored = new CountAccumulator();
        var childCalls = new CountAccumulator();
        var scope = ResolutionScope.Create(
            Aspect.Create(Pointcut.Named<IMutator<int>>("exploration"), new CountingMutatorAdvice<int>(explored, OperatorCountMetric.Calls)),
            Aspect.CountCalls(leaf, childCalls));
        var typedScope = scope.For<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>();
        var problem = FuncProblem.Create(static (int candidate) => candidate, DummySearchSpace<int>.Instance, SingleObjective.Minimize);
        var random = RandomNumberGenerator.Create(1);

        var first = typedScope.Resolve(exploration);
        var second = typedScope.Resolve(refinement);
        first.Mutate([0], random, problem.SearchSpace, problem).ShouldBe([1]);
        second.Mutate([0], random, problem.SearchSpace, problem).ShouldBe([2]);
        typedScope.Resolve(leaf).Mutate([0], random, problem.SearchSpace, problem).ShouldBe([3]);
        first.Mutate([0], random, problem.SearchSpace, problem).ShouldBe([4]);

        explored.CurrentCount.ShouldBe(2);
        childCalls.CurrentCount.ShouldBe(4);
        Pointcut.Named<IMutator<int>>("exploration").Matches(leaf).ShouldBeFalse();
    }

    [Fact]
    public void NamedMutator_CopiesVaryTheLabelWithoutChangingChildIdentity()
    {
        var child = new AdvancingMutator();
        var original = child.Named("exploration");
        var copy = original with { Name = "refinement" };

        copy.ChildMutator.ShouldBeSameAs(child);
        original.Name.ShouldBe("exploration");
        Pointcut.Named<IMutator<int>>("exploration").Matches(copy).ShouldBeFalse();
        Pointcut.Named<IMutator<int>>("refinement").Matches(copy).ShouldBeTrue();
        Pointcut.Reference(original).Matches(copy).ShouldBeFalse();
    }

    [Fact]
    public void EveryNamedRole_ResolvesItsChildInTheSameScope()
    {
        var scope = ResolutionScope.Create().For<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>, PopulationState<int>>();
        ICreator<int> creator = new SequenceCreator();
        ICrossover<int> crossover = new SelectFirstParentCrossover<int>();
        IEvaluator<int> evaluator = new ProblemEvaluator<int>();
        IMutator<int> mutator = new AdvancingMutator();
        IRefiner<int> refiner = new NoChangeRefiner<int>();
        IReplacer<int> replacer = new CommaSelectionReplacer<int>();
        ISelector<int> selector = new TournamentSelector<int>(2);
        IInterceptor<int> interceptor = new IdentityInterceptor<int, PopulationState<int>>();
        ITerminator<int> terminator = new NeverTerminator<int>();
        IAlgorithm<int, PopulationState<int>> algorithm = new AdditiveStepAlgorithm(2);

        scope.Resolve(creator.Named("creator")).ShouldBeSameAs(scope.Resolve(creator));
        scope.Resolve(crossover.Named("crossover")).ShouldBeSameAs(scope.Resolve(crossover));
        scope.Resolve(evaluator.Named("evaluator")).ShouldBeSameAs(scope.Resolve(evaluator));
        scope.Resolve(mutator.Named("mutator")).ShouldBeSameAs(scope.Resolve(mutator));
        scope.Resolve(refiner.Named("refiner")).ShouldBeSameAs(scope.Resolve(refiner));
        scope.Resolve(replacer.Named("replacer")).ShouldBeSameAs(scope.Resolve(replacer));
        scope.Resolve(selector.Named("selector")).ShouldBeSameAs(scope.Resolve(selector));
        scope.Resolve(interceptor.Named("interceptor")).ShouldBeSameAs(scope.Resolve(interceptor));
        scope.Resolve(terminator.Named("terminator")).ShouldBeSameAs(scope.Resolve(terminator));

        var namedAlgorithm = algorithm.Named("algorithm");
        var namedExecution = scope.Resolve(namedAlgorithm);
        namedExecution.ShouldNotBeNull();
        Pointcut.Named<IAlgorithm<int, PopulationState<int>>>("algorithm").Matches(namedAlgorithm).ShouldBeTrue();
    }

    [Fact]
    public async Task NamedAlgorithm_AppliesAlgorithmAdviceToItsStream()
    {
        var algorithm = new AdditiveStepAlgorithm(2).Named("search");
        var calls = new CountAccumulator();
        var aspect = Aspect.Create(Pointcut.Named<IAlgorithm<int, PopulationState<int>>>("search"),
            new CountingAlgorithmAdvice<int, PopulationState<int>>(calls));
        var scope = ResolutionScope.Create(aspect).For<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>, PopulationState<int>>();
        var problem = FuncProblem.Create(static (int candidate) => candidate, DummySearchSpace<int>.Instance, SingleObjective.Minimize);

        var states = new List<PopulationState<int>>();
        await foreach (var state in scope.Resolve(algorithm).RunStreamingAsync(problem, RandomNumberGenerator.Create(1), ct: TestContext.Current.CancellationToken))
            states.Add(state);

        states.Single().Population.EvaluatedCandidates.Single().Candidate.ShouldBe(2);
        calls.CurrentCount.ShouldBe(1);
    }

    private sealed record SequenceCreator : StatelessCreator<int>
    {
        public override IReadOnlyList<int> Create(int count, IRandomNumberGenerator random) => Enumerable.Range(0, count).ToArray();
    }

    private sealed record AdvancingMutator : StatefulMutator<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>, MutationState>
    {
        protected override MutationState CreateInitialState() => new();

        protected override IReadOnlyList<int> Mutate(IReadOnlyList<int> parents, MutationState state, IRandomNumberGenerator random,
            DummySearchSpace<int> searchSpace, FuncProblem<int, DummySearchSpace<int>> problem) =>
            parents.Select(parent => parent + ++state.Calls).ToArray();
    }

    private sealed class MutationState
    {
        public int Calls { get; set; }
    }
}
