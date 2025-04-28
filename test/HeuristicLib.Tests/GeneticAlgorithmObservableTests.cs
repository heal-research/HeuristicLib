using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using HEAL.HeuristicLib.Algorithms.GeneticAlgorithm;
using HEAL.HeuristicLib.Encodings;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems;

namespace HEAL.HeuristicLib.Tests;

public class GeneticAlgorithmObservableTests {
  [Fact]
  public Task GeneticAlgorithm_ObservableFromExecutionStream() {
    var encoding = new RealVectorEncoding(2, -5, +5);
    var creator = new UniformDistributedCreator(minimum: null, maximum: 3.0);
    var crossover = new SinglePointCrossover();
    var mutator = new GaussianMutator(0.1, 0.1);
    var decoder = Decoder.Identity<RealVector>();
    var evaluator = Evaluator.FromFitnessFunction<RealVector>(vector => vector.Sum());
    var selector = new RandomSelector();
    var replacement = new ElitismReplacer(0);
    var terminator = Terminator.OnGeneration<GeneticAlgorithmResult<RealVector>>(3);
    var problem = new EncodedProblem<RealVector, RealVector, RealVectorEncoding> { Encoding = encoding, Decoder = decoder, Evaluator = evaluator, Objective = SingleObjective.Minimize };
    
    var ga = new GeneticAlgorithm<RealVector, RealVectorEncoding>(
      //Encoding = encoding,
      populationSize: 2, creator, crossover, mutator, 0.5,
      //Decoder = decoder, Evaluator = evaluator, Objective = SingleObjective.Minimize,
      selector, replacement, randomSeed: 42, terminator
    );

    var stream = ga.ExecuteStreaming(problem);

    var subject = new Subject<GeneticAlgorithmResult<RealVector>>();
    var observableResult = new List<GeneticAlgorithmResult<RealVector>>();
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
