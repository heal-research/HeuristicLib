using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Algorithms.GeneticAlgorithm;
using HEAL.HeuristicLib.Core;
using HEAL.HeuristicLib.Encodings;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Tests;

public class GeneticAlgorithmBoundedTests {
  
  [Fact]
  public Task GeneticAlgorithmBuilder_WithBoundedAlgorithm() {
    var random = new RandomSource(42);
    var problem = new TravelingSalesmanProblem();
    var instance = TravelingSalesmanProblem.CreateDefaultInstance();
    var evaluator = Evaluator.FromProblem(problem, instance);

    var builder = new GeneticAlgorithmBuilder()
      .UsingEncoding<Permutation, Tour, PermutationEncodingParameter, PermutationEncoding<Tour>>(problem.GetEncoding())
      .WithEvaluator(evaluator)
      .WithPopulationSize(10)
      .WithMutationRate(0.1)
      .WithSelector(new ProportionalSelector())
      .WithReplacer(new ElitismReplacer(0))
      .WithObjective(SingleObjective.Minimize);
    
    GeneticAlgorithm<Permutation, Tour, PermutationEncodingParameter> ga = builder.Build();

    var boundedGa = ga.BindTo(instance.GetEncodingParameter());

    //var result = boundedGa.Execute(random.CreateRandomNumberGenerator(), Terminator.OnGeneration<EvolutionResult<Permutation, Tour>>(5));
    
    
    return Verify(boundedGa);
    // return Verify(result)
    //   .IgnoreMembersWithType<TimeSpan>();
  }
}
