using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Algorithms.GeneticAlgorithm;
using HEAL.HeuristicLib.Encodings;
using HEAL.HeuristicLib.Operators;

namespace HEAL.HeuristicLib.Tests;

public class GeneticAlgorithmObservableTests {
  [Fact]
  public Task GeneticAlgorithm_ObservableFromExecutionStream() {
    var randomSource = new RandomSource(42);
    var encoding = new RealVectorEncodingParameter(2, -5, +5);
    var creator = new UniformDistributedCreatorOperator(minimum: null, maximum: 3.0, encoding, randomSource.CreateRandomNumberGenerator());
    var crossover = new SinglePointCrossoverOperator(randomSource.CreateRandomNumberGenerator());
    var mutator = new GaussianMutatorOperator(0.1, 0.1, encoding, randomSource.CreateRandomNumberGenerator());
    var evaluator = EvaluatorOperator.UsingFitnessFunction<RealVector>(vector => vector.Sum());
    var selector = new RandomSelectorOperator(randomSource.CreateRandomNumberGenerator());
    var replacement = new ElitismReplacerOperator(0);
    var terminationCriterion = TerminatorOperator.OnGeneration(3);
    
    var ga = new GeneticAlgorithm<RealVector>(2, creator, crossover, mutator, 0.5, evaluator, SingleObjective.Minimize, selector, replacement, randomSource, terminationCriterion);

    var stream = ga.CreateExecutionStream();

    var subject = new Subject<PopulationState<RealVector>>();
    var observableResult = new List<PopulationState<RealVector>>();
    subject
      .SubscribeOn(Scheduler.CurrentThread)
      .Subscribe(state => observableResult.Add(state));
    
    observableResult.ShouldBeEmpty();
      
    var result = stream
      .Select(x => { subject.OnNext(x); return x; })
      .Take(20)
      .ToList();
    
    observableResult.ShouldBe(result);
    
    return Verify(result);
  }
}
