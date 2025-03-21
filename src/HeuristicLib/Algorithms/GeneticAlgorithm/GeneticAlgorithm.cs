using HEAL.HeuristicLib.Operators;

namespace HEAL.HeuristicLib.Algorithms.GeneticAlgorithm;

public class GeneticAlgorithm<TGenotype> : AlgorithmBase<PopulationState<TGenotype>> {
  
  public GeneticAlgorithm(
    int populationSize,
    ICreator<TGenotype> creator, ICrossover<TGenotype> crossover, IMutator<TGenotype> mutator, double mutationRate,
    ISingleObjectiveEvaluator<TGenotype> evaluator, Goal goal,
    ISingleObjectiveSelector selector, ISingleObjectiveReplacer replacer,
    IRandomSource randomSourceState,
    ITerminator<PopulationState<TGenotype>>? terminator = null, IInterceptor<PopulationState<TGenotype>>? interceptor = null)
  {
    PopulationSize = populationSize;
    Creator = creator;
    Crossover = crossover;
    Mutator = mutator;
    MutationRate = mutationRate;
    Evaluator = evaluator;
    Goal = goal;
    Selector = selector;
    Replacer = replacer;
    RandomSource = randomSourceState;
    Terminator = terminator;
    Interceptor = interceptor ?? new IdentityInterceptor<PopulationState<TGenotype>>();
  }
  
  public int PopulationSize { get; }
  public ITerminator<PopulationState<TGenotype>>? Terminator { get; }
  public ICreator<TGenotype> Creator { get; }
  public ICrossover<TGenotype> Crossover { get; }
  public IMutator<TGenotype> Mutator { get; }
  public double MutationRate { get; }
  public ISingleObjectiveEvaluator<TGenotype> Evaluator { get; }
  public Goal Goal { get; }
  public IRandomSource RandomSource { get; }
  public ISelector<Fitness, Goal> Selector { get; }
  public IReplacer<Fitness, Goal> Replacer { get; }
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
      var initialPopulation = InitializePopulation(random);
      var evaluatedPopulation = Evaluator.Evaluate(initialPopulation);
      currentState = new PopulationState<TGenotype> { Population = evaluatedPopulation, Goal = Goal };
      currentState = Interceptor.Transform(currentState);
      yield return currentState;
    }

    while (Terminator?.ShouldContinue(currentState) ?? true) {
      var offsprings = EvolvePopulation(currentState.Population, offspringCount, random);
      var evaluatedOffsprings = Evaluator.Evaluate(offsprings);

      var newPopulation = Replacer.Replace(currentState.Population, evaluatedOffsprings, Goal, random);

      currentState = currentState.Next() with { Population = newPopulation }; // increment durations and other counts
      currentState = Interceptor.Transform(currentState);

      yield return currentState;
    }
  }

  private TGenotype[] InitializePopulation(IRandomNumberGenerator random) {
    var population = new TGenotype[PopulationSize];
    for (int i = 0; i < PopulationSize; i++) {
      population[i] = Creator.Create(random);
    }
    return population;
  }

  private TGenotype[] EvolvePopulation(Phenotype<TGenotype, Fitness>[] population, int offspringCount, IRandomNumberGenerator random) {
    var newPopulation = new TGenotype[offspringCount];
    var parents = Selector.Select(population, Goal, offspringCount * 2, random).Select(p => p.Genotype).ToList();
  
    for (int i = 0; i < parents.Count; i += 2) {
      var parent1 = parents[i];
      var parent2 = parents[i + 1];
      var offspring = Crossover.Cross(parent1, parent2, random);
      if (random.Random() < MutationRate) {
        offspring = Mutator.Mutate(offspring, random);
      }
      newPopulation[i / 2] = offspring;
    }
    return newPopulation;
  }
}
