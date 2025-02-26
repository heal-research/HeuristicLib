using HEAL.HeuristicLib.Operators;

namespace HEAL.HeuristicLib.Algorithms.GeneticAlgorithm;


public record PopulationState<TSolution>(int CurrentGeneration, IReadOnlyList<TSolution> Population, IReadOnlyList<ObjectiveValue> Objectives);




public class GeneticAlgorithm<TGenotype> : AlgorithmBase<PopulationState<TGenotype>>
{
  public GeneticAlgorithm(int populationSize,
    ICreator<TGenotype> creator, ICrossover<TGenotype> crossover, IMutator<TGenotype> mutator, double mutationRate,
    ITerminator<PopulationState<TGenotype>> terminator, IEvaluator<TGenotype, ObjectiveValue> evaluator, IRandomGenerator random, ISelector<TGenotype, ObjectiveValue> selector, IReplacer<TGenotype> replacer)
  {
    PopulationSize = populationSize;
    Terminator = terminator;
    Creator = creator;
    Crossover = crossover;
    Mutator = mutator;
    MutationRate = mutationRate;
    Evaluator = evaluator;
    Random = random;
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
  public IRandomGenerator Random { get; }
  public ISelector<TGenotype, ObjectiveValue> Selector { get; }
  public IReplacer<TGenotype> Replacer { get; }
  // WIP
  public event Action<TGenotype>? OnLiveResult;

  public override PopulationState<TGenotype> Run(PopulationState<TGenotype>? state = null)
  {
    var population = state?.Population ?? InitializePopulation();
    var objectives = state?.Objectives ?? EvaluatePopulation(population);
    var currentGeneration = state?.CurrentGeneration ?? 0;
    var currentState = new PopulationState<TGenotype>(currentGeneration, population, objectives);

    while (!Terminator.ShouldTerminate(currentState))
    {
      var offspringCount = Replacer.GetOffspringCount(PopulationSize);
      var offspringPopulation = EvolvePopulation(population, offspringCount);
      var offspringQualities = EvaluatePopulation(offspringPopulation);

      (population, objectives) = Replacer.Replace(population, objectives, offspringPopulation, offspringQualities);

      var bestIndividual = GetBestIndividual(population, objectives);
      OnLiveResult?.Invoke(bestIndividual);
      currentState = currentState with { CurrentGeneration = ++currentGeneration, Population = population, Objectives = objectives };
    }

    return currentState;
  }

  private IReadOnlyList<TGenotype> InitializePopulation()
  {
    var population = new List<TGenotype>();
    for (int i = 0; i < PopulationSize; i++) {
      population.Add(Creator.Create());
    }
    return population;
  }

  private IReadOnlyList<TGenotype> EvolvePopulation(IReadOnlyList<TGenotype> population, int offspringCount)
  {
    var newPopulation = new List<TGenotype>();
    var objectives = EvaluatePopulation(population);
    var parents = Selector.Select(population, objectives, offspringCount * 2);

    for (int i = 0; i < parents.Count; i += 2)
    {
      var parent1 = parents[i];
      var parent2 = parents[i + 1];
      var offspring = Crossover.Crossover(parent1, parent2);
      if (Random.Random() < MutationRate) {
        offspring = Mutator.Mutate(offspring);
      }
      newPopulation.Add(offspring);
    }
    return newPopulation;
  }

  private IReadOnlyList<ObjectiveValue> EvaluatePopulation(IReadOnlyList<TGenotype> population)
  {
    return population.Select(individual => Evaluator.Evaluate(individual)).ToList();
  }

  private TGenotype GetBestIndividual(IReadOnlyList<TGenotype> population, IReadOnlyList<ObjectiveValue> objectives)
  {
    int bestIndex = objectives
      .Select((objective, index) => new { objective, index })
      .OrderBy(x => x.objective)
      .First().index;
    return population[bestIndex];
  }
}
