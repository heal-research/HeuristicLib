using System.Text;
using FluentValidation;
using FluentValidation.Results;
using HEAL.HeuristicLib.Configuration;
using HEAL.HeuristicLib.Encodings;
using HEAL.HeuristicLib.Operators;

namespace HEAL.HeuristicLib.Algorithms.GeneticAlgorithm;
//
// public record GeneticAlgorithmParams<TGenotype, TEncoding> where TEncoding : IEncoding<TGenotype, TEncoding> {
//   public TEncoding? Encoding { get; init; }
//
//   public required int PopulationSize { get; init; }
//   
//   public required ICreator<TGenotype> Creator { get; init; }
//   
//   public required ICrossover<TGenotype> Crossover { get; init; }
//   public required IMutator<TGenotype> Mutator { get; init; }
//   public required double MutationRate { get; init; }
//   
//   public required ISelector<TGenotype, Fitness, Goal> Selector { get; init; }
//   public required IReplacer<TGenotype, Fitness, Goal> Replacer { get; init; }
// }
//
// public record OptimizationParams<TGenotype, TFitness, TGoal> {
//   public required IEvaluator<TGenotype, TFitness> Evaluator { get; init; }
//   public required TGoal Goal { get; init; }
// }
//
// public record ExecutionParams<TState> where TState : IState {
//   public required IRandomSource RandomSource { get; init; }
//
//   public ITerminator<TState>? Terminator { get; init; }
//
//   public IInterceptor<TState>? Interceptor { get; init; }
// }
//
// public record GeneticAlgorithmConfigBag<TGenotype, TEncoding> where TEncoding : IEncoding<TGenotype, TEncoding> {
//
//   //public required DirectProperty<TEncoding?> Encoding { get; init; }
//   public required GeneticAlgorithmParams<TGenotype, TEncoding> AlgorithmParams { get; init; }
//   public required OptimizationParams<TGenotype, Fitness, Goal> OptimizationParams { get; init; }
//
//   public required ExecutionParams<PopulationState<TGenotype, Fitness, Goal>> ExecutionParams { get; init; }
//   // public TEncoding? Encoding { get; init; }
//   //
//   // public int? PopulationSize { get; init; }
//   //
//   // public Func<TEncoding, IRandomSource, ICreator<TGenotype>>? CreatorFactory { get; init; }
//   // public Func<TEncoding, IRandomSource, ICrossover<TGenotype>>? CrossoverFactory { get; init; }
//   // public Func<TEncoding, IRandomSource, IMutator<TGenotype>>? MutatorFactory { get; init; }
//   // public double? MutationRate { get; init; }
//   //
//   // public Func<TEncoding, IEvaluator<TGenotype, Fitness>>? EvaluatorFactory { get; init; }
//   // public Goal? Goal { get; init; }
//   // public Func<IRandomSource, ISelector<TGenotype, Fitness, Goal>>? SelectorFactory { get; init; }
//   //
//   // public Func<IRandomSource, IReplacer<TGenotype, Fitness, Goal>>? ReplacementFactory { get; init; }
//   //
//   // public IRandomSource? RandomSource { get; init; }
//   //
//   // public ITerminator<PopulationState<TGenotype, Fitness, Goal>>? Terminator { get; init; }
//   //
//   // public IInterceptor<PopulationState<TGenotype, Fitness, Goal>>? Interceptor { get; init; }
// }


