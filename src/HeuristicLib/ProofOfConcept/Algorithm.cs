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

public interface ICrossover<TSolution>
{
  TSolution Crossover(TSolution parent1, TSolution parent2);
}

public interface IMutation<TSolution>
{
  TSolution Mutate(TSolution individual);
}

public interface ICreator<TSolution>
{
  TSolution Create();
}

public interface IEvaluator<TSolution>
{
  double Evaluate(TSolution solution);
}

public interface ISelector<TSolution>
{
  List<TSolution> Select(List<TSolution> population, List<double> qualities, int count);
}

public interface IReplacement<TSolution>
{
  int GetOffspringCount(int populationSize);
  (List<TSolution> newPopulation, List<double> newQualities) Replace(
    List<TSolution> previousPopulation, List<double> previousQualities,
    List<TSolution> offspringPopulation, List<double> offspringQualities);
}

public class PlusSelectionReplacement<TSolution> : IReplacement<TSolution>
{
  public int GetOffspringCount(int populationSize) => populationSize;

  public (List<TSolution> newPopulation, List<double> newQualities) Replace(
    List<TSolution> previousPopulation, List<double> previousQualities,
    List<TSolution> offspringPopulation, List<double> offspringQualities)
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

public class ElitismReplacement<TSolution> : IReplacement<TSolution>
{
  private readonly int elites;

  public ElitismReplacement(int elites)
  {
    this.elites = elites;
  }

  public int GetOffspringCount(int populationSize) => populationSize - elites;

  public (List<TSolution> newPopulation, List<double> newQualities) Replace(
    List<TSolution> previousPopulation, List<double> previousQualities,
    List<TSolution> offspringPopulation, List<double> offspringQualities)
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

public class SinglePointCrossover : ICrossover<double[]>
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

public class GaussianMutation : IMutation<double[]>
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

public class RandomCreator : ICreator<double[]>
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

public class ProportionalSelector<TSolution> : ISelector<TSolution>
{
  private readonly Random random = new();

