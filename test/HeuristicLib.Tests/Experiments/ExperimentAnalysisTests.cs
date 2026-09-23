using HEAL.HeuristicLib.Experiments;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Tests.Experiments.TestSupport;
using HEAL.HeuristicLib.Tests.TestSupport.Mocks;

namespace HEAL.HeuristicLib.Tests.Experiments;

public class ExperimentAnalysisTests
{
    [Fact]
    public async Task MultipleTrialAnalyzers_ObserveTheSameOperatorInAttachmentOrder()
    {
        var invocationOrder = new List<int>();
        var evaluator = new CountingResolutionEvaluator();
        var algorithm = new CountingInstanceAlgorithm(1, evaluator);
        var experiment = algorithm.Repeat(2);
        var first = TrialAnalyzer.Create((CountingInstanceAlgorithm alg) => new OrderedEvaluationAnalyzer(alg.Evaluator, 1, invocationOrder));
        var second = TrialAnalyzer.Create((CountingInstanceAlgorithm alg) => new OrderedEvaluationAnalyzer(alg.Evaluator, 2, invocationOrder));
        var run = experiment.CreateRun(MetaAlgorithmTestHelpers.CreateIntegerProblem(), RandomNumberGenerator.Create(42)).AddTrialAnalyzer(first).AddTrialAnalyzer(second);

        Should.Throw<InvalidOperationException>(() =>
            experiment.CreateRun(MetaAlgorithmTestHelpers.CreateIntegerProblem(), RandomNumberGenerator.Create(42)).AddTrialAnalyzer(first).AddTrialAnalyzer(first));

        var stream = run.Stream(cancellationToken: TestContext.Current.CancellationToken);
        run.GetAnalyzers(first).ShouldAllBe(analysis => analysis.Analyzer.Result.Count == 0);
        _ = await stream.ToListAsync(TestContext.Current.CancellationToken);

        invocationOrder.ShouldBe([1, 2, 1, 2]);
        run.GetAnalyzers(first).Select(analysis => analysis.Trial.Key).ShouldBe([0, 1]);
        run.GetAnalyzers(first).ShouldAllBe(analysis => analysis.Analyzer.Result.Count == 1);
        run.GetAnalyzers(second).ShouldAllBe(analysis => analysis.Analyzer.Result.Count == 1);
    }

    [Fact]
    public async Task FailingAnalyzerFactory_AttachesNoPartialAnalysis()
    {
        var invocations = new List<int>();
        var firstAlgorithm = new CountingInstanceAlgorithm(1, new CountingResolutionEvaluator());
        var secondAlgorithm = new CountingInstanceAlgorithm(2, new CountingResolutionEvaluator());
        var experiment = new FixedExperiment<CountingInstanceAlgorithm>([
            ExperimentCase.From(firstAlgorithm, 0, [0]),
            ExperimentCase.From(secondAlgorithm, 1, [1])
        ]);
        var failing = TrialAnalyzer.Create(
            (CountingInstanceAlgorithm algorithm) => algorithm.Increment == 2
                ? throw new InvalidOperationException("Analyzer creation failed.")
                : new OrderedEvaluationAnalyzer(algorithm.Evaluator, 1, invocations));

        Should.Throw<InvalidOperationException>(() =>
            experiment.CreateRun(MetaAlgorithmTestHelpers.CreateIntegerProblem(), RandomNumberGenerator.Create(42)).AddTrialAnalyzer(failing));

        var run = ExperimentTestSupport.CreateRun(experiment);
        _ = await run.CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        invocations.ShouldBeEmpty();
    }

    [Fact]
    public async Task ExistingTrialAnalyzer_CanBeAttachedToAnotherRun()
    {
        var invocations = new List<int>();
        var algorithm = new CountingInstanceAlgorithm(1, new CountingResolutionEvaluator());
        var experiment = algorithm.Repeat(2);
        var trialAnalyzer = TrialAnalyzer.Create((CountingInstanceAlgorithm alg) => new OrderedEvaluationAnalyzer(alg.Evaluator, 1, invocations));
        _ = experiment.CreateRun(MetaAlgorithmTestHelpers.CreateIntegerProblem(), RandomNumberGenerator.Create(1)).AddTrialAnalyzer(trialAnalyzer);
        var run = experiment.CreateRun(MetaAlgorithmTestHelpers.CreateIntegerProblem(), RandomNumberGenerator.Create(2)).AddTrialAnalyzer(trialAnalyzer);

        _ = await run.CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        run.GetAnalyzers(trialAnalyzer).Select(analysis => analysis.Trial.Key).ShouldBe([0, 1]);
        run.GetAnalyzers(trialAnalyzer).ShouldAllBe(analysis => analysis.Analyzer.Result.Count == 1);
    }

    [Fact]
    public async Task TrialAnalyzers_CollectPerTrialAsEachTrialCompletes()
    {
        var algorithm = new CountingInstanceAlgorithm(1, new CountingResolutionEvaluator());
        var trialAnalyzer = TrialAnalyzer.Create((CountingInstanceAlgorithm alg) => new OrderedEvaluationAnalyzer(alg.Evaluator, 1, []));
        var run = algorithm.Repeat(2)
            .CreateRun(MetaAlgorithmTestHelpers.CreateIntegerProblem(), RandomNumberGenerator.Create(42)).AddTrialAnalyzer(trialAnalyzer);

        _ = await run.Trials[0].Run.CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        // A trial that has not completed simply has no data yet.
        run.GetAnalyzers(trialAnalyzer)[1].Analyzer.Result.Count.ShouldBe(0);

        _ = await run.Trials[1].Run.CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        run.GetAnalyzers(trialAnalyzer).ShouldAllBe(analysis => analysis.Analyzer.Result.Count == 1);
    }

    private sealed class OrderedEvaluationAnalyzer(
        IEvaluator<int> evaluator,
        int marker,
        List<int> invocationOrder)
        : IAnalyzer
    {
        public EvaluationResult Result { get; } = new();

        public void Install(ResolutionScopeBuilder builder) => builder.Observe<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>>(evaluator, Record);

        public void Record(EvaluatorObservation<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>> observation)
        {
            invocationOrder.Add(marker);
            Result.Count += observation.Candidates.Count;
        }
    }

    private sealed class EvaluationResult
    {
        public int Count { get; set; }
    }
}
