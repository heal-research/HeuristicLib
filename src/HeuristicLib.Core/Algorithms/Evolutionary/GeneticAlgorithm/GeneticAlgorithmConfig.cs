// using FluentValidation;
// using FluentValidation.Results;
// using HEAL.HeuristicLib.Operators;
// using HEAL.HeuristicLib.Optimization;
// using HEAL.HeuristicLib.Problems;
//
// namespace HEAL.HeuristicLib.Algorithms.GeneticAlgorithm;

// public abstract record IterativeAlgorithmConfig<TGenotype, TSearchSpace, TProblem, TIterationResult>
// {
//   public TerminatorConfig<TGenotype, TIterationResult, TSearchSpace, TProblem>? Terminator { get; }
//   public InterceptorConfig<TGenotype, TIterationResult, TSearchSpace, TProblem>? Interceptor { get; }
// }
//
// public record GeneticAlgorithmConfig<TGenotype, TSearchSpace, TProblem> : IterativeAlgorithmConfig<TGenotype, TSearchSpace, TProblem, PopulationIterationResult<TGenotype>>
//   where TSearchSpace : class, IEncoding<TGenotype>
//   where TProblem : IProblem<TGenotype>
// {
//   public int? PopulationSize { get; init; }
//   public CreatorConfig<TGenotype, TSearchSpace, TProblem>? Creator { get; init; }
//   public CrossoverConfig<TGenotype, TSearchSpace, TProblem>? Crossover { get; init; }
//   public MutatorConfig<TGenotype, TSearchSpace, TProblem>? Mutator { get; init; }
//   public double? InitialMutationStrength { get; init; }
//   public SelectorConfig<TGenotype, TSearchSpace, TProblem>? Selector { get; init; }
//   public ReplacerConfig<TGenotype, TSearchSpace, TProblem>? Replacer { get; init; }
//   public int? RandomSeed { get; init; }
// }

// public record GeneticAlgorithmConfiguration<TGenotype> : GeneticAlgorithmConfiguration<TGenotype, IOptimizable<TGenotype>>
// {
// }

