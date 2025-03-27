using HEAL.HeuristicLib.Algorithms.GeneticAlgorithm;
using HEAL.HeuristicLib.Encodings;
using HEAL.HeuristicLib.Problems;

namespace HEAL.HeuristicLib.Tests;

public class GeneticAlgorithmBuilderUsingProblemTests {
  
  [Fact]
  public Task GeneticAlgorithmBuilder_UsingProblem() {
    var problem = TravelingSalesmanProblem.CreateDefault();

    var builder = new GeneticAlgorithmBuilder<Permutation, Tour, PermutationEncodingParameter>()
      .WithFitnessFunctionFromProblem<GeneticAlgorithmBuilder<Permutation, Tour, PermutationEncodingParameter>, Permutation, Tour, PermutationEncodingParameter>(problem);
    
    return Verify(builder);
  }
  
  [Fact]
  public Task GeneticAlgorithmBuilder_UsingEncoding() {
    var problem = TravelingSalesmanProblem.CreateDefault();
    var encoding = problem.CreatePermutationEncoding();

    var builder = new GeneticAlgorithmBuilder()
      .UsingEncoding<Permutation, Tour, PermutationEncodingParameter, PermutationEncoding<Tour>>(encoding);
    
    return Verify(builder);
  }
}
