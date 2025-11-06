// using System.Reactive.Concurrency;
// using System.Reactive.Linq;
// using System.Reactive.Subjects;
// using HEAL.HeuristicLib.Algorithms.GeneticAlgorithm;
// using HEAL.HeuristicLib.Encodings;
// using HEAL.HeuristicLib.Genotypes;
// using HEAL.HeuristicLib.Operators;
// using HEAL.HeuristicLib.Operators.RealVectorOperators;
// using HEAL.HeuristicLib.Optimization;
// using HEAL.HeuristicLib.Problems;
//
// namespace HEAL.HeuristicLib.Tests;
//
// public class GeneticAlgorithmObservableTests {
//   [Fact]
//   public Task GeneticAlgorithm_ObservableFromExecutionStream() {
//     var searchSpace = new RealVectorEncoding(2, -5, +5);
//     var creator = new UniformDistributedCreator(minimum: null, maximum: 3.0);
//     var crossover = new SinglePointCrossover();
//     var mutator = new GaussianMutator(0.1, 0.1);
//     var decoder = Decoder.Identity<RealVector>();
//     //var DirectEvaluator = DirectEvaluator.FromFitnessFunction<RealVector>(vector => vector.Sum());
//     var selector = new RandomSelector();
//     var replacement = new ElitismReplacer<RealVector, RealVectorEncoding>(0);
//     var terminator = Terminator.OnGeneration<RealVector, RealVectorEncoding, PopulationResult<RealVector>>(3);
//     //var problem = new EncodedProblem<RealVector, RealVector, RealVectorSearchSpace> { SearchSpace = searchSpace, Decoder = decoder, DirectEvaluator = DirectEvaluator, Objective = SingleObjective.Minimize };
//     var problem = new RealVectorMockOptimizable();
//     
//     var ga = new GeneticAlgorithm<RealVector, RealVectorEncoding>(
//       //SearchSpace = searchSpace,
//       populationSize: 2, creator, crossover, mutator, 0.5,
//       //Decoder = decoder, DirectEvaluator = DirectEvaluator, Objective = SingleObjective.Minimize,
//       selector, replacement, randomSeed: 42, terminator
//     );
//
//     var stream = ga.ExecuteStreaming(problem);
//
//     var subject = new Subject<PopulationResult<RealVector>>();
//     var observableResult = new List<PopulationResult<RealVector>>();
//     subject
//       .SubscribeOn(Scheduler.CurrentThread)
//       .Subscribe(state => observableResult.Add(state));
//     
//     observableResult.ShouldBeEmpty();
//       
//     var result = stream
//       .Select(x => { subject.OnNext(x); return x; })
//       .Take(20)
//       .ToList();
//     
//     observableResult.ShouldBe(result);
//
//     return Verify(result)
//       .IgnoreMembersWithType<TimeSpan>();
//   }
//   
//   private class RealVectorMockOptimizable : IOptimizable<RealVector, RealVectorEncoding> {
//     public ObjectiveVector Evaluate(RealVector solution) => solution.Sum();
//     public Objective Objective => SingleObjective.Minimize;
//     public RealVectorEncoding ProblemContext => new RealVectorEncoding(2, -5, +5);
//   }
// }


