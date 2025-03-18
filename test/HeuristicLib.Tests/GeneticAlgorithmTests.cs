using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Algorithms.GeneticAlgorithm;
using HEAL.HeuristicLib.Encodings;
using HEAL.HeuristicLib.Operators;

namespace HEAL.HeuristicLib.Tests;

public class GeneticAlgorithmTests {
  [Fact]
  public Task GeneticAlgorithm_Create_WithoutBuilder() {
    var randomSource = new FixedRandomSource(42);
    var encoding = new RealVectorEncoding(10, -5, +5);
    var creator = new UniformDistributedCreator(minimum: null, maximum: 3.0, encoding, randomSource);
    var crossover = new SinglePointCrossover(randomSource);
    var mutator = new GaussianMutator(0.1, 0.1, randomSource);
    var evaluator = new RealVectorMockEvaluator();
    var selector = new RandomSelector<RealVector, Fitness, Goal>(randomSource);
    var replacement = new PlusSelectionReplacer<RealVector>();
    
    var ga = new GeneticAlgorithm<RealVector>(
      populationSize: 200, 
      creator, crossover, mutator, 0.05,
      evaluator, Goal.Minimize, selector, replacement,
      randomSource, terminator: null
    );

    return Verify(ga);
  }
  
  [Fact]
  public Task GeneticAlgorithm_Execute() {
    var randomSource = new RandomSource(42);
    var encoding = new RealVectorEncoding(3, -5, +5);
    var creator = new UniformDistributedCreator(minimum: null, maximum: 3.0, encoding, randomSource);
    var crossover = new SinglePointCrossover(randomSource);
    var mutator = new GaussianMutator(0.1, 0.1, randomSource);
    var evaluator = new RealVectorMockEvaluator();
    var selector = new RandomSelector<RealVector, Fitness, Goal>(randomSource);
    var replacement = new ElitismReplacer<RealVector>(0);
    
    var ga = new GeneticAlgorithm<RealVector>(
      populationSize: 5, 
      creator, crossover, mutator, 0.5,
      evaluator, Goal.Minimize, selector, replacement,
      randomSource, terminator: Terminator.OnGeneration(5)
    );

    var finalState = ga.Execute();
    
    return Verify(finalState);
  }
  
  [Fact]
  public Task GeneticAlgorithm_ExecuteStream() {
    var randomSource = new RandomSource(42);
    var encoding = new RealVectorEncoding(3, -5, +5);
    var creator = new UniformDistributedCreator(minimum: null, maximum: 3.0, encoding, randomSource);
    var crossover = new SinglePointCrossover(randomSource);
    var mutator = new GaussianMutator(0.1, 0.1, randomSource);
    var evaluator = new RealVectorMockEvaluator();
    var selector = new RandomSelector<RealVector, Fitness, Goal>(randomSource);
    var replacement = new ElitismReplacer<RealVector>(0);
    
    var ga = new GeneticAlgorithm<RealVector>(
      populationSize: 5, 
      creator, crossover, mutator, 0.5,
      evaluator, Goal.Minimize, selector, replacement,
      randomSource
    );

    var stream = ga.CreateExecutionStream();

    var results = stream.Take(5).ToList();
    
    return Verify(results);
  }
  
  [Fact]
  public async Task GeneticAlgorithm_TerminateWithPauseToken() {
    var randomSource = new RandomSource(42);
    var encoding = new RealVectorEncoding(5, -5, +5);
    var creator = new UniformDistributedCreator(minimum: null, maximum: null, encoding, randomSource);
    var crossover = new SinglePointCrossover(randomSource);
    var mutator = new GaussianMutator(0.1, 0.1, randomSource);
    var evaluator = new RealVectorMockEvaluator();
    var selector = new ProportionalSelector<RealVector>(randomSource);
    var replacement = new PlusSelectionReplacer<RealVector>();
    var pauseToken = new PauseToken();
    var terminationCriterion = new PauseTokenTerminator<PopulationState<RealVector, Fitness, Goal>>(pauseToken);

    var firstAlg = new GeneticAlgorithm<RealVector>(
      5,
      creator, crossover, mutator, 0.05, 
      evaluator, Goal.Minimize, selector, replacement, 
      randomSource, terminationCriterion
    );
    
    var task = Task.Run(() => firstAlg.Execute());

    await Task.Delay(200);
    
    task.Status.ShouldBe(TaskStatus.Running);
    
    pauseToken.RequestPause();

    var finalState = await task;
    
    
    task.Status.ShouldBe(TaskStatus.RanToCompletion);
    
    finalState.ShouldNotBeNull();
    finalState.Generation.ShouldBeGreaterThan(0);
  }
  
  [Fact]
  public async Task GeneticAlgorithm_ExecuteAndContinueWithOtherAlg() {
    var randomSource = new RandomSource(42);
    var encoding = new RealVectorEncoding(3, -5, +5);
    var creator = new UniformDistributedCreator(minimum: null, maximum: null, encoding, randomSource);
    var crossover = new SinglePointCrossover(randomSource);
    var mutator = new GaussianMutator(0.1, 0.1, randomSource);
    var evaluator = new RealVectorMockEvaluator();
    var selector = new ProportionalSelector<RealVector>(randomSource);
    var replacement = new ElitismReplacer<RealVector>(0);
    var terminationCriterion = Terminator.OnGeneration(5);

    var firstAlg = new GeneticAlgorithm<RealVector>(5, creator, crossover, mutator, 0.05, evaluator, Goal.Minimize, selector, replacement, randomSource, terminationCriterion);

    var firstResult = firstAlg.Execute();

    var newTerminationCriterion = Terminator.OnGeneration(12);
    var secondAlg = new GeneticAlgorithm<RealVector>(8, creator, crossover, mutator, 0.05, evaluator, Goal.Minimize, selector, replacement, randomSource, newTerminationCriterion);

    var finalState = secondAlg.Execute(firstResult);

    await Verify(new { firstResult, finalState });
  }
  
  private class RealVectorMockEvaluator : IEvaluator<RealVector, Fitness> {
    public Fitness Evaluate(RealVector solution) { return solution.Sum(); }
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
