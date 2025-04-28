using FluentValidation;
using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Algorithms.GeneticAlgorithm;
using HEAL.HeuristicLib.Encodings;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Tests;

public class GeneticAlgorithmTests {
  [Fact]
  public Task GeneticAlgorithm_Create_WithConstructor() {
    var encoding = new RealVectorEncoding(10, -5, +5);
    var creator = new UniformDistributedCreator(minimum: null, maximum: 3.0);
    var crossover = new SinglePointCrossover();
    var mutator = new GaussianMutator(0.1, 0.1);
    var decoder = Decoder.Identity<RealVector>();
    var evaluator = new RealVectorMockEvaluator();
    var selector = new RandomSelector();
    var replacement = new PlusSelectionReplacer();
    var terminator = Terminator.OnGeneration<GeneticAlgorithmIterationResult<RealVector>>(5);
    
    var ga = new GeneticAlgorithm<RealVector, RealVectorEncoding>(
      //Encoding = encoding,
      populationSize: 200, 
      creator: creator, crossover: crossover, mutator: mutator, mutationRate: 0.05,
      //Decoder = decoder, Evaluator = evaluator, Objective = SingleObjective.Minimize,
      selector: selector, replacer: replacement,
      randomSeed: 42,
      terminator: terminator
      //RandomSource = randomSource, Terminator = terminator
    );

    return Verify(ga);
  }
  
  [Fact]
  public Task GeneticAlgorithm_Create_FromConfig() {
    var creator = new UniformDistributedCreator(minimum: null, maximum: 3.0);
    var crossover = new SinglePointCrossover();
    var mutator = new GaussianMutator(0.1, 0.1);
    var selector = new RandomSelector();
    var replacement = new PlusSelectionReplacer();
    var terminator = Terminator.OnGeneration<GeneticAlgorithmIterationResult<RealVector>>(5);
    
    var config = new GeneticAlgorithmConfiguration<RealVector, RealVectorEncoding> {
      PopulationSize = 200, 
      Creator = creator, Crossover = crossover, Mutator = mutator, MutationRate = 0.05,
      Selector = selector, Replacer = replacement,
      RandomSeed = 42,
      Terminator = terminator
    };
    
    var ga = config.Build();
  
    return Verify(ga);
  }
  
  [Fact]
  public void GeneticAlgorithm_CreateThrows_FromUnderspecifiedConfig() {
    var creator = new UniformDistributedCreator(minimum: null, maximum: 3.0);
    var crossover = new SinglePointCrossover();
    var mutator = new GaussianMutator(0.1, 0.1);
   
    var config = new GeneticAlgorithmConfiguration<RealVector, RealVectorEncoding> {
      PopulationSize = 200, 
      Creator = creator, Crossover = crossover, Mutator = mutator, MutationRate = 0.05,
      Selector = null, Replacer = null, // missing
      RandomSeed = 42,
    };

    Should.Throw<ValidationException>(() => GeneticAlgorithm.FromConfiguration(config));
    Should.Throw<ValidationException>(() => config.Build());
  }
  
  [Fact]
  public Task GeneticAlgorithm_Execute() {
    var encoding = new RealVectorEncoding(3, -5, +5);
    var creator = new UniformDistributedCreator(minimum: null, maximum: 3.0);
    var crossover = new SinglePointCrossover();
    var mutator = new GaussianMutator(0.1, 0.1);
    var decoder = Decoder.Identity<RealVector>();
    var evaluator = new RealVectorMockEvaluator();
    var selector = new RandomSelector();
    var replacement = new ElitismReplacer(0);
    var terminator = Terminator.OnGeneration<GeneticAlgorithmIterationResult<RealVector>>(5);
    
    var problem = new EncodedProblem<RealVector, RealVector, RealVectorEncoding> {
      Encoding = encoding, Decoder = decoder, Evaluator = evaluator, Objective = SingleObjective.Minimize
    };
    
    var ga = new GeneticAlgorithm<RealVector, RealVectorEncoding>(
      populationSize: 5, 
      creator: creator, crossover: crossover, mutator: mutator, mutationRate: 0.5,
      selector: selector, replacer: replacement,
      randomSeed: 42, terminator: terminator
    );

    var finalState = ga.Execute(problem);
    
    return Verify(finalState)
      .IgnoreMembersWithType<TimeSpan>();
  }
  
