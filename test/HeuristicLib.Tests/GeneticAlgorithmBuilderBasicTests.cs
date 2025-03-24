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
      .WithCreator(new NormalDistributedCreator(0, 0.5, encoding))
      .WithCrossover(new SinglePointCrossover())
      .WithMutator(new GaussianMutator(0.1, 0.1, encoding))
      .WithMutationRate(0.05)
      .WithEvaluator(new MockEvaluator())
      .WithObjective(SingleObjective.Minimize)
      .WithSelector(new ProportionalSelector())
      .WithReplacer(new PlusSelectionReplacer())
      .WithTerminator(Terminator.OnGeneration(20));

    var ga = builder.Build();

    return Verify(ga);
  }
  
  [Fact]
  public Task GeneticAlgorithmBuilder_WithBuilderTypeChanging() {
    var randomSource = new RandomSource(42);
    var encoding = new RealVectorEncodingParameter(3, -5, +5);
    GeneticAlgorithmBuilder untypedBuilder = new GeneticAlgorithmBuilder()
      .WithRandomSource(randomSource)
      .WithPopulationSize(200)
      .WithMutationRate(0.05);
     
    var typedBuilder = untypedBuilder
      .UsingGenotype<RealVector>()
      .WithCreator(new NormalDistributedCreator(0, 0.5, encoding))
      .WithCrossover(new SinglePointCrossover())
      .WithMutator(new GaussianMutator(0.1, 0.1, encoding))
      .WithEvaluator(new MockEvaluator())
      .WithObjective(SingleObjective.Minimize)
      .WithSelector(new ProportionalSelector())
      .WithReplacer(new PlusSelectionReplacer())
      .WithTerminator(Terminator.OnGeneration(20));

    var ga = typedBuilder.Build();

    return Verify(new { Algorithm = ga, TypedBuilder = typedBuilder, UntypedBuilder = untypedBuilder });
  }
  
  [Fact]
  public Task GeneticAlgorithmBuilder_UsingEncodingParameter() {
    var randomSource = new RandomSource(42);
    var encoding = new RealVectorEncodingParameter(3, -5, +5);
    var builder = new GeneticAlgorithmBuilder<RealVector>()
      .WithRandomSource(randomSource)
      .WithPopulationSize(200)
      .UsingEncodingParameters(encoding)
      .WithCreator(enc => new NormalDistributedCreator(0, 0.5, enc))
      .WithCrossover(enc => new SinglePointCrossover())
      .WithMutator(enc => new GaussianMutator(0.1, 0.1, enc))
      .WithMutationRate(0.05)
      .WithEvaluator(new MockEvaluator())
      .WithObjective(SingleObjective.Minimize)
      .WithSelector(new ProportionalSelector())
      .WithReplacer(new PlusSelectionReplacer())
      .WithTerminator(Terminator.OnGeneration(20));

    var ga = builder.Build();

    return Verify(ga);
  }
  
  [Fact]
  public Task GeneticAlgorithmBuilder_WithSpec() {
    var randomSource = new RandomSource(42);
    var encoding = new RealVectorEncodingParameter(10, -5, +5);
    var spec = new GeneticAlgorithmSpec(
      PopulationSize: 500,
      Creator: new NormalRealVectorCreatorSpec(Mean: [1.5]),
      Crossover: new SinglePointRealVectorCrossoverSpec(),
      Mutator: new GaussianRealVectorMutatorSpec(Rate: 0.1, Strength: 0.1),
      MutationRate: 0.05,
      Selector: new TournamentSelectorSpec(TournamentSize: 4),
      Replacer: new ElitistReplacerSpec(2)
    );
    var builder = new GeneticAlgorithmBuilder<RealVector>()
      .WithRandomSource(randomSource).WithEvaluator(new MockEvaluator()).WithObjective(SingleObjective.Minimize)
      .UsingEncodingParameters(encoding)
      .WithSpecs(spec);

    var ga = builder.Build();
    
    return Verify(ga);
  }

  private class MockEvaluator : FitnessFunctionEvaluatorBase<RealVector> {
    public override Fitness Evaluate(RealVector solution) {
      return solution.Sum();
    }
  }
}
