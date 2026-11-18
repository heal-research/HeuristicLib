using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Operators.Evaluators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.Tests.TestSupport.Mocks;

namespace HEAL.HeuristicLib.Tests.Analysis;

// Covers attaching analyzers with CreateRun and the check made when a run reads a definition.
public sealed class AnalyzerPlacementTests
{
    private static readonly IProblem<int, DummySearchSpace<int>> Problem = MetaAlgorithmTestHelpers.CreateIntegerProblem();

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public void ATargetTheGraphDoesNotHold_IsAnErrorNamingTheCurrentConfiguration()
    {
        var algorithm = new AdditiveStepAlgorithm(1);
        var elsewhere = new Counter();

        Should.Throw<InvalidOperationException>(() => algorithm.Substitute(new ProblemEvaluator<int>().Invocations.Counting(elsewhere))).Message.ShouldContain("configuration it receives");
    }

    [Fact]
    public void Observing_PassesEachObservationToTheCallback()
    {
        IOperator<Evaluation, int> evaluator = new ProblemEvaluator<int>();
        var batches = new List<int>();
        var observer = evaluator.Invocations.Observing(observation => batches.Add(observation.Candidates.Count));

        var executor = Resolve(ResolutionScope.Create(), evaluator.Substitute(observer));
        Evaluate(executor, 1, 2);
        Evaluate(executor, 3);

        batches.ShouldBe([2, 1]);
    }

    [Fact]
    public void ReadingADefinitionNeverAttached_IsAnError()
    {
        var algorithm = new AdditiveStepAlgorithm(1);
        var trace = algorithm.Evaluator.Invocations.Trace(_ => 1);
        var run = algorithm.CreateRun(Problem, RandomNumberGenerator.Create(1));

        Should.Throw<InvalidOperationException>(() => run.Result(trace)).Message.ShouldContain("CreateRun");
    }

    [Fact]
    public void ReadingADefinitionWhoseAttachedConfigurationWasDiscarded_IsAnError()
    {
        var algorithm = new AdditiveStepAlgorithm(1);
        var trace = algorithm.Evaluator.Invocations.Trace(_ => 1);
        _ = algorithm.Substitute(trace);
        var run = algorithm.CreateRun(Problem, RandomNumberGenerator.Create(1));

        Should.Throw<InvalidOperationException>(() => run.Result(trace)).Message.ShouldContain("CreateRun");
    }

    [Fact]
    public void AnAbsentCountingTargetFailsAtCreateRun_AndAnUnplacedCounterReadsZero()
    {
        var algorithm = new AdditiveStepAlgorithm(1);
        var evaluations = new Counter();
        var run = algorithm.CreateRun(Problem, RandomNumberGenerator.Create(1));

        run.Result(evaluations).CurrentCount.ShouldBe(0);
        var trace = algorithm.Evaluator.Invocations.Trace(_ => 1, [evaluations.Clock]);
        var traced = algorithm.CreateRun(Problem, RandomNumberGenerator.Create(1), [trace]);
        traced.Complete(cancellationToken: Ct);
        traced.Result(trace).ShouldAllBe(entry => entry.At(evaluations.Clock) == 0);
        Should.Throw<InvalidOperationException>(() => algorithm.CreateRun(Problem, RandomNumberGenerator.Create(1),
            [new ProblemEvaluator<int>().Invocations.Counting(evaluations)])).Message.ShouldContain("configuration it receives");
        algorithm.CreateRun(Problem, RandomNumberGenerator.Create(1), [algorithm.Evaluator.Invocations.Counting(evaluations)]).Result(evaluations).CurrentCount.ShouldBe(0);
    }

