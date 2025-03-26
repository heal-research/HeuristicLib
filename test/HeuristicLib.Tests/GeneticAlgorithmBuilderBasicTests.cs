using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Algorithms.GeneticAlgorithm;
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
      .WithCreator(new NormalDistributedCreatorOperator(0, 0.5, encoding, randomSource.CreateRandomNumberGenerator()))
      .WithCrossover(new SinglePointCrossoverOperator(randomSource.CreateRandomNumberGenerator()))
      .WithMutator(new GaussianMutatorOperator(0.1, 0.1, encoding, randomSource.CreateRandomNumberGenerator()))
      .WithMutationRate(0.05)
      .WithEvaluator(new MockEvaluatorOperator())
      .WithObjective(SingleObjective.Minimize)
      .WithSelector(new ProportionalSelectorOperator(randomSource.CreateRandomNumberGenerator()))
      .WithReplacer(new PlusSelectionReplacerOperator())
      .WithTerminator(TerminatorOperator.OnGeneration(20));

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
      .WithCreator(new NormalDistributedCreatorOperator(0, 0.5, encoding, randomSource.CreateRandomNumberGenerator()))
      .WithCrossover(new SinglePointCrossoverOperator(randomSource.CreateRandomNumberGenerator()))
      .WithMutator(new GaussianMutatorOperator(0.1, 0.1, encoding, randomSource.CreateRandomNumberGenerator()))
      .WithEvaluator(new MockEvaluatorOperator())
      .WithObjective(SingleObjective.Minimize)
      .WithSelector(new ProportionalSelectorOperator(randomSource.CreateRandomNumberGenerator()))
      .WithReplacer(new PlusSelectionReplacerOperator())
      .WithTerminator(TerminatorOperator.OnGeneration(20));

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
      .WithCreator(new NormalRealVectorCreator([0.0], [0.5]))
      .WithCrossover(new SinglePointRealVectorCrossover())
      .WithMutator(new GaussianRealVectorMutator(0.1, 0.1))
      .WithMutationRate(0.05)
      .WithEvaluator(new MockEvaluatorOperator())
      .WithObjective(SingleObjective.Minimize)
      .WithSelector(new ProportionalSelectorOperator(randomSource.CreateRandomNumberGenerator()))
      .WithReplacer(new PlusSelectionReplacerOperator())
      .WithTerminator(TerminatorOperator.OnGeneration(20));

    var ga = builder.Build();

    return Verify(ga);
  }
  
  [Fact]
  public Task GeneticAlgorithmBuilder_WithSpec() {
    var spec = new GeneticAlgorithmConfiguration<RealVector, RealVectorEncodingParameter>(
      PopulationSize: 500,
      Creator: new NormalRealVectorCreator(Mean: [1.5]),
      Crossover: new SinglePointRealVectorCrossover(),
      Mutator: new GaussianRealVectorMutator(Rate: 0.1, Strength: 0.1),
      MutationRate: 0.05,
      Selector: new TournamentSelector(TournamentSize: 4),
      Replacer: new ElitistReplacer(2)
    );
    var builder = new GeneticAlgorithmBuilder<RealVector>()
      .WithRandomSource(new RandomSource(42)).WithEvaluator(new MockEvaluatorOperator()).WithObjective(SingleObjective.Minimize)
      .UsingEncodingParameters(new RealVectorEncodingParameter(10, -5, +5))
      .WithConfiguration(spec);

    var ga = builder.Build();
    
    return Verify(ga);
  }

  private class MockEvaluatorOperator : FitnessFunctionEvaluatorOperatorBase<RealVector> {
    public override Fitness Evaluate(RealVector solution) {
      return solution.Sum();
    }
  }
}