public class GeneticAlgorithmBuilder<TGenotype, TEncoding>
  : IBuilder<GeneticAlgorithm<TGenotype, TEncoding>>
    //, IConsumer<GeneticAlgorithmSpec>,
    //IConsumer<TEncoding>
  where TEncoding : IEncoding<TGenotype, TEncoding> 
{

  // Genetic Algorithm Params
  public TEncoding? Encoding { get; private set; }
  public int? PopulationSize { get; private set; }
  public Func<TEncoding, IRandomSource, ICreator<TGenotype, TEncoding>>? CreatorFactory { get; private set; }
  public Func<TEncoding, IRandomSource, ICrossover<TGenotype, TEncoding>>? CrossoverFactory { get; private set; }
  public Func<TEncoding, IRandomSource, IMutator<TGenotype, TEncoding>>? MutatorFactory { get; private set; }
  public double? MutationRate { get; private set; }
  public Func<IRandomSource, ISelector<TGenotype, Fitness, Goal>>? SelectorFactory { get; private set; }
  public Func<IRandomSource, IReplacer<TGenotype, Fitness, Goal>>? ReplacementFactory { get; private set; }


  // Optimization Algorithm Params
  public IEvaluator<TGenotype, Fitness>? Evaluator { get; private set; }
  public Goal? Goal { get; private set; }

  // Execution Params
  public IRandomSource? RandomSource { get; private set; }
  public ITerminator<PopulationState<TGenotype, Fitness, Goal>>? Terminator { get; private set; }
  public IInterceptor<PopulationState<TGenotype, Fitness, Goal>>? Interceptor { get; private set; }


  public GeneticAlgorithm<TGenotype, TEncoding> Build() {
    var parametersAreSetValidator = new AllParametersAreSetValidator();
    var parametersAreSetValidationResult = parametersAreSetValidator.Validate(this);
    if (!parametersAreSetValidationResult.IsValid) throw new ValidationException(parametersAreSetValidationResult.Errors);
    
    // ToDo: resolved params validation
    
    return new GeneticAlgorithm<TGenotype, TEncoding>(
      Encoding!,
      PopulationSize!.Value,
      CreatorFactory!.Invoke(Encoding!, RandomSource!),
      CrossoverFactory!.Invoke(Encoding!, RandomSource!),
      MutatorFactory!.Invoke(Encoding!, RandomSource!),
      MutationRate!.Value,
      Evaluator!,
      Goal!.Value,
      SelectorFactory!.Invoke(RandomSource!),
      ReplacementFactory!.Invoke(RandomSource!),
      RandomSource!, 
      Terminator,
      Interceptor
    );
    
  }
  
  public GeneticAlgorithmBuilder<TGenotype, TEncoding> WithEncoding(TEncoding? encoding) { Encoding = encoding; return this; }
  public GeneticAlgorithmBuilder<TGenotype, TEncoding> WithPopulationSize(int? populationSize) { PopulationSize = populationSize; return this; }
  public GeneticAlgorithmBuilder<TGenotype, TEncoding> WithCreator(Func<TEncoding, IRandomSource, ICreator<TGenotype, TEncoding>>? creatorFactory) { CreatorFactory = creatorFactory; return this; }
  public GeneticAlgorithmBuilder<TGenotype, TEncoding> WithCreator(ICreator<TGenotype, TEncoding>? creator) { CreatorFactory = creator != null ? (_, _) => creator : null; return this; }
  public GeneticAlgorithmBuilder<TGenotype, TEncoding> WithCrossover(Func<TEncoding, IRandomSource, ICrossover<TGenotype, TEncoding>>? crossoverFactory) { CrossoverFactory = crossoverFactory; return this; }
  public GeneticAlgorithmBuilder<TGenotype, TEncoding> WithCrossover(ICrossover<TGenotype, TEncoding>? crossover) { CrossoverFactory = crossover != null ? (_, _) => crossover : null; return this; }
  public GeneticAlgorithmBuilder<TGenotype, TEncoding> WithMutator(Func<TEncoding, IRandomSource, IMutator<TGenotype, TEncoding>>? mutatorFactory) { MutatorFactory = mutatorFactory; return this; }
  public GeneticAlgorithmBuilder<TGenotype, TEncoding> WithMutator(IMutator<TGenotype, TEncoding>? mutator) { MutatorFactory = mutator != null ? (_, _) => mutator : null; return this; }
  public GeneticAlgorithmBuilder<TGenotype, TEncoding> WithMutationRate(double? mutationRate) { MutationRate = mutationRate; return this; }
  public GeneticAlgorithmBuilder<TGenotype, TEncoding> WithSelector(Func<IRandomSource, ISelector<TGenotype, Fitness, Goal>>? selectorFactory) { SelectorFactory = selectorFactory; return this; }
  public GeneticAlgorithmBuilder<TGenotype, TEncoding> WithSelector(ISelector<TGenotype, Fitness, Goal>? selector) { SelectorFactory = selector != null ? _ => selector : null; return this; }
  public GeneticAlgorithmBuilder<TGenotype, TEncoding> WithReplacer(Func<IRandomSource, IReplacer<TGenotype, Fitness, Goal>>? replacementFactory) { ReplacementFactory = replacementFactory; return this; }
  public GeneticAlgorithmBuilder<TGenotype, TEncoding> WithReplacer(IReplacer<TGenotype, Fitness, Goal>? replacer) { ReplacementFactory = replacer != null ? _ => replacer : null; return this; }
  public GeneticAlgorithmBuilder<TGenotype, TEncoding> WithEvaluator(IEvaluator<TGenotype, Fitness>? evaluator) { Evaluator = evaluator; return this; }
  public GeneticAlgorithmBuilder<TGenotype, TEncoding> WithGoal(Goal? goal) { Goal = goal; return this; }
  public GeneticAlgorithmBuilder<TGenotype, TEncoding> WithRandomSource(IRandomSource? randomSource) { RandomSource = randomSource; return this; }
  public GeneticAlgorithmBuilder<TGenotype, TEncoding> WithTerminator(ITerminator<PopulationState<TGenotype, Fitness, Goal>>? terminator) { Terminator = terminator; return this; }
  public GeneticAlgorithmBuilder<TGenotype, TEncoding> WithInterceptor(IInterceptor<PopulationState<TGenotype, Fitness, Goal>>? interceptor) { Interceptor = interceptor; return this; }
  
  private sealed class AllParametersAreSetValidator : AbstractValidator<GeneticAlgorithmBuilder<TGenotype, TEncoding>> {
    public AllParametersAreSetValidator() {
      RuleFor(x => x.Encoding).NotNull().WithMessage("Encoding must not be null.");
      RuleFor(x => x.PopulationSize).NotNull().WithMessage("Population size must not be null.");
      RuleFor(x => x.CreatorFactory).NotNull().WithMessage("Creator factory must not be null.");
      RuleFor(x => x.CrossoverFactory).NotNull().WithMessage("Crossover factory must not be null.");
      RuleFor(x => x.MutatorFactory).NotNull().WithMessage("Mutator factory must not be null.");
      RuleFor(x => x.MutationRate).NotNull().WithMessage("Mutation rate must not be null.");
      RuleFor(x => x.Evaluator).NotNull().WithMessage("Evaluator must not be null.");
      RuleFor(x => x.Goal).NotNull().WithMessage("Goal must not be null.");
      RuleFor(x => x.SelectorFactory).NotNull().WithMessage("Selector factory must not be null.");
      RuleFor(x => x.ReplacementFactory).NotNull().WithMessage("Replacement factory must not be null.");
      RuleFor(x => x.RandomSource).NotNull().WithMessage("Random source must not be null.");
      //RuleFor(x => x.Terminator).NotNull().WithMessage("Termination criterion must not be null.");
    }
  }
  
  
  public GeneticAlgorithmBuilder<TGenotype, TEncoding> WithGeneticAlgorithmSpec(GeneticAlgorithmSpec<TGenotype, TEncoding> gaSpec) {
    if (gaSpec.PopulationSize.HasValue) PopulationSize = gaSpec.PopulationSize.Value;
    if (gaSpec.MutationRate.HasValue) MutationRate = gaSpec.MutationRate.Value;
    if (gaSpec.Creator is not null) CreatorFactory = (encoding, randomSource) => gaSpec.Creator.Create(encoding, randomSource);
    if (gaSpec.Crossover is not null) CrossoverFactory = (encoding, randomSource) => gaSpec.Crossover.Create(encoding, randomSource);
    if (gaSpec.Mutator is not null) MutatorFactory = (encoding, randomSource) => gaSpec.Mutator.Create(encoding, randomSource);
    if (gaSpec.Selector is not null) SelectorFactory = (randomSource) => gaSpec.Selector.Create(randomSource);
    if (gaSpec.Replacer is not null) ReplacementFactory = (randomSource) => gaSpec.Replacer.Create(randomSource);
    if (gaSpec.MaximumGenerations.HasValue) Terminator = Operators.Terminator.OnGeneration(gaSpec.MaximumGenerations.Value);
    if (gaSpec.RandomSeed.HasValue) RandomSource = new RandomSource(gaSpec.RandomSeed.Value);
    return this;
  }
  
//
//   public GeneticAlgorithmBuilder<TGenotype, TEncoding> WithGeneticAlgorithmSpec(GeneticAlgorithmSpec gaSpec) {
//     #pragma warning disable S1481
//     
//     if (gaSpec.PopulationSize.HasValue) {
//       PopulationSize = gaSpec.PopulationSize.Value;
//     }
//     
//     if (gaSpec.MutationRate.HasValue) {
//       MutationRate = gaSpec.MutationRate.Value;
//     }
//
//     if (gaSpec.Creator is not null) {
//       CreatorFactory = (encoding, randomSource) => {
//         IOperator @operator = (gaSpec.Creator, encoding) switch {
//           (RandomPermutationCreatorSpec spec, PermutationEncoding enc) => new RandomPermutationCreator(enc, randomSource),
//           (UniformRealVectorCreatorSpec spec, RealVectorEncoding enc) => new UniformDistributedCreator(enc, spec.Minimum != null ? new RealVector(spec.Minimum) : null, spec.Maximum != null ? new RealVector(spec.Maximum) : null, randomSource),
//           (NormalRealVectorCreatorSpec spec, RealVectorEncoding enc) => new NormalDistributedCreator(enc, spec.Mean != null ? new RealVector(spec.Mean) : 0.0, spec.StandardDeviation != null ? new RealVector(spec.StandardDeviation) : 1.0, randomSource),
//           _ => throw new NotImplementedException("Unknown creator configuration.")
//         };
//         if (@operator is not ICreator<TGenotype> creator) throw new InvalidOperationException("Creator must be a ICreator<TGenotype>.");
//         return creator;
//       };
//     }
//
//     if (gaSpec.Crossover is not null) {
//       CrossoverFactory = (encoding, randomSource) => {
//         IOperator @operator = (gaSpec.Crossover, encoding) switch {
//           (OrderCrossoverSpec spec, PermutationEncoding enc) => new OrderCrossover(enc, randomSource),
//           (SinglePointRealVectorCrossoverSpec spec, RealVectorEncoding enc) => new SinglePointCrossover(enc, randomSource),
//           (AlphaBlendRealVectorCrossoverSpec spec, RealVectorEncoding enc) => new AlphaBetaBlendCrossover(enc, spec.Alpha, spec.Beta),
//           _ => throw new NotImplementedException("Unknown crossover configuration.")
//         };
//         if (@operator is not ICrossover<TGenotype> crossover) throw new InvalidOperationException("Crossover must be a ICrossover<TGenotype>.");
//         return crossover;
//       };
//     }
//
//     if (gaSpec.Mutator is not null) {
//       MutatorFactory = (encoding, randomSource) => {
//         IOperator @operator = (gaSpec.Mutator, encoding) switch {
//           (SwapMutatorSpec spec, PermutationEncoding enc) => new SwapMutator(enc, randomSource),
//           (GaussianRealVectorMutatorSpec spec, RealVectorEncoding enc) => new GaussianMutator(enc, spec.Strength ?? 1.0, spec.Rate ?? 1.0, randomSource),
//           (InversionMutatorSpec spec, PermutationEncoding enc) => new InversionMutator(enc, randomSource),
//           _ => throw new NotImplementedException("Unknown mutator configuration.")
//         };
//         if (@operator is not IMutator<TGenotype> mutator) throw new InvalidOperationException("Mutator must be a IMutator<TGenotype>.");
//         return mutator;
//       };
//     }
//
//     if (gaSpec.Selector is not null) {
//       SelectorFactory = (randomSource) => {
//         IOperator @operator = (gaSpec.Selector) switch {
//           RandomSelectorSpec spec => new RandomSelector<TGenotype, Fitness, Goal>(randomSource),
//           TournamentSelectorSpec spec => new TournamentSelector<TGenotype>(spec.TournamentSize ?? 2, randomSource),
//           ProportionalSelectorSpec spec => new ProportionalSelector<TGenotype>(randomSource, spec.Windowing),
//           _ => throw new NotImplementedException("Unknown selector configuration.")
//         };
//         if (@operator is not ISelector<TGenotype, Fitness, Goal> selector) throw new InvalidOperationException("Selector must be a ISelector<TGenotype, Fitness, Goal>.");
//         return selector;
//       };
//     }
//
//     if (gaSpec.Replacer is not null) {
//       ReplacementFactory = (randomSource) => {
//         IOperator @operator = (gaSpec.Replacer) switch {
//           ElitistReplacerSpec spec => new ElitismReplacer<TGenotype>(spec.Elites ?? 1),
//           PlusSelectionReplacerSpec spec => new PlusSelectionReplacer<TGenotype>(),
//           _ => throw new NotImplementedException("Unknown replacer configuration.")
//         };
//         if (@operator is not IReplacer<TGenotype, Fitness, Goal> replacer) throw new InvalidOperationException("Replacer must be a IReplacer<TGenotype, Fitness, Goal>.");
//         return replacer;
//       };
//     }
//
//     if (gaSpec.MaximumGenerations.HasValue) {
//       Terminator = Operators.Terminator.OnGeneration(gaSpec.MaximumGenerations.Value);
//     }
//
//     if (gaSpec.RandomSeed.HasValue) {
//       RandomSource = new RandomSource(gaSpec.RandomSeed.Value);
//     }
//     
// #pragma warning restore S1481
//
//     return this;
//   }
  // public void Consume(TEncoding encoding) {
  //   this.WithEncoding(encoding);
  // }
}

