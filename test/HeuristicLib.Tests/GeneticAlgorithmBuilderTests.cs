using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Algorithms.GeneticAlgorithm;
using HEAL.HeuristicLib.Encodings;
using HEAL.HeuristicLib.Operators;
using Xunit;

namespace HEAL.HeuristicLib.Tests;

public class GeneticAlgorithmBuilderTests {
  [Fact]
  public Task GeneticAlgorithmBuilder_ShouldBuildAlgorithm() {
    var random = RandomGenerator.CreateDefault(42);
    var builder = new GeneticAlgorithmBuilder<RealVector>()
      .WithPopulationSize(200)
      .WithCrossover(new SinglePointCrossover(random))
      .WithMutation(new GaussianMutator(0.1, 0.1, random))
      .WithMutationRate(0.05)
      .WithEvaluator(new MockEvaluator())
      .WithRandom(RandomGenerator.CreateDefault())
      .WithSelector(new ProportionalSelector<RealVector>(random))
      .WithPlusSelectionReplacement()
      .WithRandom(random)
      .TerminateOnMaxGenerations(50);

    var ga = builder.Build();

    return Verify(ga);
  }

  private class MockEvaluator : IEvaluator<RealVector, ObjectiveValue> {
    public ObjectiveValue Evaluate(RealVector solution) {
      return (solution.Sum(), ObjectiveDirection.Minimize);
    }
  }
}
