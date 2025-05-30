using HEAL.HeuristicLib.Algorithms.GeneticAlgorithm;
using HEAL.HeuristicLib.Genotypes;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.PermutationSpace;
using HEAL.HeuristicLib.Problems.TravelingSalesman;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Tests;

public class ProblemSpecificOperatorTests {

  [Fact]
  public void TestOperatorCompatability() {
    var problemAgnosticCreator = new RandomPermutationCreator();
    var problemSpecificCreator = new TspCreator();
    
    ProblemAgnosticMethod(problemAgnosticCreator);
    //ProblemAgnosticMethod(problemSpecificCreator); // should not compile
    
    ProblemSpecificMethod(problemAgnosticCreator); // should do with implicit conversion
    ProblemSpecificMethod(new ProblemSpecificCreator<Permutation, PermutationSearchSpace, TravelingSalesmanProblem>(problemAgnosticCreator)); // should do with implicit conversion
    ProblemSpecificMethod(problemSpecificCreator);
  }
  private static void ProblemAgnosticMethod(Creator<Permutation, PermutationSearchSpace> creator) {}
  private static void ProblemSpecificMethod(Creator<Permutation, PermutationSearchSpace, TravelingSalesmanProblem> creator) {}
  
  [Fact]
  public Task GeneticAlgorithm_ExecuteWithTspSample() {
    var ga = new GeneticAlgorithm<Permutation, PermutationSearchSpace, TravelingSalesmanProblem>(
      populationSize: 5, 
      creator: new TspCreator(), 
      crossover: new OrderCrossover(), mutator: new InversionMutator(), mutationRate: 0.5,
      selector: new RandomSelector(), replacer: new ElitismReplacer<Permutation, PermutationSearchSpace>(0),
      randomSeed: 42, terminator: Terminator.OnGeneration<Permutation, PermutationSearchSpace, GeneticAlgorithmResult<Permutation>>(5)
    );

    var problem = TravelingSalesmanProblem.CreateDefault();
    
    var finalState = ga.CreateExecution(problem).Execute();
    
    return Verify(finalState)
      .IgnoreMembersWithType<TimeSpan>();
  }
}
