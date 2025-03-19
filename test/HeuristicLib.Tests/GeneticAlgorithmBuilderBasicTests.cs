using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Algorithms.GeneticAlgorithm;
using HEAL.HeuristicLib.Configuration;
using HEAL.HeuristicLib.Encodings;
using HEAL.HeuristicLib.Operators;

namespace HEAL.HeuristicLib.Tests;

public class GeneticAlgorithmBuilderBasicTests {
  [Fact]
  public Task GeneticAlgorithmBuilder_WithBuilder() {
    var randomSource = new RandomSource(42);
    var encoding = new RealVectorEncodingParameter(3, -5, +5);
    var builder = new GeneticAlgorithmBuilder<RealVector>()
      .WithRandomSource(randomSource)
      .WithPopulationSize(200)
      .WithCreator(new NormalDistributedCreator(0, 0.5, encoding, randomSource))
      .WithCrossover(new SinglePointCrossover(randomSource))
      .WithMutator(new GaussianMutator(0.1, 0.1, encoding, randomSource))
      .WithMutationRate(0.05)
      .WithEvaluator(new MockEvaluator())
      .WithGoal(Goal.Minimize)
      .WithSelector(new ProportionalSelector<RealVector>(randomSource))
      .WithReplacer(new PlusSelectionReplacer<RealVector>())
      .WithTerminator(Terminator.OnGeneration(20));

    var ga = builder.Build();

    return Verify(ga);
  }
  
  [Fact]
  public Task GeneticAlgorithmBuilder_UsingEncodingParameter() {
    var randomSource = new RandomSource(42);
    var encoding = new RealVectorEncodingParameter(3, -5, +5);
    var builder = new GeneticAlgorithmBuilder<RealVector>()
      .WithRandomSource(randomSource)
      .WithPopulationSize(200)
      .UsingEncodingParameters(encoding)
        .WithCreatorFactory(new NormalDistributedCreator.Factory(0, 0.5))
        .WithCrossoverFactory(new SinglePointCrossover.Factory())
        .WithMutatorFactory(new GaussianMutator.Factory(0.1, 0.1))
      .WithMutationRate(0.05)
      .WithEvaluator(new MockEvaluator())
      .WithGoal(Goal.Minimize)
      .WithSelector(new ProportionalSelector<RealVector>(randomSource))
      .WithReplacer(new PlusSelectionReplacer<RealVector>())
      .WithTerminator(Terminator.OnGeneration(20));

    var ga = builder.Build();

    return Verify(ga);
  }
  
  // [Fact]
  // public Task GeneticAlgorithmBuilder_WithSpec() {
  //   var randomSource = new RandomSource(42);
  //   var encoding = new RealVectorEncodingParameter(10, -5, +5);
  //   var spec = new GeneticAlgorithmSpec(
  //     PopulationSize: 500,
  //     Creator: new NormalRealVectorCreatorSpec(Mean: [1.5]),
  //     Crossover: new SinglePointRealVectorCrossoverSpec(),
  //     Mutator: new GaussianRealVectorMutatorSpec(Rate: 0.1, Strength: 0.1),
  //     MutationRate: 0.05,
  //     Selector: new TournamentSelectorSpec(TournamentSize: 4),
  //     Replacer: new ElitistReplacerSpec(2)
  //   );
  //   var builder = new GeneticAlgorithmBuilder<RealVector, RealVectorEncodingParameter>()
  //     .WithGeneticAlgorithmSpec(spec)
  //     .WithEncoding(encoding)
  //     .WithEvaluator(new MockEvaluator())
  //     .WithGoal(Goal.Minimize)
  //     .WithRandomSource(randomSource);
  //      
  //   var ga = builder.Build();
  //
  //   return Verify(ga);
  // }

  private class MockEvaluator : FitnessFunctionEvaluatorBase<RealVector, Fitness> {
    public override Fitness Evaluate(RealVector solution) {
      return solution.Sum();
    }
  }
}
