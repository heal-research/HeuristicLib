using FluentValidation;
using FluentValidation.Results;
using HEAL.HeuristicLib.Encodings;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Algorithms.GeneticAlgorithm;

public record GeneticAlgorithmConfiguration<TGenotype, TEncoding>
  where TEncoding : IEncoding<TGenotype>
{
  public int? PopulationSize { get; init; }
  public Creator<TGenotype, TEncoding>? Creator { get; init; }
  public Crossover<TGenotype, TEncoding>? Crossover { get; init; }
  public Mutator<TGenotype, TEncoding>? Mutator { get; init; }
  public double? MutationRate { get; init; }
  public Selector? Selector { get; init; }
  public Replacer? Replacer { get; init; }
  public int? RandomSeed { get; init; }
  public Interceptor<GeneticAlgorithmIterationResult<TGenotype>>? Interceptor { get; init; }
  public Terminator<GeneticAlgorithmIterationResult<TGenotype>>? Terminator { get; init; }
}

public static class GeneticAlgorithmConfiguration {
  
  public static GeneticAlgorithmConfiguration<TGenotype, TEncoding> Merge<TGenotype, TEncoding>(
    GeneticAlgorithmConfiguration<TGenotype, TEncoding> baseConfig, GeneticAlgorithmConfiguration<TGenotype, TEncoding> overridingConfig) 
    where TEncoding : IEncoding<TGenotype> 
  {
    return new GeneticAlgorithmConfiguration<TGenotype, TEncoding> {
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
  
  public static GeneticAlgorithmConfiguration<TGenotype, TEncoding> MergeWithOverridesFrom<TGenotype, TEncoding>(
    this GeneticAlgorithmConfiguration<TGenotype, TEncoding> baseConfig, GeneticAlgorithmConfiguration<TGenotype, TEncoding> overridingConfig)
    where TEncoding : IEncoding<TGenotype> 
  {
    return Merge(baseConfig, overridingConfig);
  }
  
  public static GeneticAlgorithmConfiguration<TGenotype, TEncoding> MergeAsOverridesInto<TGenotype, TEncoding>(
    this GeneticAlgorithmConfiguration<TGenotype, TEncoding> overridingConfig, GeneticAlgorithmConfiguration<TGenotype, TEncoding> baseConfig)
    where TEncoding : IEncoding<TGenotype> 
  {
    return Merge(baseConfig, overridingConfig);
  }

  public static GeneticAlgorithmConfiguration<TGenotype, TEncoding> FromAlgorithm<TGenotype, TEncoding>(GeneticAlgorithm<TGenotype, TEncoding> algorithm) 
    where TEncoding : IEncoding<TGenotype> 
  {
    return new GeneticAlgorithmConfiguration<TGenotype, TEncoding> {
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

  public static GeneticAlgorithmConfiguration<TGenotype, TEncoding> ToConfiguration<TGenotype, TEncoding>(this GeneticAlgorithm<TGenotype, TEncoding> algorithm)
    where TEncoding : IEncoding<TGenotype>
  {
    return FromAlgorithm(algorithm);
  }
}

public static class GeneticAlgorithmBuildExtensions {
  public static GeneticAlgorithm<TGenotype, TEncoding> Build<TGenotype, TEncoding>(this GeneticAlgorithmConfiguration<TGenotype, TEncoding> configuration)
    where TEncoding : IEncoding<TGenotype>
  {
    configuration.ThrowIfInvalid();

    return Create(configuration);
  }
  
  public static bool TryBuild<TGenotype, TEncoding>(this GeneticAlgorithmConfiguration<TGenotype, TEncoding> configuration, out  GeneticAlgorithm<TGenotype, TEncoding>? algorithm)
    where TEncoding : IEncoding<TGenotype>
  {
    if (!configuration.IsValid()) {
      algorithm = null;
      return false;
    }
    
    algorithm = Create(configuration);
    return true;
  }

  private static GeneticAlgorithm<TGenotype, TEncoding> Create<TGenotype, TEncoding>(GeneticAlgorithmConfiguration<TGenotype, TEncoding> configuration)
    where TEncoding : IEncoding<TGenotype> 
  {
    return new GeneticAlgorithm<TGenotype, TEncoding>(
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

public class GeneticAlgorithmConfigurationValidator<TGenotype, TEncoding> : AbstractValidator<GeneticAlgorithmConfiguration<TGenotype, TEncoding>>
  where TEncoding : IEncoding<TGenotype>
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
  public static ValidationResult Validate<TGenotype, TEncoding>(this GeneticAlgorithmConfiguration<TGenotype, TEncoding> configuration) where TEncoding : IEncoding<TGenotype> {
    var validator = new GeneticAlgorithmConfigurationValidator<TGenotype, TEncoding>();
    return validator.Validate(configuration); 
  }
  
  public static bool IsValid<TGenotype, TEncoding>(this GeneticAlgorithmConfiguration<TGenotype, TEncoding> configuration) where TEncoding : IEncoding<TGenotype> {
    var result = configuration.Validate();
    return result.IsValid;
  }

  public static void ThrowIfInvalid<TGenotype, TEncoding>(this GeneticAlgorithmConfiguration<TGenotype, TEncoding> configuration) where TEncoding : IEncoding<TGenotype> {
    var result = configuration.Validate();
    if (!result.IsValid) {
      throw new ValidationException(result.Errors);
    }
  }
}

