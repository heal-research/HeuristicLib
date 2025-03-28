using HEAL.HeuristicLib.Algorithms.GeneticAlgorithm;
using HEAL.HeuristicLib.Encodings;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems;

namespace HEAL.HeuristicLib.Tests;

public class GeneticAlgorithmBuilderUsingProblemTests {
  
  [Fact]
  public Task GeneticAlgorithmBuilder_UsingProblemFitness() {
    var problem = new TravelingSalesmanProblem();
    var instance = TravelingSalesmanProblem.CreateDefaultInstance();
    var evaluator = Evaluator.FromProblem(problem, instance);

    var builder = new GeneticAlgorithmBuilder<Permutation, Tour, PermutationEncodingParameter>()
        .WithEvaluator(evaluator);
    
    return Verify(builder);
  }
  
  [Fact]
  public Task GeneticAlgorithmBuilder_UsingProblemEncoding() {
    var problem = new TravelingSalesmanProblem();
    //var instance = TravelingSalesmanProblem.CreateDefaultInstance();
    PermutationEncoding<Tour> encoding = problem.GetEncoding();

    var builder = new GeneticAlgorithmBuilder()
      .UsingEncoding<Permutation, Tour, PermutationEncodingParameter, PermutationEncoding<Tour>>(encoding);
    
    return Verify(builder);
  }
  
    
  // [Fact]
  // public Task GeneticAlgorithmBuilder_UsingProblem() {
  //   var problem = TravelingSalesmanProblem.CreateDefault();
  //
  //   var builder = new GeneticAlgorithmBuilder()
  //       .SolvingProblem(problem)
  //       .UsingPermutationEncoding();
  //   
  //   return Verify(builder);
  // }
}
