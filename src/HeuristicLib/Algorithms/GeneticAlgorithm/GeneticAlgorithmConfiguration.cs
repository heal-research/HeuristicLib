using FluentValidation;
using FluentValidation.Results;
using HEAL.HeuristicLib.Operators;

namespace HEAL.HeuristicLib.Algorithms.GeneticAlgorithm;

public record GeneticAlgorithmConfiguration<TGenotype, TSearchSpace>
  where TSearchSpace : ISearchSpace<TGenotype>
{
  public int? PopulationSize { get; init; }
  public Creator<TGenotype, TSearchSpace>? Creator { get; init; }
  public Crossover<TGenotype, TSearchSpace>? Crossover { get; init; }
  public Mutator<TGenotype, TSearchSpace>? Mutator { get; init; }
  public double? MutationRate { get; init; }
  public Selector? Selector { get; init; }
  public Replacer? Replacer { get; init; }
  public int? RandomSeed { get; init; }
  public Interceptor<GeneticAlgorithmIterationResult<TGenotype>>? Interceptor { get; init; }
  public Terminator<GeneticAlgorithmResult<TGenotype>>? Terminator { get; init; }
}

public static class GeneticAlgorithmConfiguration {
  
  public static GeneticAlgorithmConfiguration<TGenotype, TSearchSpace> Merge<TGenotype, TSearchSpace>(
    GeneticAlgorithmConfiguration<TGenotype, TSearchSpace> baseConfig, GeneticAlgorithmConfiguration<TGenotype, TSearchSpace> overridingConfig) 
    where TSearchSpace : ISearchSpace<TGenotype> 
  {
    return new GeneticAlgorithmConfiguration<TGenotype, TSearchSpace> {
      PopulationSize = overridingConfig.PopulationSize ?? baseConfig.PopulationSize,
      Creator = overridingConfig.Creator ?? baseConfig.Creator,
      Crossover = overridingConfig.Crossover ?? baseConfig.Crossover,
      Mutator = overridingConfig.Mutator ?? baseConfig.Mutator,
      MutationRate = overridingConfig.MutationRate ?? baseConfig.MutationRate,
      Selector = overridingConfig.Selector ?? baseConfig.Selector,
      Replacer = overridingConfig.Replacer ?? baseConfig.Replacer,
      RandomSeed = overridingConfig.RandomSeed ?? baseConfig.RandomSeed,
      Interceptor = overridingConfig.Interceptor ?? baseConfig.Interceptor,
      Terminator = overridingConfig.Terminator ?? baseConfig.Terminator
    };
  }
  
  public static GeneticAlgorithmConfiguration<TGenotype, TSearchSpace> MergeWithOverridesFrom<TGenotype, TSearchSpace>(
    this GeneticAlgorithmConfiguration<TGenotype, TSearchSpace> baseConfig, GeneticAlgorithmConfiguration<TGenotype, TSearchSpace> overridingConfig)
    where TSearchSpace : ISearchSpace<TGenotype> 
  {
    return Merge(baseConfig, overridingConfig);
  }
  
  public static GeneticAlgorithmConfiguration<TGenotype, TSearchSpace> MergeAsOverridesInto<TGenotype, TSearchSpace>(
    this GeneticAlgorithmConfiguration<TGenotype, TSearchSpace> overridingConfig, GeneticAlgorithmConfiguration<TGenotype, TSearchSpace> baseConfig)
    where TSearchSpace : ISearchSpace<TGenotype> 
  {
    return Merge(baseConfig, overridingConfig);
  }

  public static GeneticAlgorithmConfiguration<TGenotype, TSearchSpace> FromAlgorithm<TGenotype, TSearchSpace>(GeneticAlgorithm<TGenotype, TSearchSpace> algorithm) 
    where TSearchSpace : ISearchSpace<TGenotype> 
  {
    return new GeneticAlgorithmConfiguration<TGenotype, TSearchSpace> {
      PopulationSize = algorithm.PopulationSize,
      Creator = algorithm.Creator,
      Crossover = algorithm.Crossover,
      Mutator = algorithm.Mutator,
      MutationRate = algorithm.MutationRate,
      Selector = algorithm.Selector,
      Replacer = algorithm.Replacer,
      RandomSeed = algorithm.RandomSeed
    };
  }

