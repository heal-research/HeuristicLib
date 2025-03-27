using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Algorithms.GeneticAlgorithm;
using HEAL.HeuristicLib.Core;
using HEAL.HeuristicLib.Encodings;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Tests;

public class GeneticAlgorithmObservableTests {
  [Fact]
  public Task GeneticAlgorithm_ObservableFromExecutionStream() {
    var randomSource = new RandomSource(42);
    var encoding = new RealVectorEncodingParameter(2, -5, +5);
    var creator = new UniformDistributedCreator(minimum: null, maximum: 3.0);
    var crossover = new SinglePointCrossover();
    var mutator = new GaussianMutator(0.1, 0.1);
    var evaluator = Evaluator.UsingFitnessFunction<RealVector>(vector => vector.Sum());
    var selector = new RandomSelector();
    var replacement = new ElitismReplacer(0);
    var terminationCriterion = Terminator.OnGeneration<EvolutionResult<RealVector, RealVector>>(3);
    
    var ga = new GeneticAlgorithm<RealVector, RealVector, RealVectorEncodingParameter>(encoding, 2, creator, crossover, mutator, 0.5, evaluator, SingleObjective.Minimize, selector, replacement/*, randomSource, terminationCriterion*/);

    var stream = ga.CreateResultStream(randomSource.CreateRandomNumberGenerator(), terminator: terminationCriterion);

    var subject = new Subject<EvolutionResult<RealVector, RealVector>>();
    var observableResult = new List<EvolutionResult<RealVector, RealVector>>();
    subject
      .SubscribeOn(Scheduler.CurrentThread)
      .Subscribe(state => observableResult.Add(state));
    
    observableResult.ShouldBeEmpty();
      
    var result = stream
      .Select(x => { subject.OnNext(x); return x; })
      .Take(20)
      .ToList();
    
    observableResult.ShouldBe(result);

    return Verify(result)
      .IgnoreMembersWithType<TimeSpan>();
  }
}
