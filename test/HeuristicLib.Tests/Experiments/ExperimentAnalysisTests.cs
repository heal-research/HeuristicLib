using HEAL.HeuristicLib.Experiments;
using HEAL.HeuristicLib.Inspection;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Tests.Experiments.TestSupport;
using HEAL.HeuristicLib.Tests.TestSupport.Mocks;

namespace HEAL.HeuristicLib.Tests.Experiments;

public class ExperimentAnalysisTests
{
    [Fact]
    public async Task ModulesAndAnalyzersPerTrial_ShareAttachmentOrderAndTypedLookup()
    {
        var invocationOrder = new List<int>();
        var algorithm = new CountingExecutionAlgorithm(1, new CountingResolutionEvaluator());
        var first = AspectFactory.Create((CountingExecutionAlgorithm alg) => new OrderedEvaluationModule(alg.Evaluator, 1, invocationOrder));
        var middle = AspectFactory.Create((CountingExecutionAlgorithm alg) => new OrderedEvaluationAnalyzer(alg.Evaluator, 2, invocationOrder));
        var last = AspectFactory.Create((CountingExecutionAlgorithm alg) => new OrderedEvaluationModule(alg.Evaluator, 3, invocationOrder));
        var run = algorithm.Repeat(2)
            .CreateRun(MetaAlgorithmTestHelpers.CreateIntegerProblem(), RandomNumberGenerator.Create(42))
            .AddAspectPerTrial(first).AddAspectPerTrial(middle).AddAspectPerTrial(last);

        var attachments = run.GetAspectPerTrial(first);
        attachments.Select(entry => entry.Trial.Key).ShouldBe([0, 1]);
        attachments[0].Aspect.ShouldNotBeSameAs(attachments[1].Aspect);
        run.GetAspectPerTrial(first)[0].Aspect.ShouldBeSameAs(attachments[0].Aspect);
        attachments.ShouldAllBe(entry => entry.Aspect.Observations == 0);

        await run.CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        invocationOrder.ShouldBe([1, 2, 3, 1, 2, 3]);
        attachments.ShouldAllBe(entry => entry.Aspect.Observations == 1);
        run.GetAspectPerTrial(middle).ShouldAllBe(entry => entry.Aspect.Result.Count == 1);
    }

    private sealed class OrderedEvaluationModule
        : Aspect
    {
        public int Observations { get; private set; }

        public OrderedEvaluationModule(IOperator<Evaluation, int> evaluator, int marker, List<int> invocationOrder)
        {
            Advices = Aspect.Observe(evaluator, _ =>
        {
            invocationOrder.Add(marker);
            Observations++;
        }).Advices;
        }
    }

    [Fact]
    public async Task MultipleTrialAnalyzers_ObserveTheSameOperatorInAttachmentOrder()
    {
        var invocationOrder = new List<int>();
        var evaluator = new CountingResolutionEvaluator();
        var algorithm = new CountingExecutionAlgorithm(1, evaluator);
        var experiment = algorithm.Repeat(2);
        var first = AspectFactory.Create((CountingExecutionAlgorithm alg) => new OrderedEvaluationAnalyzer(alg.Evaluator, 1, invocationOrder));
        var second = AspectFactory.Create((CountingExecutionAlgorithm alg) => new OrderedEvaluationAnalyzer(alg.Evaluator, 2, invocationOrder));
        var run = experiment.CreateRun(MetaAlgorithmTestHelpers.CreateIntegerProblem(), RandomNumberGenerator.Create(42)).AddAspectPerTrial(first).AddAspectPerTrial(second);

        Should.Throw<InvalidOperationException>(() =>
            experiment.CreateRun(MetaAlgorithmTestHelpers.CreateIntegerProblem(), RandomNumberGenerator.Create(42)).AddAspectPerTrial(first).AddAspectPerTrial(first));

        var stream = run.Stream(cancellationToken: TestContext.Current.CancellationToken);
        run.GetAspectPerTrial(first).ShouldAllBe(analysis => analysis.Aspect.Result.Count == 0);
        _ = await stream.ToListAsync(TestContext.Current.CancellationToken);

        invocationOrder.ShouldBe([1, 2, 1, 2]);
        run.GetAspectPerTrial(first).Select(analysis => analysis.Trial.Key).ShouldBe([0, 1]);
        run.GetAspectPerTrial(first).ShouldAllBe(analysis => analysis.Aspect.Result.Count == 1);
        run.GetAspectPerTrial(second).ShouldAllBe(analysis => analysis.Aspect.Result.Count == 1);
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
        var failing = AspectFactory.Create(
            (CountingExecutionAlgorithm algorithm) => algorithm.Increment == 2
                ? throw new InvalidOperationException("Analyzer creation failed.")
                : new OrderedEvaluationAnalyzer(algorithm.Evaluator, 1, invocations));

        Should.Throw<InvalidOperationException>(() =>
            experiment.CreateRun(MetaAlgorithmTestHelpers.CreateIntegerProblem(), RandomNumberGenerator.Create(42)).AddAspectPerTrial(failing));

        var run = ExperimentTestSupport.CreateRun(experiment);
        _ = await run.CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        invocations.ShouldBeEmpty();
    }

    [Fact]
    public async Task ExistingTrialAnalyzer_CanBeAttachedToAnotherRun()
    {
        var invocations = new List<int>();
        var algorithm = new CountingExecutionAlgorithm(1, new CountingResolutionEvaluator());
        var experiment = algorithm.Repeat(2);
        var trialAnalyzer = AspectFactory.Create((CountingExecutionAlgorithm alg) => new OrderedEvaluationAnalyzer(alg.Evaluator, 1, invocations));
        _ = experiment.CreateRun(MetaAlgorithmTestHelpers.CreateIntegerProblem(), RandomNumberGenerator.Create(1)).AddAspectPerTrial(trialAnalyzer);
        var run = experiment.CreateRun(MetaAlgorithmTestHelpers.CreateIntegerProblem(), RandomNumberGenerator.Create(2)).AddAspectPerTrial(trialAnalyzer);

        _ = await run.CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        run.GetAspectPerTrial(trialAnalyzer).Select(analysis => analysis.Trial.Key).ShouldBe([0, 1]);
        run.GetAspectPerTrial(trialAnalyzer).ShouldAllBe(analysis => analysis.Aspect.Result.Count == 1);
    }

    [Fact]
    public async Task TrialAnalyzers_CollectPerTrialAsEachTrialCompletes()
    {
        var algorithm = new CountingExecutionAlgorithm(1, new CountingResolutionEvaluator());
        var trialAnalyzer = AspectFactory.Create((CountingExecutionAlgorithm alg) => new OrderedEvaluationAnalyzer(alg.Evaluator, 1, []));
        var run = algorithm.Repeat(2)
            .CreateRun(MetaAlgorithmTestHelpers.CreateIntegerProblem(), RandomNumberGenerator.Create(42)).AddAspectPerTrial(trialAnalyzer);

        _ = await run.Trials[0].Run.CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        // A trial that has not completed simply has no data yet.
        run.GetAspectPerTrial(trialAnalyzer)[1].Aspect.Result.Count.ShouldBe(0);

        _ = await run.Trials[1].Run.CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        run.GetAspectPerTrial(trialAnalyzer).ShouldAllBe(analysis => analysis.Aspect.Result.Count == 1);
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
            Advices = Aspect.Observe(evaluator, Record).Advices;
        }

        public void Record(OperatorObservation<Evaluation, int> observation)
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
