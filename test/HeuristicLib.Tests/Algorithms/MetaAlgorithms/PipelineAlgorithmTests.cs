using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Algorithms.MetaAlgorithms;
using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Evaluators;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.States;
using HEAL.HeuristicLib.Tests.TestSupport.Execution;
using HEAL.HeuristicLib.Tests.TestSupport.Mocks;

namespace HEAL.HeuristicLib.Tests.Algorithms.MetaAlgorithms;

public class PipelineAlgorithmTests
{
    [Fact]
    public void PipelineAlgorithm_RequiresAtLeastOneAlgorithm()
    {
        var exception = Should.Throw<ArgumentException>(() =>
            new PipelineAlgorithm<AdditiveStepAlgorithm, int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>, PopulationState<int>>([]));

        exception.ParamName.ShouldBe("algorithms");
    }

    [Fact]
    public void PipelineAlgorithm_ChecksCancellationBeforeCreatingAStageInstance()
    {
        var problem = MetaAlgorithmTestHelpers.CreateIntegerProblem();
        var evaluator = new CountingResolutionEvaluator();
        var algorithm = new CountingInstanceAlgorithm(1, evaluator);
        var pipeline = new PipelineAlgorithm<CountingInstanceAlgorithm, int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>, PopulationState<int>>([algorithm]);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        Should.Throw<OperationCanceledException>(() => pipeline.RunStreaming(problem, RandomNumberGenerator.Create(42), ct: cts.Token).ToList());

        algorithm.InstanceCount.ShouldBe(0);
    }

    [Fact]
    public void PipelineAlgorithm_RunStreaming_PassesEachStageResultToNextStage()
    {
        var problem = MetaAlgorithmTestHelpers.CreateIntegerProblem();
        var pipeline =
            new PipelineAlgorithm<AdditiveStepAlgorithm, int, DummySearchSpace<int>,
                IProblem<int, DummySearchSpace<int>>, PopulationState<int>>(
            [
                new AdditiveStepAlgorithm(1),
                new AdditiveStepAlgorithm(10),
                new AdditiveStepAlgorithm(100)
            ]);

        var states = pipeline
                     .RunStreaming(problem, RandomNumberGenerator.Create(42), ct: TestContext.Current.CancellationToken)
                     .ToList();

        states.Select(MetaAlgorithmTestHelpers.StateCandidate).ShouldBe([1, 11, 111]);
        states.Select(MetaAlgorithmTestHelpers.StateObjective).ShouldBe([1.0, 11.0, 111.0]);
    }

    [Fact]
    public void PipelineAlgorithm_RunScopedAnalyzer_ObservesInnerStages()
    {
        var problem = MetaAlgorithmTestHelpers.CreateIntegerProblem();
        var evaluator = new ForwardingEvaluator();
        var pipeline =
            new PipelineAlgorithm<AdditiveStepAlgorithm, int, DummySearchSpace<int>,
                IProblem<int, DummySearchSpace<int>>, PopulationState<int>>(
            [
                new AdditiveStepAlgorithm(1) { Evaluator = evaluator },
                new AdditiveStepAlgorithm(10) { Evaluator = evaluator },
                new AdditiveStepAlgorithm(100) { Evaluator = evaluator }
            ]);
        var analysis = new EvaluationCountAnalysis(evaluator);
        var run = pipeline.CreateRun(problem, analysis);

        var states = run.Stream(RandomNumberGenerator.Create(42),
            cancellationToken: TestContext.Current.CancellationToken).ToList();

        states.Select(MetaAlgorithmTestHelpers.StateCandidate).ShouldBe([1, 11, 111]);
        run.GetAnalyzerResult(analysis).Count.ShouldBe(3);
    }

    [Fact]
    public void PipelineAlgorithm_CreatesEachStageInstanceWhileReusingResolvedParentDependencies()
    {
        var problem = MetaAlgorithmTestHelpers.CreateIntegerProblem();
        var evaluator = new CountingResolutionEvaluator();
        var algorithm = new CountingInstanceAlgorithm(1, evaluator);
        var pipeline = new PipelineAlgorithm<CountingInstanceAlgorithm, int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>, PopulationState<int>>([algorithm, algorithm]);
        var registry = new ExecutionInstanceRegistry(TestRun.Instance);
        _ = registry.Resolve(evaluator);
        var pipelineInstance = registry.Resolve(pipeline);

        var states = pipelineInstance.RunStreaming(problem, RandomNumberGenerator.Create(42), ct: TestContext.Current.CancellationToken).ToList();

        states.Select(MetaAlgorithmTestHelpers.StateCandidate).ShouldBe([1, 2]);
        algorithm.InstanceCount.ShouldBe(2);
        evaluator.InstanceCount.ShouldBe(1);
    }

    private sealed record ForwardingEvaluator
        : StatelessEvaluator<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>>
    {
        public override IReadOnlyList<ObjectiveVector> Evaluate(
            IReadOnlyList<int> candidates,
            IRandomNumberGenerator random,
            DummySearchSpace<int> searchSpace,
            IProblem<int, DummySearchSpace<int>> problem)
        {
            return problem.Evaluate(candidates, random);
        }
    }

    private sealed record EvaluationCountAnalysis(
        IEvaluator<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>> Evaluator)
        : Analyzer<EvaluationCountAnalysis.Result>
    {
        public override Result CreateInitialResult() => new();

        public override void RegisterObservations(ObservationPlan observations, Result result)
        {
            observations.Observe(Evaluator, (_, objectiveVectors, _, _) => result.Count += objectiveVectors.Count);
        }

        public sealed class Result
        {
            public int Count { get; set; }
        }
    }
}
