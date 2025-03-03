using FluentValidation;
using FluentValidation.Results;
using HEAL.HeuristicLib.Encodings;
using HEAL.HeuristicLib.Operators;

namespace HEAL.HeuristicLib.Algorithms.GeneticAlgorithm;

public class GeneticAlgorithmBuilder<TEncoding, TGenotype> where TEncoding : IEncoding<TGenotype> {
  
  private int? populationSize;
  private ITerminator<PopulationState<TGenotype>>? terminationCriterion;

  private TEncoding? encoding;
  
  //private CreatorParameters? creatorParameters;
  //private Func<CreatorParameters, ICreator<TGenotype>> creatorFactory;
  
  private ICreator<TGenotype>? creator;
  private ICrossover<TGenotype>? crossover;
  private IMutator<TGenotype>? mutator;
  private IEvaluator<TGenotype, ObjectiveValue>? evaluator;
  private Action<TGenotype>? onLiveResult;
  private double mutationRate;
  private RandomSource? randomSource;
  private ISelector<TGenotype, ObjectiveValue>? selector;
  private IReplacer<TGenotype>? replacement;

  public GeneticAlgorithmBuilder() {
    populationSize = 100;
    terminationCriterion = new ThresholdTerminator<PopulationState<TGenotype>>(50, state => state.CurrentGeneration);
  }

  public GeneticAlgorithmBuilder<TEncoding, TGenotype> WithPopulationSize(int populationSize) {
    this.populationSize = populationSize;
    return this;
  }
  
  
  // public GeneticAlgorithmBuilder<TGenotype> WithCreatorFactory(Func<CreatorParameters, ICreator<TGenotype>> creatorFactory) {
  //   this.creatorFactory = creatorFactory;
  //   return this;
  // }
  public GeneticAlgorithmBuilder<TEncoding, TGenotype> WithCreator(ICreator<TGenotype> creator) {
    //creatorFactory = _ => creator;
    this.creator = creator;
    return this;
  }

  public GeneticAlgorithmBuilder<TEncoding, TGenotype> WithCrossover(ICrossover<TGenotype> crossover) {
    this.crossover = crossover;
    return this;
  }

  public GeneticAlgorithmBuilder<TEncoding, TGenotype> WithMutation(IMutator<TGenotype> mutator) {
    this.mutator = mutator;
    return this;
  }

  public GeneticAlgorithmBuilder<TEncoding, TGenotype> WithMutationRate(double mutationRate) {
    this.mutationRate = mutationRate;
    return this;
  }

  public GeneticAlgorithmBuilder<TEncoding, TGenotype> WithEvaluator(IEvaluator<TGenotype, ObjectiveValue> evaluator) {
    this.evaluator = evaluator;
    return this;
  }

  public GeneticAlgorithmBuilder<TEncoding, TGenotype> WithRandomSource(RandomSource randomSource) {
    this.randomSource = randomSource;
    return this;
  }

  public GeneticAlgorithmBuilder<TEncoding, TGenotype> WithSelector(ISelector<TGenotype, ObjectiveValue> selector) {
    this.selector = selector;
    return this;
  }

  public GeneticAlgorithmBuilder<TEncoding, TGenotype> WithReplacement(IReplacer<TGenotype> replacer) {
    this.replacement = replacer;
    return this;
  }

  public GeneticAlgorithmBuilder<TEncoding, TGenotype> WithPlusSelectionReplacement() {
    replacement = new PlusSelectionReplacer<TGenotype>();
    return this;
  }

  public GeneticAlgorithmBuilder<TEncoding, TGenotype> WithElitismReplacement(int elites) {
    replacement = new ElitismReplacer<TGenotype>(elites);
    return this;
  }

  public GeneticAlgorithmBuilder<TEncoding, TGenotype> TerminateOnMaxGenerations(int maxGenerations) {
    // ToDo: add or replace criterion?
    terminationCriterion = new ThresholdTerminator<PopulationState<TGenotype>>(maxGenerations, state => state.CurrentGeneration);
    return this;
  }

  public GeneticAlgorithmBuilder<TEncoding, TGenotype> TerminateOnPauseToken(PauseToken pauseToken) {
    // ToDo: add or replace criterion?
    terminationCriterion = new PauseTokenTerminator<PopulationState<TGenotype>>(pauseToken);
    return this;
  }

  public GeneticAlgorithmBuilder<TEncoding, TGenotype> OnLiveResult(Action<TGenotype> handler) {
    onLiveResult = handler;
    return this;
  }
  
 

  public GeneticAlgorithm<TGenotype> Build() {
    var result = Validate();
    if (!result.IsValid) {
      throw new ValidationException(result.Errors);
    }
    
    //var creator = creatorFactory(creatorParameters!);
    
    var ga = new GeneticAlgorithm<TGenotype>(
      populationSize!.Value,
      creator!,
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
  
  public class GeneticAlgorithmBuilderValidator : AbstractValidator<GeneticAlgorithmBuilder<TEncoding, TGenotype>> {
    public GeneticAlgorithmBuilderValidator() {
      RuleFor(x => x.populationSize).NotNull().WithMessage("Population size must not be null.");
      RuleFor(x => x.creator).NotNull().WithMessage("Creator must not be null.");
      RuleFor(x => x.crossover).NotNull().WithMessage("Crossover must not be null.");
      RuleFor(x => x.mutator).NotNull().WithMessage("Mutation must not be null.");
      RuleFor(x => x.mutationRate).InclusiveBetween(0, 1).WithMessage("Mutation rate must be between 0 and 1.");
      RuleFor(x => x.evaluator).NotNull().WithMessage("Evaluator must not be null.");
      RuleFor(x => x.selector).NotNull().WithMessage("Selector must not be null.");
      RuleFor(x => x.replacement).NotNull().WithMessage("Replacement must not be null.");
      RuleFor(x => x.terminationCriterion).NotNull().WithMessage("Termination criterion must not be null.");
      When(x => x.encoding is not null, () => {
        When(x => x.creator is IEncodingOperator<TGenotype, TEncoding>, () => {
          RuleFor(x => x.creator).Must((builder, creator) => builder.encoding!.IsValidOperator((IEncodingOperator<TGenotype, TEncoding>)creator!)).WithMessage("Creator must be compatible with encoding.");
        });
        When(x => x.crossover is IEncodingOperator<TGenotype, TEncoding>, () => {
          RuleFor(x => x.crossover).Must((builder, crossover) => builder.encoding!.IsValidOperator((IEncodingOperator<TGenotype, TEncoding>)crossover!)).WithMessage("Crossover must be compatible with encoding.");
        });
        When(x => x.mutator is IEncodingOperator<TGenotype, TEncoding>, () => {
          RuleFor(x => x.mutator).Must((builder, mutator) => builder.encoding!.IsValidOperator((IEncodingOperator<TGenotype, TEncoding>)mutator!)).WithMessage("Mutator must be compatible with encoding.");
        });
      });
    }
  }
}
