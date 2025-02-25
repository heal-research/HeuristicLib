using FluentValidation;
using FluentValidation.Results;
using HEAL.HeuristicLib.Encodings;
using HEAL.HeuristicLib.Operators;

namespace HEAL.HeuristicLib.Algorithms;


public record PopulationState<TSolution>(int CurrentGeneration, IReadOnlyList<TSolution> Population, IReadOnlyList<ObjectiveValue> Objectives);




public class GeneticAlgorithm<TGenotype>
  : AlgorithmBase<PopulationState<TGenotype>>
{
  public GeneticAlgorithm(int populationSize,
    ICreator<TGenotype> creator, ICrossover<TGenotype> crossover, IMutator<TGenotype> mutator, double mutationRate,
    ITerminator<PopulationState<TGenotype>> terminator, IEvaluator<TGenotype, ObjectiveValue> evaluator, Random random, ISelector<TGenotype, ObjectiveValue> selector, IReplacer<TGenotype> replacer)
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
  public Random Random { get; }
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
      if (Random.NextDouble() < MutationRate) {
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


public class GeneticAlgorithmBuilder<TSolution>
{
  private int populationSize = 100; // Default value
  private readonly List<ITerminator<PopulationState<TSolution>>> terminationCriteria = [];
  private ICreator<TSolution>? creator;
  private ICrossover<TSolution>? crossover;
  private IMutator<TSolution>? mutator;
  private IEvaluator<TSolution, ObjectiveValue>? evaluator;
  private Action<TSolution>? onLiveResult;
  private double mutationRate;
  private Random? random;
  private ISelector<TSolution, ObjectiveValue>? selector;
  private IReplacer<TSolution>? replacement;

  public GeneticAlgorithmBuilder<TSolution> WithPopulationSize(int populationSize) {
    this.populationSize = populationSize;
    return this;
  }

  public GeneticAlgorithmBuilder<TSolution> WithCrossover(ICrossover<TSolution> crossover) {
    this.crossover = crossover;
    return this;
  }

  public GeneticAlgorithmBuilder<TSolution> WithMutation(IMutator<TSolution> mutator) {
    this.mutator = mutator;
    return this;
  }

  public GeneticAlgorithmBuilder<TSolution> WithMutationRate(double mutationRate) {
    this.mutationRate = mutationRate;
    return this;
  }

  public GeneticAlgorithmBuilder<TSolution> WithEvaluator(IEvaluator<TSolution, ObjectiveValue> evaluator) {
    this.evaluator = evaluator;
    return this;
  }

  public GeneticAlgorithmBuilder<TSolution> WithRandom(Random random) {
    this.random = random;
    return this;
  }

  public GeneticAlgorithmBuilder<TSolution> WithSelector(ISelector<TSolution, ObjectiveValue> selector) {
    this.selector = selector;
    return this;
  }

  public GeneticAlgorithmBuilder<TSolution> WithReplacement(IReplacer<TSolution> replacer) {
    this.replacement = replacer;
    return this;
  }

  public GeneticAlgorithmBuilder<TSolution> WithPlusSelectionReplacement() {
    replacement = new PlusSelectionReplacer<TSolution>();
    return this;
  }

  public GeneticAlgorithmBuilder<TSolution> WithElitismReplacement(int elites) {
    replacement = new ElitismReplacer<TSolution>(elites);
    return this;
  }

  public GeneticAlgorithmBuilder<TSolution> TerminateOnMaxGenerations(int maxGenerations) {
    terminationCriteria.Add(new ThresholdTerminator<PopulationState<TSolution>>(maxGenerations, state => state.CurrentGeneration));
    return this;
  }

  public GeneticAlgorithmBuilder<TSolution> TerminateOnPauseToken(PauseToken pauseToken) {
    terminationCriteria.Add(new PauseTokenTerminator<PopulationState<TSolution>>(pauseToken));
    return this;
  }

  public GeneticAlgorithmBuilder<TSolution> OnLiveResult(Action<TSolution> handler) {
    onLiveResult = handler;
    return this;
  }
  
  public GeneticAlgorithmBuilder<TSolution> WithEncodingBundle<TEncoding>(IEncodingBundle<TSolution, TEncoding> encoding)
  {
    if (encoding is ICreatorProvider<TSolution> creatorProvider) {
      creator = creatorProvider.Creator;
    }
    if (encoding is ICrossoverProvider<TSolution> crossoverProvider) {
      crossover = crossoverProvider.Crossover;
    }
    if (encoding is IMutatorProvider<TSolution> mutatorProvider) {
      mutator = mutatorProvider.Mutator;
    }
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
      mutator!, mutationRate, new AnyTerminator<PopulationState<TSolution>>(terminationCriteria.ToList()),
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
      RuleFor(x => x.mutator).NotNull().WithMessage("Mutation must not be null.");
      RuleFor(x => x.mutationRate)
        .InclusiveBetween(0, 1)
        .WithMessage("Mutation rate must be between 0 and 1.");
      RuleFor(x => x.evaluator).NotNull().WithMessage("Evaluator must not be null.");
      RuleFor(x => x.selector).NotNull().WithMessage("Selector must not be null.");
      RuleFor(x => x.replacement).NotNull().WithMessage("Replacement must not be null.");
    }
  }
}
