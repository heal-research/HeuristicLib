using HEAL.HeuristicLib.Inspection;
using HEAL.HeuristicLib.Operators.Evaluators;
using HEAL.HeuristicLib.Operators.Mutators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Tests.TestSupport.Mocks;

namespace HEAL.HeuristicLib.Tests.Analysis;

/// <summary>
/// Covers the observation sources: each operation's observation reads the search space and problem
/// at the candidate's interfaces, so it fits every run over the candidate.
/// </summary>
public class ObservationModuleTests
{
    [Fact]
    public void AMutatorObservation_CapturesTheCallWithTheRunsSearchSpaceAndProblem()
    {
        var mutator = new AddOneMutator();
        var problem = CreateProblem();
        var observations = new List<MutationObservation<int>>();
        var scope = ResolutionScope.Create(Aspect.Observe(mutator.Invocations, observations.Add));

        var offspring = scope.Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>(mutator)
            .Mutate([1, 2], RandomNumberGenerator.Create(1), problem.SearchSpace, problem);

        var observation = observations.ShouldHaveSingleItem();
        observation.Mutator.ShouldBeSameAs(mutator);
        observation.Parents.ShouldBe([1, 2]);
        observation.Offspring.ShouldBeSameAs(offspring);
        observation.SearchSpace.ShouldBeSameAs(problem.SearchSpace);
        observation.Problem.ShouldBeSameAs(problem);
    }

    /// <summary>
    /// Observing an operator written for a narrower problem than the run supplies leaves the operator's own check in
    /// place, so resolving it fails with the mismatch every authoring base reports.
    /// </summary>
    [Fact]
    public void AnObservationWrittenForANarrowerProblem_IsReportedWhenTheGraphIsResolved()
    {
        IOperator<Mutation, int> mutator = new AddOneMutator();
        var scope = ResolutionScope.Create(Aspect.Observe(mutator.Invocations, _ => { }));

        var exception = Should.Throw<InvalidOperationException>(() =>
            scope.Resolve<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>>(mutator));

        exception.Message.ShouldContain("cannot run over");
    }

    /// <summary>
    /// A mismatch names generic types with their type arguments rather than by metadata name, so the run's types and the
    /// types the operator was written for can be told apart.
    /// </summary>
    [Fact]
    public void AMismatch_NamesGenericTypesWithTheirTypeArguments()
    {
        IOperator<Mutation, int> mutator = new AddOneMutator();
        var scope = ResolutionScope.Create(Aspect.Observe(mutator.Invocations, _ => { }));

        var exception = Should.Throw<InvalidOperationException>(() =>
            scope.Resolve<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>>(mutator));

        exception.Message.ShouldNotContain("`");
        exception.Message.ShouldContain("FuncProblem<Int32, DummySearchSpace<Int32>>");
        exception.Message.ShouldContain("IProblem<Int32, DummySearchSpace<Int32>>");
    }

    /// <summary>
    /// An observation reports the configuration it was declared for, even when a configuration wrapper such as a
    /// budget wraps it more tightly, and the two wrappers both act.
    /// </summary>
    [Fact]
    public void AnEvaluatorObservation_ReportsTheObservedEvaluatorAndComposesWithABudget()
    {
        IOperator<Evaluation, int> evaluator = new ProblemEvaluator<int>();
        var problem = CreateProblem();
        var counter = new CountAccumulator();
        var observed = new List<IOperator<Evaluation, int>>();
        var root = ResolutionScope.Create(Aspect.Observe(evaluator.Invocations, observation => observed.Add(observation.Evaluator)));
        var budget = root.CreateChildScope(Aspect.CountInvocations(evaluator, counter));

        budget.Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>(evaluator)
            .Evaluate([3], RandomNumberGenerator.Create(1), problem.SearchSpace, problem);

        counter.CurrentCount.ShouldBe(1);
        observed.ShouldHaveSingleItem().ShouldBeSameAs(evaluator);
    }

    [Fact]
    public async Task AnAlgorithmObservation_NumbersIterationsPerCallAndCarriesTheStateItFollowed()
    {
        var algorithm = new AdditiveStepAlgorithm(2);
        var problem = CreateProblem();
        var observations = new List<AlgorithmObservation<int, PopulationState<int>>>();
        var scope = ResolutionScope.Create(Aspect.Observe(algorithm.Iterations, observations.Add));
        var executor = scope.Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>, PopulationState<int>>(algorithm);

        var states = new List<PopulationState<int>>();
        await foreach (var state in executor.RunStreamingAsync(problem, RandomNumberGenerator.Create(1), ct: TestContext.Current.CancellationToken))
            states.Add(state);
        await foreach (var state in executor.RunStreamingAsync(problem, RandomNumberGenerator.Create(2), states[^1], TestContext.Current.CancellationToken))
            states.Add(state);

        observations.Select(observation => observation.Iteration).ShouldBe([1L, 1L]);
        observations[0].PreviousState.ShouldBeNull();
        observations[1].PreviousState.ShouldBeSameAs(states[0]);
        observations.Select(observation => observation.State).ShouldBe(states);
        observations.ShouldAllBe(observation => ReferenceEquals(observation.Algorithm, algorithm));
    }

    /// <summary>
    /// The interceptor names its search state, so its observation reads the states it transforms at that type.
    /// </summary>
    [Fact]
    public void AnInterceptorObservation_ReadsTheStatesAtTheInterceptorsSearchState()
    {
        IOperator<Interception<PopulationState<int>>, int> interceptor = new IdentityInterceptor<int, PopulationState<int>>();
        var problem = CreateProblem();
        var state = Population.From([EvaluatedCandidate.From(4, 4)]).ToPopulationState();
        var observed = new List<PopulationState<int>>();
        var scope = ResolutionScope.Create(Aspect.Observe(interceptor.Invocations, observation => observed.Add(observation.State)));

        scope.Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>, PopulationState<int>>(interceptor)
            .Transform(state, null, RandomNumberGenerator.Create(1), problem.SearchSpace, problem);

        observed.ShouldHaveSingleItem().ShouldBeSameAs(state);
    }

    /// <summary>
    /// A method group with one overload per observation binds to each source, because the source fixes the observation.
    /// </summary>
    [Fact]
    public void AnObservation_AcceptsMethodGroups()
    {
        IOperator<Evaluation, int> evaluator = new ProblemEvaluator<int>();
        IOperator<Mutation, int> mutator = new AddOneMutator();
        var algorithm = new AdditiveStepAlgorithm(2);
        var problem = CreateProblem();
        var recorder = new Recorder();
        var scope = ResolutionScope.Create(Aspect.Observe(evaluator.Invocations, recorder.Record),
            Aspect.Observe(mutator.Invocations, recorder.Record),
            Aspect.Observe(algorithm.Iterations, recorder.Record));

        scope.Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>(evaluator)
            .Evaluate([3], RandomNumberGenerator.Create(1), problem.SearchSpace, problem);
        scope.Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>(mutator)
            .Mutate([1], RandomNumberGenerator.Create(1), problem.SearchSpace, problem);

        recorder.Problems.ShouldBe([problem, problem]);
    }

    private sealed class Recorder
    {
        public List<IProblem> Problems { get; } = [];

        public void Record(EvaluationObservation<int> observation) =>
            Problems.Add(observation.Problem);

        public void Record(MutationObservation<int> observation) =>
            Problems.Add(observation.Problem);

        public void Record(AlgorithmObservation<int, PopulationState<int>> observation) =>
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