//
// public class OldGeneticAlgorithmBuilder<TEncoding, TGenotype>
//   : AccumulatorBuilder<
//       GeneticAlgorithm<TGenotype>,
//       GeneticAlgorithmConfigBag<TGenotype, TEncoding>
//     >
//   where TEncoding : IEncoding<TGenotype, TEncoding> {
//
//   protected override ResolvedConfig ResolveConfig(GeneticAlgorithmConfigBag<TGenotype, TEncoding> configBag) {
//     return new ResolvedConfig {
//       Encoding = configBag.Encoding,
//       PopulationSize = configBag.PopulationSize!.Value,
//       Creator = configBag.CreatorFactory!.Invoke(configBag.Encoding!, configBag.RandomSource!),
//       Crossover = configBag.CrossoverFactory!.Invoke(configBag.Encoding!, configBag.RandomSource!),
//       Mutator = configBag.MutatorFactory!.Invoke(configBag.Encoding!, configBag.RandomSource!),
//       MutationRate = configBag.MutationRate!.Value,
//       Evaluator = configBag.EvaluatorFactory!.Invoke(configBag.Encoding!),
//       Goal = configBag.Goal!.Value,
//       Selector = configBag.SelectorFactory!.Invoke(configBag.RandomSource!),
//       Replacer = configBag.ReplacementFactory!.Invoke(configBag.RandomSource!),
//       RandomSource = configBag.RandomSource!,
//       Terminator = configBag.Terminator,
//       Interceptor = configBag.Interceptor
//     };
//   }
//   
//   protected override GeneticAlgorithm<TGenotype> Build(ResolvedConfig config) {
//     return new GeneticAlgorithm<TGenotype>(
//       config.PopulationSize,
//       config.Creator,
//       config.Crossover,
//       config.Mutator,
//       config.MutationRate,
//       config.Evaluator,
//       config.Goal,
//       config.Selector,
//       config.Replacer,
//       config.RandomSource, 
//       config.Terminator,
//       config.Interceptor
//     );
//   }
//
//   protected override ValidationResult ValidateConfigBag(GeneticAlgorithmConfigBag<TGenotype, TEncoding> configBag) {
//     var validator = new ConfigBagValidator();
//     return validator.Validate(configBag);
//   }
//
//   protected override ValidationResult ValidateResolvedConfig(GeneticAlgorithmBuilder<TEncoding, TGenotype>.ResolvedConfig config) {
//     var validator = new ResolvedConfigValidator();
//     return validator.Validate(config);
//   }
//   
// //
// //
// // public class GeneticAlgorithmBuilder<TEncoding, TGenotype> 
// //   : IBuilder<GeneticAlgorithm<TGenotype>>, IConsumer<TEncoding>
// //   where TEncoding : IEncoding<TGenotype, TEncoding> {
// //   private readonly GeneticAlgorithmConfig<TGenotype, TEncoding> baseConfig = new();
// //   private readonly List<IConfigSource<TGenotype, TEncoding>> sources = [];
//   
//   // public GeneticAlgorithmBuilder<TEncoding, TGenotype> AddSource(IConfigSource<TGenotype, TEncoding> source) {
//   //   sources.Add(source);
//   //   return this;
//   // }
//   //
//   // public GeneticAlgorithm<TGenotype> Build() {
//   //   var config = ResolveConfig();
//   //
//   //   return new GeneticAlgorithm<TGenotype>(
//   //     config.PopulationSize,
//   //     config.Creator,
//   //     config.Crossover,
//   //     config.Mutator,
//   //     config.MutationRate,
//   //     config.Evaluator,
//   //     config.Goal,
//   //     config.Selector,
//   //     config.Replacer,
//   //     config.RandomSource, 
//   //     config.Terminator,
//   //     config.Interceptor);
//   // }
//
//   // private ResolvedConfig ResolveConfig() {
//   //   var config = CollectConfig(baseConfig, sources);
//   //   
//   //   ValidateConfig(config);
//   //   
//   //   var resolvedConfig = new ResolvedConfig {
//   //     Encoding = config.Encoding,
//   //     PopulationSize = config.PopulationSize!.Value,
//   //     Creator = config.CreatorFactory!.Invoke(config.Encoding!, config.RandomSource!),
//   //     Crossover = config.CrossoverFactory!.Invoke(config.Encoding!, config.RandomSource!),
//   //     Mutator = config.MutatorFactory!.Invoke(config.Encoding!, config.RandomSource!),
//   //     MutationRate = config.MutationRate!.Value,
//   //     Evaluator = config.EvaluatorFactory!.Invoke(config.Encoding!),
//   //     Goal = config.Goal!.Value,
//   //     Selector = config.SelectorFactory!.Invoke(config.RandomSource!),
//   //     Replacer = config.ReplacementFactory!.Invoke(config.RandomSource!),
//   //     RandomSource = config.RandomSource!,
//   //     Terminator = config.Terminator,
//   //     Interceptor = config.Interceptor
//   //   };
//   //
//   //   ValidateResolvedConfig(resolvedConfig);
//   //
//   //   return resolvedConfig;
//   // }
//   
//   // private static GeneticAlgorithmConfig<TGenotype, TEncoding> CollectConfig(GeneticAlgorithmConfig<TGenotype, TEncoding> baseConfig, IEnumerable<IConfigSource<TGenotype, TEncoding>> sources) {
//   //   var config = baseConfig;
//   //   foreach (var source in sources) {
//   //     config = source.Apply(config);
//   //   }
//   //   return config;
//   // }
//   
//   // private static void ValidateConfig(GeneticAlgorithmConfig<TGenotype, TEncoding> config) {
//   //   var validator = new ConfigValidator();
//   //   var validationResult = validator.Validate(config);
//   //   if (!validationResult.IsValid) {
//   //     throw new ValidationException(validationResult.Errors);
//   //   }
//   // }
//   
//   // private static void ValidateResolvedConfig(ResolvedConfig resolvedConfig) {
//   //   var validator = new ResolvedConfigValidator();
//   //   var validationResult = validator.Validate(resolvedConfig);
//   //   if (!validationResult.IsValid) {
//   //     throw new ValidationException(validationResult.Errors);
//   //   }
//   // }
//   
//   protected sealed class ResolvedConfig {
//     public TEncoding? Encoding { get; init; }
//     
//     public required int PopulationSize { get; init; }
//   
//     public required ICreator<TGenotype> Creator { get; init; }
//     public required ICrossover<TGenotype> Crossover { get; init; }
//     public required IMutator<TGenotype> Mutator { get; init; }
//     public required double MutationRate { get; init; }
//  
//     public required IEvaluator<TGenotype, Fitness> Evaluator { get; init; }
//     public required Goal Goal { get; init; }
//     public required ISelector<TGenotype, Fitness, Goal> Selector { get; init; }
//  
//     public required IReplacer<TGenotype, Fitness, Goal> Replacer { get; init; }
//   
//     public required IRandomSource RandomSource { get; init; }
//  
//     public ITerminator<PopulationState<TGenotype, Fitness, Goal>>? Terminator { get; init; }
//     
//     public IInterceptor<PopulationState<TGenotype, Fitness, Goal>>? Interceptor { get; init; }
//   }
//   
//   private sealed class ConfigBagValidator : AbstractValidator<GeneticAlgorithmConfigBag<TGenotype, TEncoding>> {
//     public ConfigBagValidator() {
//       RuleFor(x => x.PopulationSize).NotNull().WithMessage("Population size must not be null.");
//       RuleFor(x => x.CreatorFactory).NotNull().WithMessage("Creator factory must not be null.");
//       RuleFor(x => x.CrossoverFactory).NotNull().WithMessage("Crossover factory must not be null.");
//       RuleFor(x => x.MutatorFactory).NotNull().WithMessage("Mutator factory must not be null.");
//       RuleFor(x => x.MutationRate).NotNull().WithMessage("Mutation rate must not be null.");
//       RuleFor(x => x.EvaluatorFactory).NotNull().WithMessage("Evaluator factory must not be null.");
//       RuleFor(x => x.Goal).NotNull().WithMessage("Goal must not be null.");
//       RuleFor(x => x.SelectorFactory).NotNull().WithMessage("Selector factory must not be null.");
//       RuleFor(x => x.ReplacementFactory).NotNull().WithMessage("Replacement factory must not be null.");
//       RuleFor(x => x.RandomSource).NotNull().WithMessage("Random source must not be null.");
//       //RuleFor(x => x.Terminator).NotNull().WithMessage("Termination criterion must not be null.");
//     }
//   }
//   
//   private sealed class ResolvedConfigValidator : AbstractValidator<ResolvedConfig> {
//     public ResolvedConfigValidator() {
//       RuleFor(x => x.PopulationSize).GreaterThan(0).WithMessage("Population size must be greater than 0.");
//       RuleFor(x => x.MutationRate).InclusiveBetween(0, 1).WithMessage("Mutation rate must be between 0 and 1.");
//       When(x => x.Encoding is not null, () => {
//         When(x => x.Creator is IEncodingOperator<TGenotype, TEncoding>, () => {
//           RuleFor(x => x.Creator).Must((builder, creator) => builder.Encoding!.IsValidOperator((IEncodingOperator<TGenotype, TEncoding>)creator!)).WithMessage("Creator must be compatible with encoding.");
//         });
//         When(x => x.Crossover is IEncodingOperator<TGenotype, TEncoding>, () => {
//           RuleFor(x => x.Crossover).Must((builder, crossover) => builder.Encoding!.IsValidOperator((IEncodingOperator<TGenotype, TEncoding>)crossover!)).WithMessage("Crossover must be compatible with encoding.");
//         });
//         When(x => x.Mutator is IEncodingOperator<TGenotype, TEncoding>, () => {
//           RuleFor(x => x.Mutator).Must((builder, mutator) => builder.Encoding!.IsValidOperator((IEncodingOperator<TGenotype, TEncoding>)mutator!)).WithMessage("Mutator must be compatible with encoding.");
//         });
//       });
//     }
//   }
//
//   public void Consume(TEncoding item) {
//     AddSource(new ChainedConfigSource<TGenotype, TEncoding>(new GeneticAlgorithmConfigBag<TGenotype, TEncoding>() { Encoding = item }));
//   }
// }