  [Fact]
  public Task GeneticAlgorithm_ExecuteStream() {
    var encoding = new RealVectorEncoding(3, -5, +5);
    var creator = new UniformDistributedCreator(minimum: null, maximum: 3.0);
    var crossover = new SinglePointCrossover();
    var mutator = new GaussianMutator(0.1, 0.1);
    var decoder = Decoder.Identity<RealVector>();
    var evaluator = new RealVectorMockEvaluator();
    var selector = new RandomSelector();
    var replacement = new ElitismReplacer(0);
    var terminator = Terminator.NeverTerminate<GeneticAlgorithmIterationResult<RealVector>>();
    var problem = new EncodedProblem<RealVector, RealVector, RealVectorEncoding> {
      Encoding = encoding, Decoder = decoder, Evaluator = evaluator, Objective = SingleObjective.Minimize
    };
    
    var ga = new GeneticAlgorithm<RealVector, RealVectorEncoding>(
      //Encoding = encoding,
      populationSize: 5, 
      creator: creator, crossover: crossover, mutator: mutator, mutationRate: 0.5,
      //Decoder = decoder, Evaluator = evaluator, Objective = SingleObjective.Minimize,
      selector: selector, replacer: replacement,
      randomSeed: 42, terminator: terminator
    );

    var stream = ga.ExecuteStreaming(problem);

    var results = stream.Take(5).ToList();
    
    return Verify(results)
      .IgnoreMembersWithType<TimeSpan>();
  }
  
  // [Fact]
  // public async Task GeneticAlgorithm_TerminateWithPauseToken() {
  //   var encoding = new RealVectorEncoding(5, -5, +5);
  //   var creator = new UniformDistributedCreator(minimum: null, maximum: null);
  //   var crossover = new SinglePointCrossover();
  //   var mutator = new GaussianMutator(0.1, 0.1);
  //   var decoder = Decoder.Identity<RealVector>();
  //   var evaluator = new RealVectorMockEvaluator();
  //   var selector = new ProportionalSelector();
  //   var replacement = new PlusSelectionReplacer();
  //   var pauseToken = new PauseToken();
  //   var terminator = new PauseTokenTerminator<GeneticAlgorithmIterationResult<RealVector>>(pauseToken);
  //   var problem = new EncodedProblem<RealVector, RealVector, RealVectorEncoding> {
  //     Encoding = encoding, Decoder = decoder, Evaluator = evaluator, Objective = SingleObjective.Minimize
  //   };
  //
  //   var firstAlg = new GeneticAlgorithm<RealVector, RealVectorEncoding>(
  //     //Encoding = encoding,
  //     populationSize: 5,
  //     creator: creator, crossover: crossover, mutator: mutator, mutationRate: 0.05, 
  //     //Decoder = decoder, Evaluator = evaluator, Objective = SingleObjective.Minimize,
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
  
  [Fact]
  public async Task GeneticAlgorithm_ExecuteAndContinueWithOtherAlg() {
    var encoding = new RealVectorEncoding(3, -5, +5);
    var creator = new UniformDistributedCreator(minimum: null, maximum: null);
    var crossover = new SinglePointCrossover();
    var mutator = new GaussianMutator(0.1, 0.1);
    var decoder = Decoder.Identity<RealVector>();
    var evaluator = new RealVectorMockEvaluator();
    var selector = new ProportionalSelector();
    var replacement = new ElitismReplacer(0);
    var terminator = Terminator.OnGeneration<GeneticAlgorithmIterationResult<RealVector>>(5);
    
    var problem = new EncodedProblem<RealVector, RealVector, RealVectorEncoding> {
      Encoding = encoding, Decoder = decoder, Evaluator = evaluator, Objective = SingleObjective.Minimize
    };
    
    var firstAlg = new GeneticAlgorithm<RealVector, RealVectorEncoding>(
      populationSize: 5, 
      creator: creator, crossover: crossover, mutator: mutator, mutationRate: 0.05,
      selector: selector, replacer: replacement, 
      randomSeed: 42, terminator: terminator
    );

    var firstResult = firstAlg.ExecuteStreaming(problem).Last();

    var newTerminationCriterion = Terminator.OnGeneration<GeneticAlgorithmIterationResult<RealVector>>(12);
    var continuationState = firstResult.GetContinuationState();
    var secondAlg = (firstAlg.ToConfiguration() with {
      PopulationSize = 8, Terminator = newTerminationCriterion
    }).Build();

    var finalState = secondAlg.Execute(problem, initialState: continuationState);

    await Verify(new { firstResult, finalState })
      .IgnoreMembersWithType<TimeSpan>();
  }

