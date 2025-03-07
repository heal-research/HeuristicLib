using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Algorithms.GeneticAlgorithm;
using HEAL.HeuristicLib.Encodings;
using HEAL.HeuristicLib.Operators;

namespace HEAL.HeuristicLib.Tests;

public class GeneticAlgorithmTests {
  [Fact]
  public Task GeneticAlgorithm_Create_WithoutBuilder() {
    var randomSource = new SeededRandomSource(42);
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
  public async Task GeneticAlgorithm_ShouldPauseAndContinue() {
    var randomSource = new SeededRandomSource(42);
    var encoding = new RealVectorEncoding(10, -5, +5);
    var creator = new UniformDistributedCreator(encoding, minimum: null, maximum: null, randomSource);
    var crossover = new SinglePointCrossover(encoding, randomSource);
    var mutator = new GaussianMutator(encoding, 0.1, 0.1, randomSource);
    var evaluator = new RealVectorMockEvaluator();
    var selector = new ProportionalSelector<RealVector>(randomSource);
    var replacement = new PlusSelectionReplacer<RealVector>();
    var pauseToken = new PauseToken();
    var terminationCriterion = new PauseTokenTerminator<PopulationState<RealVector>>(pauseToken);

    var ga = new GeneticAlgorithm<RealVector>(
      200, creator, crossover, mutator, 0.05, evaluator, selector, replacement, randomSource, terminationCriterion);

    var task = Task.Run(() => ga.Execute());

    await Task.Delay(1000);
    pauseToken.RequestPause();

    var pausedState = await task;
    Assert.True(pausedState.Generation > 0);

    // Continue running the GA
    var newTerminationCriterion = new ThresholdTerminator<PopulationState<RealVector>>(pausedState.Generation + 50, state => state.Generation);
    var continuedGA = new GeneticAlgorithm<RealVector>(
      200, creator, crossover, mutator, 0.05, evaluator, selector, replacement, randomSource, newTerminationCriterion);

    var finalState = continuedGA.Execute(pausedState);
    Assert.True(finalState.Generation > pausedState.Generation);

    await Verify(finalState);
  }
  
  private class RealVectorMockEvaluator : IEvaluator<RealVector, ObjectiveValue> {
    public ObjectiveValue Evaluate(RealVector solution) { return (solution.Sum(), ObjectiveDirection.Minimize); }
  }
  private class PermutationMockEvaluator : IEvaluator<Permutation, ObjectiveValue> {
    public ObjectiveValue Evaluate(Permutation solution) { return (solution[0], ObjectiveDirection.Maximize); }
  }


  public async Task GeneticAlgorithm_WithMultiChomosomeGenotype() {
    var randomSource = new SeededRandomSource(42);
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
    var randomSource = new SeededRandomSource(42);
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
