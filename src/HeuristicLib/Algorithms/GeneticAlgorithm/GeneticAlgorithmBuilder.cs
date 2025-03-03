using FluentValidation;
using FluentValidation.Results;
using HEAL.HeuristicLib.Operators;

namespace HEAL.HeuristicLib.Algorithms.GeneticAlgorithm;

public class GeneticAlgorithmBuilder<TSolution> {
  
  private int? populationSize;
  private ITerminator<PopulationState<TSolution>>? terminationCriterion;

  private CreatorParameters? creatorParameters;
  private Func<CreatorParameters, ICreator<TSolution>> creatorFactory;
  
  //private ICreator<TSolution>? creator;
  private ICrossover<TSolution>? crossover;
  private IMutator<TSolution>? mutator;
  private IEvaluator<TSolution, ObjectiveValue>? evaluator;
  private Action<TSolution>? onLiveResult;
  private double mutationRate;
  private RandomSource? randomSource;
  private ISelector<TSolution, ObjectiveValue>? selector;
  private IReplacer<TSolution>? replacement;

  public GeneticAlgorithmBuilder() {
    populationSize = 100;
    terminationCriterion = new ThresholdTerminator<PopulationState<TSolution>>(50, state => state.CurrentGeneration);
  }

  public GeneticAlgorithmBuilder<TSolution> WithPopulationSize(int populationSize) {
    this.populationSize = populationSize;
    return this;
  }
  
  
  public GeneticAlgorithmBuilder<TSolution> WithCreatorFactory(Func<CreatorParameters, ICreator<TSolution>> creatorFactory) {
    this.creatorFactory = creatorFactory;
    return this;
  }
  public GeneticAlgorithmBuilder<TSolution> WithCreator(ICreator<TSolution> creator) {
    creatorFactory = _ => creator;
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

  public GeneticAlgorithmBuilder<TSolution> WithRandomSource(RandomSource randomSource) {
    this.randomSource = randomSource;
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
    // ToDo: add or replace criterion?
    terminationCriterion = new ThresholdTerminator<PopulationState<TSolution>>(maxGenerations, state => state.CurrentGeneration);
    return this;
  }

  public GeneticAlgorithmBuilder<TSolution> TerminateOnPauseToken(PauseToken pauseToken) {
    // ToDo: add or replace criterion?
    terminationCriterion = new PauseTokenTerminator<PopulationState<TSolution>>(pauseToken);
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
    
    var creator = creatorFactory(creatorParameters!);
    
    var ga = new GeneticAlgorithm<TSolution>(
      populationSize!.Value,
      creator,
      crossover!,
      mutator!, mutationRate, terminationCriterion!,
      evaluator!,
      randomSource ?? RandomSource.CreateDefault(42),
      selector!, replacement!
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
      RuleFor(x => x.populationSize).NotNull().WithMessage("Population size must not be null.");
      RuleFor(x => x.creatorFactory).NotNull().WithMessage("Creator must not be null.");
      RuleFor(x => x.crossover).NotNull().WithMessage("Crossover must not be null.");
      RuleFor(x => x.mutator).NotNull().WithMessage("Mutation must not be null.");
      RuleFor(x => x.mutationRate).InclusiveBetween(0, 1).WithMessage("Mutation rate must be between 0 and 1.");
      RuleFor(x => x.evaluator).NotNull().WithMessage("Evaluator must not be null.");
      RuleFor(x => x.selector).NotNull().WithMessage("Selector must not be null.");
      RuleFor(x => x.replacement).NotNull().WithMessage("Replacement must not be null.");
      RuleFor(x => x.terminationCriterion).NotNull().WithMessage("Termination criterion must not be null.");
    }
  }
}
