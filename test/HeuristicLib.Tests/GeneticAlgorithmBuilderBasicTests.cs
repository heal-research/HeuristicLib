using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Algorithms.GeneticAlgorithm;
using HEAL.HeuristicLib.Encodings;
using HEAL.HeuristicLib.Operators;
using Xunit;

namespace HEAL.HeuristicLib.Tests;

public class GeneticAlgorithmBuilderBasicTests {
  [Fact]
  public Task GeneticAlgorithmBuilder_ShouldBuildAlgorithm() {
    var randomSource = RandomSource.CreateDefault(42);
    var builder = new GeneticAlgorithmBuilderBasic<RealVector>()
      .WithPopulationSize(200)
      .WithCrossover(new SinglePointCrossover(randomSource))
      .WithMutation(new GaussianMutator(0.1, 0.1, randomSource))
      .WithMutationRate(0.05)
      .WithEvaluator(new MockEvaluator())
      .WithRandomSource(RandomSource.CreateDefault())
      .WithSelector(new ProportionalSelector<RealVector>(randomSource))
      .WithPlusSelectionReplacement()
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
