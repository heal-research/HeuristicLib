using FluentValidation;
using HEAL.HeuristicLib.Encodings;
using HEAL.HeuristicLib.Operators;

namespace HEAL.HeuristicLib.Algorithms.GeneticAlgorithm;

public record GeneticAlgorithmConfig<TGenotype, TEncoding> where TEncoding : IEncoding<TGenotype, TEncoding> {
  public TEncoding? Encoding { get; init; }

  public int? PopulationSize { get; init; }

  public Func<TEncoding, IRandomSource, ICreator<TGenotype>>? CreatorFactory { get; init; }
  public Func<TEncoding, IRandomSource, ICrossover<TGenotype>>? CrossoverFactory { get; init; }
  public Func<TEncoding, IRandomSource, IMutator<TGenotype>>? MutatorFactory { get; init; }
  public double? MutationRate { get; init; }

  public Func<TEncoding, IEvaluator<TGenotype, ObjectiveValue>>? EvaluatorFactory { get; init; }
  public Func<IRandomSource, ISelector<TGenotype, ObjectiveValue>>? SelectorFactory { get; init; }
  
  public Func<IRandomSource, IReplacer<TGenotype>>? ReplacementFactory { get; init; }
  
  public IRandomSource? RandomSource { get; init; }
  
  public ITerminator<PopulationState<TGenotype>>? Terminator { get; init; }
  
  public IInterceptor<PopulationState<TGenotype>>? Interceptor { get; init; }
}

public class GeneticAlgorithmBuilder<TEncoding, TGenotype> where TEncoding : IEncoding<TGenotype, TEncoding> {
  private readonly GeneticAlgorithmConfig<TGenotype, TEncoding> baseConfig = new();
  private readonly List<IConfigSource<TGenotype, TEncoding>> sources = [];
  
  public GeneticAlgorithmBuilder<TEncoding, TGenotype> AddSource(IConfigSource<TGenotype, TEncoding> source) {
    sources.Add(source);
    return this;
  }
  
  public GeneticAlgorithm<TGenotype> Build() {
    var config = ResolveConfig();

    return new GeneticAlgorithm<TGenotype>(
      config.PopulationSize,
      config.Creator,
      config.Crossover,
      config.Mutator,
      config.MutationRate,
      config.Evaluator,
      config.Selector,
      config.Replacer,
      config.RandomSource, 
      config.Terminator,
      config.Interceptor);
  }

  private ResolvedConfig ResolveConfig() {
    var config = CollectConfig(baseConfig, sources);
    
    ValidateConfig(config);
    
    var resolvedConfig = new ResolvedConfig {
      Encoding = config.Encoding,
      PopulationSize = config.PopulationSize!.Value,
      Creator = config.CreatorFactory!.Invoke(config.Encoding!, config.RandomSource!),
      Crossover = config.CrossoverFactory!.Invoke(config.Encoding!, config.RandomSource!),
      Mutator = config.MutatorFactory!.Invoke(config.Encoding!, config.RandomSource!),
      MutationRate = config.MutationRate!.Value,
      Evaluator = config.EvaluatorFactory!.Invoke(config.Encoding!),
      Selector = config.SelectorFactory!.Invoke(config.RandomSource!),
      Replacer = config.ReplacementFactory!.Invoke(config.RandomSource!),
      RandomSource = config.RandomSource!,
      Terminator = config.Terminator,
      Interceptor = config.Interceptor
    };

    ValidateResolvedConfig(resolvedConfig);

    return resolvedConfig;
  }
  
  private static GeneticAlgorithmConfig<TGenotype, TEncoding> CollectConfig(GeneticAlgorithmConfig<TGenotype, TEncoding> baseConfig, IEnumerable<IConfigSource<TGenotype, TEncoding>> sources) {
    var config = baseConfig;
    foreach (var source in sources) {
      config = source.Apply(config);
    }
    return config;
  }
  
  private static void ValidateConfig(GeneticAlgorithmConfig<TGenotype, TEncoding> config) {
    var validator = new ConfigValidator();
    var validationResult = validator.Validate(config);
    if (!validationResult.IsValid) {
      throw new ValidationException(validationResult.Errors);
    }
  }
  
