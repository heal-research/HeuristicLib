namespace HEAL.HeuristicLib.Tests;

using Algorithms;
using Algorithms.GeneticAlgorithm;
using Encodings;
using Operators;
using Problems;

public class GeneticAlgorithmTests {
  [Fact]
  public Task GeneticAlgorithm_ShouldRunWithoutBuilder() {
    var randomSource = new SeededRandomSource(42);
    var creator = new UniformDistributedCreator(10, -5, 5, randomSource);
    var crossover = new SinglePointCrossover(randomSource);
    var mutator = new GaussianMutator(0.1, 0.1, randomSource);
    var evaluator = new MockEvaluator();
    var selector = new RandomSelector<RealVector, ObjectiveValue>(randomSource);
    var replacement = new PlusSelectionReplacer<RealVector>();
    var terminationCriterion = new ThresholdTerminator<PopulationState<RealVector>>(50, state => state.CurrentGeneration);
    
    var ga = new GeneticAlgorithm<RealVector>(
      200, creator, crossover, mutator, 0.05, terminationCriterion, evaluator, randomSource, selector, replacement
    );

    var finalState = ga.Run();

    return Verify(finalState);
  }

  [Fact]
  public async Task GeneticAlgorithm_ShouldPauseAndContinue() {
    var randomSource = new SeededRandomSource(42);
    var creator = new UniformDistributedCreator(10, -5, 5, randomSource);
    var crossover = new SinglePointCrossover(randomSource);
    var mutator = new GaussianMutator(0.1, 0.1, randomSource);
    var evaluator = new MockEvaluator();
    var selector = new ProportionalSelector<RealVector>(randomSource);
    var replacement = new PlusSelectionReplacer<RealVector>();
    var pauseToken = new PauseToken();
    var terminationCriterion = new PauseTokenTerminator<PopulationState<RealVector>>(pauseToken);

    var ga = new GeneticAlgorithm<RealVector>(
    200, creator, crossover, mutator, 0.05, terminationCriterion, evaluator, randomSource, selector, replacement
    );

    var task = Task.Run(() => ga.Run());

    await Task.Delay(1000);
    pauseToken.RequestPause();

    var pausedState = await task;
    Assert.True(pausedState.CurrentGeneration > 0);

    // Continue running the GA
    var newTerminationCriterion = new ThresholdTerminator<PopulationState<RealVector>>(pausedState.CurrentGeneration + 50, state => state.CurrentGeneration);
    var continuedGA = new GeneticAlgorithm<RealVector>(
    200, creator, crossover, mutator, 0.05, newTerminationCriterion, evaluator, randomSource, selector, replacement
    );

    var finalState = continuedGA.Run(pausedState);
    Assert.True(finalState.CurrentGeneration > pausedState.CurrentGeneration);

    await Verify(finalState);
  }

  private class MockEvaluator : IEvaluator<RealVector, ObjectiveValue> {
    public ObjectiveValue Evaluate(RealVector solution) {
      return (solution.Sum(), ObjectiveDirection.Minimize);
    }
  }
}
