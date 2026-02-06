using HEAL.HeuristicLib.Algorithms.Evolutionary;
using HEAL.HeuristicLib.Genotypes.Vectors;
using HEAL.HeuristicLib.Operators.Creators.RealVectorCreators;
using HEAL.HeuristicLib.Operators.Crossovers.RealVectorCrossovers;
using HEAL.HeuristicLib.Operators.Evaluators;
using HEAL.HeuristicLib.Operators.Mutators;
using HEAL.HeuristicLib.Operators.Mutators.RealVectorMutators;
using HEAL.HeuristicLib.Operators.Replacers;
using HEAL.HeuristicLib.Operators.Selectors;
using HEAL.HeuristicLib.Operators.Terminators;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.SearchSpaces.Vectors;

namespace HEAL.HeuristicLib.Tests;

public class DummyEvaluator
{
  public static readonly ObjectiveVector DummyObjectives = new(0.0);
}

public class DummyEvaluator<TGenotype, TSearchSpace, TProblem> : SingleSolutionStatelessEvaluator<TGenotype, TSearchSpace, TProblem>
  where TGenotype : class
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
{
  public override ObjectiveVector Evaluate(TGenotype solution, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem) => DummyEvaluator.DummyObjectives;
}

public class GeneticAlgorithmTests
{
  [Fact]
  public void GeneticAlgorithm_Create_WithConstructor()
  {
    var searchSpace = new RealVectorSearchSpace(10, -5, +5);
    var creator = new UniformDistributedCreator(null, 3.0);
    var crossover = new SinglePointCrossover();
    var mutator = new GaussianMutator(0.1, 0.1);
    //var decoder = Decoder.Identity<RealVector>();
    // var DirectEvaluator = new RealVectorMockEvaluator();
    var selector = new RandomSelector<RealVector>();
    var replacement = new PlusSelectionReplacer<RealVector>();
    var terminator = new AfterIterationsTerminator<RealVector>(5);

    var ga = new GeneticAlgorithm<RealVector, RealVectorSearchSpace> {
      PopulationSize = 200,
      Creator = creator,
      Crossover = crossover,
      Mutator = mutator.WithRate(0.05),
      Evaluator = new DummyEvaluator<RealVector, RealVectorSearchSpace, IProblem<RealVector, RealVectorSearchSpace>>(),
      Selector = selector,
      Replacer = replacement,
      //AlgorithmRandom = SystemRandomNumberGenerator.Default(42),
      //Terminator = terminator
    };
    //SearchSpace = searchSpace,

    //RandomSource = randomSource, Terminator = terminator

    //return Verify(ga);
  }

  // [Fact]
  // public Task GeneticAlgorithm_Create_FromConfig() {
  //   var creator = new UniformDistributedCreator(minimum: null, maximum: 3.0);
  //   var crossover = new SinglePointCrossover();
  //   var mutator = new GaussianMutator(0.1, 0.1);
  //   var selector = new RandomSelector<RealVector>();
  //   var replacement = new PlusSelectionReplacer<RealVector>();
  //   var terminator = new AfterIterationsTerminator<RealVector>(5);
  //   
  //   var config = new GeneticAlgorithmConfiguration<RealVector, RealVectorEncoding> {
  //     PopulationSize = 200, 
  //     Creator = creator, Crossover = crossover, Mutator = mutator, InitialMutationStrength = 0.05,
  //     Selector = selector, Replacer = replacement,
  //     RandomSeed = 42,
  //     Terminator = terminator
  //   };
  //   
  //   var ga = config.Build();
  //
  //   return Verify(ga);
  // }

