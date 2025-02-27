using FluentValidation;
using FluentValidation.Results;
using HEAL.HeuristicLib.Operators;

namespace HEAL.HeuristicLib.Algorithms.GeneticAlgorithm;

public class GeneticAlgorithmBuilderBasic<TSolution> {
  
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

  public GeneticAlgorithmBuilderBasic() {
    populationSize = 100;
    terminationCriterion = new ThresholdTerminator<PopulationState<TSolution>>(50, state => state.CurrentGeneration);
  }

  public GeneticAlgorithmBuilderBasic<TSolution> WithPopulationSize(int populationSize) {
    this.populationSize = populationSize;
    return this;
  }
  
  
  public GeneticAlgorithmBuilderBasic<TSolution> WithCreatorFactory(Func<CreatorParameters, ICreator<TSolution>> creatorFactory) {
    this.creatorFactory = creatorFactory;
    return this;
  }
  public GeneticAlgorithmBuilderBasic<TSolution> WithCreator(ICreator<TSolution> creator) {
    creatorFactory = _ => creator;
    return this;
  }

  public GeneticAlgorithmBuilderBasic<TSolution> WithCrossover(ICrossover<TSolution> crossover) {
    this.crossover = crossover;
    return this;
  }

  public GeneticAlgorithmBuilderBasic<TSolution> WithMutation(IMutator<TSolution> mutator) {
    this.mutator = mutator;
    return this;
  }

  public GeneticAlgorithmBuilderBasic<TSolution> WithMutationRate(double mutationRate) {
    this.mutationRate = mutationRate;
    return this;
  }

  public GeneticAlgorithmBuilderBasic<TSolution> WithEvaluator(IEvaluator<TSolution, ObjectiveValue> evaluator) {
    this.evaluator = evaluator;
    return this;
  }

  public GeneticAlgorithmBuilderBasic<TSolution> WithRandomSource(RandomSource randomSource) {
    this.randomSource = randomSource;
    return this;
  }

  public GeneticAlgorithmBuilderBasic<TSolution> WithSelector(ISelector<TSolution, ObjectiveValue> selector) {
    this.selector = selector;
    return this;
  }

  public GeneticAlgorithmBuilderBasic<TSolution> WithReplacement(IReplacer<TSolution> replacer) {
    this.replacement = replacer;
    return this;
  }

  public GeneticAlgorithmBuilderBasic<TSolution> WithPlusSelectionReplacement() {
    replacement = new PlusSelectionReplacer<TSolution>();
    return this;
  }

  public GeneticAlgorithmBuilderBasic<TSolution> WithElitismReplacement(int elites) {
    replacement = new ElitismReplacer<TSolution>(elites);
    return this;
  }

  public GeneticAlgorithmBuilderBasic<TSolution> TerminateOnMaxGenerations(int maxGenerations) {
    // ToDo: add or replace criterion?
    terminationCriterion = new ThresholdTerminator<PopulationState<TSolution>>(maxGenerations, state => state.CurrentGeneration);
    return this;
  }

  public GeneticAlgorithmBuilderBasic<TSolution> TerminateOnPauseToken(PauseToken pauseToken) {
    // ToDo: add or replace criterion?
    terminationCriterion = new PauseTokenTerminator<PopulationState<TSolution>>(pauseToken);
    return this;
  }

  public GeneticAlgorithmBuilderBasic<TSolution> OnLiveResult(Action<TSolution> handler) {
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
  
  public class GeneticAlgorithmBuilderValidator : AbstractValidator<GeneticAlgorithmBuilderBasic<TSolution>> {
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
