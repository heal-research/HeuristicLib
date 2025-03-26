using HEAL.HeuristicLib.Operators;

namespace HEAL.HeuristicLib.Algorithms.GeneticAlgorithm;

public class GeneticAlgorithm<TGenotype> : AlgorithmBase<PopulationState<TGenotype>> {
  
  public GeneticAlgorithm(
    int populationSize,
    ICreatorOperator<TGenotype> creator, ICrossoverOperator<TGenotype> crossover, IMutatorOperator<TGenotype> mutator, double mutationRate,
    IEvaluatorOperator<TGenotype> evaluator, Objective objective,
    ISelectorOperator selector, IReplacerOperator replacer,
    IRandomSource randomSourceState,
    ITerminatorOperator<PopulationState<TGenotype>>? terminator = null, IInterceptor<PopulationState<TGenotype>>? interceptor = null)
  {
    PopulationSize = populationSize;
    Creator = creator;
    Crossover = crossover;
    Mutator = mutator;
    MutationRate = mutationRate;
    Evaluator = evaluator;
    Objective = objective;
    Selector = selector;
    Replacer = replacer;
    RandomSource = randomSourceState;
    Terminator = terminator;
    Interceptor = interceptor ?? new IdentityInterceptor<PopulationState<TGenotype>>();
  }
  
  public int PopulationSize { get; }
  public ITerminatorOperator<PopulationState<TGenotype>>? Terminator { get; }
  public ICreatorOperator<TGenotype> Creator { get; }
  public ICrossoverOperator<TGenotype> Crossover { get; }
  public IMutatorOperator<TGenotype> Mutator { get; }
  public double MutationRate { get; }
  public IEvaluatorOperator<TGenotype> Evaluator { get; }
  public Objective Objective { get; }
  public IRandomSource RandomSource { get; }
  public ISelectorOperator Selector { get; }
  public IReplacerOperator Replacer { get; }
  public IInterceptor<PopulationState<TGenotype>> Interceptor { get; }
  
  public override ExecutionStream<PopulationState<TGenotype>> CreateExecutionStream(PopulationState<TGenotype>? initialState = null) {
    return new ExecutionStream<PopulationState<TGenotype>>(InternalCreateExecutionStream(initialState));
  }

  private IEnumerable<PopulationState<TGenotype>> InternalCreateExecutionStream(PopulationState<TGenotype>? initialState) {
    var random = RandomSource.CreateRandomNumberGenerator();
    
    int offspringCount = Replacer.GetOffspringCount(PopulationSize);

    PopulationState<TGenotype> currentState;
    if (initialState is not null) {
      currentState = initialState;
    } else {
      var initialPopulation = InitializePopulation();
      var evaluatedPopulation = Evaluator.Evaluate(initialPopulation);
      currentState = new PopulationState<TGenotype> { Population = evaluatedPopulation, Objective = Objective };
      currentState = Interceptor.Transform(currentState);
      yield return currentState;
    }

    while (Terminator?.ShouldContinue(currentState) ?? true) {
      var offsprings = EvolvePopulation(currentState.Population, offspringCount, random);
      var evaluatedOffsprings = Evaluator.Evaluate(offsprings);

      var newPopulation = Replacer.Replace(currentState.Population, evaluatedOffsprings, Objective);

      currentState = currentState.Next() with { Population = newPopulation }; // increment durations and other counts
      currentState = Interceptor.Transform(currentState);

      yield return currentState;
    }
  }

  private TGenotype[] InitializePopulation() {
    var population = new TGenotype[PopulationSize];
    for (int i = 0; i < PopulationSize; i++) {
      population[i] = Creator.Create();
    }
    return population;
  }

  private TGenotype[] EvolvePopulation(Phenotype<TGenotype>[] population, int offspringCount, IRandomNumberGenerator random) {
    var newPopulation = new TGenotype[offspringCount];
    var parents = Selector.Select(population, Objective, offspringCount * 2).Select(p => p.Genotype).ToList();
  
    for (int i = 0; i < parents.Count; i += 2) {
      var parent1 = parents[i];
      var parent2 = parents[i + 1];
      var offspring = Crossover.Cross(parent1, parent2);
      if (random.Random() < MutationRate) {
        offspring = Mutator.Mutate(offspring);
      }
      newPopulation[i / 2] = offspring;
    }
    return newPopulation;
  }
}
