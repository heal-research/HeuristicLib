using HEAL.HeuristicLib.Operators.Evaluators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Tests.TestSupport.Mocks;

namespace HEAL.HeuristicLib.Tests.Algorithms.MetaAlgorithms;

public class PipelineAlgorithmTests
{
    [Fact]
    public void PipelineAlgorithm_SnapshotsAlgorithms()
    {
        var first = new AdditiveStepAlgorithm(1);
        var second = new AdditiveStepAlgorithm(2);
        var algorithms = new List<AdditiveStepAlgorithm> { first, second };
        var pipeline = new PipelineAlgorithm<AdditiveStepAlgorithm, int, PopulationState<int>>(algorithms);

        algorithms.Clear();

        pipeline.Algorithms.ShouldBe([first, second]);
    }

    [Fact]
    public void PipelineAlgorithm_RequiresAtLeastOneAlgorithm()
    {
        var exception = Should.Throw<ArgumentException>(() =>
            new PipelineAlgorithm<AdditiveStepAlgorithm, int, PopulationState<int>>([]));

        exception.ParamName.ShouldBe("algorithms");
    }

    [Fact]
    public void PipelineAlgorithm_ChecksCancellationBeforeCreatingAStageInstance()
    {
        var problem = MetaAlgorithmTestHelpers.CreateIntegerProblem();
        var evaluator = new CountingResolutionEvaluator();
        var algorithm = new CountingInstanceAlgorithm(1, evaluator);
        var pipeline = PipelineAlgorithm.Create(algorithm);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        Should.Throw<OperationCanceledException>(() => pipeline.Stream(problem, RandomNumberGenerator.Create(42), ct: cts.Token).ToList());

        algorithm.InstanceCount.ShouldBe(0);
    }

    [Fact]
    public void PipelineAlgorithm_Stream_PassesEachStageResultToNextStage()
    {
        var problem = MetaAlgorithmTestHelpers.CreateIntegerProblem();
        var pipeline = new AdditiveStepAlgorithm(1).Then(new AdditiveStepAlgorithm(10), new AdditiveStepAlgorithm(100));

        var states = pipeline
                     .Stream(problem, RandomNumberGenerator.Create(42), ct: TestContext.Current.CancellationToken)
                     .ToList();

        states.Select(MetaAlgorithmTestHelpers.StateCandidate).ShouldBe([1, 11, 111]);
        states.Select(MetaAlgorithmTestHelpers.StateObjective).ShouldBe([1.0, 11.0, 111.0]);
    }

    [Fact]
    public void PipelineAlgorithm_RunScopedAnalyzer_ObservesInnerStages()
    {
        var problem = MetaAlgorithmTestHelpers.CreateIntegerProblem();
        var evaluator = new ForwardingEvaluator();
        var pipeline = new AdditiveStepAlgorithm(1) { Evaluator = evaluator }.Then(new AdditiveStepAlgorithm(10) { Evaluator = evaluator }, new AdditiveStepAlgorithm(100) { Evaluator = evaluator });
        var analysis = new EvaluationCountAnalysis(evaluator);
        var run = pipeline.CreateRun(problem, RandomNumberGenerator.Create(0)).WithAnalyzer(analysis);

        var states = run.Stream(cancellationToken: TestContext.Current.CancellationToken).ToList();

        states.Select(MetaAlgorithmTestHelpers.StateCandidate).ShouldBe([1, 11, 111]);
        run.GetResult(analysis).Count.ShouldBe(3);
    }

    [Fact]
    public void PipelineAlgorithm_CreatesEachStageInstanceWhileReusingResolvedParentDependencies()
    {
        var problem = MetaAlgorithmTestHelpers.CreateIntegerProblem();
        var evaluator = new CountingResolutionEvaluator();
        var algorithm = new CountingInstanceAlgorithm(1, evaluator);
        var pipeline = algorithm.Then(algorithm);
        var registry = new ExecutionInstanceRegistry();
        _ = registry.Resolve<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>>(evaluator);
        var pipelineInstance = registry.Resolve<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>, PopulationState<int>>(pipeline);

        var states = pipelineInstance.Stream(problem, RandomNumberGenerator.Create(42), ct: TestContext.Current.CancellationToken).ToList();

        states.Select(MetaAlgorithmTestHelpers.StateCandidate).ShouldBe([1, 2]);
        algorithm.InstanceCount.ShouldBe(2);
        evaluator.InstanceCount.ShouldBe(1);
    }

    private sealed record ForwardingEvaluator
        : StatelessEvaluator<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>>
    {
        public override IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<int> candidates, IRandomNumberGenerator random, DummySearchSpace<int> searchSpace, IProblem<int, DummySearchSpace<int>> problem)
        {
            return problem.Evaluate(candidates, random)
                .ToArray();
        }
    }

    private sealed record EvaluationCountAnalysis(
        IEvaluator<int> Evaluator)
        : Analyzer<EvaluationCountAnalysis.Result>
    {
        public override Result CreateInitialResult() => new();

        public override void RegisterObservations(ObservationPlan observations, Result result)
        {
            observations.Observe<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>>(Evaluator, (_, objectiveVectors, _, _) => result.Count += objectiveVectors.Count);
        }

        public sealed class Result
        {
            public int Count { get; set; }
        }
    }
}
