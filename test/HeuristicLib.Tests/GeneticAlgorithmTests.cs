using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Algorithms.GeneticAlgorithm;
using HEAL.HeuristicLib.Encodings;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems;

namespace HEAL.HeuristicLib.Tests;

public class GeneticAlgorithmTests {
  [Fact]
  public Task GeneticAlgorithm_ShouldRunWithoutBuilder() {
    var random = RandomGenerator.CreateDefault(42);
    var creator = new UniformDistributedCreator(10, -5, 5, random);
    var crossover = new SinglePointCrossover(random);
    var mutator = new GaussianMutator(0.1, 0.1, random);
    var evaluator = new MockEvaluator();
    var selector = new RandomSelector<RealVector, ObjectiveValue>(random);
    var replacement = new PlusSelectionReplacer<RealVector>();
    var terminationCriterion = new ThresholdTerminator<PopulationState<RealVector>>(50, state => state.CurrentGeneration);
    
    var ga = new GeneticAlgorithm<RealVector>(
      200, creator, crossover, mutator, 0.05, terminationCriterion, evaluator, random, selector, replacement
    );

    var finalState = ga.Run();

    return Verify(finalState);
  }

  [Fact]
  public async Task GeneticAlgorithm_ShouldPauseAndContinue() {
    var random = RandomGenerator.CreateDefault(42);
    var creator = new UniformDistributedCreator(10, -5, 5, random);
    var crossover = new SinglePointCrossover(random);
    var mutator = new GaussianMutator(0.1, 0.1, random);
    var evaluator = new MockEvaluator();
    var selector = new ProportionalSelector<RealVector>(random);
    var replacement = new PlusSelectionReplacer<RealVector>();
    var pauseToken = new PauseToken();
    var terminationCriterion = new PauseTokenTerminator<PopulationState<RealVector>>(pauseToken);

    var ga = new GeneticAlgorithm<RealVector>(
      200, creator, crossover, mutator, 0.05, terminationCriterion, evaluator, random, selector, replacement
    );

    var task = Task.Run(() => ga.Run());

    await Task.Delay(1000);
    pauseToken.RequestPause();

    var pausedState = await task;
    Assert.True(pausedState.CurrentGeneration > 0);

    // Continue running the GA
    var newTerminationCriterion = new ThresholdTerminator<PopulationState<RealVector>>(pausedState.CurrentGeneration + 50, state => state.CurrentGeneration);
    var continuedGA = new GeneticAlgorithm<RealVector>(
      200, creator, crossover, mutator, 0.05, newTerminationCriterion, evaluator, random, selector, replacement
    );

    var finalState = continuedGA.Run(pausedState);
    Assert.True(finalState.CurrentGeneration > pausedState.CurrentGeneration);

    await Verify(finalState);
  }

  [Fact]
  public Task GeneticAlgorithm_ShouldSolveTSPWithBuilder()
  {
    // Define a small TSP instance (5 cities)
    var distances = new double[,] {
      { 0, 2, 9, 10, 6 },
      { 2, 0, 4, 8, 5 },
      { 9, 4, 0, 3, 7 },
      { 10, 8, 3, 0, 4 },
      { 6, 5, 7, 4, 0 }
    };

    var problem = new TravelingSalesmanProblem(distances);
    var encoding = problem.CreatePermutationEncodingBundle();
    var evaluator = problem.CreateEvaluator();
    var random = RandomGenerator.CreateDefault(42);

    var terminationCriterion = new ThresholdTerminator<PopulationState<Permutation>>(100, state => state.CurrentGeneration);
    var selector = new TournamentSelector<Permutation>(3, random);
    var replacement = new ElitismReplacer<Permutation>(2);
    //var creator = encoding.CreatorFactory(new CreatorParameters(5, random));
    
    var ga = new GeneticAlgorithm<Permutation>(
      50, 
      null!,
      encoding.Crossover,
      new SwapMutation(),
      0.1,
      terminationCriterion,
      evaluator,
      random,
      selector,
      replacement
    );

    var finalState = ga.Run();
    return Verify(finalState);
  }

  private class MockEvaluator : IEvaluator<RealVector, ObjectiveValue> {
    public ObjectiveValue Evaluate(RealVector solution) {
      return (solution.Sum(), ObjectiveDirection.Minimize);
    }
  }
}
