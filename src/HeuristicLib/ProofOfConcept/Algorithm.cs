using FluentValidation;
using FluentValidation.Results;

namespace HEAL.HeuristicLib.ProofOfConcept;

public interface IAlgorithm
{
  // nothing yet
}

public interface IAlgorithm<TState>
  : IAlgorithm
  where TState : class
{
  TState Run(TState? initialState = null);
}

public interface ICrossover
{
  double[] Crossover(double[] parent1, double[] parent2);
}

public interface IMutation
{
  double[] Mutate(double[] individual);
}

public interface ICreator
{
  double[] Create();
}

public interface IEvaluator
{
  double Evaluate(double[] solution);
}

public interface ISelector
{
  List<double[]> Select(List<double[]> population, List<double> qualities, int count);
}

public interface IReplacement
{
  int GetOffspringCount(int populationSize);
  (List<double[]> newPopulation, List<double> newQualities) Replace(
    List<double[]> previousPopulation, List<double> previousQualities,
    List<double[]> offspringPopulation, List<double> offspringQualities);
}

public class PlusSelectionReplacement : IReplacement
{
  public int GetOffspringCount(int populationSize) => populationSize;

  public (List<double[]> newPopulation, List<double> newQualities) Replace(
    List<double[]> previousPopulation, List<double> previousQualities,
    List<double[]> offspringPopulation, List<double> offspringQualities)
  {
    var combinedPopulation = previousPopulation.Concat(offspringPopulation).ToList();
    var combinedQualities = previousQualities.Concat(offspringQualities).ToList();

    var sortedIndices = combinedQualities
      .Select((quality, index) => new { quality, index })
      .OrderBy(x => x.quality)
      .Select(x => x.index)
      .ToList();

    var newPopulation = sortedIndices.Take(previousPopulation.Count).Select(i => combinedPopulation[i]).ToList();
    var newQualities = sortedIndices.Take(previousPopulation.Count).Select(i => combinedQualities[i]).ToList();

    return (newPopulation, newQualities);
  }
}

public class ElitismReplacement : IReplacement
{
  private readonly int elites;

  public ElitismReplacement(int elites)
  {
    this.elites = elites;
  }

  public int GetOffspringCount(int populationSize) => populationSize - elites;

  public (List<double[]> newPopulation, List<double> newQualities) Replace(
    List<double[]> previousPopulation, List<double> previousQualities,
    List<double[]> offspringPopulation, List<double> offspringQualities)
  {
    var sortedPreviousIndices = previousQualities
      .Select((quality, index) => new { quality, index })
      .OrderBy(x => x.quality)
      .Select(x => x.index)
      .ToList();

    var elitesPopulation = sortedPreviousIndices.Take(elites).Select(i => previousPopulation[i]).ToList();
    var elitesQualities = sortedPreviousIndices.Take(elites).Select(i => previousQualities[i]).ToList();

    var newPopulation = elitesPopulation.Concat(offspringPopulation).ToList();
    var newQualities = elitesQualities.Concat(offspringQualities).ToList();

    return (newPopulation, newQualities);
  }
}

#region Sample Operators

public class SinglePointCrossover : ICrossover
{
  private readonly Random random = new();

  public double[] Crossover(double[] parent1, double[] parent2)
  {
    int crossoverPoint = random.Next(1, parent1.Length);
    double[] offspring = new double[parent1.Length];
    Array.Copy(parent1, offspring, crossoverPoint);
    Array.Copy(parent2, crossoverPoint, offspring, crossoverPoint, parent2.Length - crossoverPoint);
    return offspring;
  }
}

public class GaussianMutation : IMutation
{
  private readonly Random random = new();
  private readonly double mutationRate;
  private readonly double mutationStrength;

  public GaussianMutation(double mutationRate, double mutationStrength)
  {
    this.mutationRate = mutationRate;
    this.mutationStrength = mutationStrength;
  }

