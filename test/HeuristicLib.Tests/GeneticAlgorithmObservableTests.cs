using System.Reactive.Concurrency;
using System.Reactive.Linq;
using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Algorithms.GeneticAlgorithm;
using HEAL.HeuristicLib.Encodings;
using HEAL.HeuristicLib.Operators;
using Shouldly;

namespace HEAL.HeuristicLib.Tests;

public class GeneticAlgorithmObservableTests {
  [Fact]
  public Task GeneticAlgorithm_ShouldRunWithoutBuilder() {
    var randomSource = new RandomSource(42);
    var encoding = new RealVectorEncoding(10, -5, +5);
    var creator = new UniformDistributedCreator(encoding, minimum: null, maximum: 3.0, randomSource);
    var crossover = new SinglePointCrossover(encoding, randomSource);
    var mutator = new GaussianMutator(encoding, 0.1, 0.1, randomSource);
    var evaluator = Evaluator.Create((RealVector vector) => new ObjectiveValue(vector.Sum(), ObjectiveDirection.Minimize));
    var selector = new RandomSelector<RealVector, ObjectiveValue>(randomSource);
    var replacement = new PlusSelectionReplacer<RealVector>();
    var terminationCriterion = new ThresholdTerminator<PopulationState<RealVector>>(50, state => state.Generation);
    
    var ga = new GeneticAlgorithm<RealVector>(
      200, creator, crossover, mutator, 0.05, evaluator, selector, replacement, randomSource, terminationCriterion);

    var stream = ga.CreateExecutionStream();

    var observableResult = new List<PopulationState<RealVector>>();
    stream
      .ToObservable(Scheduler.CurrentThread)
      .Subscribe(state => observableResult.Add(state));

    observableResult.ShouldBeEmpty();
      
    var result = stream.Take(20).ToList();
    
    observableResult.ShouldBe(result);
    
    return Verify(result);
  }
}
