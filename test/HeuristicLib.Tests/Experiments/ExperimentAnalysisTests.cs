using HEAL.HeuristicLib.Experiments;
using HEAL.HeuristicLib.Inspection;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Tests.Experiments.TestSupport;
using HEAL.HeuristicLib.Tests.TestSupport.Mocks;

namespace HEAL.HeuristicLib.Tests.Experiments;

public class ExperimentAnalysisTests
{
    [Fact]
    public async Task AspectsPerTrial_KeepAttachmentOrderAndReturnEachTrialsAspect()
    {
        var invocationOrder = new List<int>();
        var algorithm = new CountingExecutionAlgorithm(1, new CountingResolutionEvaluator());
        var run = algorithm.Repeat(2)
            .CreateRun(MetaAlgorithmTestHelpers.CreateIntegerProblem(), RandomNumberGenerator.Create(42));
        var first = run.AddAspectPerTrial(alg => new OrderedEvaluationModule(alg.Evaluator, 1, invocationOrder));
        var middle = run.AddAspectPerTrial(alg => new OrderedEvaluationAnalyzer(alg.Evaluator, 2, invocationOrder));
        _ = run.AddAspectPerTrial(alg => new OrderedEvaluationModule(alg.Evaluator, 3, invocationOrder));

        first.Select(entry => entry.Trial.Key).ShouldBe([0, 1]);
        first[0].Aspect.ShouldNotBeSameAs(first[1].Aspect);
        first.ShouldAllBe(entry => entry.Aspect.Observations == 0);

        await run.CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        invocationOrder.ShouldBe([1, 2, 3, 1, 2, 3]);
        first.ShouldAllBe(entry => entry.Aspect.Observations == 1);
        middle.ShouldAllBe(entry => entry.Aspect.Result.Count == 1);
    }

    private sealed class OrderedEvaluationModule
        : Aspect
    {
        public int Observations { get; private set; }

        public OrderedEvaluationModule(IOperator<Evaluation, int> evaluator, int marker, List<int> invocationOrder)
        {
            Advices = [.. Aspect.Observe(evaluator.Calls, _ =>
        {
            invocationOrder.Add(marker);
            Observations++;
        })];
        }
    }

    [Fact]
    public async Task MultipleTrialAnalyzers_ObserveTheSameOperatorInAttachmentOrder()
    {
        var invocationOrder = new List<int>();
        var evaluator = new CountingResolutionEvaluator();
        var algorithm = new CountingExecutionAlgorithm(1, evaluator);
        var run = algorithm.Repeat(2).CreateRun(MetaAlgorithmTestHelpers.CreateIntegerProblem(), RandomNumberGenerator.Create(42));
        var first = run.AddAspectPerTrial(alg => new OrderedEvaluationAnalyzer(alg.Evaluator, 1, invocationOrder));
        var second = run.AddAspectPerTrial(alg => new OrderedEvaluationAnalyzer(alg.Evaluator, 2, invocationOrder));

        var stream = run.Stream(cancellationToken: TestContext.Current.CancellationToken);
        first.ShouldAllBe(analysis => analysis.Aspect.Result.Count == 0);
        _ = await stream.ToListAsync(TestContext.Current.CancellationToken);

        invocationOrder.ShouldBe([1, 2, 1, 2]);
        first.Select(analysis => analysis.Trial.Key).ShouldBe([0, 1]);
        first.ShouldAllBe(analysis => analysis.Aspect.Result.Count == 1);
        second.ShouldAllBe(analysis => analysis.Aspect.Result.Count == 1);
    }

    [Fact]
    public async Task FailingAnalyzerFactory_AttachesNoPartialAnalysis()
    {
        var invocations = new List<int>();
        var firstAlgorithm = new CountingExecutionAlgorithm(1, new CountingResolutionEvaluator());
        var secondAlgorithm = new CountingExecutionAlgorithm(2, new CountingResolutionEvaluator());
        var experiment = new FixedExperiment<CountingExecutionAlgorithm>([
            ExperimentCase.From(firstAlgorithm, 0, [0]),
            ExperimentCase.From(secondAlgorithm, 1, [1])
        ]);
        var run = ExperimentTestSupport.CreateRun(experiment);

        Should.Throw<InvalidOperationException>(() => run.AddAspectPerTrial(algorithm => algorithm.Increment == 2
            ? throw new InvalidOperationException("Analyzer creation failed.")
            : new OrderedEvaluationAnalyzer(algorithm.Evaluator, 1, invocations)));

        _ = await run.CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        invocations.ShouldBeEmpty();
    }

    [Fact]
    public async Task OneAspectFunction_CanBeAddedToAnotherRun()
    {
        var invocations = new List<int>();
        var algorithm = new CountingExecutionAlgorithm(1, new CountingResolutionEvaluator());
        var experiment = algorithm.Repeat(2);
        Func<CountingExecutionAlgorithm, OrderedEvaluationAnalyzer> createAnalyzer = alg => new OrderedEvaluationAnalyzer(alg.Evaluator, 1, invocations);
        _ = experiment.CreateRun(MetaAlgorithmTestHelpers.CreateIntegerProblem(), RandomNumberGenerator.Create(1)).AddAspectPerTrial(createAnalyzer);
        var run = experiment.CreateRun(MetaAlgorithmTestHelpers.CreateIntegerProblem(), RandomNumberGenerator.Create(2));
        var analyzers = run.AddAspectPerTrial(createAnalyzer);

        _ = await run.CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        analyzers.Select(analysis => analysis.Trial.Key).ShouldBe([0, 1]);
        analyzers.ShouldAllBe(analysis => analysis.Aspect.Result.Count == 1);
    }

    [Fact]
    public async Task TrialAnalyzers_CollectPerTrialAsEachTrialCompletes()
    {
        var algorithm = new CountingExecutionAlgorithm(1, new CountingResolutionEvaluator());
        var run = algorithm.Repeat(2)
            .CreateRun(MetaAlgorithmTestHelpers.CreateIntegerProblem(), RandomNumberGenerator.Create(42));
        var analyzers = run.AddAspectPerTrial(alg => new OrderedEvaluationAnalyzer(alg.Evaluator, 1, []));

        _ = await run.Trials[0].Run.CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        // A trial that has not completed simply has no data yet.
        analyzers[1].Aspect.Result.Count.ShouldBe(0);

        _ = await run.Trials[1].Run.CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        analyzers.ShouldAllBe(analysis => analysis.Aspect.Result.Count == 1);
    }

    private sealed class OrderedEvaluationAnalyzer
        : Aspect
    {
        private readonly int marker;
        private readonly List<int> invocationOrder;
        public EvaluationResult Result { get; } = new();

        public OrderedEvaluationAnalyzer(
        IOperator<Evaluation, int> evaluator,
        int marker,
        List<int> invocationOrder)
        {
            this.marker = marker;
            this.invocationOrder = invocationOrder;
            Advices = [.. Aspect.Observe(evaluator.Calls, Record)];
        }

        public void Record(EvaluationObservation<int> observation)
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