  // [Fact]
  // public void GeneticAlgorithm_CreateThrows_FromUnderspecifiedConfig() {
  //   var creator = new UniformDistributedCreator(minimum: null, maximum: 3.0);
  //   var crossover = new SinglePointCrossover();
  //   var mutator = new GaussianMutator(0.1, 0.1);
  //  
  //   var config = new GeneticAlgorithmConfiguration<RealVector, RealVectorEncoding> {
  //     PopulationSize = 200, 
  //     Creator = creator, Crossover = crossover, Mutator = mutator, InitialMutationStrength = 0.05,
  //     Selector = null, Replacer = null, // missing
  //     RandomSeed = 42,
  //   };
  //
  //   Should.Throw<ValidationException>(() => GeneticAlgorithm.FromConfiguration(config));
  //   Should.Throw<ValidationException>(() => config.Build());
  // }

  // [Fact]
  // public Task GeneticAlgorithm_Execute() {
  //   var searchSpace = new RealVectorEncoding(3, -5, +5);
  //   var creator = new UniformDistributedCreator(minimum: null, maximum: 3.0);
  //   var crossover = new SinglePointCrossover();
  //   var mutator = new GaussianMutator(0.1, 0.1);
  //   var decoder = Decoder.Identity<RealVector>();
  //   // var DirectEvaluator = new RealVectorMockEvaluator();
  //   var selector = new RandomSelector<RealVector>();
  //   var replacement = new ElitismReplacer<RealVector>(0);
  //   var terminator = new AfterIterationsTerminator<RealVector>(5)
  //   ; 
  //   
  //   // var problem = new EncodedProblem<RealVector, RealVector, RealVectorSearchSpace> {
  //   //   SearchSpace = searchSpace, Decoder = decoder, DirectEvaluator = DirectEvaluator, Objective = SingleObjective.Minimize
  //   // };
  //   var problem = new TestFunctionProblem(new SphereFunction(3));
  //   
  //   var ga = new GeneticAlgorithm<RealVector, RealVectorEncoding>(
  //     populationSize: 5, 
  //     creator: creator, crossover: crossover, mutator: mutator, mutationRate: 0.5,
  //     selector: selector, replacer: replacement,
  //     randomSeed: 42, terminator: terminator
  //   );
  //
  //   var finalState = ga.CreateExecution(problem).Execute();
  //   
  //   return Verify(finalState)
  //     .IgnoreMembersWithType<TimeSpan>();
  // }
  //
  // [Fact]
  // public Task GeneticAlgorithm_ExecuteStream() {
  //   var searchSpace = new RealVectorEncoding(3, -5, +5);
  //   var creator = new UniformDistributedCreator(minimum: null, maximum: 3.0);
  //   var crossover = new SinglePointCrossover();
  //   var mutator = new GaussianMutator(0.1, 0.1);
  //   var decoder = Decoder.Identity<RealVector>();
  //   // var DirectEvaluator = new RealVectorMockEvaluator();
  //   var selector = new RandomSelector();
  //   var replacement = new ElitismReplacer(0);
  //   var terminator = Terminator.NeverTerminate<PopulationResult<RealVector>>();
  //   var problem = new RealVectorMockOptimizable();
  //   // var problem = new EncodedProblem<RealVector, RealVector, RealVectorSearchSpace> {
  //   //   SearchSpace = searchSpace, Decoder = decoder, DirectEvaluator = DirectEvaluator, Objective = SingleObjective.Minimize
  //   // };
  //   
  //   var ga = new GeneticAlgorithm<RealVector, RealVectorEncoding>(
  //     //SearchSpace = searchSpace,
  //     populationSize: 5, 
  //     creator: creator, crossover: crossover, mutator: mutator, mutationRate: 0.5,
  //     //Decoder = decoder, DirectEvaluator = DirectEvaluator, Objective = SingleObjective.Minimize,
  //     selector: selector, replacer: replacement,
  //     randomSeed: 42, terminator: terminator
  //   );
  //
  //   var stream = ga.ExecuteStreaming(problem);
  //
  //   var results = stream.Take(5).ToList();
  //   
  //   return Verify(results)
  //     .IgnoreMembersWithType<TimeSpan>();
  // }

