// using System.Text;
// using HEAL.HeuristicLib.Algorithms;
// using HEAL.HeuristicLib.Algorithms.GeneticAlgorithm;
// using HEAL.HeuristicLib.Core;
// using HEAL.HeuristicLib.SearchSpaces;
// using HEAL.HeuristicLib.Operators;
// using HEAL.HeuristicLib.Problems;
// using HEAL.HeuristicLib.Random;
// using Decoder=HEAL.HeuristicLib.Operators.Decoder;
//
// namespace HEAL.HeuristicLib.Tests;
//
// public class GeneticAlgorithmBuilderBasicTests {
//   [Fact]
//   public Task GeneticAlgorithmBuilder_WithBuilder() {
//     var randomSource = new RandomSource(42);
//     var searchSpace = new RealVectorSearchSpace(3, -5, +5);
//     var builder = new GeneticAlgorithmConfiguration<RealVector, RealVectorSearchSpace>() {
//       //RandomSource = randomSource,
//       RandomSeed = 42,
//       //SearchSpace = searchSpace,
//       PopulationSize = 200,
//       Creator = new NormalDistributedCreator(0, 0.5),
//       Crossover = new SinglePointCrossover(),
//       Mutator = new GaussianMutator(0.1, 0.1),
//       InitialMutationStrength = 0.05,
//       // Decoder = Decoder.Identity<RealVector>(),
//       // DirectEvaluator = new MockEvaluator(),
//       // Objective = SingleObjective.Minimize,
//       Selector = new ProportionalSelector(),
//       Replacer = new PlusSelectionReplacer()
//     };
//     //.WithTerminator(Terminator.OnGeneration(20));
//
//     var ga = builder.Build();
//
//     return Verify(ga);
//   }
//   //
//   // [Fact]
//   // public Task GeneticAlgorithmBuilder_WithBuilderTypeChanging() {
//   //   var randomSource = new RandomSource(42);
//   //   var searchSpace = new RealVectorSearchSpace(3, -5, +5);
//   //   GeneticAlgorithmConfiguration untypedConfig = new GeneticAlgorithmConfiguration() {
//   //     RandomSource = randomSource,
//   //     PopulationSize = 200,
//   //     InitialMutationStrength = 0.05
//   //   };
//   //
//   //
//   //   GeneticAlgorithmConfiguration<RealVector, RealVector, RealVectorSearchSpace> typedConfig = new GeneticAlgorithmConfiguration<RealVector, RealVector, RealVectorSearchSpace>() {
//   //     SearchSpace = searchSpace,
//   //     Creator = new NormalDistributedCreator(0, 0.5),
//   //     Crossover = new SinglePointCrossover(),
//   //     Mutator = new GaussianMutator(0.1, 0.1),
//   //     Decoder = Decoder.Identity<RealVector>(),
//   //     DirectEvaluator = new MockEvaluator(),
//   //     Objective = SingleObjective.Minimize,
//   //     Selector = new ProportionalSelector(),
//   //     Replacer = new PlusSelectionReplacer()
//   //   };
//   //   // GeneticAlgorithmConfiguration<RealVector, RealVector, RealVectorSearchSpace> typedConfig = ((GeneticAlgorithmConfiguration<RealVector, RealVector, RealVectorSearchSpace>)((GeneticAlgorithmConfiguration<RealVector, RealVectorSearchSpace>)untypedConfig))
//   //   //   .CombineWith(
//   //   var combinedConfig = GeneticAlgorithmConfiguration.Combine(untypedConfig, typedConfig);
//   //   //.WithTerminator(Terminator.OnGeneration(20));
//   //
//   //   var ga = combinedConfig.Build();
//   //
//   //   return Verify(new { Algorithm = ga, CombinedConfig = combinedConfig, TypedConfig = typedConfig, UntypedConfig = untypedConfig });
//   // }
//   
//   // [Fact]
//   // public Task GeneticAlgorithmBuilder_UsingSearchSpace) {
//   //   var randomSource = new RandomSource(42);
//   //   var searchSpace = new RealVectorSearchSpace(3, -5, +5);
//   //   var builder = new GeneticAlgorithmBuilder/*<RealVector, RealVector, RealVectorSearchSpace>*/()
//   //     //.WithRandomSource(randomSource)
//   //     //.WithSearchSpace(searchSpace)
//   //     .WithPopulationSize(200)
//   //     .UsingSearchSpaces<RealVector, RealVector, RealVectorSearchSpace>(searchSpace)
//   //     .WithCreator(new NormalDistributedCreator(0.0, 0.5))
//   //     .WithCrossover(new SinglePointCrossover())
//   //     .WithMutator(new GaussianMutator(0.1, 0.1))
//   //     .WithMutationRate(0.05)
//   //     .WithDecoder(Decoder.Identity<RealVector>())
//   //     .WithEvaluator(new MockEvaluator())
//   //     .WithObjective(SingleObjective.Minimize)
//   //     .WithSelector(new ProportionalSelector())
//   //     .WithReplacer(new PlusSelectionReplacer());
//   //     //.WithTerminator(Terminator.OnGeneration(20));
//   //
//   //   var ga = builder.Build();
//   //
//   //   return Verify(ga);
//   // }
//   
//   // [Fact]
//   // public Task GeneticAlgorithmBuilder_WithSpec() {
//   //   var config = new GeneticAlgorithmConfiguration<RealVector, RealVectorSearchSpace> {
//   //     PopulationSize = 500,
//   //     Creator = new NormalDistributedCreator(1.5, 1.0),
//   //     Crossover = new SinglePointCrossover(),
//   //     Mutator = new GaussianMutator(0.1, 0.1),
//   //     InitialMutationStrength = 0.05,
//   //     Selector = new TournamentSelector(tournamentSize: 4),
//   //     Replacer = new ElitismReplacer(2)
//   //   };
//   //   var additionalConfig = new GeneticAlgorithmConfiguration<RealVector, RealVector, RealVectorSearchSpace>() {
//   //     RandomSource = new RandomSource(42),
//   //     Decoder = Decoder.Identity<RealVector>(),
//   //     DirectEvaluator = new MockEvaluator(),
//   //     Objective = SingleObjective.Minimize,
//   //     SearchSpace = new RealVectorSearchSpace(10, -5, +5)
//   //   };
//   //
//   //   var combinedConfig = config + additionalConfig; 
//   //
//   //   var ga = combinedConfig.Build();
//   //   
//   //   return Verify(ga);
//   // }
//
//   private class MockEvaluator : EvaluatorBase<RealVector> {
//     public override Fitness PredictAndTrain(RealVector phenotype) {
//       return phenotype.Sum();
//     }
//   }
// }