  private static void ValidateResolvedConfig(ResolvedConfig resolvedConfig) {
    var validator = new ResolvedConfigValidator();
    var validationResult = validator.Validate(resolvedConfig);
    if (!validationResult.IsValid) {
      throw new ValidationException(validationResult.Errors);
    }
  }
  
  private sealed class ResolvedConfig {
    public TEncoding? Encoding { get; init; }
    
    public required int PopulationSize { get; init; }
  
    public required ICreator<TGenotype> Creator { get; init; }
    public required ICrossover<TGenotype> Crossover { get; init; }
    public required IMutator<TGenotype> Mutator { get; init; }
    public required double MutationRate { get; init; }
 
    public required IEvaluator<TGenotype, ObjectiveValue> Evaluator { get; init; }
    public required ISelector<TGenotype, ObjectiveValue> Selector { get; init; }
 
    public required IReplacer<TGenotype> Replacer { get; init; }
  
    public required IRandomSource RandomSource { get; init; }
 
    public ITerminator<PopulationState<TGenotype>>? Terminator { get; init; }
    
    public IInterceptor<PopulationState<TGenotype>>? Interceptor { get; init; }
  }
  
  private sealed class ConfigValidator : AbstractValidator<GeneticAlgorithmConfig<TGenotype, TEncoding>> {
    public ConfigValidator() {
      RuleFor(x => x.PopulationSize).NotNull().WithMessage("Population size must not be null.");
      RuleFor(x => x.CreatorFactory).NotNull().WithMessage("Creator factory must not be null.");
      RuleFor(x => x.CrossoverFactory).NotNull().WithMessage("Crossover factory must not be null.");
      RuleFor(x => x.MutatorFactory).NotNull().WithMessage("Mutator factory must not be null.");
      RuleFor(x => x.MutationRate).NotNull().WithMessage("Mutation rate must not be null.");
      RuleFor(x => x.EvaluatorFactory).NotNull().WithMessage("Evaluator factory must not be null.");
      RuleFor(x => x.SelectorFactory).NotNull().WithMessage("Selector factory must not be null.");
      RuleFor(x => x.ReplacementFactory).NotNull().WithMessage("Replacement factory must not be null.");
      RuleFor(x => x.RandomSource).NotNull().WithMessage("Random source must not be null.");
      //RuleFor(x => x.Terminator).NotNull().WithMessage("Termination criterion must not be null.");
    }
  }
  
  private sealed class ResolvedConfigValidator : AbstractValidator<ResolvedConfig> {
    public ResolvedConfigValidator() {
      RuleFor(x => x.PopulationSize).GreaterThan(0).WithMessage("Population size must be greater than 0.");
      RuleFor(x => x.MutationRate).InclusiveBetween(0, 1).WithMessage("Mutation rate must be between 0 and 1.");
      When(x => x.Encoding is not null, () => {
        When(x => x.Creator is IEncodingOperator<TGenotype, TEncoding>, () => {
          RuleFor(x => x.Creator).Must((builder, creator) => builder.Encoding!.IsValidOperator((IEncodingOperator<TGenotype, TEncoding>)creator!)).WithMessage("Creator must be compatible with encoding.");
        });
        When(x => x.Crossover is IEncodingOperator<TGenotype, TEncoding>, () => {
          RuleFor(x => x.Crossover).Must((builder, crossover) => builder.Encoding!.IsValidOperator((IEncodingOperator<TGenotype, TEncoding>)crossover!)).WithMessage("Crossover must be compatible with encoding.");
        });
        When(x => x.Mutator is IEncodingOperator<TGenotype, TEncoding>, () => {
          RuleFor(x => x.Mutator).Must((builder, mutator) => builder.Encoding!.IsValidOperator((IEncodingOperator<TGenotype, TEncoding>)mutator!)).WithMessage("Mutator must be compatible with encoding.");
        });
      });
    }
  }
}