  // [Fact]
  // public async Task GeneticAlgorithm_TerminateWithPauseToken() {
  //   var searchSpace = new RealVectorSearchSpace(5, -5, +5);
  //   var creator = new UniformDistributedCreator(minimum: null, maximum: null);
  //   var crossover = new SinglePointCrossover();
  //   var mutator = new GaussianMutator(0.1, 0.1);
  //   var decoder = Decoder.Identity<RealVector>();
  //   var DirectEvaluator = new RealVectorMockEvaluator();
  //   var selector = new ProportionalSelector();
  //   var replacement = new PlusSelectionReplacer();
  //   var pauseToken = new PauseToken();
  //   var terminator = new PauseTokenTerminator<PopulationIterationState<RealVector>>(pauseToken);
  //   var problem = new EncodedProblem<RealVector, RealVector, RealVectorSearchSpace> {
  //     SearchSpace = searchSpace, Decoder = decoder, DirectEvaluator = DirectEvaluator, Objective = SingleObjective.Minimize
  //   };
  //
  //   var firstAlg = new GeneticAlgorithm<RealVector, RealVectorSearchSpace>(
  //     //SearchSpace = searchSpace,
  //     populationSize: 5,
  //     creator: creator, crossover: crossover, mutator: mutator, mutationRate: 0.05, 
  //     //Decoder = decoder, DirectEvaluator = DirectEvaluator, Objective = SingleObjective.Minimize,
  //     selector: selector, replacer: replacement, 
  //     randomSeed: 42, terminator: terminator
  //   );
  //   
  //   var task = Task.Run(() => firstAlg.Execute(problem));
  //
  //   await Task.Delay(200);
  //   
  //   task.Status.ShouldBe(TaskStatus.Running);
  //   
  //   pauseToken.RequestPause();
  //
  //   var finalState = await task;
  //   
  //   
  //   task.Status.ShouldBe(TaskStatus.RanToCompletion);
  //   
  //   finalState.ShouldNotBeNull();
  //   finalState.TotalGenerations.ShouldBeGreaterThan(0);
  // }

