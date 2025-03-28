using HEAL.HeuristicLib.Algorithms.GeneticAlgorithm;
using HEAL.HeuristicLib.Encodings;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems;

namespace HEAL.HeuristicLib.Tests;

public class GeneticAlgorithmBuilderUsingProblemTests {
  
  [Fact]
  public Task GeneticAlgorithmBuilder_UsingProblemFitness() {
    var problem = TravelingSalesmanProblem.CreateDefault();
    var evaluator = Evaluator.FromProblem(problem);

    var builder = new GeneticAlgorithmBuilder<Permutation, Tour, PermutationEncodingParameter>()
        .WithEvaluator(evaluator);
    
    return Verify(builder);
  }
  
  [Fact]
  public Task GeneticAlgorithmBuilder_UsingProblemEncoding() {
    var problem = TravelingSalesmanProblem.CreateDefault();
    var encoding = problem.CreatePermutationEncoding();

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
