using HEAL.HeuristicLib.Operators.Evaluators;
using HEAL.HeuristicLib.Operators.Mutators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.Tests.TestSupport.Mocks;

namespace HEAL.HeuristicLib.Tests.Analysis;

/// <summary>
/// Covers the observation hooks at the reduced arity: a role configuration names only its candidate, so the
/// observation names the search space and problem it reads, and a run is checked against them when it resolves.
/// </summary>
public class ObservationHookTests
{
    [Fact]
    public void AMutatorObservation_CapturesTheCallWithTheRunsSearchSpaceAndProblem()
    {
        var mutator = new AddOneMutator();
        var problem = CreateProblem();
        var observations = new List<MutatorObservation<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>>();
        var resolver = ExecutionInstanceResolver.Create(builder =>
            builder.Observe<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>(mutator, observations.Add));

        var offspring = resolver.Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>(mutator)
            .Mutate([1, 2], RandomNumberGenerator.Create(1), problem.SearchSpace, problem);

        var observation = observations.ShouldHaveSingleItem();
        observation.Mutator.ShouldBeSameAs(mutator);
        observation.Parents.ShouldBe([1, 2]);
        observation.Offspring.ShouldBeSameAs(offspring);
        observation.SearchSpace.ShouldBeSameAs(problem.SearchSpace);
        observation.Problem.ShouldBeSameAs(problem);
    }

    /// <summary>
    /// An observation written for a narrower problem than the run supplies cannot read the run's problem as its own,
    /// so resolving it fails with the mismatch every authoring base reports.
    /// </summary>
    [Fact]
    public void AnObservationWrittenForANarrowerProblem_IsReportedWhenTheGraphIsResolved()
    {
        IMutator<int> mutator = new AddOneMutator();
        var resolver = ExecutionInstanceResolver.Create(builder =>
            builder.Observe<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>(mutator, _ => { }));

        var exception = Should.Throw<InvalidOperationException>(() =>
            resolver.Resolve<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>>(mutator));

        exception.Message.ShouldContain("cannot run over");
    }

    /// <summary>
    /// A mismatch names generic types with their type arguments rather than by metadata name, so the wrapper and the
    /// types it was written for can be told apart.
    /// </summary>
    [Fact]
    public void AMismatch_NamesGenericTypesWithTheirTypeArguments()
    {
        IMutator<int> mutator = new AddOneMutator();
        var resolver = ExecutionInstanceResolver.Create(builder =>
            builder.Observe<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>(mutator, _ => { }));

        var exception = Should.Throw<InvalidOperationException>(() =>
            resolver.Resolve<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>>(mutator));

        exception.Message.ShouldNotContain("`");
        exception.Message.ShouldContain("FuncProblem<Int32, DummySearchSpace<Int32>>");
        exception.Message.ShouldContain("IProblem<Int32, DummySearchSpace<Int32>>");
    }

    /// <summary>
    /// An observation reports the configuration it was declared for, even when a configuration decoration such as a
    /// budget wraps it more tightly, and the two decorations both act.
    /// </summary>
    [Fact]
    public void AnEvaluatorObservation_ReportsTheObservedEvaluatorAndComposesWithABudget()
    {
        IEvaluator<int> evaluator = new ProblemEvaluator<int>();
        var problem = CreateProblem();
        var counter = new ObservationCounter();
        var observed = new List<IEvaluator<int>>();
        var root = ExecutionInstanceResolver.Create(builder =>
            builder.Observe<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>(evaluator, observation => observed.Add(observation.Evaluator)));
        var budget = root.CreateChildResolver(builder => builder.Decorate<IEvaluator<int>>(evaluator, current => current.CountCalls(counter)));

        budget.Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>(evaluator)
            .Evaluate([3], RandomNumberGenerator.Create(1), problem.SearchSpace, problem);

        counter.CurrentCount.ShouldBe(1);
        observed.ShouldHaveSingleItem().ShouldBeSameAs(evaluator);
    }

    [Fact]
    public async Task AnAlgorithmObservation_NumbersIterationsAndCarriesTheStateItFollowed()
    {
        var algorithm = new AdditiveStepAlgorithm(2);
        var problem = CreateProblem();
        var observations = new List<AlgorithmObservation<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>, PopulationState<int>>>();
        var resolver = ExecutionInstanceResolver.Create(builder =>
            builder.Observe<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>, PopulationState<int>>(algorithm, observations.Add));
        var instance = resolver.Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>, PopulationState<int>>(algorithm);

        var states = new List<PopulationState<int>>();
        await foreach (var state in instance.RunStreamingAsync(problem, RandomNumberGenerator.Create(1), ct: TestContext.Current.CancellationToken))
            states.Add(state);
        await foreach (var state in instance.RunStreamingAsync(problem, RandomNumberGenerator.Create(2), states[^1], TestContext.Current.CancellationToken))
            states.Add(state);

        observations.Select(observation => observation.Iteration).ShouldBe([1L, 2L]);
        observations[0].PreviousState.ShouldBeNull();
        observations[1].PreviousState.ShouldBeSameAs(states[0]);
        observations.Select(observation => observation.State).ShouldBe(states);
        observations.ShouldAllBe(observation => ReferenceEquals(observation.Algorithm, algorithm));
    }