//
// public interface IConfigSource<TGenotype, TEncoding> where TEncoding : IEncoding<TGenotype, TEncoding> {
//   GeneticAlgorithmConfig<TGenotype, TEncoding> Apply(GeneticAlgorithmConfig<TGenotype, TEncoding> config);
// }
//
// public static class GeneticAlgorithmBuilderConfigExtensions {
//   public static GeneticAlgorithmBuilder<TEncoding, TGenotype> WithConfig<TEncoding, TGenotype>
//     (this GeneticAlgorithmBuilder<TEncoding, TGenotype> builder, GeneticAlgorithmConfig<TGenotype, TEncoding> config)
//     where TEncoding : IEncoding<TGenotype, TEncoding> {
//     return builder.AddSource(new ChainedConfigSource<TGenotype, TEncoding>(config));
//   }
//   
//   // public static GeneticAlgorithmBuilder<TEncoding, TGenotype> WithEncoding<TEncoding, TGenotype>
//   //   (this GeneticAlgorithmBuilder<TEncoding, TGenotype> builder, TEncoding encoding)
//   //   where TEncoding : IEncoding<TGenotype, TEncoding> {
//   //   return builder.AddSource(new ChainedConfigSource<TGenotype, TEncoding>(new GeneticAlgorithmConfig<TGenotype, TEncoding>() {
//   //     Encoding = encoding
//   //   }));
//   // }
//   
//   public static GeneticAlgorithmBuilder<TEncoding, TGenotype> WithRandomSource<TEncoding, TGenotype>
//     (this GeneticAlgorithmBuilder<TEncoding, TGenotype> builder, IRandomSource randomSource)
//     where TEncoding : IEncoding<TGenotype, TEncoding> {
//     return builder.AddSource(new ChainedConfigSource<TGenotype, TEncoding>(new GeneticAlgorithmConfig<TGenotype, TEncoding>() {
//       RandomSource = randomSource
//     }));
//   }
//   
//   public static GeneticAlgorithmBuilder<TEncoding, TGenotype> WithEvaluator<TEncoding, TGenotype>
//     (this GeneticAlgorithmBuilder<TEncoding, TGenotype> builder, IEvaluator<TGenotype, Fitness> evaluator)
//     where TEncoding : IEncoding<TGenotype, TEncoding> {
//     return builder.AddSource(new ChainedConfigSource<TGenotype, TEncoding>(new GeneticAlgorithmConfig<TGenotype, TEncoding>() {
//       EvaluatorFactory = _ => evaluator
//     }));
//   }
//
//   public static GeneticAlgorithmBuilder<TEncoding, TGenotype> WithGoal<TEncoding, TGenotype>
//     (this GeneticAlgorithmBuilder<TEncoding, TGenotype> builder, Goal goal)
//     where TEncoding : IEncoding<TGenotype, TEncoding> {
//     return builder.AddSource(new ChainedConfigSource<TGenotype, TEncoding>(new GeneticAlgorithmConfig<TGenotype, TEncoding>() {
//       Goal = goal
//     }));
//   }
//   public static GeneticAlgorithmBuilder<TEncoding, TGenotype> Minimizing<TEncoding, TGenotype>(this GeneticAlgorithmBuilder<TEncoding, TGenotype> builder) where TEncoding : IEncoding<TGenotype, TEncoding> => 
//     builder.WithGoal(Goal.Minimize);
//   public static GeneticAlgorithmBuilder<TEncoding, TGenotype> Maximizing<TEncoding, TGenotype>(this GeneticAlgorithmBuilder<TEncoding, TGenotype> builder) where TEncoding : IEncoding<TGenotype, TEncoding> => 
//     builder.WithGoal(Goal.Maximize);
// }

