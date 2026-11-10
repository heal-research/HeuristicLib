using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Operators.Evaluators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.Tests.TestSupport.Mocks;

namespace HEAL.HeuristicLib.Tests.Analysis;

// Covers attaching analyzers with AnalyzedBy and the check made when a run reads a definition.
public sealed class AnalysisPlacementTests
{
    private static readonly IProblem<int, DummySearchSpace<int>> Problem = MetaAlgorithmTestHelpers.CreateIntegerProblem();

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public void ATargetTheGraphDoesNotHold_IsAnErrorNamingAnalyzeFirst()
    {
        var algorithm = new AdditiveStepAlgorithm(1);
        var elsewhere = new ProblemEvaluator<int>().Invocations.Count();

        Should.Throw<InvalidOperationException>(() => algorithm.AnalyzedBy(elsewhere)).Message.ShouldContain("analyze first");
    }

    [Fact]
    public void Observing_PassesEachObservationToTheCallback()
    {
        var evaluator = new ProblemEvaluator<int>();
        var batches = new List<int>();
        var observer = evaluator.Invocations.Observing(observation => batches.Add(observation.Candidates.Count));

        var executor = Resolve(ResolutionScope.Create(), evaluator.AnalyzedBy(observer));
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

        Should.Throw<InvalidOperationException>(() => run.Result(trace)).Message.ShouldContain("AnalyzedBy");
    }

    [Fact]
    public void ReadingADefinitionWhoseAttachedConfigurationWasDiscarded_IsAnError()
    {
        var algorithm = new AdditiveStepAlgorithm(1);
        var trace = algorithm.Evaluator.Invocations.Trace(_ => 1);
        _ = algorithm.AnalyzedBy(trace);
        var run = algorithm.CreateRun(Problem, RandomNumberGenerator.Create(1));

        Should.Throw<InvalidOperationException>(() => run.Result(trace)).Message.ShouldContain("AnalyzedBy");
    }

    [Fact]
    public void AnAttachedDefinition_IsReadableBeforeTheRunReachesIt()
    {
        var algorithm = new AdditiveStepAlgorithm(1);
        var trace = algorithm.Evaluator.Invocations.Trace(_ => 1);
        var run = algorithm.AnalyzedBy(trace).CreateRun(Problem, RandomNumberGenerator.Create(1));

        run.Result(trace).SampleCount.ShouldBe(0);
    }

    [Fact]
    public void AnInstrumentThatNeedsNoPlacement_IsReadable()
    {
        var run = new AdditiveStepAlgorithm(1).CreateRun(Problem, RandomNumberGenerator.Create(1));

        run.Result(new NoPlacement()).ShouldNotBeNull();
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

    private static IEvaluationExecutor<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>> Resolve(ResolutionScope scope, IOperator<Evaluation, int> evaluator) =>
        scope.For<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>>().Resolve(evaluator);

    private static void Evaluate(IEvaluationExecutor<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>> executor, params int[] candidates) =>
        executor.Evaluate(candidates, RandomNumberGenerator.Create(1), DummySearchSpace<int>.Instance, Problem);

    private sealed class NoPlacement : Instrument<object>
    {
        protected override IReadOnlyList<Substitution> CreateSubstitutions() => [];

        protected override object CreateResult(RunResults results) => new();
    }

    // Holds a delegate as a declared parameter, as a node building its children does.
    private sealed record WithDelegate(IAlgorithm<int, PopulationState<int>> ChildAlgorithm, Func<int> Build)
        : WrappingAlgorithm<int, PopulationState<int>>(ChildAlgorithm)
    {
        public static Parameter<WithDelegate, Func<int>> BuildParameter { get; } =
            new(nameof(Build), static node => node.Build, static (node, value) => node with { Build = value });

        public override IReadOnlyList<IParameter> GetParameters() => [.. base.GetParameters(), BuildParameter];

        protected override IAlgorithmExecutor<int, TSearchSpace, TProblem, PopulationState<int>> CreateExecutor<TSearchSpace, TProblem>(
            ResolutionScope<int, TSearchSpace, TProblem, PopulationState<int>> scope) => scope.Resolve(ChildAlgorithm);
    }
}
