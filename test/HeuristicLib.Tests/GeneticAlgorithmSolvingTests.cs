using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Algorithms.GeneticAlgorithm;
using HEAL.HeuristicLib.Encodings;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems;

namespace HEAL.HeuristicLib.Tests;

public class GeneticAlgorithmSolvingTests {

  [Fact]
  public Task GeneticAlgorithm_SolveTestFunction() {
    var creator = new UniformDistributedCreator(minimum: null, maximum: 3.0);
    var crossover = new SinglePointCrossover();
    var mutator = new GaussianMutator(0.1, 0.1);
    var selector = new RandomSelector();
    var replacement = new ElitismReplacer(0);
    var terminator = Terminator.OnGeneration<GeneticAlgorithmResult<RealVector>>(5);

    var ga = new GeneticAlgorithm<RealVector, RealVectorEncoding>(
      populationSize: 5, 
      creator: creator, crossover: crossover, mutator: mutator, mutationRate: 0.5,
      selector: selector, replacer: replacement,
      randomSeed: 42, terminator: terminator
    );
    var problem = new TestFunctionProblem(new SphereFunction(dimension: 3));

    EvaluatedIndividual<RealVector, RealVector>? result = ga.Solve(problem);
    
    return Verify(result)
      .IgnoreMembersWithType<TimeSpan>();
  }
  
  [Fact]
  public Task GeneticAlgorithm_SolveStreamingTestFunction() {
    var creator = new UniformDistributedCreator(minimum: null, maximum: 3.0);
    var crossover = new SinglePointCrossover();
    var mutator = new GaussianMutator(0.1, 0.1);
    var selector = new RandomSelector();
    var replacement = new ElitismReplacer(0);
    var terminator = Terminator.OnGeneration<GeneticAlgorithmResult<RealVector>>(5);

    var ga = new GeneticAlgorithm<RealVector, RealVectorEncoding>(
      populationSize: 5, 
      creator: creator, crossover: crossover, mutator: mutator, mutationRate: 0.5,
      selector: selector, replacer: replacement,
      randomSeed: 42, terminator: terminator
    );
    var problem = new TestFunctionProblem(new SphereFunction(dimension: 3));

    List<EvaluatedIndividual<RealVector, RealVector>> results = ga.SolveStreaming(problem).ToList();
    
    return Verify(results)
      .IgnoreMembersWithType<TimeSpan>();
  }

  [Fact]
  public void GeneticAlgorithm_SolveAndSolveStreaming_HaveSameResults() {
    var creator = new UniformDistributedCreator(minimum: null, maximum: 3.0);
    var crossover = new SinglePointCrossover();
    var mutator = new GaussianMutator(0.1, 0.1);
    var selector = new RandomSelector();
    var replacement = new ElitismReplacer(0);
    var terminator = Terminator.OnGeneration<GeneticAlgorithmResult<RealVector>>(5);

    var ga = new GeneticAlgorithm<RealVector, RealVectorEncoding>(
      populationSize: 5, 
      creator: creator, crossover: crossover, mutator: mutator, mutationRate: 0.5,
      selector: selector, replacer: replacement,
      randomSeed: 42, terminator: terminator
    );
    var problem = new TestFunctionProblem(new SphereFunction(dimension: 3));

    var result = ga.Solve(problem);
    var streamingResult = ga.SolveStreaming(problem).Last();
    //var bestStreamingResult = streamingResults.MinBy(x => x.Fitness, problem.Objective.TotalOrderComparer);
    
    result.ShouldBe(streamingResult);
  }
  
  // [Fact]
  // public Task GeneticAlgorithmBuilder_UsingProblemFitness() {
  //   var problem = TravelingSalesmanProblem.CreateDefault();
  //   var evaluator = Evaluator.FromProblem(problem);
  //
  //   var builder = new GeneticAlgorithmBuilder<Permutation, Tour, PermutationEncoding>()
  //     .WithEvaluator(evaluator);
  //   
  //   return Verify(builder);
  // }
  
  // [Fact]
  // public Task GeneticAlgorithmBuilder_UsingProblemEncoding() {
  //   var problem = TravelingSalesmanProblem.CreateDefault();
  //   var encodedProblem = problem.EncodeAsPermutation();
  //
  //   // var builder = new GeneticAlgorithmBuilder()
  //   //   .UsingEncoding<Permutation, Tour, PermutationEncoding>(encoding);
  //   var config = encodedProblem.ToConfiguration<Permutation, PermutationEncoding>();
  //   
  //   return Verify(config);
  // }
  
    
  // [Fact]
  // public Task GeneticAlgorithmBuilder_UsingProblem() {
  //   var problem = TravelingSalesmanProblem.CreateDefault();
  //
  //   // var builder = new GeneticAlgorithmBuilder()
  //   //     .SolvingProblem(problem)
  //   //     .UsingPermutationEncoding();
  //   var config = problem.EncodeAsPermutation();
  //   
  //   return Verify(config);
  // }
}