// public class ChainedConfigSource<TGenotype, TEncoding> 
//   : IConfigBagSource<GeneticAlgorithmConfig<TGenotype, TEncoding>> 
//   where TEncoding : IEncoding<TGenotype, TEncoding> {
//   public ChainedConfigSource(GeneticAlgorithmConfig<TGenotype, TEncoding> chainedConfig) {
//     ChainedConfig = chainedConfig;
//   }
//   public GeneticAlgorithmConfig<TGenotype, TEncoding> ChainedConfig { get; }
//
//   public GeneticAlgorithmConfig<TGenotype, TEncoding> Apply(GeneticAlgorithmConfig<TGenotype, TEncoding> config) {
//     return config with {
//       Encoding = ChainedConfig.Encoding ?? config.Encoding,
//       PopulationSize = ChainedConfig.PopulationSize ?? config.PopulationSize,
//       CreatorFactory = ChainedConfig.CreatorFactory ?? config.CreatorFactory,
//       CrossoverFactory = ChainedConfig.CrossoverFactory ?? config.CrossoverFactory,
//       MutatorFactory = ChainedConfig.MutatorFactory ?? config.MutatorFactory,
//       MutationRate = ChainedConfig.MutationRate ?? config.MutationRate,
//       EvaluatorFactory = ChainedConfig.EvaluatorFactory ?? config.EvaluatorFactory,
//       Goal = ChainedConfig.Goal ?? config.Goal,
//       SelectorFactory = ChainedConfig.SelectorFactory ?? config.SelectorFactory,
//       ReplacementFactory = ChainedConfig.ReplacementFactory ?? config.ReplacementFactory,
//       RandomSource = ChainedConfig.RandomSource ?? config.RandomSource,
//       Terminator = ChainedConfig.Terminator ?? config.Terminator
//     };
//   }
// }
