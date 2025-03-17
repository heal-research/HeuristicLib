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
    var creator = new UniformDistributedCreator(minimum: null, maximum: 3.0);
    var crossover = new SinglePointCrossover();
    var mutator = new GaussianMutator(0.1, 0.1);
    var evaluator = Evaluator.Create((RealVector vector) => new Fitness(vector.Sum()));
    var selector = new RandomSelector<RealVector, Fitness, Goal>();
    var replacement = new ElitismReplacer<RealVector>(0);
    var terminationCriterion = Terminator.OnGeneration(3);
    
    var ga = new GeneticAlgorithm<RealVector, RealVectorEncoding>(encoding, 2, creator, crossover, mutator, 0.5, evaluator, Goal.Minimize, selector, replacement, randomSource, terminationCriterion);

    var stream = ga.CreateExecutionStream();

    var subject = new Subject<PopulationState<RealVector, Fitness, Goal>>();
    var observableResult = new List<PopulationState<RealVector, Fitness, Goal>>();
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
