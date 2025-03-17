using HEAL.HeuristicLib.Encodings;
using HEAL.HeuristicLib.Operators;

namespace HEAL.HeuristicLib.Algorithms.GeneticAlgorithm;

public class GeneticAlgorithm<TGenotype, TEncoding> : AlgorithmBase<PopulationState<TGenotype, Fitness, Goal>> 
  where TEncoding : IEncoding<TGenotype, TEncoding> {
  
  public GeneticAlgorithm(TEncoding encoding,
    int populationSize,
    ICreator<TGenotype, TEncoding> creator, ICrossover<TGenotype, TEncoding> crossover, IMutator<TGenotype, TEncoding> mutator, double mutationRate,
    IEvaluator<TGenotype, Fitness> evaluator, Goal goal,
    ISelector<TGenotype, Fitness, Goal> selector, IReplacer<TGenotype, Fitness, Goal> replacer,
    IRandomSource randomSourceState,
    ITerminator<PopulationState<TGenotype, Fitness, Goal>>? terminator = null, IInterceptor<PopulationState<TGenotype, Fitness, Goal>>? interceptor = null)
  {
    Encoding = encoding;
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
  
  public TEncoding Encoding { get; }
  public int PopulationSize { get; }
  public ITerminator<PopulationState<TGenotype, Fitness, Goal>>? Terminator { get; }
  public ICreator<TGenotype, TEncoding> Creator { get; }
  public ICrossover<TGenotype, TEncoding> Crossover { get; }
  public IMutator<TGenotype, TEncoding> Mutator { get; }
  public double MutationRate { get; }
  public IEvaluator<TGenotype, Fitness> Evaluator { get; }
  public Goal Goal { get; }
  public IRandomSource RandomSource { get; }
  public ISelector<TGenotype, Fitness, Goal> Selector { get; }
  public IReplacer<TGenotype, Fitness, Goal> Replacer { get; }
  public IInterceptor<PopulationState<TGenotype, Fitness, Goal>> Interceptor { get; }
  
  public override PopulationState<TGenotype, Fitness, Goal> Execute(PopulationState<TGenotype, Fitness, Goal>? initialState = null) {
    if (Terminator is null) throw new InvalidOperationException("Execute requires a terminator to be set.");
    return CreateExecutionStream(initialState).Last();
  }

  public override ExecutionStream<PopulationState<TGenotype, Fitness, Goal>> CreateExecutionStream(PopulationState<TGenotype, Fitness, Goal>? initialState = null) {
    return new ExecutionStream<PopulationState<TGenotype, Fitness, Goal>>(InternalCreateExecutionStream(initialState));
  }

  private IEnumerable<PopulationState<TGenotype, Fitness, Goal>> InternalCreateExecutionStream(PopulationState<TGenotype, Fitness, Goal>? initialState) {
    var context = new AlgorithmContext<TEncoding>() { Random = RandomSource.CreateRandomNumberGenerator(), Encoding = Encoding };
    
    int offspringCount = Replacer.GetOffspringCount(PopulationSize);

    PopulationState<TGenotype, Fitness, Goal> currentState;
    if (initialState is not null) {
      currentState = initialState;
    } else {
      var initialPopulation = InitializePopulation(context);
      var evaluatedPopulation = EvaluatePopulation(initialPopulation);
      currentState = new PopulationState<TGenotype, Fitness, Goal> { Population = evaluatedPopulation, Goal = Goal };
      currentState = Interceptor.Transform(currentState);
      yield return currentState;
    }

    while (Terminator?.ShouldContinue(currentState) ?? true) {
      var offsprings = EvolvePopulation(currentState.Population, offspringCount, context);
      var evaluatedOffsprings = EvaluatePopulation(offsprings);

      var newPopulation = Replacer.Replace(currentState.Population, evaluatedOffsprings, Goal);

      currentState = currentState.Next() with { Population = newPopulation }; // increment durations and other counts
      currentState = Interceptor.Transform(currentState);

      yield return currentState;
    }
  }

  private TGenotype[] InitializePopulation(AlgorithmContext<TEncoding> context) {
    var population = new TGenotype[PopulationSize];
    for (int i = 0; i < PopulationSize; i++) {
      population[i] = Creator.Create(context);
    }
    return population;
  }

  private TGenotype[] EvolvePopulation(Phenotype<TGenotype, Fitness>[] population, int offspringCount, AlgorithmContext<TEncoding> context) {
    var newPopulation = new TGenotype[offspringCount];
    var parents = Selector.Select(population, Goal, offspringCount * 2, context).Select(p => p.Genotype).ToList();
  
    for (int i = 0; i < parents.Count; i += 2) {
      var parent1 = parents[i];
      var parent2 = parents[i + 1];
      var offspring = Crossover.Cross(parent1, parent2, context);
      if (context.Random.Random() < MutationRate) {
        offspring = Mutator.Mutate(offspring, context);
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
