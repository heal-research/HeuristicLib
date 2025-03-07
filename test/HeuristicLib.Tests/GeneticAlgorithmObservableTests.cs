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
    var encoding = new RealVectorEncoding(2, -5, +5);
    var creator = new UniformDistributedCreator(encoding, minimum: null, maximum: 3.0, randomSource);
    var crossover = new SinglePointCrossover(encoding, randomSource);
    var mutator = new GaussianMutator(encoding, 0.1, 0.1, randomSource);
    var evaluator = Evaluator.Create((RealVector vector) => new ObjectiveValue(vector.Sum(), ObjectiveDirection.Minimize));
    var selector = new RandomSelector<RealVector, ObjectiveValue>(randomSource);
    var replacement = new ElitismReplacer<RealVector>(0);
    var terminationCriterion = Terminator.OnGeneration(3);
    
    var ga = new GeneticAlgorithm<RealVector>(2, creator, crossover, mutator, 0.5, evaluator, selector, replacement, randomSource, terminationCriterion);

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
