using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Algorithms.GeneticAlgorithm;
using HEAL.HeuristicLib.Configuration;
using HEAL.HeuristicLib.Encodings;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems;

namespace HEAL.HeuristicLib.Tests;

public class GeneticAlgorithmBuilderWithEncodingTests {
  
  [Fact]
  public Task GeneticAlgorithm_ShouldSolveTSPWithBuilder() {
    var distances = new double[10, 10];
    var problem = new TravelingSalesmanProblem(distances);
    
    var builder = new GeneticAlgorithmBuilder<Permutation, PermutationEncoding>()
      .UsingProblem(problem)
      .WithGeneticAlgorithmSpec(new GeneticAlgorithmSpec<Permutation, PermutationEncoding> {
        PopulationSize = 5, 
        Selector = new TournamentSelectorSpec<Permutation>(),
        Replacer = new ElitistReplacerSpec<Permutation>(1)
      })
      .WithRandomSource(new RandomSource(42));
    
    var ga = builder.Build();
    
    return Verify(ga);
  }
}