  // [Fact]
  // public async Task GeneticAlgorithm_ExecuteAndContinueWithOtherAlg() {
  //   var searchSpace = new RealVectorEncoding(3, -5, +5);
  //   var creator = new UniformDistributedCreator(minimum: null, maximum: null);
  //   var crossover = new SinglePointCrossover();
  //   var mutator = new GaussianMutator(0.1, 0.1);
  //   var decoder = Decoder.Identity<RealVector>();
  //   // var DirectEvaluator = new RealVectorMockEvaluator();
  //   var selector = new ProportionalSelector();
  //   var replacement = new ElitismReplacer<RealVector, RealVectorEncoding>(0);
  //   var terminator = Terminator.OnGeneration<RealVector, RealVectorEncoding, PopulationResult<RealVector>>(5);
  //
  //   var problem = new RealVectorMockOptimizable();
  //   // var problem = new EncodedProblem<RealVector, RealVector, RealVectorSearchSpace> {
  //   //   SearchSpace = searchSpace, Decoder = decoder, DirectEvaluator = DirectEvaluator, Objective = SingleObjective.Minimize
  //   // };
  //   
  //   var firstAlg = new GeneticAlgorithm<RealVector, RealVectorEncoding>(
  //     populationSize: 5, 
  //     creator: creator, crossover: crossover, mutator: mutator, mutationRate: 0.05,
  //     selector: selector, replacer: replacement, 
  //     randomSeed: 42, terminator: terminator
  //   );
  //
  //   var firstResult = firstAlg.ExecuteStreaming(problem).Last();
  //
  //   var newTerminationCriterion = Terminator.OnGeneration<RealVector, RealVectorEncoding, PopulationResult<RealVector>>(12);
  //   var continuationState = firstResult.GetContinuationState();
  //   var secondAlg = firstAlg with {
  //     PopulationSize = 8, Terminator = newTerminationCriterion
  //   };
  //
  //   var finalState = secondAlg.CreateExecution(problem).Execute(initialState: continuationState);
  //
  //   await Verify(new { firstResult, finalState })
  //     .IgnoreMembersWithType<TimeSpan>();
  // }
  //
  // [Fact]
  // public void GeneticAlgorithm_ExecutingWithInitialStateThatShouldImmediatelyTerminate_DoesNotIterate() {
  //   var searchSpace = new RealVectorEncoding(3, -5, +5);
  //   var creator = new UniformDistributedCreator();
  //   var crossover = new SinglePointCrossover();
  //   var mutator = new GaussianMutator(0.1, 0.1);
  //   var decoder = Decoder.Identity<RealVector>();
  //   // var DirectEvaluator = new RealVectorMockEvaluator();
  //   var selector = new ProportionalSelector();
  //   var replacement = new ElitismReplacer<RealVector, RealVectorEncoding>(0);
  //   var terminator = Terminator.OnGeneration<RealVector, RealVectorEncoding, PopulationResult<RealVector>>(5);
  //
  //   var problem = new RealVectorMockOptimizable();
  //   // var problem = new EncodedProblem<RealVector, RealVector, RealVectorSearchSpace> {
  //   //   SearchSpace = searchSpace, Decoder = decoder, DirectEvaluator = DirectEvaluator, Objective = SingleObjective.Minimize
  //   // };
  //   var alg = new GeneticAlgorithm<RealVector, RealVectorEncoding>(
  //     populationSize: 5, 
  //     creator: creator, crossover: crossover, mutator: mutator, mutationRate: 0.05,
  //     selector: selector, replacer: replacement, 
  //     randomSeed: 42, terminator: terminator
  //   );
  //   var initialState = new GeneticAlgorithmState<RealVector> {
  //     Generation = 100, Population = []
  //   };
  //
  //   var iterationResults = alg.ExecuteStreaming(problem, initialState).ToList();
  //   iterationResults.ShouldBeEmpty();
  // }

