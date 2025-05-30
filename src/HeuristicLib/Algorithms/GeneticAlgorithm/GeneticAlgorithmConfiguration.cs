using FluentValidation;
using FluentValidation.Results;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Optimization;

namespace HEAL.HeuristicLib.Algorithms.GeneticAlgorithm;

public record GeneticAlgorithmConfiguration<TGenotype, TSearchSpace, TProblem>
  where TSearchSpace : ISearchSpace<TGenotype>
  where TProblem : IOptimizable<TGenotype, TSearchSpace>
{
  public int? PopulationSize { get; init; }
  public Creator<TGenotype, TSearchSpace, TProblem>? Creator { get; init; }
  public Crossover<TGenotype, TSearchSpace, TProblem>? Crossover { get; init; }
  public Mutator<TGenotype, TSearchSpace, TProblem>? Mutator { get; init; }
  public double? MutationRate { get; init; }
  public Selector<TGenotype, TSearchSpace, TProblem>? Selector { get; init; }
  public Replacer<TGenotype, TSearchSpace, TProblem>? Replacer { get; init; }
  public int? RandomSeed { get; init; }
  public Interceptor<TGenotype, TSearchSpace, TProblem, GeneticAlgorithmIterationResult<TGenotype>>? Interceptor { get; init; }
  public Terminator<TGenotype, TSearchSpace, TProblem, GeneticAlgorithmResult<TGenotype>>? Terminator { get; init; }
}

public record GeneticAlgorithmConfiguration<TGenotype, TSearchSpace> : GeneticAlgorithmConfiguration<TGenotype, TSearchSpace, IOptimizable<TGenotype, TSearchSpace>>
  where TSearchSpace : ISearchSpace<TGenotype>
{
}

public static class GeneticAlgorithmConfiguration {
  
