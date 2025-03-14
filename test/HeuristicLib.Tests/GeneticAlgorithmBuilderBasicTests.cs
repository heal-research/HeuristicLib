using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Algorithms.GeneticAlgorithm;
using HEAL.HeuristicLib.Configuration;
using HEAL.HeuristicLib.Encodings;
using HEAL.HeuristicLib.Operators;
using Xunit;

namespace HEAL.HeuristicLib.Tests;

public class GeneticAlgorithmBuilderBasicTests {
  [Fact]
  public Task GeneticAlgorithmBuilder_WithBuilder() {
    var randomSource = new RandomSource(42);
    var encoding = new RealVectorEncoding(3, -5, +5);
    var builder = new GeneticAlgorithmBuilder<RealVector, RealVectorEncoding>()
      .WithEncoding(encoding)
      .WithPopulationSize(200)
      .WithCreator((encoding, randomSource) => new NormalDistributedCreator(encoding, 0, 0.5, randomSource))
      .WithCrossover((encoding, randomSource) => new SinglePointCrossover(encoding, randomSource))
      .WithMutator((encoding, randomSource) => new GaussianMutator(encoding, 0.1, 0.1, randomSource))
      .WithMutationRate(0.05)
      .WithEvaluator(new MockEvaluator())
      .WithGoal(Goal.Minimize)
      .WithRandomSource(randomSource)
      .WithSelector((randomSource) => new ProportionalSelector<RealVector>(randomSource))
      .WithReplacer((randomSource) => new PlusSelectionReplacer<RealVector>())
      .WithTerminator(Terminator.OnGeneration(20));

    var ga = builder.Build();

    return Verify(ga);
  }
  
  [Fact]
  public Task GeneticAlgorithmBuilder_WithSpec() {
    var randomSource = new RandomSource(42);
    var encoding = new RealVectorEncoding(10, -5, +5);
    var spec = new GeneticAlgorithmSpec<RealVector, RealVectorEncoding>(
      PopulationSize: 500,
      Creator: new NormalRealVectorCreatorSpec(Mean: [1.5]),
      Crossover: new SinglePointRealVectorCrossoverSpec(),
      Mutator: new GaussianRealVectorMutatorSpec(Rate: 0.1, Strength: 0.1),
      MutationRate: 0.05,
      Selector: new TournamentSelectorSpec<RealVector>(TournamentSize: 4),
      Replacer: new ElitistReplacerSpec<RealVector>(2)
    );
    var builder = new GeneticAlgorithmBuilder<RealVector, RealVectorEncoding>()
      .WithGeneticAlgorithmSpec(spec)
      .WithEncoding(encoding)
      .WithEvaluator(new MockEvaluator())
      .WithGoal(Goal.Minimize)
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
