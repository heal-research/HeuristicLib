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
    var creator = new UniformDistributedCreator(encoding, minimum: null, maximum: 3.0, randomSource);
    var crossover = new SinglePointCrossover(encoding, randomSource);
    var mutator = new GaussianMutator(encoding, 0.1, 0.1, randomSource);
    var evaluator = new RealVectorMockEvaluator();
    var selector = new RandomSelector<RealVector, ObjectiveValue>(randomSource);
    var replacement = new PlusSelectionReplacer<RealVector>();
    
    var ga = new GeneticAlgorithm<RealVector>(
      populationSize: 200, 
      creator, crossover, mutator, 0.05,
      evaluator, selector, replacement,
      randomSource, terminator: null
    );

    return Verify(ga);
  }
  
  [Fact]
  public Task GeneticAlgorithm_Execute() {
    var randomSource = new RandomSource(42);
    var encoding = new RealVectorEncoding(3, -5, +5);
    var creator = new UniformDistributedCreator(encoding, minimum: null, maximum: 3.0, randomSource);
    var crossover = new SinglePointCrossover(encoding, randomSource);
    var mutator = new GaussianMutator(encoding, 0.1, 0.1, randomSource);
    var evaluator = new RealVectorMockEvaluator();
    var selector = new RandomSelector<RealVector, ObjectiveValue>(randomSource);
    var replacement = new ElitismReplacer<RealVector>(0);
    
    var ga = new GeneticAlgorithm<RealVector>(
      populationSize: 5, 
      creator, crossover, mutator, 0.5,
      evaluator, selector, replacement,
      randomSource, terminator: Terminator.OnGeneration(5)
    );

    var finalState = ga.Execute();
    
    return Verify(finalState);
  }
  
  [Fact]
  public Task GeneticAlgorithm_ExecuteStream() {
    var randomSource = new RandomSource(42);
    var encoding = new RealVectorEncoding(3, -5, +5);
    var creator = new UniformDistributedCreator(encoding, minimum: null, maximum: 3.0, randomSource);
    var crossover = new SinglePointCrossover(encoding, randomSource);
    var mutator = new GaussianMutator(encoding, 0.1, 0.1, randomSource);
    var evaluator = new RealVectorMockEvaluator();
    var selector = new RandomSelector<RealVector, ObjectiveValue>(randomSource);
    var replacement = new ElitismReplacer<RealVector>(0);
    
    var ga = new GeneticAlgorithm<RealVector>(
      populationSize: 5, 
      creator, crossover, mutator, 0.5,
      evaluator, selector, replacement,
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
    var creator = new UniformDistributedCreator(encoding, minimum: null, maximum: null, randomSource);
    var crossover = new SinglePointCrossover(encoding, randomSource);
    var mutator = new GaussianMutator(encoding, 0.1, 0.1, randomSource);
    var evaluator = new RealVectorMockEvaluator();
    var selector = new ProportionalSelector<RealVector>(randomSource);
    var replacement = new PlusSelectionReplacer<RealVector>();
    var pauseToken = new PauseToken();
    var terminationCriterion = new PauseTokenTerminator<PopulationState<RealVector>>(pauseToken);

    var firstAlg = new GeneticAlgorithm<RealVector>(
      5,
      creator, crossover, mutator, 0.05, 
      evaluator, selector, replacement, 
      randomSource, terminationCriterion
    );
    
    var task = Task.Run(() => firstAlg.Execute());

    await Task.Delay(200);
    
    task.Status.ShouldBe(TaskStatus.Running);
    
    pauseToken.RequestPause();

    var finalState = await task;
    
    
    task.Status.ShouldBe(TaskStatus.RanToCompletion);
    finalState.Generation.ShouldBeGreaterThan(0);
  }
  
  [Fact]
  public async Task GeneticAlgorithm_ExecuteAndContinueWithOtherAlg() {
    var randomSource = new RandomSource(42);
    var encoding = new RealVectorEncoding(3, -5, +5);
    var creator = new UniformDistributedCreator(encoding, minimum: null, maximum: null, randomSource);
    var crossover = new SinglePointCrossover(encoding, randomSource);
    var mutator = new GaussianMutator(encoding, 0.1, 0.1, randomSource);
    var evaluator = new RealVectorMockEvaluator();
    var selector = new ProportionalSelector<RealVector>(randomSource);
    var replacement = new ElitismReplacer<RealVector>(0);
    var terminationCriterion = Terminator.OnGeneration(5);

    var firstAlg = new GeneticAlgorithm<RealVector>(5, creator, crossover, mutator, 0.05, evaluator, selector, replacement, randomSource, terminationCriterion);

    var firstResult = firstAlg.Execute();

    var newTerminationCriterion = Terminator.OnGeneration(12);
    var secondAlg = new GeneticAlgorithm<RealVector>(8, creator, crossover, mutator, 0.05, evaluator, selector, replacement, randomSource, newTerminationCriterion);

    var finalState = secondAlg.Execute(firstResult);

    await Verify(new { firstResult, finalState });
  }
  
  private class RealVectorMockEvaluator : IEvaluator<RealVector, ObjectiveValue> {
    public ObjectiveValue Evaluate(RealVector solution) { return (solution.Sum(), ObjectiveDirection.Minimize); }
  }
  private class PermutationMockEvaluator : IEvaluator<Permutation, ObjectiveValue> {
    public ObjectiveValue Evaluate(Permutation solution) { return (solution[0], ObjectiveDirection.Maximize); }
  }


  public async Task GeneticAlgorithm_WithMultiChomosomeGenotype() {
    var randomSource = new RandomSource(42);
    var realVectorEncoding = new RealVectorEncoding(10, -5, +5);
    var permutationEncoding = new PermutationEncoding(5);
    
    var creator = new MultiGenotypeCreator(new UniformDistributedCreator(realVectorEncoding, null, null, randomSource), new RandomPermutationCreator(permutationEncoding, randomSource));
    var crossover = new MultiGenotypeCrossover(new SinglePointCrossover(realVectorEncoding, randomSource), new OrderCrossover(permutationEncoding));
    var mutator = new MultiGenotypeMutator(new GaussianMutator(realVectorEncoding, 0.1, 0.1, randomSource), new SwapMutator(permutationEncoding));
    var evaluator = new MultiGenotypeEvaluator();
    var selector = new RandomSelector<MultiGenotype, ObjectiveValue>(randomSource);
    var replacement = new PlusSelectionReplacer<MultiGenotype>();
    var terminationCriterion = new ThresholdTerminator<PopulationState<MultiGenotype>>(50, state => state.Generation);
    
    var ga = new GeneticAlgorithm<MultiGenotype>(
      200, creator, crossover, mutator, 0.05, evaluator, selector, replacement, randomSource, terminationCriterion);

    var finalState = ga.Execute();

    await Verify(finalState);
  }

  private record MultiGenotype(RealVector RealVector, Permutation Permutation) : IRecordGenotypeBase<MultiGenotype, RealVector, Permutation> {
    public static MultiGenotype Construct(RealVector realVector, Permutation permutation) => new MultiGenotype(realVector, permutation);
    public void Deconstruct(out RealVector realVector, out Permutation permutation) { realVector = RealVector; permutation = Permutation; }
  };
  private class MultiGenotypeCreator(ICreator<RealVector> realVectorCreator, ICreator<Permutation> permutationCreator) : CreatorBase<MultiGenotype> {
    public override MultiGenotype Create() { return new MultiGenotype(realVectorCreator.Create(), permutationCreator.Create()); }
  }
  private class MultiGenotypeEvaluator : IEvaluator<MultiGenotype, ObjectiveValue> {
    public ObjectiveValue Evaluate(MultiGenotype solution) { return (solution.RealVector.Sum() + solution.Permutation.Count, ObjectiveDirection.Minimize); }
  }
  private class MultiGenotypeCrossover(ICrossover<RealVector> realVectorCrossover, ICrossover<Permutation> permutationCrossover) : ICrossover<MultiGenotype> {
    public MultiGenotype Cross(MultiGenotype parent1, MultiGenotype parent2) {
      return new MultiGenotype(realVectorCrossover.Cross(parent1.RealVector, parent2.RealVector), permutationCrossover.Cross(parent1.Permutation, parent2.Permutation));
    }
  }
  private class MultiGenotypeMutator(IMutator<RealVector> realVectorMutator, IMutator<Permutation> permutationMutator) : IMutator<MultiGenotype> {
    public MultiGenotype Mutate(MultiGenotype genotype) {
      return new MultiGenotype(realVectorMutator.Mutate(genotype.RealVector), permutationMutator.Mutate(genotype.Permutation));
    }
  }

  public async Task GeneticAlgorithm_WithMultiChomosomeGenotypeWithRecordOperators() {
    var randomSource = new RandomSource(42);
    var realVectorEncoding = new RealVectorEncoding(10, -5, +5);
    var permutationEncoding = new PermutationEncoding(5);
    var creator = new MultiGenotypeCreator(new UniformDistributedCreator(realVectorEncoding, null, null, randomSource), new RandomPermutationCreator(permutationEncoding, randomSource));
    var crossover = new RecordCrossover<MultiGenotype, RealVector, Permutation>(new SinglePointCrossover(realVectorEncoding, randomSource), new OrderCrossover(permutationEncoding));
    var mutator = new MultiGenotypeMutator(new GaussianMutator(realVectorEncoding, 0.1, 0.1, randomSource), new SwapMutator(permutationEncoding));
    var evaluator = new MultiGenotypeEvaluator();
    var selector = new RandomSelector<MultiGenotype, ObjectiveValue>(randomSource);
    var replacement = new PlusSelectionReplacer<MultiGenotype>();
    var terminationCriterion = new ThresholdTerminator<PopulationState<MultiGenotype>>(50, state => state.Generation);
    
    var ga = new GeneticAlgorithm<MultiGenotype>(
      200, creator, crossover, mutator, 0.05, evaluator, selector, replacement, randomSource, terminationCriterion);

    var finalState = ga.Execute();

    await Verify(finalState);
  }
  
}
