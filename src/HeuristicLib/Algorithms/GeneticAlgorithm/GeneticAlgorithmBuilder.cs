using FluentValidation;
using HEAL.HeuristicLib.Encodings;
using HEAL.HeuristicLib.Operators;

namespace HEAL.HeuristicLib.Algorithms.GeneticAlgorithm;

public interface IUntypedGeneticAlgorithmBuilder {
  int? PopulationSize { get; set; }
  double? MutationRate { get; set; }
  ISelector<Fitness, Goal>? Selector { get; set; }
  IReplacer<Fitness, Goal>? Replacer { get; set; }
  Goal? Goal { get; set; }
  IRandomSource? RandomSource { get; set; }
}

public static class UntypedGeneticAlgorithmBuilderExtensions {
  public static TBuilder WithPopulationSize<TBuilder>(this TBuilder builder, int populationSize) where TBuilder : IUntypedGeneticAlgorithmBuilder {
    builder.PopulationSize = populationSize;
    return builder;
  }
  public static TBuilder WithMutationRate<TBuilder>(this TBuilder builder, double mutationRate) where TBuilder : IUntypedGeneticAlgorithmBuilder {
    builder.MutationRate = mutationRate;
    return builder;
  }
  public static TBuilder WithSelector<TBuilder>(this TBuilder builder, ISelector<Fitness, Goal> selector) where TBuilder : IUntypedGeneticAlgorithmBuilder {
    builder.Selector = selector;
    return builder;
  }
  public static TBuilder WithReplacer<TBuilder>(this TBuilder builder, IReplacer<Fitness, Goal> replacer) where TBuilder : IUntypedGeneticAlgorithmBuilder {
    builder.Replacer = replacer;
    return builder;
  }
  public static TBuilder WithGoal<TBuilder>(this TBuilder builder, Goal goal) where TBuilder : IUntypedGeneticAlgorithmBuilder {
    builder.Goal = goal;
    return builder;
  }
  public static TBuilder WithRandomSource<TBuilder>(this TBuilder builder, IRandomSource randomSource) where TBuilder : IUntypedGeneticAlgorithmBuilder {
    builder.RandomSource = randomSource;
    return builder;
  }

  public static IGeneticAlgorithmBuilder<TGenotype> UsingGenotype<TGenotype>(this IUntypedGeneticAlgorithmBuilder builder) {
    return new GeneticAlgorithmBuilder<TGenotype>(builder);
  }
}

public interface IGeneticAlgorithmBuilder<TGenotype> : IUntypedGeneticAlgorithmBuilder, IBuilder<GeneticAlgorithm<TGenotype>> {
  ICreator<TGenotype>? Creator { get; set; }
  ICrossover<TGenotype>? Crossover { get; set; }
  IMutator<TGenotype>? Mutator { get; set; }
  IEvaluator<TGenotype, Fitness>? Evaluator { get; set; }
  ITerminator<PopulationState<TGenotype, Fitness, Goal>>? Terminator { get; set; }
  IInterceptor<PopulationState<TGenotype, Fitness, Goal>>? Interceptor { get; set; }
}

public static class GeneticAlgorithmBuilderExtensions {
  public static TBuilder WithCreator<TBuilder, TGenotype>(this TBuilder builder, ICreator<TGenotype> creator) where TBuilder : IGeneticAlgorithmBuilder<TGenotype> {
    builder.Creator = creator;
    return builder;
  }
  public static TBuilder WithCrossover<TBuilder, TGenotype>(this TBuilder builder, ICrossover<TGenotype> crossover) where TBuilder : IGeneticAlgorithmBuilder<TGenotype> {
    builder.Crossover = crossover;
    return builder;
  }
  public static TBuilder WithMutator<TBuilder, TGenotype>(this TBuilder builder, IMutator<TGenotype> mutator) where TBuilder : IGeneticAlgorithmBuilder<TGenotype> {
    builder.Mutator = mutator;
    return builder;
  }
  public static TBuilder WithEvaluator<TBuilder, TGenotype>(this TBuilder builder, IEvaluator<TGenotype, Fitness> evaluator) where TBuilder : IGeneticAlgorithmBuilder<TGenotype> {
    builder.Evaluator = evaluator;
    return builder;
  }
  public static TBuilder WithTerminator<TBuilder, TGenotype>(this TBuilder builder, ITerminator<PopulationState<TGenotype, Fitness, Goal>> terminator) where TBuilder : IGeneticAlgorithmBuilder<TGenotype> {
    builder.Terminator = terminator;
    return builder;
  }
  public static TBuilder WithInterceptor<TBuilder, TGenotype>(this TBuilder builder, IInterceptor<PopulationState<TGenotype, Fitness, Goal>> interceptor) where TBuilder : IGeneticAlgorithmBuilder<TGenotype> {
    builder.Interceptor = interceptor;
    return builder;
  }
  
