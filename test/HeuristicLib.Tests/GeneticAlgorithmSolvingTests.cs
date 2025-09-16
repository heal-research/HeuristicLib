// using HEAL.HeuristicLib.Algorithms;
// using HEAL.HeuristicLib.Algorithms.GeneticAlgorithm;
// using HEAL.HeuristicLib.Encodings;
// using HEAL.HeuristicLib.Genotypes;
// using HEAL.HeuristicLib.Operators;
// using HEAL.HeuristicLib.Operators.RealVectorOperators;
// using HEAL.HeuristicLib.Optimization;
// using HEAL.HeuristicLib.Problems;
// using HEAL.HeuristicLib.Problems.TestFunctions;
//
// namespace HEAL.HeuristicLib.Tests;
//
// public class GeneticAlgorithmSolvingTests {
//
//   [Fact]
//   public Task GeneticAlgorithm_SolveTestFunction() {
//     var creator = new UniformDistributedCreator(minimum: null, maximum: 3.0);
//     var crossover = new SinglePointCrossover();
//     var mutator = new GaussianMutator(0.1, 0.1);
//     var selector = new RandomSelector();
//     var replacement = new ElitismReplacer<RealVector, RealVectorEncoding>(0);
//     var terminator = Terminator.OnGeneration<RealVector, RealVectorEncoding, GeneticAlgorithmResult<RealVector>>(5);
//
//     var ga = new GeneticAlgorithm<RealVector, RealVectorEncoding>(
//       populationSize: 5, 
//       creator: creator, crossover: crossover, mutator: mutator, mutationRate: 0.5,
//       selector: selector, replacer: replacement,
//       randomSeed: 42, terminator: terminator
//     );
//     var problem = new TestFunctionProblem(new SphereFunction(dimension: 3));
//
//     Solution<RealVector>? result = ga.Solve(problem);
//     //Solution<RealVector, RealVector>? result = AlgorithmSolveExtensions.Solve<RealVector, RealVector, RealVectorSearchSpace, TestFunctionProblem, GeneticAlgorithmState<RealVector>, GeneticAlgorithmResult<RealVector>>(ga, problem);
//     
//     return Verify(result)
//       .IgnoreMembersWithType<TimeSpan>();
//   }
//   
//   [Fact]
//   public Task GeneticAlgorithm_SolveStreamingTestFunction() {
//     var creator = new UniformDistributedCreator(minimum: null, maximum: 3.0);
//     var crossover = new SinglePointCrossover();
//     var mutator = new GaussianMutator(0.1, 0.1);
//     var selector = new RandomSelector();
//     var replacement = new ElitismReplacer<RealVector, RealVectorEncoding>(0);
//     var terminator = Terminator.OnGeneration<RealVector, RealVectorEncoding, GeneticAlgorithmResult<RealVector>>(5);
//
//     var ga = new GeneticAlgorithm<RealVector, RealVectorEncoding>(
//       populationSize: 5, 
//       creator: creator, crossover: crossover, mutator: mutator, mutationRate: 0.5,
//       selector: selector, replacer: replacement,
//       randomSeed: 42, terminator: terminator
//     );
//     var problem = new TestFunctionProblem(new SphereFunction(dimension: 3));
//
//     List<Solution<RealVector>> results = ga.SolveStreaming(problem).ToList();
//     
//     return Verify(results)
//       .IgnoreMembersWithType<TimeSpan>();
//   }
//
//   [Fact]
//   public void GeneticAlgorithm_SolveAndSolveStreaming_HaveSameResults() {
//     var creator = new UniformDistributedCreator(minimum: null, maximum: 3.0);
//     var crossover = new SinglePointCrossover();
//     var mutator = new GaussianMutator(0.1, 0.1);
//     var selector = new RandomSelector();
//     var replacement = new ElitismReplacer<RealVector, RealVectorEncoding>(0);
//     var terminator = Terminator.OnGeneration<RealVector, RealVectorEncoding, GeneticAlgorithmResult<RealVector>>(5);
//
//     var ga = new GeneticAlgorithm<RealVector, RealVectorEncoding>(
//       populationSize: 5, 
//       creator: creator, crossover: crossover, mutator: mutator, mutationRate: 0.5,
//       selector: selector, replacer: replacement,
//       randomSeed: 42, terminator: terminator
//     );
//     var problem = new TestFunctionProblem(new SphereFunction(dimension: 3));
//
//     var result = ga.Solve(problem);
//     var streamingResult = ga.SolveStreaming(problem).Last();
//     //var bestStreamingResult = streamingResults.MinBy(x => x.Fitness, problem.Objective.TotalOrderComparer);
//     
//     result.ShouldBe(streamingResult);
//   }
//   
//   // [Fact]
//   // public Task GeneticAlgorithmBuilder_UsingProblemFitness() {
//   //   var problem = TravelingSalesmanProblem.CreateDefault();
//   //   var Evaluator = Evaluator.FromProblem(problem);
//   //
//   //   var builder = new GeneticAlgorithmBuilder<Permutation, Tour, PermutationSearchSpace>()
//   //     .WithEvaluator(Evaluator);
//   //   
//   //   return Verify(builder);
//   // }
//   
//   // [Fact]
//   // public Task GeneticAlgorithmBuilder_UsingProblemSearchSpace() {
//   //   var problem = TravelingSalesmanProblem.CreateDefault();
//   //   var encodedProblem = problem.EncodeAsPermutation();
//   //
//   //   // var builder = new GeneticAlgorithmBuilder()
//   //   //   .UsingSearchSpace<Permutation, Tour, PermutationSearchSpace>(searchSpace);
//   //   var config = encodedProblem.ToConfiguration<Permutation, PermutationSearchSpace>();
//   //   
//   //   return Verify(config);
//   // }
//   
//     
//   // [Fact]
//   // public Task GeneticAlgorithmBuilder_UsingProblem() {
//   //   var problem = TravelingSalesmanProblem.CreateDefault();
//   //
//   //   // var builder = new GeneticAlgorithmBuilder()
//   //   //     .SolvingProblem(problem)
//   //   //     .UsingPermutationSearchSpace();
//   //   var config = problem.EncodeAsPermutation();
//   //   
//   //   return Verify(config);
//   // }
// }