// public static class GeneticAlgorithmConfiguration {
//   
//   public static GeneticAlgorithmConfiguration<TGenotype, TProblem> Merge<TGenotype, TProblem>(
//     GeneticAlgorithmConfiguration<TGenotype, TProblem> baseConfig, GeneticAlgorithmConfiguration<TGenotype, TProblem> overridingConfig) 
//     where TProblem : IOptimizable<TGenotype>
//   {
//     return new GeneticAlgorithmConfiguration<TGenotype, TProblem> {
//       PopulationSize = overridingConfig.PopulationSize ?? baseConfig.PopulationSize,
//       Creator = overridingConfig.Creator ?? baseConfig.Creator,
//       Crossover = overridingConfig.Crossover ?? baseConfig.Crossover,
//       Mutator = overridingConfig.Mutator ?? baseConfig.Mutator,
//       InitialMutationStrength = overridingConfig.InitialMutationStrength ?? baseConfig.InitialMutationStrength,
//       Selector = overridingConfig.Selector ?? baseConfig.Selector,
//       Replacer = overridingConfig.Replacer ?? baseConfig.Replacer,
//       RandomSeed = overridingConfig.RandomSeed ?? baseConfig.RandomSeed,
//       Interceptor = overridingConfig.Interceptor ?? baseConfig.Interceptor,
//       Terminator = overridingConfig.Terminator ?? baseConfig.Terminator
//     };
//   }
//   
//   public static GeneticAlgorithmConfiguration<TGenotype, TProblem> MergeWithOverridesFrom<TGenotype, TProblem>(
//     this GeneticAlgorithmConfiguration<TGenotype, TProblem> baseConfig, GeneticAlgorithmConfiguration<TGenotype, TProblem> overridingConfig)
//     where TProblem : IOptimizable<TGenotype>
//   {
//     return Merge(baseConfig, overridingConfig);
//   }
//   
//   public static GeneticAlgorithmConfiguration<TGenotype, TProblem> MergeAsOverridesInto<TGenotype, TProblem>(
//     this GeneticAlgorithmConfiguration<TGenotype, TProblem> overridingConfig, GeneticAlgorithmConfiguration<TGenotype, TProblem> baseConfig)
//     where TProblem : IOptimizable<TGenotype>
//   {
//     return Merge(baseConfig, overridingConfig);
//   }
//
//   public static GeneticAlgorithmConfiguration<TGenotype, TProblem> FromAlgorithm<TGenotype, TProblem>(GeneticAlgorithm<TGenotype, TProblem> algorithm) 
//     where TProblem : IOptimizable<TGenotype>
//   {
//     return new GeneticAlgorithmConfiguration<TGenotype, TProblem> {
//       PopulationSize = algorithm.PopulationSize,
//       Creator = algorithm.Creator,
//       Crossover = algorithm.Crossover,
//       Mutator = algorithm.Mutator,
//       InitialMutationStrength = algorithm.InitialMutationStrength,
//       Selector = algorithm.Selector,
//       Replacer = algorithm.Replacer,
//       RandomSeed = algorithm.RandomSeed
//     };
//   }
//
//   public static GeneticAlgorithmConfiguration<TGenotype, TProblem> ToConfiguration<TGenotype, TProblem>(this GeneticAlgorithm<TGenotype, TProblem> algorithm)
//     where TProblem : IOptimizable<TGenotype>
//   {
//     return FromAlgorithm(algorithm);
//   }
// }
//
// public static class GeneticAlgorithmBuildExtensions {
//   public static GeneticAlgorithm<TGenotype, TProblem> Build<TGenotype, TProblem>(this GeneticAlgorithmConfiguration<TGenotype, TProblem> configuration)
//     where TProblem : IOptimizable<TGenotype>
//   {
//     configuration.ThrowIfInvalid();
//
//     return GetEvaluator(configuration);
//   }
//   
//   public static bool TryBuild<TGenotype, TProblem>(this GeneticAlgorithmConfiguration<TGenotype, TProblem> configuration, out  GeneticAlgorithm<TGenotype, TProblem>? algorithm)
//     where TProblem : IOptimizable<TGenotype>
//   {
//     if (!configuration.IsValid()) {
//       algorithm = null;
//       return false;
//     }
//     
//     algorithm = GetEvaluator(configuration);
//     return true;
//   }
//
//   private static GeneticAlgorithm<TGenotype, TProblem> GetEvaluator<TGenotype, TProblem>(GeneticAlgorithmConfiguration<TGenotype, TProblem> configuration)
//     where TProblem : IOptimizable<TGenotype>
//   {
//     return new GeneticAlgorithm<TGenotype, TProblem>(
//       configuration.PopulationSize!.Value,
//       configuration.Creator!,
//       configuration.Crossover!,
//       configuration.Mutator!,
//       configuration.InitialMutationStrength!.Value,
//       configuration.Selector!,
//       configuration.Replacer!,
//       configuration.RandomSeed!.Value,
//       configuration.Terminator!,
//       configuration.Interceptor!
//     );
//   }
// }
//
// public class GeneticAlgorithmConfigurationValidator<TGenotype, TProblem> : AbstractValidator<GeneticAlgorithmConfiguration<TGenotype, TProblem>>
//   where TProblem : IOptimizable<TGenotype>
// {
//   public GeneticAlgorithmConfigurationValidator() {
//     RuleFor(x => x.PopulationSize).NotNull().WithMessage("Population size must not be null.");
//     RuleFor(x => x.Creator).NotNull().WithMessage("Creator must not be null.");
//     RuleFor(x => x.Crossover).NotNull().WithMessage("Crossover must not be null.");
//     RuleFor(x => x.Mutator).NotNull().WithMessage("Mutator must not be null.");
//     RuleFor(x => x.InitialMutationStrength).NotNull().WithMessage("Mutation rate must not be null.");
//     RuleFor(x => x.Selector).NotNull().WithMessage("Selector must not be null.");
//     RuleFor(x => x.Replacer).NotNull().WithMessage("Replacer must not be null.");
//     RuleFor(x => x.RandomSeed).NotNull().WithMessage("Random source must not be null.");
//     //RuleFor(x => x.Interceptor).NotNull().WithMessage("Interceptor must not be null.");
//     //RuleFor(x => x.Terminator).NotNull().WithMessage("Terminator must not be null.");
//   }
// }
//
// public static class GeneticAlgorithmConfigurationValidationExtensions {
//   public static ValidationResult Validate<TGenotype, TProblem>(this GeneticAlgorithmConfiguration<TGenotype, TProblem> configuration) 
//     where TProblem : IOptimizable<TGenotype>
//   {
//     var validator = new GeneticAlgorithmConfigurationValidator<TGenotype, TProblem>();
//     return validator.Validate(configuration); 
//   }
//   
//   public static bool IsValid<TGenotype, TProblem>(this GeneticAlgorithmConfiguration<TGenotype, TProblem> configuration) 
//     where TProblem : IOptimizable<TGenotype>
//   {
//     var result = configuration.Validate();
//     return result.IsValid;
//   }
//
//   public static void ThrowIfInvalid<TGenotype, TProblem>(this GeneticAlgorithmConfiguration<TGenotype, TProblem> configuration)
//     where TProblem : IOptimizable<TGenotype>
//   {
//     var result = configuration.Validate();
//     if (!result.IsValid) {
//       throw new ValidationException(result.Errors);
//     }
//   }
// }



