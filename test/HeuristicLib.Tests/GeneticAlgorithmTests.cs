using HEAL.HeuristicLib.ProofOfConcept;

namespace HEAL.HeuristicLib.Tests;

public class GeneticAlgorithmTests {
  [Fact]
  public Task GeneticAlgorithm_ShouldRunWithoutBuilder() {
    var creator = new RandomCreator(10, -5, 5);
    var crossover = new SinglePointCrossover();
    var mutator = new GaussianMutator(0.1, 0.1);
    var evaluator = new MockEvaluator();
    var selector = new ProportionalSelector<RealVector>();
    var replacement = new PlusSelectionReplacement<RealVector>();
    var terminationCriterion = new ThresholdCriterion<PopulationState<RealVector>>(50, state => state.CurrentGeneration);
    var random = new Random();

    var ga = new GeneticAlgorithm<RealVector>(
      200, creator, crossover, mutator, 0.05, terminationCriterion, evaluator, random, selector, replacement
    );

    var finalState = ga.Run();

    return Verify(finalState);
  }

  [Fact]
  public async Task GeneticAlgorithm_ShouldPauseAndContinue() {
    var creator = new RandomCreator(10, -5, 5);
    var crossover = new SinglePointCrossover();
    var mutator = new GaussianMutator(0.1, 0.1);
    var evaluator = new MockEvaluator();
    var selector = new ProportionalSelector<RealVector>();
    var replacement = new PlusSelectionReplacement<RealVector>();
    var pauseToken = new PauseToken();
    var terminationCriterion = new PauseTokenCriterion<PopulationState<RealVector>>(pauseToken);
    var random = new Random();

    var ga = new GeneticAlgorithm<RealVector>(
      200, creator, crossover, mutator, 0.05, terminationCriterion, evaluator, random, selector, replacement
    );

    var task = Task.Run(() => ga.Run());

    await Task.Delay(1000);
    pauseToken.RequestPause();

    var pausedState = await task;
    Assert.True(pausedState.CurrentGeneration > 0);

    // Continue running the GA
    var newTerminationCriterion = new ThresholdCriterion<PopulationState<RealVector>>(pausedState.CurrentGeneration + 50, state => state.CurrentGeneration);
    var continuedGA = new GeneticAlgorithm<RealVector>(
      200, creator, crossover, mutator, 0.05, newTerminationCriterion, evaluator, random, selector, replacement
    );

    var finalState = continuedGA.Run(pausedState);
    Assert.True(finalState.CurrentGeneration > pausedState.CurrentGeneration);

    await Verify(finalState);
  }

  private class MockEvaluator : IEvaluator<RealVector> {
    public double Evaluate(RealVector solution) {
      return solution.Sum();
    }
  }
}
