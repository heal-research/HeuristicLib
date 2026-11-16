using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators.Creators;
using HEAL.HeuristicLib.Operators.Mutators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Tests.TestSupport.Mocks;

namespace HEAL.HeuristicLib.Tests.Operators;

public sealed class NamedOperatorTests
{
    [Fact]
    public void NamedMutators_ShareTheirChildInAScopeAndObserveTheirNames()
    {
        var leaf = new AdvancingMutator();
        IOperator<Mutation, int> exploration = leaf.Named("exploration");
        IOperator<Mutation, int> refinement = leaf.Named("refinement");
        Counter explored = new(), childCalls = new();
        var countingExploration = CountingNamed("exploration", explored);
        ISubstitutionBundle[] observing = [Substitution.Counting(leaf, childCalls), countingExploration];
        var scope = ResolutionScope.Create();
        var problem = FuncProblem.Create(static (int candidate) => candidate, DummySearchSpace<int>.Instance, SingleObjective.Minimize);
        var typedScope = scope.For<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>(problem);
        var random = RandomNumberGenerator.Create(1);

        var first = typedScope.Resolve(exploration.Substitute(observing));
        var second = typedScope.Resolve(refinement.Substitute(observing));
        first.Mutate([0], random).ShouldBe([1]);
        second.Mutate([0], random).ShouldBe([2]);
        typedScope.Resolve(leaf.Counted(childCalls)).Mutate([0], random).ShouldBe([3]);
        first.Mutate([0], random).ShouldBe([4]);

        scope.Results.Result(explored).CurrentCount.ShouldBe(2);
        scope.Results.Result(childCalls).CurrentCount.ShouldBe(4);
        leaf.Substitute(countingExploration).ShouldBeSameAs(leaf);
    }

    [Fact]
    public void NamedMutator_CopiesVaryTheLabelWithoutChangingChildIdentity()
    {
        var child = new AdvancingMutator();
        var original = child.Named("exploration");
        var copy = original with { Name = "refinement" };

        copy.ChildOperator.ShouldBeSameAs(child);
        original.Name.ShouldBe("exploration");
        copy.Name.ShouldBe("refinement");
        copy.ShouldNotBeSameAs(original);
    }

    [Fact]
    public void EveryNamedRole_ResolvesItsChildInTheSameScope()
    {
        // The name resolves its child in the same scope, so the named and the plain mutator share the child's state.
        var problem = FuncProblem.Create(static (int candidate) => candidate, DummySearchSpace<int>.Instance, SingleObjective.Minimize);
        var scope = ResolutionScope.Create().For<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>, PopulationState<int>>(problem);
        IOperator<Creation, int> creator = new SequenceCreator();
        IOperator<Crossover, int> crossover = new SelectFirstParentCrossover<int>();
        IOperator<Evaluation, int> evaluator = new ProblemEvaluator<int>();
        IOperator<Mutation, int> mutator = new AdvancingMutator();
        IOperator<Refinement, int> refiner = new NoChangeRefiner<int>();
        IOperator<Replacement, int> replacer = new CommaSelectionReplacer<int>();
        IOperator<Selection, int> selector = new TournamentSelector<int>(2);
        IOperator<Interception<PopulationState<int>>, int> interceptor = new IdentityInterceptor<int, PopulationState<int>>();
        IOperator<Termination, int> terminator = new NeverTerminator<int>();
        IAlgorithm<int, PopulationState<int>> algorithm = new AdditiveStepAlgorithm(2);

        scope.Resolve(creator.Named("creator")).ShouldBeSameAs(scope.Resolve(creator));
        scope.Resolve(crossover.Named("crossover")).ShouldBeSameAs(scope.Resolve(crossover));
        scope.Resolve(evaluator.Named("evaluator")).ShouldBeSameAs(scope.Resolve(evaluator));
        scope.Resolve(mutator.Named("mutator")).Mutate([0], RandomNumberGenerator.Create(1)).ShouldBe([1]);
        scope.Resolve(mutator).Mutate([0], RandomNumberGenerator.Create(1)).ShouldBe([2]);
        scope.Resolve(refiner.Named("refiner")).ShouldBeSameAs(scope.Resolve(refiner));
        scope.Resolve(replacer.Named("replacer")).ShouldBeSameAs(scope.Resolve(replacer));
        scope.Resolve(selector.Named("selector")).ShouldBeSameAs(scope.Resolve(selector));
        scope.Resolve(interceptor.Named("interceptor")).ShouldBeSameAs(scope.Resolve(interceptor));
        scope.Resolve(terminator.Named("terminator")).ShouldBeSameAs(scope.Resolve(terminator));

        var namedAlgorithm = algorithm.Named("algorithm");
        var namedExecutor = scope.Resolve(namedAlgorithm);
        namedExecutor.ShouldNotBeNull();
        namedAlgorithm.Name.ShouldBe("algorithm");
    }

    [Fact]
    public async Task NamedAlgorithm_IsSelectedByItsNameAndWrappedAsAWhole()
    {
        IAlgorithm<int, PopulationState<int>> algorithm = new AdditiveStepAlgorithm(2).Named("search");
        var calls = new Counter();
        var counted = algorithm.Substitute(Substitution.Of<IAlgorithm<int, PopulationState<int>>>(
            selected => selected is INamedConfiguration { Name: "search" },
            selected => new CountedAlgorithm<int, PopulationState<int>, AlgorithmCallObservation<int, PopulationState<int>>>(selected, calls)));
        var root = ResolutionScope.Create();
        var problem = FuncProblem.Create(static (int candidate) => candidate, DummySearchSpace<int>.Instance, SingleObjective.Minimize);
        var scope = root.For<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>, PopulationState<int>>(problem);

        var states = new List<PopulationState<int>>();
        await foreach (var state in scope.Resolve(counted).RunStreamingAsync(RandomNumberGenerator.Create(1), ct: TestContext.Current.CancellationToken))
            states.Add(state);

        states.Single().Population.EvaluatedCandidates.Single().Candidate.ShouldBe(2);
        root.Results.Result(calls).CurrentCount.ShouldBe(1);
    }

    // Counts the calls of every mutator carrying the name.
    private static Substitution CountingNamed(string name, Counter counter) =>
        Substitution.Of<IOperator<Mutation, int>>(mutator => mutator is INamedConfiguration named && named.Name == name, mutator => mutator.Counted(counter));

    private sealed record SequenceCreator : StatelessCreator<int>
    {
        public override IReadOnlyList<int> Create(int count, IRandomNumberGenerator random) => Enumerable.Range(0, count).ToArray();
    }

    private sealed record AdvancingMutator : Mutator<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>
    {

        protected override IMutationExecutor<int> CreateExecutor<TRunSearchState>(ResolutionScope<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>, TRunSearchState> scope) =>
        new Executor(scope.Problem);

        private sealed class Executor
            : MutationExecutor<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>
        {
            public Executor(FuncProblem<int, DummySearchSpace<int>> problem) : base(problem) { }

            private int invocations;

            public override IReadOnlyList<int> Mutate(IReadOnlyList<int> parents, IRandomNumberGenerator random) =>
            parents.Select(parent => parent + ++invocations).ToArray();
        }
    }

}
