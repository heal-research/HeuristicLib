using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Algorithms.GeneticAlgorithm;
using HEAL.HeuristicLib.Core;
using HEAL.HeuristicLib.Encodings;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Tests;

public class GeneticAlgorithmBuilderBasicTests {
  [Fact]
  public Task GeneticAlgorithmBuilder_WithBuilder() {
    var randomSource = new RandomSource(42);
    var encoding = new RealVectorEncodingParameter(3, -5, +5);
    var builder = new GeneticAlgorithmBuilder<RealVector, RealVector, RealVectorEncodingParameter>()
      //.WithRandomSource(randomSource)
      //.WithEncodingParameter(encoding)
      .WithPopulationSize(200)
      .WithCreator(new NormalDistributedCreator(0, 0.5))
      .WithCrossover(new SinglePointCrossover())
      .WithMutator(new GaussianMutator(0.1, 0.1))
      .WithMutationRate(0.05)
      .WithDecoder(Decoder.Identity<RealVector>())
      .WithEvaluator(new MockEvaluator())
      .WithObjective(SingleObjective.Minimize)
      .WithSelector(new ProportionalSelector())
      .WithReplacer(new PlusSelectionReplacer());
      //.WithTerminator(Terminator.OnGeneration(20));

    var ga = builder.Build();

    return Verify(ga);
  }
  
  [Fact]
  public Task GeneticAlgorithmBuilder_WithBuilderTypeChanging() {
    var randomSource = new RandomSource(42);
    var encoding = new RealVectorEncodingParameter(3, -5, +5);
    GeneticAlgorithmBuilder untypedBuilder = new GeneticAlgorithmBuilder()
      //.WithRandomSource(randomSource)
      .WithPopulationSize(200)
      .WithMutationRate(0.05);

    var typedBuilder = untypedBuilder
      .UsingGenotype<RealVector, RealVector, RealVectorEncodingParameter>()
      //.WithEncodingParameter(encoding)
      .WithCreator(new NormalDistributedCreator(0, 0.5))
      .WithCrossover(new SinglePointCrossover())
      .WithMutator(new GaussianMutator(0.1, 0.1))
      .WithDecoder(Decoder.Identity<RealVector>())
      .WithEvaluator(new MockEvaluator())
      .WithObjective(SingleObjective.Minimize)
      .WithSelector(new ProportionalSelector())
      .WithReplacer(new PlusSelectionReplacer());
      //.WithTerminator(Terminator.OnGeneration(20));

    var ga = typedBuilder.Build();

    return Verify(new { Algorithm = ga, TypedBuilder = typedBuilder, UntypedBuilder = untypedBuilder });
  }
  
  // [Fact]
  // public Task GeneticAlgorithmBuilder_UsingEncodingParameter() {
  //   var randomSource = new RandomSource(42);
  //   var encoding = new RealVectorEncodingParameter(3, -5, +5);
  //   var builder = new GeneticAlgorithmBuilder/*<RealVector, RealVector, RealVectorEncodingParameter>*/()
  //     //.WithRandomSource(randomSource)
  //     //.WithEncodingParameter(encoding)
  //     .WithPopulationSize(200)
  //     .UsingEncodingParameters<RealVector, RealVector, RealVectorEncodingParameter>(encoding)
  //     .WithCreator(new NormalDistributedCreator(0.0, 0.5))
  //     .WithCrossover(new SinglePointCrossover())
  //     .WithMutator(new GaussianMutator(0.1, 0.1))
  //     .WithMutationRate(0.05)
  //     .WithDecoder(Decoder.Identity<RealVector>())
  //     .WithEvaluator(new MockEvaluator())
  //     .WithObjective(SingleObjective.Minimize)
  //     .WithSelector(new ProportionalSelector())
  //     .WithReplacer(new PlusSelectionReplacer());
  //     //.WithTerminator(Terminator.OnGeneration(20));
  //
  //   var ga = builder.Build();
  //
  //   return Verify(ga);
  // }
  
  [Fact]
  public Task GeneticAlgorithmBuilder_WithSpec() {
    var spec = new GeneticAlgorithmConfiguration<RealVector, RealVectorEncodingParameter>(
      PopulationSize: 500,
      Creator: new NormalDistributedCreator(1.5, 1.0),
      Crossover: new SinglePointCrossover(),
      Mutator: new GaussianMutator(0.1, 0.1),
      MutationRate: 0.05,
      Selector: new TournamentSelector(tournamentSize: 4),
      Replacer: new ElitismReplacer(2)
    );
    var builder = new GeneticAlgorithmBuilder<RealVector, RealVector, RealVectorEncodingParameter>()
      //.WithRandomSource(new RandomSource(42))
      .WithDecoder(Decoder.Identity<RealVector>())
      .WithEvaluator(new MockEvaluator()).WithObjective(SingleObjective.Minimize)
      //.WithEncodingParameter(new RealVectorEncodingParameter(10, -5, +5))
      .WithConfiguration(spec);

    var ga = builder.Build();
    
    return Verify(ga);
  }

  private class MockEvaluator : FitnessFunctionEvaluatorBase<RealVector> {
    public override Fitness Evaluate(RealVector phenotype) {
      return phenotype.Sum();
    }
  }
}