    // CreateRun places its analyzers with one Substitute call, which may also mix analyzers and plain substitutions.
    [Fact]
    public void Substitute_TakesAnalyzersAndSubstitutionsInOneCall()
    {
        var algorithm = new AdditiveStepAlgorithm(1);
        var evaluations = new Counter();
        var trace = algorithm.Evaluator.Invocations.Trace(_ => 1, [evaluations.Clock]);
        var marks = new Counter();
        var mark = Substitution.Of(algorithm.Evaluator, node => node.Counted(marks));

        var substituted = algorithm.Substitute(trace, mark, algorithm.Evaluator.Invocations.Counting(evaluations));

        substituted.ShouldBe(algorithm with { Evaluator = algorithm.Evaluator.Counted(evaluations).Counted(marks).Observed(trace) });
        algorithm.Substitute(trace, mark, algorithm.Evaluator.Invocations.Counting(evaluations)).ShouldBe(substituted);
    }

    [Fact]
    public void AnAttachedDefinition_IsReadableBeforeTheRunReachesIt()
    {
        var algorithm = new AdditiveStepAlgorithm(1);
        var trace = algorithm.Evaluator.Invocations.Trace(_ => 1);
        var run = algorithm.CreateRun(Problem, RandomNumberGenerator.Create(1), [trace]);

        run.Result(trace).SampleCount.ShouldBe(0);
    }

    [Fact]
    public void ADefinitionThatNeedsNoPlacement_IsReadable()
    {
        var run = new AdditiveStepAlgorithm(1).CreateRun(Problem, RandomNumberGenerator.Create(1));

        run.Result(new NoPlacement()).ShouldNotBeNull();
        run.Result(new Counter()).CurrentCount.ShouldBe(0);
    }

    [Fact]
    public async Task ADelegateInTheGraph_MayHaveAttachedTheDefinition()
    {
        var inner = new AdditiveStepAlgorithm(1);
        var trace = inner.Evaluator.Invocations.Trace(_ => 1);
        var run = new WithDelegate(inner, () => 1).CreateRun(Problem, RandomNumberGenerator.Create(1));

        await run.CompleteAsync(cancellationToken: Ct);

        run.Result(trace).SampleCount.ShouldBe(0);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CallbackCachesItsSubstitutionAndRecordsThroughBothPlacementPaths(bool substitute)
    {
        var algorithm = new AdditiveStepAlgorithm(3);
        var values = new List<long>();
        var callback = algorithm.Iterations.Observing(observation => values.Add(observation.Iteration));

        callback.Expand().ShouldBeSameAs(callback.Expand());
        callback.Expand().Single().ShouldBeSameAs(callback.Expand().Single());
        IAlgorithm<int, PopulationState<int>> receiver = algorithm;
        var run = substitute
            ? receiver.Substitute(callback).CreateRun(Problem, RandomNumberGenerator.Create(1))
            : receiver.CreateRun(Problem, RandomNumberGenerator.Create(1), [callback]);
        run.Complete(cancellationToken: TestContext.Current.CancellationToken);

        values.ShouldBe([1L]);
    }

    private static IEvaluationExecutor<int> Resolve(ResolutionScope scope, IOperator<Evaluation, int> evaluator) =>
        scope.For<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>>(Problem).Resolve(evaluator);

    private static void Evaluate(IEvaluationExecutor<int> executor, params int[] candidates) =>
        executor.Evaluate(candidates, RandomNumberGenerator.Create(1));

    private sealed class NoPlacement : Analyzer<CountAccumulator>
    {
        protected override IReadOnlyList<ISubstitution> CreateSubstitutions() => [];

        public override CountAccumulator CreateExecutionNode(ResolutionScope scope) => new();
    }

    // Holds a delegate as a declared parameter, as a node building its children does.
    private sealed record WithDelegate(IAlgorithm<int, PopulationState<int>> ChildAlgorithm, Func<int> Build)
        : WrappingAlgorithm<int, PopulationState<int>>(ChildAlgorithm)
    {
        public static Parameter<WithDelegate, Func<int>> BuildParameter { get; } =
            new(nameof(Build), static node => node.Build, static (node, value) => node with { Build = value });

        public override IReadOnlyList<IParameter> GetParameters() => [.. base.GetParameters(), BuildParameter];

        protected override IAlgorithmExecutor<PopulationState<int>> CreateExecutor<TSearchSpace, TProblem>(
            ResolutionScope<int, TSearchSpace, TProblem, PopulationState<int>> scope) => scope.Resolve(ChildAlgorithm);
    }
}