  public double[] Mutate(double[] individual)
  {
    for (int i = 0; i < individual.Length; i++)
    {
      if (random.NextDouble() < mutationRate)
      {
        individual[i] += random.NextDouble() * mutationStrength * 2 - mutationStrength;
      }
    }
    return individual;
  }
}

public class RandomCreator : ICreator
{
  private readonly Random random = new();
  private readonly int chromosomeLength;
  private readonly double minValue;
  private readonly double maxValue;

  public RandomCreator(int chromosomeLength, double minValue, double maxValue)
  {
    this.chromosomeLength = chromosomeLength;
    this.minValue = minValue;
    this.maxValue = maxValue;
  }

  public double[] Create()
  {
    double[] individual = new double[chromosomeLength];
    for (int i = 0; i < chromosomeLength; i++)
    {
      individual[i] = random.NextDouble() * (maxValue - minValue) + minValue;
    }
    return individual;
  }
}


public class ProportionalSelector : ISelector
{
  private readonly Random random = new();

  public List<double[]> Select(List<double[]> population, List<double> qualities, int count)
  {
    var selected = new List<double[]>();
    for (int j = 0; j < count; j++)
    {
      double totalQuality = qualities.Sum();
      double randomValue = random.NextDouble() * totalQuality;
      double cumulativeQuality = 0.0;

      for (int i = 0; i < population.Count; i++)
      {
        cumulativeQuality += qualities[i];
        if (cumulativeQuality >= randomValue)
        {
          selected.Add(population[i]);
          break;
        }
      }
    }
    return selected;
  }
}

public class RandomSelector : ISelector
{
  private readonly Random random = new();

  public List<double[]> Select(List<double[]> population, List<double> qualities, int count)
  {
    var selected = new List<double[]>();
    for (int i = 0; i < count; i++)
    {
      int index = random.Next(population.Count);
      selected.Add(population[index]);
    }
    return selected;
  }
}
#endregion

public record PopulationState(int CurrentGeneration, List<double[]> Population, List<double> Qualities);


public class GeneticAlgorithm
  : IAlgorithm<PopulationState>
{
  public GeneticAlgorithm(int populationSize,
    ICreator creator, ICrossover crossover, IMutation mutation, double mutationRate,
    ITerminationCriterion<PopulationState> terminationCriterion, IEvaluator evaluator, Random random, ISelector selector, IReplacement replacement)
  {
    PopulationSize = populationSize;
    TerminationCriterion = terminationCriterion;
    Creator = creator;
    Crossover = crossover;
    Mutation = mutation;
    MutationRate = mutationRate;
    Evaluator = evaluator;
    Random = random;
    Selector = selector;
    Replacement = replacement;
  }
  
  public int PopulationSize { get; }
  public ITerminationCriterion<PopulationState> TerminationCriterion { get; }
  public ICreator Creator { get; }
  public ICrossover Crossover { get; }
  public IMutation Mutation { get; }
  public double MutationRate { get; }
  public IEvaluator Evaluator { get; }
  public Random Random { get; }
  public ISelector Selector { get; }
  public IReplacement Replacement { get; }
  // WIP
  public event Action<double[]>? OnLiveResult;



  public PopulationState Run(PopulationState? state = null)
  {
    var population = state?.Population ?? InitializePopulation();
    var qualities = state?.Qualities ?? EvaluatePopulation(population);
    var currentGeneration = state?.CurrentGeneration ?? 0;
    var currentState = new PopulationState(currentGeneration, population, qualities);

    while (!TerminationCriterion.ShouldTerminate(currentState))
    {
      var offspringCount = Replacement.GetOffspringCount(PopulationSize);
      var offspringPopulation = EvolvePopulation(population, offspringCount);
      var offspringQualities = EvaluatePopulation(offspringPopulation);

      (population, qualities) = Replacement.Replace(population, qualities, offspringPopulation, offspringQualities);

      var bestIndividual = GetBestIndividual(population, qualities);
      OnLiveResult?.Invoke(bestIndividual);
      currentState = currentState with { CurrentGeneration = ++currentGeneration, Population = population, Qualities = qualities };
    }

    return currentState;
  }

  private List<double[]> InitializePopulation()
  {
    var population = new List<double[]>();
    for (int i = 0; i < PopulationSize; i++) {
      population.Add(Creator.Create());
    }
    return population;
  }

  private List<double[]> EvolvePopulation(List<double[]> population, int offspringCount)
  {
    var newPopulation = new List<double[]>();
    var qualities = EvaluatePopulation(population);
    var parents = Selector.Select(population, qualities, offspringCount * 2);

    for (int i = 0; i < parents.Count; i += 2)
    {
      var parent1 = parents[i];
      var parent2 = parents[i + 1];
      var offspring = Crossover.Crossover(parent1, parent2);
      if (Random.NextDouble() < MutationRate) {
        offspring = Mutation.Mutate(offspring);
      }
      newPopulation.Add(offspring);
    }
    return newPopulation;
  }

  private List<double> EvaluatePopulation(List<double[]> population)
  {
    return population.Select(individual => Evaluator.Evaluate(individual)).ToList();
  }

  private double[] GetBestIndividual(List<double[]> population, List<double> qualities) {
    var bestIndex = qualities.IndexOf(qualities.Min());
    return population[bestIndex];
  }
}

