using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Algorithms.MetaAlgorithms;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.States;
using HEAL.HeuristicLib.Tests.TestSupport.Mocks;

namespace HEAL.HeuristicLib.Tests.Algorithms.MetaAlgorithms;

public class CycleAlgorithmTests
{
  [Fact]
  public void CycleAlgorithm_RunStreaming_RepeatsStagesAndPassesStateAcrossCycles()
  {
    var problem = MetaAlgorithmTestHelpers.CreateIntegerProblem();
    var cycle = new CycleAlgorithm<AdditiveStepAlgorithm, int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>, PopulationState<int>>(
      [
        new AdditiveStepAlgorithm(1),
        new AdditiveStepAlgorithm(10)
      ]) {
      MaximumCycles = 2
    };

    var states = cycle.RunStreaming(problem, RandomNumberGenerator.Create(42), ct: TestContext.Current.CancellationToken).ToList();

    states.Select(MetaAlgorithmTestHelpers.StateGenotype).ShouldBe([1, 11, 12, 22]);
    states.Select(MetaAlgorithmTestHelpers.StateObjective).ShouldBe([1.0, 11.0, 12.0, 22.0]);
  }
}
