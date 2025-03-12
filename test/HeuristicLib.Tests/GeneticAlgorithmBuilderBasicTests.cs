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
    var randomSource = new RandomSource(42);
    var encoding = new RealVectorEncoding(3, -5, +5);
    var config = new GeneticAlgorithmConfig<RealVector, RealVectorEncoding>() {
      Encoding = encoding,
      PopulationSize = 200,
      CreatorFactory = (encoding, randomSource) => new NormalDistributedCreator(encoding, 0, 0.5, randomSource),
      CrossoverFactory = (encoding, randomSource) => new SinglePointCrossover(encoding, randomSource),
      MutatorFactory = (encoding, randomSource) => new GaussianMutator(encoding, 0.1, 0.1, randomSource),
      MutationRate = 0.05,
      EvaluatorFactory = (encoding) => new MockEvaluator(),
      Goal = Goal.Minimize,
      RandomSource = randomSource,
      SelectorFactory = (randomSource) => new ProportionalSelector<RealVector>(randomSource),
      ReplacementFactory = (randomSource) => new PlusSelectionReplacer<RealVector>(),
      Terminator = Terminator.OnGeneration(20)
    };
    
    var builder = new GeneticAlgorithmBuilder<RealVectorEncoding, RealVector>()
      .WithConfig(config);

    var ga = builder.Build();

    return Verify(ga);
  }
  
  [Fact]
  public Task GeneticAlgorithmBuilder_WithSpec() {
    var randomSource = new RandomSource(42);
    var encoding = new RealVectorEncoding(10, -5, +5);
    var spec = new GeneticAlgorithmSpec(
      PopulationSize: 500,
      Creator: new NormalRealVectorCreatorSpec(Mean: [1.5]),
      Crossover: new SinglePointRealVectorCrossoverSpec(),
      Mutator: new GaussianRealVectorMutatorSpec(Rate: 0.1, Strength: 0.1),
      MutationRate: 0.05,
      Selector: new TournamentSelectorSpec(TournamentSize: 4),
      Replacer: new ElitistReplacerSpec(2)
    );
    var builder = new GeneticAlgorithmBuilder<RealVectorEncoding, RealVector>()
      .WithSpecs(spec)
      .WithEncoding(encoding)
      .WithEvaluator(new MockEvaluator())
      .Minimizing()
      .WithRandomSource(randomSource);
       
    var ga = builder.Build();

    return Verify(ga);
  }

  private class MockEvaluator : IEvaluator<RealVector, Fitness> {
    public Fitness Evaluate(RealVector solution) {
      return solution.Sum();
    }
  }
}