  public static IEncodingParameterizedGeneticAlgorithmBuilder<TGenotype, TEncodingParameter> UsingEncodingParameters<TGenotype, TEncodingParameter>(
    this IGeneticAlgorithmBuilder<TGenotype> builder, TEncodingParameter encodingParameter)
    where TEncodingParameter : IEncodingParameter<TGenotype> {
    return new EncodingParameterizedGeneticAlgorithmBuilder<TGenotype, TEncodingParameter>(builder).WithEncodingParameter<EncodingParameterizedGeneticAlgorithmBuilder<TGenotype, TEncodingParameter>, TGenotype, TEncodingParameter>(encodingParameter);
  }
}

public interface IEncodingParameterizedGeneticAlgorithmBuilder<TGenotype, TEncodingParameter> : IGeneticAlgorithmBuilder<TGenotype> where TEncodingParameter : IEncodingParameter<TGenotype> {
  TEncodingParameter? EncodingParameter { get; set; }
  Func<TEncodingParameter, ICreator<TGenotype>>? CreatorFactory { get; set; }
  Func<TEncodingParameter, ICrossover<TGenotype>>? CrossoverFactory { get; set; }
  Func<TEncodingParameter, IMutator<TGenotype>>? MutatorFactory { get; set; }
}

public static class EncodingParameterizedGeneticAlgorithmBuilderExtensions {
  public static TBuilder WithEncodingParameter<TBuilder, TGenotype, TEncodingParameter>(this TBuilder builder, TEncodingParameter encodingParameter) 
    where TBuilder : IEncodingParameterizedGeneticAlgorithmBuilder<TGenotype, TEncodingParameter> where TEncodingParameter : IEncodingParameter<TGenotype> {
    builder.EncodingParameter = encodingParameter;
    return builder;
  }
  public static TBuilder WithCreator<TBuilder, TGenotype, TEncodingParameter>(this TBuilder builder, Func<TEncodingParameter, ICreator<TGenotype>> creatorFactory) 
    where TBuilder : IEncodingParameterizedGeneticAlgorithmBuilder<TGenotype, TEncodingParameter> where TEncodingParameter : IEncodingParameter<TGenotype> {
    builder.CreatorFactory = creatorFactory;
    return builder;
  }
  public static TSelf WithCrossover<TSelf, TGenotype, TEncodingParameter>(this TSelf builder, Func<TEncodingParameter, ICrossover<TGenotype>> crossoverFactory) 
    where TSelf : IEncodingParameterizedGeneticAlgorithmBuilder<TGenotype, TEncodingParameter> where TEncodingParameter : IEncodingParameter<TGenotype> {
    builder.CrossoverFactory = crossoverFactory;
    return builder;
  }
  public static TSelf WithMutator<TSelf, TGenotype, TEncodingParameter>(this TSelf builder, Func<TEncodingParameter, IMutator<TGenotype>> mutatorFactory) 
    where TSelf : IEncodingParameterizedGeneticAlgorithmBuilder<TGenotype, TEncodingParameter> where TEncodingParameter : IEncodingParameter<TGenotype> {
    builder.MutatorFactory = mutatorFactory;
    return builder;
  }
}


public class UntypedGeneticAlgorithmBuilder : IUntypedGeneticAlgorithmBuilder {
  // Algorithm Params
  public int? PopulationSize { get; set; }
  public double? MutationRate { get; set; }
  public ISelector<Fitness, Goal>? Selector { get; set; }
  public IReplacer<Fitness, Goal>? Replacer { get; set; }
  // Problem Params
  public Goal? Goal { get; set; }
  // Execution Params
  public IRandomSource? RandomSource { get; set; }
}

public class GeneticAlgorithmBuilder<TGenotype> : IGeneticAlgorithmBuilder<TGenotype> {
  private readonly IUntypedGeneticAlgorithmBuilder baseBuilder;
  public GeneticAlgorithmBuilder(IUntypedGeneticAlgorithmBuilder baseBuilder) {
    this.baseBuilder = baseBuilder;
  }
  public GeneticAlgorithmBuilder() : this(new UntypedGeneticAlgorithmBuilder()) {}

  // Genetic Algorithm Params
  public int? PopulationSize { get => baseBuilder.PopulationSize; set => baseBuilder.PopulationSize = value; }
  public ICreator<TGenotype>? Creator { get; set; }
  public ICrossover<TGenotype>? Crossover { get; set; }
  public IMutator<TGenotype>? Mutator { get; set; }
  public double? MutationRate { get => baseBuilder.MutationRate; set => baseBuilder.MutationRate = value; }
  public ISelector<Fitness, Goal>? Selector { get => baseBuilder.Selector; set => baseBuilder.Selector = value; }
  public IReplacer<Fitness, Goal>? Replacer { get => baseBuilder.Replacer; set => baseBuilder.Replacer = value; }
  // Problem Params
  public IEvaluator<TGenotype, Fitness>? Evaluator { get; set; }
  public Goal? Goal { get => baseBuilder.Goal; set => baseBuilder.Goal = value; }
  // Execution Params
  public ITerminator<PopulationState<TGenotype, Fitness, Goal>>? Terminator { get; set; }
  public IInterceptor<PopulationState<TGenotype, Fitness, Goal>>? Interceptor { get; set; }
  public IRandomSource? RandomSource { get => baseBuilder.RandomSource; set => baseBuilder.RandomSource = value; }
  
