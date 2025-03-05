using HEAL.HeuristicLib.Encodings;

namespace HEAL.HeuristicLib.Algorithms.GeneticAlgorithm;

using Operators;

public record PopulationState<TSolution>(
  int CurrentGeneration,
  TSolution[] Population,
  ObjectiveValue[] Objectives
);

public class GeneticAlgorithm<TGenotype> : AlgorithmBase<PopulationState<TGenotype>> {
  
  public GeneticAlgorithm(int populationSize,
    ICreator<TGenotype> creator, ICrossover<TGenotype> crossover, IMutator<TGenotype> mutator, double mutationRate,
    ITerminator<PopulationState<TGenotype>> terminator, IEvaluator<TGenotype, ObjectiveValue> evaluator,
    RandomSource randomSourceState, ISelector<TGenotype, ObjectiveValue> selector, IReplacer<TGenotype> replacer)
  {
    PopulationSize = populationSize;
    Terminator = terminator;
    Creator = creator;
    Crossover = crossover;
    Mutator = mutator;
    MutationRate = mutationRate;
    Evaluator = evaluator;
    RandomSource = randomSourceState;
    Selector = selector;
    Replacer = replacer;
  }
  
  public int PopulationSize { get; }
  public ITerminator<PopulationState<TGenotype>> Terminator { get; }
  public ICreator<TGenotype> Creator { get; }
  public ICrossover<TGenotype> Crossover { get; }
  public IMutator<TGenotype> Mutator { get; }
  public double MutationRate { get; }
  public IEvaluator<TGenotype, ObjectiveValue> Evaluator { get; }
  public RandomSource RandomSource { get; }
  public ISelector<TGenotype, ObjectiveValue> Selector { get; }
  public IReplacer<TGenotype> Replacer { get; }
  // WIP
  public event Action<TGenotype>? OnLiveResult;

  public override PopulationState<TGenotype> Run(PopulationState<TGenotype>? state = null) {
    var rng = RandomSource.CreateRandomNumberGenerator();
    
    var population = state?.Population ?? InitializePopulation();
    var objectives = state?.Objectives ?? EvaluatePopulation(population);
    var currentGeneration = state?.CurrentGeneration ?? 0;
    var currentState = new PopulationState<TGenotype>(currentGeneration, population, objectives);

    while (!Terminator.ShouldTerminate(currentState))
    {
      var offspringCount = Replacer.GetOffspringCount(PopulationSize);
      var offspringPopulation = EvolvePopulation(population, offspringCount, rng);
      var offspringQualities = EvaluatePopulation(offspringPopulation);

      (population, objectives) = Replacer.Replace(population, objectives, offspringPopulation, offspringQualities);

      var bestIndividual = GetBestIndividual(population, objectives);
      OnLiveResult?.Invoke(bestIndividual);
      currentState = currentState with { CurrentGeneration = ++currentGeneration, Population = population, Objectives = objectives };
    }

    return currentState;
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
      var offspring = Crossover.Crossover(parent1, parent2);
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

  private TGenotype GetBestIndividual(TGenotype[] population, ObjectiveValue[] objectives) {
    int bestIndex = objectives
      .Select((objective, index) => new { objective, index })
      .OrderBy(x => x.objective)
      .First().index;
    return population[bestIndex];
  }
}
