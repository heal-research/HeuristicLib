using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Algorithms.MetaAlgorithms;
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
}