  public GeneticAlgorithm<TGenotype> Build() {
    var parametersAreSetValidator = new RequiredParametersAreSetValidator();
    var parametersAreSetValidationResult = parametersAreSetValidator.Validate(this);

    if (!parametersAreSetValidationResult.IsValid) throw new ValidationException(parametersAreSetValidationResult.Errors);
    
    return new GeneticAlgorithm<TGenotype>(
      PopulationSize!.Value,
      Creator!,
      Crossover!,
      Mutator!,
      MutationRate!.Value,
      Evaluator!,
      Goal!.Value,
      Selector!,
      Replacer!,
      RandomSource!,
      Terminator,
      Interceptor
    );
  }

  private sealed class RequiredParametersAreSetValidator : AbstractValidator<GeneticAlgorithmBuilder<TGenotype>> {
    public RequiredParametersAreSetValidator() {
      RuleFor(x => x.PopulationSize).NotNull().WithMessage("Population size must not be null.");
      RuleFor(x => x.Creator).NotNull().WithMessage("Creator must not be null.");
      RuleFor(x => x.Crossover).NotNull().WithMessage("Crossover must not be null.");
      RuleFor(x => x.Mutator).NotNull().WithMessage("Mutator must not be null.");
      RuleFor(x => x.MutationRate).NotNull().WithMessage("Mutation rate must not be null.");
      RuleFor(x => x.Evaluator).NotNull().WithMessage("Evaluator must not be null.");
      RuleFor(x => x.Goal).NotNull().WithMessage("Goal must not be null.");
      RuleFor(x => x.Selector).NotNull().WithMessage("Selector must not be null.");
      RuleFor(x => x.Replacer).NotNull().WithMessage("Replacer must not be null.");
      RuleFor(x => x.RandomSource).NotNull().WithMessage("Random source must not be null.");
    }
  }
}

public class EncodingParameterizedGeneticAlgorithmBuilder<TGenotype, TEncodingParameter> : IEncodingParameterizedGeneticAlgorithmBuilder<TGenotype, TEncodingParameter>
  where TEncodingParameter : IEncodingParameter<TGenotype> 
{
  private readonly IGeneticAlgorithmBuilder<TGenotype> baseBuilder;
  public EncodingParameterizedGeneticAlgorithmBuilder(IGeneticAlgorithmBuilder<TGenotype> baseBuilder) {
    this.baseBuilder = baseBuilder;
  }
  
  public TEncodingParameter? EncodingParameter { get; set; }
  public Func<TEncodingParameter, ICreator<TGenotype>>? CreatorFactory { get; set; }
  public Func<TEncodingParameter, ICrossover<TGenotype>>? CrossoverFactory { get; set; }
  public Func<TEncodingParameter, IMutator<TGenotype>>? MutatorFactory { get; set; }

  public GeneticAlgorithm<TGenotype> Build() {
    if (EncodingParameter is not null) {
      if (CreatorFactory is not null) Creator = CreatorFactory(EncodingParameter);
      if (CrossoverFactory is not null) Crossover = CrossoverFactory(EncodingParameter);
      if (MutatorFactory is not null) Mutator = MutatorFactory(EncodingParameter);
    }
    return baseBuilder.Build();
  }
  
  // Algorithm Params
  public int? PopulationSize { get => baseBuilder.PopulationSize; set => baseBuilder.PopulationSize = value; }
  public ICreator<TGenotype>? Creator { get => baseBuilder.Creator; set => baseBuilder.Creator = value; }
  public ICrossover<TGenotype>? Crossover { get => baseBuilder.Crossover; set => baseBuilder.Crossover = value; }
  public IMutator<TGenotype>? Mutator { get => baseBuilder.Mutator; set => baseBuilder.Mutator = value; }
  public double? MutationRate { get => baseBuilder.MutationRate; set => baseBuilder.MutationRate = value; }
  public ISelector<Fitness, Goal>? Selector { get => baseBuilder.Selector; set => baseBuilder.Selector = value; }
  public IReplacer<Fitness, Goal>? Replacer { get => baseBuilder.Replacer; set => baseBuilder.Replacer = value; }
  // Problem Params
  public IEvaluator<TGenotype, Fitness>? Evaluator { get => baseBuilder.Evaluator; set => baseBuilder.Evaluator = value; }
  public Goal? Goal { get => baseBuilder.Goal; set => baseBuilder.Goal = value; }
  // Execution Params
  public IRandomSource? RandomSource { get => baseBuilder.RandomSource; set => baseBuilder.RandomSource = value; }
  public ITerminator<PopulationState<TGenotype, Fitness, Goal>>? Terminator { get => baseBuilder.Terminator; set => baseBuilder.Terminator = value; }
  public IInterceptor<PopulationState<TGenotype, Fitness, Goal>>? Interceptor { get => baseBuilder.Interceptor; set => baseBuilder.Interceptor = value; }
}