  public List<TSolution> Select(List<TSolution> population, List<double> qualities, int count)
  {
    var selected = new List<TSolution>();
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

public class RandomSelector<TSolution> : ISelector<TSolution>
{
  private readonly Random random = new();

  public List<TSolution> Select(List<TSolution> population, List<double> qualities, int count)
  {
    var selected = new List<TSolution>();
    for (int i = 0; i < count; i++)
    {
      int index = random.Next(population.Count);
      selected.Add(population[index]);
    }
    return selected;
  }
}
#endregion

public record PopulationState<TSolution>(int CurrentGeneration, List<TSolution> Population, List<double> Qualities);

public class GeneticAlgorithm<TSolution>
  : IAlgorithm<PopulationState<TSolution>>
{
  public GeneticAlgorithm(int populationSize,
    ICreator<TSolution> creator, ICrossover<TSolution> crossover, IMutation<TSolution> mutation, double mutationRate,
    ITerminationCriterion<PopulationState<TSolution>> terminationCriterion, IEvaluator<TSolution> evaluator, Random random, ISelector<TSolution> selector, IReplacement<TSolution> replacement)
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
  public ITerminationCriterion<PopulationState<TSolution>> TerminationCriterion { get; }
  public ICreator<TSolution> Creator { get; }
  public ICrossover<TSolution> Crossover { get; }
  public IMutation<TSolution> Mutation { get; }
  public double MutationRate { get; }
  public IEvaluator<TSolution> Evaluator { get; }
  public Random Random { get; }
  public ISelector<TSolution> Selector { get; }
  public IReplacement<TSolution> Replacement { get; }
  // WIP
  public event Action<TSolution>? OnLiveResult;

  public PopulationState<TSolution> Run(PopulationState<TSolution>? state = null)
  {
    var population = state?.Population ?? InitializePopulation();
    var qualities = state?.Qualities ?? EvaluatePopulation(population);
    var currentGeneration = state?.CurrentGeneration ?? 0;
    var currentState = new PopulationState<TSolution>(currentGeneration, population, qualities);

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

  private List<TSolution> InitializePopulation()
  {
    var population = new List<TSolution>();
    for (int i = 0; i < PopulationSize; i++) {
      population.Add(Creator.Create());
    }
    return population;
  }

  private List<TSolution> EvolvePopulation(List<TSolution> population, int offspringCount)
  {
    var newPopulation = new List<TSolution>();
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

  private List<double> EvaluatePopulation(List<TSolution> population)
  {
    return population.Select(individual => Evaluator.Evaluate(individual)).ToList();
  }

  private TSolution GetBestIndividual(List<TSolution> population, List<double> qualities) {
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

public class GeneticAlgorithmBuilder<TSolution>
{
  private int populationSize = 100; // Default value
  private readonly List<ITerminationCriterion<PopulationState<TSolution>>> terminationCriteria = [];
  private ICreator<TSolution>? creator;
  private ICrossover<TSolution>? crossover;
  private IMutation<TSolution>? mutation;
  private IEvaluator<TSolution>? evaluator;
  private Action<TSolution>? onLiveResult;
  private double mutationRate;
  private Random? random;
  private ISelector<TSolution>? selector;
  private IReplacement<TSolution>? replacement;

  public GeneticAlgorithmBuilder<TSolution> WithPopulationSize(int populationSize) {
    this.populationSize = populationSize;
    return this;
  }

  public GeneticAlgorithmBuilder<TSolution> WithCrossover(ICrossover<TSolution> crossover) {
    this.crossover = crossover;
    return this;
  }

  public GeneticAlgorithmBuilder<TSolution> WithMutation(IMutation<TSolution> mutation) {
    this.mutation = mutation;
    return this;
  }

  public GeneticAlgorithmBuilder<TSolution> WithMutationRate(double mutationRate) {
    this.mutationRate = mutationRate;
    return this;
  }

  public GeneticAlgorithmBuilder<TSolution> WithEvaluator(IEvaluator<TSolution> evaluator) {
    this.evaluator = evaluator;
    return this;
  }

  public GeneticAlgorithmBuilder<TSolution> WithRandom(Random random) {
    this.random = random;
    return this;
  }

  public GeneticAlgorithmBuilder<TSolution> WithSelector(ISelector<TSolution> selector) {
    this.selector = selector;
    return this;
  }

  public GeneticAlgorithmBuilder<TSolution> WithReplacement(IReplacement<TSolution> replacement) {
    this.replacement = replacement;
    return this;
  }

  public GeneticAlgorithmBuilder<TSolution> WithPlusSelectionReplacement() {
    replacement = new PlusSelectionReplacement<TSolution>();
    return this;
  }

  public GeneticAlgorithmBuilder<TSolution> WithElitismReplacement(int elites) {
    replacement = new ElitismReplacement<TSolution>(elites);
    return this;
  }

  public GeneticAlgorithmBuilder<TSolution> TerminateOnMaxGenerations(int maxGenerations) {
    terminationCriteria.Add(new ThresholdCriterion<PopulationState<TSolution>>(maxGenerations, state => state.CurrentGeneration));
    return this;
  }

  public GeneticAlgorithmBuilder<TSolution> TerminateOnPauseToken(PauseToken pauseToken) {
    terminationCriteria.Add(new PauseTokenCriterion<PopulationState<TSolution>>(pauseToken));
    return this;
  }

  public GeneticAlgorithmBuilder<TSolution> OnLiveResult(Action<TSolution> handler) {
    onLiveResult = handler;
    return this;
  }

  public GeneticAlgorithm<TSolution> Build() {
    var result = Validate();
    if (!result.IsValid) {
      throw new ValidationException(result.Errors);
    }
    
    var ga = new GeneticAlgorithm<TSolution>(
      populationSize,
      creator!,
      crossover!,
      mutation!, mutationRate, new AnyTerminationCriterion<PopulationState<TSolution>>(terminationCriteria.ToList()),
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
  
  public class GeneticAlgorithmBuilderValidator : AbstractValidator<GeneticAlgorithmBuilder<TSolution>> {
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

    var ga = new GeneticAlgorithmBuilder<double[]>()
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
