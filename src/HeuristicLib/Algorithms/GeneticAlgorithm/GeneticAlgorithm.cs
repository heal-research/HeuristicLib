using HEAL.HeuristicLib.Operators;

namespace HEAL.HeuristicLib.Algorithms.GeneticAlgorithm;

public class GeneticAlgorithm<TGenotype> : AlgorithmBase<PopulationState<TGenotype>> {
  
  public GeneticAlgorithm(int populationSize,
    ICreator<TGenotype> creator, ICrossover<TGenotype> crossover, IMutator<TGenotype> mutator, double mutationRate,
    IEvaluator<TGenotype, ObjectiveValue> evaluator, ISelector<TGenotype, ObjectiveValue> selector, IReplacer<TGenotype> replacer,
    IRandomSource randomSourceState, ITerminator<PopulationState<TGenotype>>? terminator = null, IInterceptor<PopulationState<TGenotype>>? interceptor = null)
  {
    PopulationSize = populationSize;
    Creator = creator;
    Crossover = crossover;
    Mutator = mutator;
    MutationRate = mutationRate;
    Evaluator = evaluator;
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
  public IEvaluator<TGenotype, ObjectiveValue> Evaluator { get; }
  public IRandomSource RandomSource { get; }
  public ISelector<TGenotype, ObjectiveValue> Selector { get; }
  public IReplacer<TGenotype> Replacer { get; }
  public IInterceptor<PopulationState<TGenotype>> Interceptor { get; }
  
  public override PopulationState<TGenotype> Execute(PopulationState<TGenotype>? initialState = null, ITerminator<PopulationState<TGenotype>>? terminator = null) {
    return CreateExecutionStream(initialState, terminator).Last();
  }

  public override ExecutionStream<PopulationState<TGenotype>> CreateExecutionStream(PopulationState<TGenotype>? initialState = null, ITerminator<PopulationState<TGenotype>>? terminator = null) {
    if (Terminator is null && terminator is null) throw new InvalidOperationException("At least one terminator must be provided.");
    return new ExecutionStream<PopulationState<TGenotype>>(InternalCreateExecutionStream(initialState, terminator));
  }

  private IEnumerable<PopulationState<TGenotype>> InternalCreateExecutionStream(PopulationState<TGenotype>? initialState, ITerminator<PopulationState<TGenotype>>? terminator) {
    var rng = RandomSource.CreateRandomNumberGenerator();

    var activeTerminator = terminator ?? Terminator;
    int offspringCount = Replacer.GetOffspringCount(PopulationSize);

    PopulationState<TGenotype> currentState;
    if (initialState is not null) {
      currentState = initialState;
    } else {
      var initialPopulation = InitializePopulation();
      var initialObjectives = EvaluatePopulation(initialPopulation);
      currentState = new PopulationState<TGenotype> { Generation = 0, Population = initialPopulation, Objectives = initialObjectives };
      currentState = Interceptor.Transform(currentState);
      yield return currentState;
    }

    while (activeTerminator?.ShouldContinue(currentState) ?? true) {
      var offspringPopulation = EvolvePopulation(currentState.Population, offspringCount, rng);
      var offspringQualities = EvaluatePopulation(offspringPopulation);

      var (newPopulation, newObjectives) = Replacer.Replace(currentState.Population, currentState.Objectives, offspringPopulation, offspringQualities);
      
      currentState = currentState with { Generation = currentState.Generation + 1, Population = newPopulation, Objectives = newObjectives };
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

  private TGenotype[] EvolvePopulation(TGenotype[] population, int offspringCount, IRandomNumberGenerator rng) {
    var newPopulation = new TGenotype[offspringCount];
    var objectives = EvaluatePopulation(population);
    var parents = Selector.Select(population, objectives, offspringCount * 2);
  
    for (int i = 0; i < parents.Length; i += 2) {
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

  private ObjectiveValue[] EvaluatePopulation(TGenotype[] population) {
    return population.Select(individual => Evaluator.Evaluate(individual)).ToArray();
  }
  
}