public interface ITerminationCriterion<in TState>
{
  bool ShouldTerminate(TState state);
}

public class ThresholdCriterion<TState> 
  : ITerminationCriterion<TState>
{
  public ThresholdCriterion(int threshold, Func<TState, int> accessor)
  {
    Threshold = threshold;
    Accessor = accessor;
  }

  public int Threshold { get; }
  public Func<TState, int> Accessor { get; }

  public bool ShouldTerminate(TState state)
  {
    return Accessor(state) >= Threshold;
  }
}


public class PauseToken
{
  public bool IsPaused { get; private set; }
  public void RequestPause() => IsPaused = true;
}

public class PauseTokenCriterion<TState>
  : ITerminationCriterion<TState>
{
  public PauseTokenCriterion(PauseToken pauseToken) {
    Token = pauseToken;
  }

  public PauseToken Token { get; }

  public bool ShouldTerminate(TState state) => Token.IsPaused;
}

public class AnyTerminationCriterion<TState>
  : ITerminationCriterion<TState>
{
  public AnyTerminationCriterion(IReadOnlyList<ITerminationCriterion<TState>> criteria)
  {
    this.criteria = criteria;
  }

  private readonly IReadOnlyList<ITerminationCriterion<TState>> criteria;

  public bool ShouldTerminate(TState state)
  {
    return criteria.Any(criterion => criterion.ShouldTerminate(state));
  }
}


public class GeneticAlgorithmBuilder
{
  private int populationSize = 100; // Default value
  private readonly List<ITerminationCriterion<PopulationState>> terminationCriteria = [];
  private ICreator? creator;
  private ICrossover? crossover;
  private IMutation? mutation;
  private IEvaluator? evaluator;
  private Action<double[]>? onLiveResult;
  private double mutationRate;
  private Random? random;
  private ISelector? selector;
  private IReplacement? replacement;

  public GeneticAlgorithmBuilder WithPopulationSize(int populationSize) {
    this.populationSize = populationSize;
    return this;
  }
  
  public GeneticAlgorithmBuilder WithRandomCreator(int chromosomeLength, double minValue, double maxValue) {
    creator = new RandomCreator(chromosomeLength, minValue, maxValue);
    return this;
  }

  public GeneticAlgorithmBuilder WithCrossover(ICrossover crossover) {
    this.crossover = crossover;
    return this;
  }

  public GeneticAlgorithmBuilder WithMutation(IMutation mutation) {
    this.mutation = mutation;
    return this;
  }