public interface IConfigSource<TGenotype, TEncoding> where TEncoding : IEncoding<TGenotype, TEncoding> {
  GeneticAlgorithmConfig<TGenotype, TEncoding> Apply(GeneticAlgorithmConfig<TGenotype, TEncoding> config);
}

public static class GeneticAlgorithmBuilderConfigExtensions {
  public static GeneticAlgorithmBuilder<TEncoding, TGenotype> WithConfig<TEncoding, TGenotype>
    (this GeneticAlgorithmBuilder<TEncoding, TGenotype> builder, GeneticAlgorithmConfig<TGenotype, TEncoding> config)
    where TEncoding : IEncoding<TGenotype, TEncoding> {
    return builder.AddSource(new ChainedConfigSource<TGenotype, TEncoding>(config));
  }
  
  public static GeneticAlgorithmBuilder<TEncoding, TGenotype> WithEncoding<TEncoding, TGenotype>
    (this GeneticAlgorithmBuilder<TEncoding, TGenotype> builder, TEncoding encoding)
    where TEncoding : IEncoding<TGenotype, TEncoding> {
    return builder.AddSource(new ChainedConfigSource<TGenotype, TEncoding>(new GeneticAlgorithmConfig<TGenotype, TEncoding>() {
      Encoding = encoding
    }));
  }
  
  public static GeneticAlgorithmBuilder<TEncoding, TGenotype> WithRandomSource<TEncoding, TGenotype>
    (this GeneticAlgorithmBuilder<TEncoding, TGenotype> builder, IRandomSource randomSource)
    where TEncoding : IEncoding<TGenotype, TEncoding> {
    return builder.AddSource(new ChainedConfigSource<TGenotype, TEncoding>(new GeneticAlgorithmConfig<TGenotype, TEncoding>() {
      RandomSource = randomSource
    }));
  }
  
  public static GeneticAlgorithmBuilder<TEncoding, TGenotype> WithEvaluator<TEncoding, TGenotype>
    (this GeneticAlgorithmBuilder<TEncoding, TGenotype> builder, IEvaluator<TGenotype, ObjectiveValue> evaluator)
    where TEncoding : IEncoding<TGenotype, TEncoding> {
    return builder.AddSource(new ChainedConfigSource<TGenotype, TEncoding>(new GeneticAlgorithmConfig<TGenotype, TEncoding>() {
      EvaluatorFactory = _ => evaluator
    }));
  }
}

public class ChainedConfigSource<TGenotype, TEncoding> : IConfigSource<TGenotype, TEncoding>  where TEncoding : IEncoding<TGenotype, TEncoding> {
  public ChainedConfigSource(GeneticAlgorithmConfig<TGenotype, TEncoding> chainedConfig) {
    ChainedConfig = chainedConfig;
  }
  public GeneticAlgorithmConfig<TGenotype, TEncoding> ChainedConfig { get; }

  public GeneticAlgorithmConfig<TGenotype, TEncoding> Apply(GeneticAlgorithmConfig<TGenotype, TEncoding> config) {
    return config with {
      Encoding = ChainedConfig.Encoding ?? config.Encoding,
      PopulationSize = ChainedConfig.PopulationSize ?? config.PopulationSize,
      CreatorFactory = ChainedConfig.CreatorFactory ?? config.CreatorFactory,
      CrossoverFactory = ChainedConfig.CrossoverFactory ?? config.CrossoverFactory,
      MutatorFactory = ChainedConfig.MutatorFactory ?? config.MutatorFactory,
      MutationRate = ChainedConfig.MutationRate ?? config.MutationRate,
      EvaluatorFactory = ChainedConfig.EvaluatorFactory ?? config.EvaluatorFactory,
      SelectorFactory = ChainedConfig.SelectorFactory ?? config.SelectorFactory,
      ReplacementFactory = ChainedConfig.ReplacementFactory ?? config.ReplacementFactory,
      RandomSource = ChainedConfig.RandomSource ?? config.RandomSource,
      Terminator = ChainedConfig.Terminator ?? config.Terminator
    };
  }
}
