using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Experiments;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Evaluators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.States;
using HEAL.HeuristicLib.Tests.Experiments.TestSupport;
using HEAL.HeuristicLib.Tests.TestSupport.Mocks;

namespace HEAL.HeuristicLib.Tests.Experiments;

public class ExperimentAnalysisTests
{
    [Fact]
    public async Task MultipleBindings_ObserveTheSameOperatorInAttachmentOrder()
    {
        var invocationOrder = new List<int>();
        var evaluator = new CountingResolutionEvaluator();
        var algorithm = new CountingInstanceAlgorithm(1, evaluator);
        var experiment = algorithm.Repeat(2);
        var run = experiment.CreateRun(MetaAlgorithmTestHelpers.CreateIntegerProblem(), RandomNumberGenerator.Create(42));
        var first = CreateBinding(marker: 1, invocationOrder);
        var second = CreateBinding(marker: 2, invocationOrder);

        run.WithAnalysis(first).WithAnalysis(second);
        Should.Throw<InvalidOperationException>(() => run.WithAnalysis(first));
        Should.Throw<InvalidOperationException>(() => run.GetResults(first));

        var stream = run.Stream(cancellationToken: TestContext.Current.CancellationToken);
        run.GetResults(first).ShouldAllBe(result => result.Result.Count == 0);
        _ = await stream.ToListAsync(TestContext.Current.CancellationToken);

        invocationOrder.ShouldBe([1, 2, 1, 2]);
        run.GetResults(first).Select(result => result.Trial.Key).ShouldBe([0, 1]);
        run.GetResults(first).ShouldAllBe(result => result.Result.Count == 1);
        run.GetResults(second).ShouldAllBe(result => result.Result.Count == 1);
        Should.Throw<InvalidOperationException>(() => run.WithAnalysis(CreateBinding(marker: 3, invocationOrder)));
    }

    [Fact]
    public async Task FailingAnalyzerFactory_AttachesNoPartialAnalysis()
    {
        var invocations = new List<int>();
        var firstAlgorithm = new CountingInstanceAlgorithm(1, new CountingResolutionEvaluator());
        var secondAlgorithm = new CountingInstanceAlgorithm(2, new CountingResolutionEvaluator());
        var experiment = new FixedExperiment<CountingInstanceAlgorithm>([
            new ExperimentCase<CountingInstanceAlgorithm, int>(firstAlgorithm, 0, [0]),
            new ExperimentCase<CountingInstanceAlgorithm, int>(secondAlgorithm, 1, [1])
        ]);
        var run = ExperimentTestSupport.CreateRun(experiment);
        var binding = ExperimentAnalysis.ForEach<CountingInstanceAlgorithm, CountingInstanceAlgorithm, EvaluationResult>(
            algorithm => algorithm,
            algorithm => algorithm.Increment == 2
                ? throw new InvalidOperationException("Analyzer creation failed.")
                : new OrderedEvaluationAnalyzer(algorithm.Evaluator, 1, invocations));

        Should.Throw<InvalidOperationException>(() => run.WithAnalysis(binding));
        _ = await run.CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        invocations.ShouldBeEmpty();
    }

    private static AnalyzerBinding<CountingInstanceAlgorithm, IEvaluator<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>>, EvaluationResult> CreateBinding(
        int marker, List<int> invocationOrder) => 
        ExperimentAnalysis.ForEach(
            (CountingInstanceAlgorithm algorithm) => algorithm.Evaluator,
            evaluator => new OrderedEvaluationAnalyzer(evaluator, marker, invocationOrder));

    private sealed record OrderedEvaluationAnalyzer(
        IEvaluator<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>> Evaluator,
        int Marker,
        List<int> InvocationOrder) : Analyzer<EvaluationResult>
    {
        public override EvaluationResult CreateInitialResult() => new();

        public override void RegisterObservations(ObservationPlan observations, EvaluationResult result)
        {
            observations.Observe(Evaluator, (_, objectiveVectors, _, _) =>
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
