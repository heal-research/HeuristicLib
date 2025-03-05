using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Algorithms.GeneticAlgorithm;
using HEAL.HeuristicLib.Configuration;
using HEAL.HeuristicLib.Encodings;
using HEAL.HeuristicLib.Operators;
using Xunit;

namespace HEAL.HeuristicLib.Tests;

public class GeneticAlgorithmBuilderBasicTests {
  [Fact]
  public Task GeneticAlgorithmBuilder_WithConfigObject() {
    var randomSource = RandomSource.CreateDefault(42);
    var encoding = new RealVectorEncoding(10, -5, +5);
    var builder = new GeneticAlgorithmBuilder<RealVectorEncoding, RealVector>()
      .WithConfig(new GeneticAlgorithmConfig<RealVector, RealVectorEncoding>() {
        Encoding = encoding,
        PopulationSize = 200,
        CreatorFactory = (encoding, randomSource) => new NormalDistributedCreator(encoding, 0, 0.5, randomSource),
        CrossoverFactory = (encoding, randomSource) => new SinglePointCrossover(encoding, randomSource),
        MutatorFactory = (encoding, randomSource) => new GaussianMutator(encoding, 0.1, 0.1, randomSource),
        MutationRate = 0.05,
        EvaluatorFactory = (encoding) => new MockEvaluator(),
        RandomSource = randomSource,
        SelectorFactory = (randomSource) => new ProportionalSelector<RealVector>(randomSource),
        ReplacementFactory = (randomSource) => new PlusSelectionReplacer<RealVector>(),
        Terminator = new ThresholdTerminator<PopulationState<RealVector>>(50, state => state.CurrentGeneration)
      });

    var ga = builder.Build();

    return Verify(ga);
  }
  
  [Fact]
  public Task GeneticAlgorithmBuilder_WithSpec() {
    var randomSource = RandomSource.CreateDefault(42);
    var encoding = new RealVectorEncoding(10, -5, +5);
    var builder = new GeneticAlgorithmBuilder<RealVectorEncoding, RealVector>()
      .WithSpecs(new GeneticAlgorithmSpec(
        PopulationSize: 200,
        Creator: new NormalRealVectorCreatorSpec(Mean: [0.0], StandardDeviation: [0]),
        Crossover: new SinglePointRealVectorCrossoverSpec(),
        Mutator: new GaussianRealVectorMutatorSpec(Rate: 0.1, Strength: 0.1),
        MutationRate: 0.05,
        Selector: new TournamentSelectorSpec(TournamentSize: 4)))
      .WithEncoding(encoding)
      .WithRandomSource(randomSource);
      // others are missing
       
    var ga = builder.Build();

    return Verify(ga);
  }

  private class MockEvaluator : IEvaluator<RealVector, ObjectiveValue> {
    public ObjectiveValue Evaluate(RealVector solution) {
      return (solution.Sum(), ObjectiveDirection.Minimize);
    }
  }
}