    /// <summary>
    /// An implicitly typed lambda cannot name the search space and problem, so it binds to the overload typed at the
    /// interfaces, which fits a run over any search space and problem for the candidate.
    /// </summary>
    [Fact]
    public void AnImplicitlyTypedLambda_ObservesWithoutNamingTheSearchSpaceOrProblem()
    {
        IMutator<int> mutator = new AddOneMutator();
        var problem = CreateProblem();
        var offspring = new List<int>();
        var resolver = ExecutionInstanceResolver.Create(builder =>
            builder.Observe(mutator, observation => offspring.AddRange(observation.Offspring)));

        resolver.Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>(mutator)
            .Mutate([1, 2], RandomNumberGenerator.Create(1), problem.SearchSpace, problem);

        offspring.ShouldBe([2, 3]);
    }

    /// <summary>
    /// The interceptor carries no search state, so the overload typed at the interfaces reads the states it transforms
    /// as <see cref="ISearchState"/>.
    /// </summary>
    [Fact]
    public void AnInterceptorObservation_WithoutNamedTypes_ReadsTheStatesAsSearchStates()
    {
        IInterceptor<int> interceptor = new IdentityInterceptor<int, PopulationState<int>>();
        var problem = CreateProblem();
        var state = Population.From([EvaluatedCandidate.From(4, 4)]).ToPopulationState();
        var observed = new List<ISearchState>();
        var resolver = ExecutionInstanceResolver.Create(builder =>
            builder.Observe(interceptor, observation => observed.Add(observation.State)));

        resolver.Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>, PopulationState<int>>(interceptor)
            .Transform(state, null, RandomNumberGenerator.Create(1), problem.SearchSpace, problem);

        observed.ShouldHaveSingleItem().ShouldBeSameAs(state);
    }

    /// <summary>
    /// Binding the types once lets a method group typed at concrete types be passed without naming them at every call,
    /// and the observation still checks the run against those types.
    /// </summary>
    [Fact]
    public void ABoundBuilder_AcceptsMethodGroupsTypedAtTheBoundTypes()
    {
        IEvaluator<int> evaluator = new ProblemEvaluator<int>();
        IMutator<int> mutator = new AddOneMutator();
        var algorithm = new AdditiveStepAlgorithm(2);
        var problem = CreateProblem();
        var recorder = new BoundRecorder();
        var resolver = ExecutionInstanceResolver.Create(builder =>
        {
            var typed = builder.For<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>();
            typed.Observe(evaluator, recorder.Record);
            typed.Observe(mutator, recorder.Record);
            typed.Observe(algorithm, recorder.Record);
        });

        resolver.Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>(evaluator)
            .Evaluate([3], RandomNumberGenerator.Create(1), problem.SearchSpace, problem);
        resolver.Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>(mutator)
            .Mutate([1], RandomNumberGenerator.Create(1), problem.SearchSpace, problem);

        recorder.Problems.ShouldBe([problem, problem]);
        Should.Throw<InvalidOperationException>(() =>
            resolver.Resolve<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>>(mutator));
    }

    private sealed class BoundRecorder
    {
        public List<FuncProblem<int, DummySearchSpace<int>>> Problems { get; } = [];

        public void Record(EvaluatorObservation<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>> observation) =>
            Problems.Add(observation.Problem);

        public void Record(MutatorObservation<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>> observation) =>
            Problems.Add(observation.Problem);

        public void Record(AlgorithmObservation<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>, PopulationState<int>> observation) =>
            Problems.Add(observation.Problem);
    }

    private static FuncProblem<int, DummySearchSpace<int>> CreateProblem() =>
        FuncProblem.Create(
            evaluateFunc: static (int candidate) => candidate,
            encoding: DummySearchSpace<int>.Instance,
            objective: SingleObjective.Minimize);

    private sealed record AddOneMutator
        : StatelessMutator<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>
    {
        public override IReadOnlyList<int> Mutate(
            IReadOnlyList<int> parents,
            IRandomNumberGenerator random,
            DummySearchSpace<int> searchSpace,
            FuncProblem<int, DummySearchSpace<int>> problem) =>
            parents.Select(parent => parent + 1).ToArray();
    }
}
