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
        var run = experiment.CreateRun(MetaAlgorithmTestHelpers.CreateIntegerProblem(), RandomNumberGenerator.Create(42))
            .WithAnalyzer(
                alg => alg.Evaluator,
                eval => new OrderedEvaluationAnalyzer(eval, 1, invocationOrder),
                out var first)
            .WithAnalyzer(
                alg => alg.Evaluator,
                eval => new OrderedEvaluationAnalyzer(eval, 2, invocationOrder),
                out var second);

        Should.Throw<InvalidOperationException>(() => run.WithAnalyzer(first));
        Should.Throw<InvalidOperationException>(() => run.GetResults(first));

        var stream = run.Stream(cancellationToken: TestContext.Current.CancellationToken);
        run.GetResults(first).ShouldAllBe(result => result.Result.Count == 0);
        _ = await stream.ToListAsync(TestContext.Current.CancellationToken);

        invocationOrder.ShouldBe([1, 2, 1, 2]);
        run.GetResults(first).Select(result => result.Trial.Key).ShouldBe([0, 1]);
        run.GetResults(first).ShouldAllBe(result => result.Result.Count == 1);
        run.GetResults(second).ShouldAllBe(result => result.Result.Count == 1);
        var third = TrialAnalyzer.Create(
            (CountingInstanceAlgorithm alg) => alg.Evaluator,
            eval => new OrderedEvaluationAnalyzer(eval, 3, invocationOrder));
        Should.Throw<InvalidOperationException>(() => run.WithAnalyzer(third));
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
        var run = ExperimentTestSupport.CreateRun(experiment);

        Should.Throw<InvalidOperationException>(() => run.WithAnalyzer(
            algorithm => algorithm,
            algorithm => algorithm.Increment == 2
                ? throw new InvalidOperationException("Analyzer creation failed.")
                : new OrderedEvaluationAnalyzer(algorithm.Evaluator, 1, invocations),
            out _));
        _ = await run.CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        invocations.ShouldBeEmpty();
    }

    [Fact]
    public async Task ExistingTrialAnalyzer_CanBeAttachedToAnotherRun()
    {
        var invocations = new List<int>();
        var algorithm = new CountingInstanceAlgorithm(1, new CountingResolutionEvaluator());
        var experiment = algorithm.Repeat(2);
        _ = experiment.CreateRun(MetaAlgorithmTestHelpers.CreateIntegerProblem(), RandomNumberGenerator.Create(1))
            .WithAnalyzer(
                alg => alg.Evaluator,
                evaluator => new OrderedEvaluationAnalyzer(evaluator, 1, invocations),
                out var trialAnalyzer);
        var run = experiment.CreateRun(MetaAlgorithmTestHelpers.CreateIntegerProblem(), RandomNumberGenerator.Create(2))
            .WithAnalyzer(trialAnalyzer);

        _ = await run.CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        run.GetResults(trialAnalyzer).Select(result => result.Trial.Key).ShouldBe([0, 1]);
        run.GetResults(trialAnalyzer).ShouldAllBe(result => result.Result.Count == 1);
    }

    [Fact]
    public async Task GetResults_RequiresEveryTrialToHaveStarted()
    {
        var algorithm = new CountingInstanceAlgorithm(1, new CountingResolutionEvaluator());
        var run = algorithm.Repeat(2)
            .CreateRun(MetaAlgorithmTestHelpers.CreateIntegerProblem(), RandomNumberGenerator.Create(42))
            .WithAnalyzer(
                alg => alg.Evaluator,
                evaluator => new OrderedEvaluationAnalyzer(evaluator, 1, []),
                out var trialAnalyzer);

        _ = await run.Trials[0].Run.CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        Should.Throw<InvalidOperationException>(() => run.GetResults(trialAnalyzer));

        _ = await run.Trials[1].Run.CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        run.GetResults(trialAnalyzer).ShouldAllBe(result => result.Result.Count == 1);
    }

    private sealed record OrderedEvaluationAnalyzer(
        IEvaluator<int> Evaluator,
        int Marker,
        List<int> InvocationOrder) : Analyzer<EvaluationResult>
    {
        public override EvaluationResult CreateInitialResult() => new();

        public override void RegisterObservations(ObservationPlan observations, EvaluationResult result)
        {
            observations.Observe<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>>(Evaluator, (_, objectiveVectors, _, _) =>
            {
                InvocationOrder.Add(Marker);
                result.Count += objectiveVectors.Count;
            });
        }
    }

    private sealed class EvaluationResult
    {
        public int Count { get; set; }
    }
}
