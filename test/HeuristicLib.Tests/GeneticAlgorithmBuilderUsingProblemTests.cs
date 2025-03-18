using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Algorithms.GeneticAlgorithm;
using HEAL.HeuristicLib.Configuration;
using HEAL.HeuristicLib.Encodings;
using HEAL.HeuristicLib.Problems;

namespace HEAL.HeuristicLib.Tests;

public class GeneticAlgorithmBuilderUsingProblemTests {
  
  [Fact]
  public Task GeneticAlgorithm_ShouldSolveTSPWithBuilder() {
    var problem = TravelingSalesmanProblem.CreateDefault();
    
    var builder = new GeneticAlgorithmBuilder<Permutation, PermutationEncoding>()
      .UsingProblem(problem)
      .WithGeneticAlgorithmSpec(new GeneticAlgorithmSpec {
        PopulationSize = 5, 
        Selector = new TournamentSelectorSpec(),
        Replacer = new ElitistReplacerSpec(1)
      })
      .WithRandomSource(new RandomSource(42));
    
    var ga = builder.Build();
    
    return Verify(ga);
  }
}
