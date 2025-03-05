using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Algorithms.GeneticAlgorithm;
using HEAL.HeuristicLib.Encodings;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems;

namespace HEAL.HeuristicLib.Tests;

public class GeneticAlgorithmBuilderWithEncodingTests {
  
  [Fact]
  public Task GeneticAlgorithm_ShouldSolveTSPWithBuilder() {
    // Define a small TSP instance (5 cities)
    var distances = new double[,] {
      { 0, 2, 9, 10, 6 },
      { 2, 0, 4, 8, 5 },
      { 9, 4, 0, 3, 7 },
      { 10, 8, 3, 0, 4 },
      { 6, 5, 7, 4, 0 }
    };

    var problem = new TravelingSalesmanProblem(distances);

    var encoding = problem.CreatePermutationEncoding();
    var config = problem.CreateGeneticAlgorithmDefaultConfig();
    var evaluator = problem.CreateEvaluator();
    var randomState = RandomSource.CreateDefault(42);

    var terminationCriterion = new ThresholdTerminator<PopulationState<Permutation>>(100, state => state.CurrentGeneration);
    var selector = new TournamentSelector<Permutation>(3, randomState);
    var replacement = new ElitismReplacer<Permutation>(2);

    var builder = new GeneticAlgorithmBuilder<PermutationEncoding, Permutation>()
      .WithProblemEncoding(problem);
      // .WithEncoding(encoding)
      // .WithConfiguration(config)
      // .WithTerminationCriterion(terminationCriterion)
      // .WithSelector(selector);


    var ga = builder.Build();

    var finalState = ga.Run();
    return Verify(finalState);
  }

  private class MockEvaluator : IEvaluator<RealVector, ObjectiveValue> {
    public ObjectiveValue Evaluate(RealVector solution) {
      return (solution.Sum(), ObjectiveDirection.Minimize);
    }
  }
}