  public GeneticAlgorithmBuilder WithGaussianMutation(double mutationRate, double mutationStrength) {
    mutation = new GaussianMutation(mutationRate, mutationStrength);
    return this;
  }

  public GeneticAlgorithmBuilder WithMutationRate(double mutationRate) {
    this.mutationRate = mutationRate;
    return this;
  }

  public GeneticAlgorithmBuilder WithEvaluator(IEvaluator evaluator) {
    this.evaluator = evaluator;
    return this;
  }

  public GeneticAlgorithmBuilder WithRandom(Random random) {
    this.random = random;
    return this;
  }

  public GeneticAlgorithmBuilder WithSelector(ISelector selector) {
    this.selector = selector;
    return this;
  }

  public GeneticAlgorithmBuilder WithReplacement(IReplacement replacement) {
    this.replacement = replacement;
    return this;
  }

  public GeneticAlgorithmBuilder WithPlusSelectionReplacement() {
    replacement = new PlusSelectionReplacement();
    return this;
  }

  public GeneticAlgorithmBuilder WithElitismReplacement(int elites) {
    replacement = new ElitismReplacement(elites);
    return this;
  }

  public GeneticAlgorithmBuilder TerminateOnMaxGenerations(int maxGenerations) {
    terminationCriteria.Add(new ThresholdCriterion<PopulationState>(maxGenerations, state => state.CurrentGeneration));
    return this;
  }

  public GeneticAlgorithmBuilder TerminateOnPauseToken(PauseToken pauseToken) {
    terminationCriteria.Add(new PauseTokenCriterion<PopulationState>(pauseToken));
    return this;
  }

  public GeneticAlgorithmBuilder OnLiveResult(Action<double[]> handler) {
    onLiveResult = handler;
    return this;
  }

  public GeneticAlgorithm Build() {
    var result = Validate();
    if (!result.IsValid) {
      throw new ValidationException(result.Errors);
    }
    
    var ga = new GeneticAlgorithm(
      populationSize,
      creator!,
      crossover!,
      mutation!, mutationRate, new AnyTerminationCriterion<PopulationState>(terminationCriteria.ToList()),
      evaluator!, random ?? new Random(), selector!, replacement!
    );
    if (onLiveResult != null) {
      ga.OnLiveResult += onLiveResult;
    }
    return ga;
  }
  
  
  private ValidationResult Validate() {
    var validator = new GeneticAlgorithmBuilderValidator();
    return validator.Validate(this);
  }
  
  public class GeneticAlgorithmBuilderValidator : AbstractValidator<GeneticAlgorithmBuilder> {
    public GeneticAlgorithmBuilderValidator() {
      RuleFor(x => x.creator).NotNull().WithMessage("Creator must not be null.");
      RuleFor(x => x.crossover).NotNull().WithMessage("Crossover must not be null.");
      RuleFor(x => x.mutation).NotNull().WithMessage("Mutation must not be null.");
      RuleFor(x => x.mutationRate)
        .InclusiveBetween(0, 1)
        .WithMessage("Mutation rate must be between 0 and 1.");
      RuleFor(x => x.evaluator).NotNull().WithMessage("Evaluator must not be null.");
      RuleFor(x => x.selector).NotNull().WithMessage("Selector must not be null.");
      RuleFor(x => x.replacement).NotNull().WithMessage("Replacement must not be null.");
    }
  }
}

class AlgorithmExample
{
  void Execute() {
    var pauseToken = new PauseToken();

    var ga = new GeneticAlgorithmBuilder()
      .WithPopulationSize(200)
      .TerminateOnMaxGenerations(50)
      .TerminateOnPauseToken(pauseToken)
      .OnLiveResult(best => Console.WriteLine($"Best so far: {string.Join(",", best)}"))
      .Build();

    Task.Run(async () =>
    {
      await Task.Delay(1000);
      pauseToken.RequestPause();
    });

    var finalState = ga.Run();
    Console.WriteLine($"Paused at generation {finalState.CurrentGeneration}");
  }
}