  [Fact]
  public void GeneticAlgorithm_ExecutingWithInitialStateThatShouldImmediatelyTerminate_DoesNotIterate() {
    var encoding = new RealVectorEncoding(3, -5, +5);
    var creator = new UniformDistributedCreator();
    var crossover = new SinglePointCrossover();
    var mutator = new GaussianMutator(0.1, 0.1);
    var decoder = Decoder.Identity<RealVector>();
    var evaluator = new RealVectorMockEvaluator();
    var selector = new ProportionalSelector();
    var replacement = new ElitismReplacer(0);
    var terminator = Terminator.OnGeneration<GeneticAlgorithmIterationResult<RealVector>>(5);
    
    var problem = new EncodedProblem<RealVector, RealVector, RealVectorEncoding> {
      Encoding = encoding, Decoder = decoder, Evaluator = evaluator, Objective = SingleObjective.Minimize
    };
    var alg = new GeneticAlgorithm<RealVector, RealVectorEncoding>(
      populationSize: 5, 
      creator: creator, crossover: crossover, mutator: mutator, mutationRate: 0.05,
      selector: selector, replacer: replacement, 
      randomSeed: 42, terminator: terminator
    );
    var initialState = new GeneticAlgorithmState<RealVector> {
      Generation = 100, Population = []
    };

    var iterationResults = alg.ExecuteStreaming(problem, initialState).ToList();
    iterationResults.ShouldBeEmpty();
  }
  
  
  private class RealVectorMockEvaluator : EvaluatorBase<RealVector> {
    public override Fitness Evaluate(RealVector phenotype) => phenotype.Sum();
  }
//
//   [Fact]
//   public async Task GeneticAlgorithm_WithMultiChromosomeGenotype() {
//     var randomSource = new RandomSource(42);
//     var realVectorEncoding = new RealVectorEncoding(5, -5, +5);
//     var permutationEncoding = new PermutationEncoding(3);
//     var multiGenotypeEncoding = new MultiGenotypeEncoding(realVectorEncoding, permutationEncoding);
//     
//     var creator = new MultiGenotypeCreator(new UniformDistributedCreator(null, null), new RandomPermutationCreator());
//     var crossover = new MultiGenotypeCrossover(new SinglePointCrossover(), new OrderCrossover());
//     var mutator = new MultiGenotypeMutator(new GaussianMutator(0.1, 0.1), new SwapMutator());
//     var evaluator = new MultiGenotypeEvaluator();
//     var selector = new RandomSelector<MultiGenotype, Fitness, Goal>();
//     var replacement = new ElitismReplacer<MultiGenotype>(0);
//     var terminationCriterion = Terminator.OnGeneration(10);
//     
//     var ga = new GeneticAlgorithm<MultiGenotype, MultiGenotypeEncoding>(encoding, 5, creator, crossover, mutator, 0.05, evaluator, Goal.Minimize, selector, replacement, randomSource, terminationCriterion);
//
//     var finalState = ga.Execute();
//
//     await Verify(finalState);
//   }
//
//   private record MultiGenotypeEncoding(RealVectorEncoding RealVectorEncoding, PermutationEncoding PermutationEncoding) : IEncoding<MultiGenotype, MultiGenotypeEncoding>> {
//
//     public bool IsValidGenotype(MultiGenotype genotype) {
//       return RealVectorEncoding.IsValidGenotype(genotype.RealVector) && PermutationEncoding.IsValidGenotype(genotype.Permutation);
//     }
//   }
//
//   private record MultiGenotype(RealVector RealVector, Permutation Permutation);/* : IRecordGenotypeBase<MultiGenotype, RealVector, Permutation> {
//     public static MultiGenotype Construct(RealVector realVector, Permutation permutation) => new MultiGenotype(realVector, permutation);
//     public void Deconstruct(out RealVector realVector, out Permutation permutation) { realVector = RealVector; permutation = Permutation; }
//   };*/
//   private class MultiGenotypeCreator(ICreator<RealVector> realVectorCreator, ICreator<Permutation> permutationCreator) : CreatorBase<MultiGenotype> {
//     public override MultiGenotype Create() { return new MultiGenotype(realVectorCreator.Create(), permutationCreator.Create()); }
//   }
//   private class MultiGenotypeEvaluator : IEvaluator<MultiGenotype, Fitness> {
//     public Fitness Evaluate(MultiGenotype solution) { return solution.RealVector.Sum() + solution.Permutation.Count; }
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
//     var realVectorEncoding = new RealVectorEncoding(2, -5, +5);
//     var permutationEncoding = new PermutationEncoding(3);
//     var creator = new MultiGenotypeCreator(new UniformDistributedCreator(realVectorEncoding, null, null, randomSource), new RandomPermutationCreator(permutationEncoding, randomSource));
//     var crossover = new RecordCrossover<MultiGenotype, RealVector, Permutation>(new SinglePointCrossover(realVectorEncoding, randomSource), new OrderCrossover(permutationEncoding, randomSource));
//     var mutator = new MultiGenotypeMutator(new GaussianMutator(realVectorEncoding, 0.1, 0.1, randomSource), new SwapMutator(permutationEncoding, randomSource));
//     var evaluator = new MultiGenotypeEvaluator();
//     var selector = new RandomSelector<MultiGenotype, Fitness, Goal>(randomSource);
//     var replacement = new ElitismReplacer<MultiGenotype>(0);
//     var terminationCriterion = Terminator.OnGeneration(10);
//     
//     var ga = new GeneticAlgorithm<MultiGenotype>(5, creator, crossover, mutator, 0.05, evaluator, Goal.Minimize, selector, replacement, randomSource, terminationCriterion);
//
//     var finalState = ga.Execute();
//
//     await Verify(finalState);
//   }
}
