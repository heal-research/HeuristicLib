using HEAL.HeuristicLib.ProofOfConcept;
using Xunit;

namespace HEAL.HeuristicLib.Tests;

public class GeneticAlgorithmBuilderTests {
  [Fact]
  public void GeneticAlgorithmBuilder_ShouldBuildAlgorithm() {
    var builder = new GeneticAlgorithmBuilder<RealVector>()
      .WithPopulationSize(200)
      .WithCrossover(new SinglePointCrossover())
      .WithMutation(new GaussianMutator(0.1, 0.1))
      .WithMutationRate(0.05)
      .WithEvaluator(new MockEvaluator())
      .WithRandom(new Random())
      .WithSelector(new ProportionalSelector<RealVector>())
      .WithPlusSelectionReplacement()
      .TerminateOnMaxGenerations(50);

    var ga = builder.Build();

    Verify(ga);
  }

  private class MockEvaluator : IEvaluator<RealVector> {
    public double Evaluate(RealVector solution) {
      return solution.Sum();
    }
  }
}