  public static GeneticAlgorithmConfiguration<TGenotype, TSearchSpace> ToConfiguration<TGenotype, TSearchSpace>(this GeneticAlgorithm<TGenotype, TSearchSpace> algorithm)
    where TSearchSpace : ISearchSpace<TGenotype>
  {
    return FromAlgorithm(algorithm);
  }
}

public static class GeneticAlgorithmBuildExtensions {
  public static GeneticAlgorithm<TGenotype, TSearchSpace> Build<TGenotype, TSearchSpace>(this GeneticAlgorithmConfiguration<TGenotype, TSearchSpace> configuration)
    where TSearchSpace : ISearchSpace<TGenotype>
  {
    configuration.ThrowIfInvalid();

    return Create(configuration);
  }
  
  public static bool TryBuild<TGenotype, TSearchSpace>(this GeneticAlgorithmConfiguration<TGenotype, TSearchSpace> configuration, out  GeneticAlgorithm<TGenotype, TSearchSpace>? algorithm)
    where TSearchSpace : ISearchSpace<TGenotype>
  {
    if (!configuration.IsValid()) {
      algorithm = null;
      return false;
    }
    
    algorithm = Create(configuration);
    return true;
  }

  private static GeneticAlgorithm<TGenotype, TSearchSpace> Create<TGenotype, TSearchSpace>(GeneticAlgorithmConfiguration<TGenotype, TSearchSpace> configuration)
    where TSearchSpace : ISearchSpace<TGenotype> 
  {
    return new GeneticAlgorithm<TGenotype, TSearchSpace>(
      configuration.PopulationSize!.Value,
      configuration.Creator!,
      configuration.Crossover!,
      configuration.Mutator!,
      configuration.MutationRate!.Value,
      configuration.Selector!,
      configuration.Replacer!,
      configuration.RandomSeed!.Value,
      configuration.Terminator!,
      configuration.Interceptor!
    );
  }
}

public class GeneticAlgorithmConfigurationValidator<TGenotype, TSearchSpace> : AbstractValidator<GeneticAlgorithmConfiguration<TGenotype, TSearchSpace>>
  where TSearchSpace : ISearchSpace<TGenotype>
{
  public GeneticAlgorithmConfigurationValidator() {
    RuleFor(x => x.PopulationSize).NotNull().WithMessage("Population size must not be null.");
    RuleFor(x => x.Creator).NotNull().WithMessage("Creator must not be null.");
    RuleFor(x => x.Crossover).NotNull().WithMessage("Crossover must not be null.");
    RuleFor(x => x.Mutator).NotNull().WithMessage("Mutator must not be null.");
    RuleFor(x => x.MutationRate).NotNull().WithMessage("Mutation rate must not be null.");
    RuleFor(x => x.Selector).NotNull().WithMessage("Selector must not be null.");
    RuleFor(x => x.Replacer).NotNull().WithMessage("Replacer must not be null.");
    RuleFor(x => x.RandomSeed).NotNull().WithMessage("Random source must not be null.");
    //RuleFor(x => x.Interceptor).NotNull().WithMessage("Interceptor must not be null.");
    //RuleFor(x => x.Terminator).NotNull().WithMessage("Terminator must not be null.");
  }
}

public static class GeneticAlgorithmConfigurationValidationExtensions {
  public static ValidationResult Validate<TGenotype, TSearchSpace>(this GeneticAlgorithmConfiguration<TGenotype, TSearchSpace> configuration) where TSearchSpace : ISearchSpace<TGenotype> {
    var validator = new GeneticAlgorithmConfigurationValidator<TGenotype, TSearchSpace>();
    return validator.Validate(configuration); 
  }
  
  public static bool IsValid<TGenotype, TSearchSpace>(this GeneticAlgorithmConfiguration<TGenotype, TSearchSpace> configuration) where TSearchSpace : ISearchSpace<TGenotype> {
    var result = configuration.Validate();
    return result.IsValid;
  }

  public static void ThrowIfInvalid<TGenotype, TSearchSpace>(this GeneticAlgorithmConfiguration<TGenotype, TSearchSpace> configuration) where TSearchSpace : ISearchSpace<TGenotype> {
    var result = configuration.Validate();
    if (!result.IsValid) {
      throw new ValidationException(result.Errors);
    }
  }
}

