using HEAL.HeuristicLib.Operators;

namespace HEAL.HeuristicLib.Algorithms.GeneticAlgorithm;

public class GeneticAlgorithm<TGenotype> : AlgorithmBase<PopulationState<TGenotype, Fitness, Goal>> {
  
  public GeneticAlgorithm(int populationSize,
    ICreator<TGenotype> creator, ICrossover<TGenotype> crossover, IMutator<TGenotype> mutator, double mutationRate,
    IEvaluator<TGenotype, Fitness> evaluator, Goal goal,
    ISelector<TGenotype, Fitness, Goal> selector, IReplacer<TGenotype, Fitness, Goal> replacer,
    IRandomSource randomSourceState,
    ITerminator<PopulationState<TGenotype, Fitness, Goal>>? terminator = null, IInterceptor<PopulationState<TGenotype, Fitness, Goal>>? interceptor = null)
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
    Interceptor = interceptor ?? new IdentityInterceptor<PopulationState<TGenotype, Fitness, Goal>>();
  }
  
  public int PopulationSize { get; }
  public ITerminator<PopulationState<TGenotype, Fitness, Goal>>? Terminator { get; }
  public ICreator<TGenotype> Creator { get; }
  public ICrossover<TGenotype> Crossover { get; }
  public IMutator<TGenotype> Mutator { get; }
  public double MutationRate { get; }
  public IEvaluator<TGenotype, Fitness> Evaluator { get; }
  public Goal Goal { get; }
  public IRandomSource RandomSource { get; }
  public ISelector<TGenotype, Fitness, Goal> Selector { get; }
  public IReplacer<TGenotype, Fitness, Goal> Replacer { get; }
  public IInterceptor<PopulationState<TGenotype, Fitness, Goal>> Interceptor { get; }
  
  public override PopulationState<TGenotype, Fitness, Goal> Execute(PopulationState<TGenotype, Fitness, Goal>? initialState = null, ITerminator<PopulationState<TGenotype, Fitness, Goal>>? terminator = null) {
    return CreateExecutionStream(initialState, terminator).Last();
  }

  public override ExecutionStream<PopulationState<TGenotype, Fitness, Goal>> CreateExecutionStream(PopulationState<TGenotype, Fitness, Goal>? initialState = null, ITerminator<PopulationState<TGenotype, Fitness, Goal>>? terminator = null) {
    return new ExecutionStream<PopulationState<TGenotype, Fitness, Goal>>(InternalCreateExecutionStream(initialState, terminator));
  }

  private IEnumerable<PopulationState<TGenotype, Fitness, Goal>> InternalCreateExecutionStream(PopulationState<TGenotype, Fitness, Goal>? initialState, ITerminator<PopulationState<TGenotype, Fitness, Goal>>? terminator) {
    var rng = RandomSource.CreateRandomNumberGenerator();

    var activeTerminator = terminator ?? Terminator;
    int offspringCount = Replacer.GetOffspringCount(PopulationSize);

    PopulationState<TGenotype, Fitness, Goal> currentState;
    if (initialState is not null) {
      currentState = initialState;
    } else {
      var initialPopulation = InitializePopulation();
      var evaluatedPopulation = EvaluatePopulation(initialPopulation);
      currentState = new PopulationState<TGenotype, Fitness, Goal> { Population = evaluatedPopulation, Goal = Goal };
      currentState = Interceptor.Transform(currentState);
      yield return currentState;
    }

    while (activeTerminator?.ShouldContinue(currentState) ?? true) {
      var offsprings = EvolvePopulation(currentState.Population, offspringCount, rng);
      var evaluatedOffsprings = EvaluatePopulation(offsprings);

      var newPopulation = Replacer.Replace(currentState.Population, evaluatedOffsprings, Goal);

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

  private TGenotype[] EvolvePopulation(Phenotype<TGenotype, Fitness>[] population, int offspringCount, IRandomNumberGenerator rng) {
    var newPopulation = new TGenotype[offspringCount];
    var parents = Selector.Select(population, Goal, offspringCount * 2).Select(p => p.Genotype).ToList();
  
    for (int i = 0; i < parents.Count; i += 2) {
      var parent1 = parents[i];
      var parent2 = parents[i + 1];
      var offspring = Crossover.Cross(parent1, parent2);
      if (rng.Random() < MutationRate) {
        offspring = Mutator.Mutate(offspring);
      }
      newPopulation[i / 2] = offspring;
    }
    return newPopulation;
  }

  private Phenotype<TGenotype, Fitness>[] EvaluatePopulation(TGenotype[] population) {
    return population.Select(genotype => {
      var fitness = Evaluator.Evaluate(genotype);
      return new Phenotype<TGenotype, Fitness>(genotype, fitness);
    }).ToArray();
  }
  
}
