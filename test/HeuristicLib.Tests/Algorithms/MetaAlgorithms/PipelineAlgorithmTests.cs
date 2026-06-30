using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Algorithms.MetaAlgorithms;
using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Evaluators;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.States;
using HEAL.HeuristicLib.Tests.TestSupport.Mocks;

namespace HEAL.HeuristicLib.Tests.Algorithms.MetaAlgorithms;

public class PipelineAlgorithmTests
{
    [Fact]
    public void PipelineAlgorithm_RunStreaming_PassesEachStageResultToNextStage()
    {
        var problem = MetaAlgorithmTestHelpers.CreateIntegerProblem();
        var pipeline = new PipelineAlgorithm<AdditiveStepAlgorithm, int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>, PopulationState<int>>(
          [
            new AdditiveStepAlgorithm(1),
        new AdditiveStepAlgorithm(10),
        new AdditiveStepAlgorithm(100)
          ]);

        var states = pipeline.RunStreaming(problem, RandomNumberGenerator.Create(42), ct: TestContext.Current.CancellationToken).ToList();

        states.Select(MetaAlgorithmTestHelpers.StateGenotype).ShouldBe([1, 11, 111]);
        states.Select(MetaAlgorithmTestHelpers.StateObjective).ShouldBe([1.0, 11.0, 111.0]);
    }

    [Fact]
    public void PipelineAlgorithm_RunScopedAnalyzer_ObservesInnerStages()
    {
        var problem = MetaAlgorithmTestHelpers.CreateIntegerProblem();
        var evaluator = new ForwardingEvaluator();
        var pipeline = new PipelineAlgorithm<AdditiveStepAlgorithm, int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>, PopulationState<int>>(
          [
            new AdditiveStepAlgorithm(1) { Evaluator = evaluator },
            new AdditiveStepAlgorithm(10) { Evaluator = evaluator },
            new AdditiveStepAlgorithm(100) { Evaluator = evaluator }
          ]);
        var analysis = new EvaluationCountAnalysis(evaluator);
        var run = pipeline.CreateRun(problem, analysis);

        var states = run.RunStreaming(RandomNumberGenerator.Create(42), cancellationToken: TestContext.Current.CancellationToken).ToList();

        states.Select(MetaAlgorithmTestHelpers.StateGenotype).ShouldBe([1, 11, 111]);
        run.GetAnalyzerResult(analysis).Count.ShouldBe(3);
    }

    private sealed record ForwardingEvaluator
      : StatelessEvaluator<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>>
    {
        public override IReadOnlyList<ObjectiveVector> Evaluate(
          IReadOnlyList<int> genotypes,
          IRandomNumberGenerator random,
          DummySearchSpace<int> searchSpace,
          IProblem<int, DummySearchSpace<int>> problem)
        {
            return problem.Evaluate(genotypes, random);
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