  // private class RealVectorMockOptimizable : IOptimizable<RealVector, RealVectorSearchSpace> {
  //   public ObjectiveVector Evaluate(RealVector Solution) => Solution.Sum();
  //   public Objective Objective => SingleObjective.Minimize;
  //   public RealVectorSearchSpace ProblemContext => new RealVectorSearchSpace(3, -5, +5);
  // }
//
//   [Fact]
//   public async Task GeneticAlgorithm_WithMultiChromosomeGenotype() {
//     var randomSource = new RandomSource(42);
//     var realVectorSearchSpace = new RealVectorSearchSpace(5, -5, +5);
//     var permutationSearchSpace = new PermutationSearchSpace(3);
//     var multiGenotypeSearchSpace = new MultiGenotypeSearchSpace(realVectorSearchSpace, permutationSearchSpace);
//     
//     var creator = new MultiGenotypeCreator(new UniformDistributedCreator(null, null), new RandomPermutationCreator());
//     var crossover = new MultiGenotypeCrossover(new SinglePointCrossover(), new OrderCrossover());
//     var mutator = new MultiGenotypeMutator(new GaussianMutator(0.1, 0.1), new SwapMutator());
//     var DirectEvaluator = new MultiGenotypeEvaluator();
//     var selector = new RandomSelector<MultiGenotype, Fitness, Goal>();
//     var replacement = new ElitismReplacer<MultiGenotype>(0);
//     var terminationCriterion = Terminator.OnGeneration(10);
//     
//     var ga = new GeneticAlgorithm<MultiGenotype, MultiGenotypeSearchSpace>(searchSpace, 5, creator, crossover, mutator, 0.05, DirectEvaluator, Goal.Minimize, selector, replacement, randomSource, terminationCriterion);
//
//     var finalState = ga.Execute();
//
//     await Verify(finalState);
//   }
//
//   private record MultiGenotypeSearchSpace(RealVectorSearchSpace RealVectorSearchSpace, PermutationSearchSpace PermutationSearchSpace) : ISearchSpace<MultiGenotype, MultiGenotypeSearchSpace>> {
//
//     public bool IsValidGenotype(MultiGenotype genotype) {
//       return RealVectorSearchSpace.IsValidGenotype(genotype.RealVector) && PermutationSearchSpace.IsValidGenotype(genotype.Permutation);
//     }
//   }
//
//   private record MultiGenotype(RealVector RealVector, Permutation Permutation);/* : IRecordGenotypeBase<MultiGenotype, RealVector, Permutation> {
//     public static MultiGenotype Construct(RealVector realVector, Permutation permutation) => new MultiGenotype(realVector, permutation);
//     public void Deconstruct(out RealVector realVector, out Permutation permutation) { realVector = RealVector; permutation = Permutation; }
//   };*/
//   private class MultiGenotypeCreator(ICreator<RealVector> realVectorCreator, ICreator<Permutation> permutationCreator) : CreatorBase<MultiGenotype> {
//     public override MultiGenotype GetEvaluator() { return new MultiGenotype(realVectorCreator.GetEvaluator(), permutationCreator.GetEvaluator()); }
//   }
//   private class MultiGenotypeEvaluator : IEvaluator<MultiGenotype, Fitness> {
//     public Fitness Evaluate(MultiGenotype Solution) { return Solution.RealVector.Sum() + Solution.Permutation.Count; }
//   }
//   private class MultiGenotypeCrossover(ICrossover<RealVector> realVectorCrossover, ICrossover<Permutation> permutationCrossover) : ICrossover<MultiGenotype> {
//     public MultiGenotype Cross(MultiGenotype parent1, MultiGenotype parent2) {
//       return new MultiGenotype(realVectorCrossover.Cross(parent1.RealVector, parent2.RealVector), permutationCrossover.Cross(parent1.Permutation, parent2.Permutation));
//     }
//   }
//   private class MultiGenotypeMutator(IMutator<RealVector> realVectorMutator, IMutator<Permutation> permutationMutator) : IMutator<MultiGenotype> {
//     public MultiGenotype Mutate(MultiGenotype genotype) {
//       return new MultiGenotype(realVectorMutator.Mutate(genotype.RealVector), permutationMutator.Mutate(genotype.Permutation));
//     }
//   }
//
//   [Fact]
//   public async Task GeneticAlgorithm_WithMultiChromosomeGenotypeWithRecordOperators() {
//     var randomSource = new RandomSource(42);
//     var realVectorSearchSpace = new RealVectorSearchSpace(2, -5, +5);
//     var permutationSearchSpace = new PermutationSearchSpace(3);
//     var creator = new MultiGenotypeCreator(new UniformDistributedCreator(realVectorSearchSpace, null, null, randomSource), new RandomPermutationCreator(permutationSearchSpace, randomSource));
//     var crossover = new RecordCrossover<MultiGenotype, RealVector, Permutation>(new SinglePointCrossover(realVectorSearchSpace, randomSource), new OrderCrossover(permutationSearchSpace, randomSource));
//     var mutator = new MultiGenotypeMutator(new GaussianMutator(realVectorSearchSpace, 0.1, 0.1, randomSource), new SwapMutator(permutationSearchSpace, randomSource));
//     var DirectEvaluator = new MultiGenotypeEvaluator();
//     var selector = new RandomSelector<MultiGenotype, Fitness, Goal>(randomSource);
//     var replacement = new ElitismReplacer<MultiGenotype>(0);
//     var terminationCriterion = Terminator.OnGeneration(10);
//     
//     var ga = new GeneticAlgorithm<MultiGenotype>(5, creator, crossover, mutator, 0.05, DirectEvaluator, Goal.Minimize, selector, replacement, randomSource, terminationCriterion);
//
//     var finalState = ga.Execute();
//
//     await Verify(finalState);
//   }
}