  public static GeneticAlgorithmConfiguration<TGenotype, TSearchSpace, TProblem> Merge<TGenotype, TSearchSpace, TProblem>(
    GeneticAlgorithmConfiguration<TGenotype, TSearchSpace, TProblem> baseConfig, GeneticAlgorithmConfiguration<TGenotype, TSearchSpace, TProblem> overridingConfig) 
    where TSearchSpace : ISearchSpace<TGenotype> 
    where TProblem : IOptimizable<TGenotype, TSearchSpace>
  {
    return new GeneticAlgorithmConfiguration<TGenotype, TSearchSpace, TProblem> {
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
  
  public static GeneticAlgorithmConfiguration<TGenotype, TSearchSpace, TProblem> MergeWithOverridesFrom<TGenotype, TSearchSpace, TProblem>(
    this GeneticAlgorithmConfiguration<TGenotype, TSearchSpace, TProblem> baseConfig, GeneticAlgorithmConfiguration<TGenotype, TSearchSpace, TProblem> overridingConfig)
    where TSearchSpace : ISearchSpace<TGenotype> 
    where TProblem : IOptimizable<TGenotype, TSearchSpace>
  {
    return Merge(baseConfig, overridingConfig);
  }
  
  public static GeneticAlgorithmConfiguration<TGenotype, TSearchSpace, TProblem> MergeAsOverridesInto<TGenotype, TSearchSpace, TProblem>(
    this GeneticAlgorithmConfiguration<TGenotype, TSearchSpace, TProblem> overridingConfig, GeneticAlgorithmConfiguration<TGenotype, TSearchSpace, TProblem> baseConfig)
    where TSearchSpace : ISearchSpace<TGenotype>
    where TProblem : IOptimizable<TGenotype, TSearchSpace>
  {
    return Merge(baseConfig, overridingConfig);
  }

  public static GeneticAlgorithmConfiguration<TGenotype, TSearchSpace, TProblem> FromAlgorithm<TGenotype, TSearchSpace, TProblem>(GeneticAlgorithm<TGenotype, TSearchSpace, TProblem> algorithm) 
    where TSearchSpace : ISearchSpace<TGenotype> 
    where TProblem : IOptimizable<TGenotype, TSearchSpace>
  {
    return new GeneticAlgorithmConfiguration<TGenotype, TSearchSpace, TProblem> {
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

  public static GeneticAlgorithmConfiguration<TGenotype, TSearchSpace, TProblem> ToConfiguration<TGenotype, TSearchSpace, TProblem>(this GeneticAlgorithm<TGenotype, TSearchSpace, TProblem> algorithm)
    where TSearchSpace : ISearchSpace<TGenotype>
    where TProblem : IOptimizable<TGenotype, TSearchSpace>
  {
    return FromAlgorithm(algorithm);
  }
}

public static class GeneticAlgorithmBuildExtensions {
  public static GeneticAlgorithm<TGenotype, TSearchSpace, TProblem> Build<TGenotype, TSearchSpace, TProblem>(this GeneticAlgorithmConfiguration<TGenotype, TSearchSpace, TProblem> configuration)
    where TSearchSpace : ISearchSpace<TGenotype>
    where TProblem : IOptimizable<TGenotype, TSearchSpace>
  {
    configuration.ThrowIfInvalid();

    return Create(configuration);
  }
  
  public static bool TryBuild<TGenotype, TSearchSpace, TProblem>(this GeneticAlgorithmConfiguration<TGenotype, TSearchSpace, TProblem> configuration, out  GeneticAlgorithm<TGenotype, TSearchSpace, TProblem>? algorithm)
    where TSearchSpace : ISearchSpace<TGenotype>
    where TProblem : IOptimizable<TGenotype, TSearchSpace>
  {
    if (!configuration.IsValid()) {
      algorithm = null;
      return false;
    }
    
    algorithm = Create(configuration);
    return true;
  }

  private static GeneticAlgorithm<TGenotype, TSearchSpace, TProblem> Create<TGenotype, TSearchSpace, TProblem>(GeneticAlgorithmConfiguration<TGenotype, TSearchSpace, TProblem> configuration)
    where TSearchSpace : ISearchSpace<TGenotype> 
    where TProblem : IOptimizable<TGenotype, TSearchSpace>
  {
    return new GeneticAlgorithm<TGenotype, TSearchSpace, TProblem>(
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

public class GeneticAlgorithmConfigurationValidator<TGenotype, TSearchSpace, TProblem> : AbstractValidator<GeneticAlgorithmConfiguration<TGenotype, TSearchSpace, TProblem>>
  where TSearchSpace : ISearchSpace<TGenotype>
  where TProblem : IOptimizable<TGenotype, TSearchSpace>
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
  public static ValidationResult Validate<TGenotype, TSearchSpace, TProblem>(this GeneticAlgorithmConfiguration<TGenotype, TSearchSpace, TProblem> configuration) 
    where TSearchSpace : ISearchSpace<TGenotype> 
    where TProblem : IOptimizable<TGenotype, TSearchSpace>
  {
    var validator = new GeneticAlgorithmConfigurationValidator<TGenotype, TSearchSpace, TProblem>();
    return validator.Validate(configuration); 
  }
  
  public static bool IsValid<TGenotype, TSearchSpace, TProblem>(this GeneticAlgorithmConfiguration<TGenotype, TSearchSpace, TProblem> configuration) 
    where TSearchSpace : ISearchSpace<TGenotype> 
    where TProblem : IOptimizable<TGenotype, TSearchSpace>
  {
    var result = configuration.Validate();
    return result.IsValid;
  }

  public static void ThrowIfInvalid<TGenotype, TSearchSpace, TProblem>(this GeneticAlgorithmConfiguration<TGenotype, TSearchSpace, TProblem> configuration)
    where TSearchSpace : ISearchSpace<TGenotype>
    where TProblem : IOptimizable<TGenotype, TSearchSpace>
  {
    var result = configuration.Validate();
    if (!result.IsValid) {
      throw new ValidationException(result.Errors);
    }
  }
}

