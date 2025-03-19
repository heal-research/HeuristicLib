using FluentValidation;
using HEAL.HeuristicLib.Encodings;
using HEAL.HeuristicLib.Operators;

namespace HEAL.HeuristicLib.Algorithms.GeneticAlgorithm;

public interface IGeneticAlgorithmBuilder<TGenotype> : IBuilder<GeneticAlgorithm<TGenotype>> {
  GeneticAlgorithmBuilder<TGenotype>.BuilderParameter Parameter { get; }
  
  IGeneticAlgorithmBuilder<TGenotype> WithPopulationSize(int? populationSize);
  IGeneticAlgorithmBuilder<TGenotype> WithCreator(ICreator<TGenotype>? creator);
  IGeneticAlgorithmBuilder<TGenotype> WithCrossover(ICrossover<TGenotype>? crossover);
  IGeneticAlgorithmBuilder<TGenotype> WithMutator(IMutator<TGenotype>? mutator);
  IGeneticAlgorithmBuilder<TGenotype> WithMutationRate(double? mutationRate);
  IGeneticAlgorithmBuilder<TGenotype> WithSelector(ISelector<TGenotype, Fitness, Goal>? selector);
  IGeneticAlgorithmBuilder<TGenotype> WithReplacer(IReplacer<TGenotype, Fitness, Goal>? replacer);
  IGeneticAlgorithmBuilder<TGenotype> WithEvaluator(IEvaluator<TGenotype, Fitness>? evaluator);
  IGeneticAlgorithmBuilder<TGenotype> WithGoal(Goal? goal);
  IGeneticAlgorithmBuilder<TGenotype> WithRandomSource(IRandomSource? randomSource);
  IGeneticAlgorithmBuilder<TGenotype> WithTerminator(ITerminator<PopulationState<TGenotype, Fitness, Goal>>? terminator);
  IGeneticAlgorithmBuilder<TGenotype> WithInterceptor(IInterceptor<PopulationState<TGenotype, Fitness, Goal>>? interceptor);
}



public class GeneticAlgorithmBuilder<TGenotype> : IGeneticAlgorithmBuilder<TGenotype> {
  
  public class BuilderParameter {
    // Genetic Algorithm Params
    public int? PopulationSize { get; set; }
    public ICreator<TGenotype>? Creator { get; set; }
    public ICrossover<TGenotype>? Crossover { get; set; }
    public IMutator<TGenotype>? Mutator { get; set; }
    public double? MutationRate { get; set; }
    public ISelector<TGenotype, Fitness, Goal>? Selector { get; set; }
    public IReplacer<TGenotype, Fitness, Goal>? Replacer { get; set; }
    // Problem Params
    public IEvaluator<TGenotype, Fitness>? Evaluator { get; set; }
    public Goal? Goal { get; set; }
    // Execution Params
    public IRandomSource? RandomSource { get; set; }
    public ITerminator<PopulationState<TGenotype, Fitness, Goal>>? Terminator { get; set; }
    public IInterceptor<PopulationState<TGenotype, Fitness, Goal>>? Interceptor { get; set; }
  }

  public BuilderParameter Parameter { get; } = new();

  public GeneticAlgorithm<TGenotype> Build() {
    var parametersAreSetValidator = new RequiredParametersAreSetValidator();
    var parametersAreSetValidationResult = parametersAreSetValidator.Validate(Parameter);

    if (!parametersAreSetValidationResult.IsValid) throw new ValidationException(parametersAreSetValidationResult.Errors);

    //if (CreatorFactory is IStochasticOperatorFactory stochasticOperatorFactory) { stochasticOperatorFactory.SetRandom(RandomSource!); }
    //if (CreatorFactory is IEncodingDependentOperatorFactory<TParameter> encodingDependentCreatorFactory) encodingDependentCreatorFactory.SetEncoding(Encoding!.Parameter);
    //if (CreatorFactory is IProblemDependentOperatorFactory<IProblem<TGenotype, Fitness, Goal>> problemDependentCreatorFactory) problemDependentCreatorFactory.SetProblem(Problem!);

    //if (CrossoverFactory is IStochasticOperatorFactory stochasticCrossoverFactory) stochasticCrossoverFactory.SetRandom(RandomSource!);
    //if (CrossoverFactory is IEncodingDependentOperatorFactory<TParameter> encodingDependentCrossoverFactory) encodingDependentCrossoverFactory.SetEncoding(Encoding!.Parameter);
    //if (CrossoverFactory is IProblemDependentOperatorFactory<IProblem<TGenotype, Fitness, Goal>> problemDependentCrossoverFactory) problemDependentCrossoverFactory.SetProblem(Problem!);

    //if (MutatorFactory is IStochasticOperatorFactory stochasticMutatorFactory) stochasticMutatorFactory.SetRandom(RandomSource!);
    //if (MutatorFactory is IEncodingDependentOperatorFactory<TParameter> encodingDependentMutatorFactory) encodingDependentMutatorFactory.SetEncoding(Encoding!.Parameter);
    //if (MutatorFactory is IProblemDependentOperatorFactory<IProblem<TGenotype, Fitness, Goal>> problemDependentMutatorFactory) problemDependentMutatorFactory.SetProblem(Problem!);

    //if (SelectorFactory is IStochasticOperatorFactory stochasticSelectorFactory) stochasticSelectorFactory.SetRandom(RandomSource!);
    //if (SelectorFactory is IEncodingDependentOperatorFactory<TParameter> encodingDependentSelectorFactory) encodingDependentSelectorFactory.SetEncoding(Encoding!.Parameter);
    //if (SelectorFactory is IProblemDependentOperatorFactory<IProblem<TGenotype, Fitness, Goal>> problemDependentSelectorFactory) problemDependentSelectorFactory.SetProblem(Problem!);

    //if (ReplacerFactory is IStochasticOperatorFactory stochasticReplacerFactory) stochasticReplacerFactory.SetRandom(RandomSource!);
    //if (ReplacerFactory is IEncodingDependentOperatorFactory<TParameter> encodingDependentReplacerFactory) encodingDependentReplacerFactory.SetEncoding(Encoding!.Parameter);
    //if (ReplacerFactory is IProblemDependentOperatorFactory<IProblem<TGenotype, Fitness, Goal>> problemDependentReplacerFactory) problemDependentReplacerFactory.SetProblem(Problem!);

    return new GeneticAlgorithm<TGenotype>(
      Parameter.PopulationSize!.Value,
      Parameter.Creator!,
      Parameter.Crossover!,
      Parameter.Mutator!,
      Parameter.MutationRate!.Value,
      Parameter.Evaluator!,
      Parameter.Goal!.Value,
      Parameter.Selector!,
      Parameter.Replacer!,
      Parameter.RandomSource!,
      Parameter.Terminator,
      Parameter.Interceptor
    );

  }

  // public GeneticAlgorithmBuilder<TGenotype> WithEncoding(TEncoding? encoding) {
  //   Encoding = encoding;
  //   return this;
  // }
  
  // public GeneticAlgorithmBuilder<TGenotype> WithProblem(IProblem<TGenotype, Fitness, Goal>? problem) {
  //   Problem = problem;
  //
  //   return this;
  // }
  
  
  public IGeneticAlgorithmBuilder<TGenotype> WithPopulationSize(int? populationSize) { Parameter.PopulationSize = populationSize; return this; }
  
  //public GeneticAlgorithmBuilder<TGenotype> WithCreator(IOperatorFactory<ICreator<TGenotype>>? creatorFactory) { CreatorFactory = creatorFactory; return this; }
  //public GeneticAlgorithmBuilder<TGenotype> WithCreator(ICreator<TGenotype>? creator) { CreatorFactory = creator != null ? OperatorFactory.Create(creator) : null; return this; }
  public IGeneticAlgorithmBuilder<TGenotype> WithCreator(ICreator<TGenotype>? creator) { Parameter.Creator = creator; return this; }
  
  //public GeneticAlgorithmBuilder<TGenotype> WithCrossover(IOperatorFactory<ICrossover<TGenotype>>? crossoverFactory) { CrossoverFactory = crossoverFactory; return this; }
  //public GeneticAlgorithmBuilder<TGenotype> WithCrossover(ICrossover<TGenotype>? crossover) { CrossoverFactory = crossover != null ? OperatorFactory.Create(crossover) : null; return this; }
  public IGeneticAlgorithmBuilder<TGenotype> WithCrossover(ICrossover<TGenotype>? crossover) { Parameter.Crossover = crossover; return this; }
  
  //public GeneticAlgorithmBuilder<TGenotype> WithMutator(IOperatorFactory<IMutator<TGenotype>>? mutatorFactory) { MutatorFactory = mutatorFactory; return this; }
  //public GeneticAlgorithmBuilder<TGenotype> WithMutator(IMutator<TGenotype>? mutator) { MutatorFactory = mutator != null ? OperatorFactory.Create(mutator) : null; return this; }
  public IGeneticAlgorithmBuilder<TGenotype> WithMutator(IMutator<TGenotype>? mutator) { Parameter.Mutator = mutator; return this; }
  
  public IGeneticAlgorithmBuilder<TGenotype> WithMutationRate(double? mutationRate) { Parameter.MutationRate = mutationRate; return this; }
  
  //public GeneticAlgorithmBuilder<TGenotype> WithSelector(IOperatorFactory<ISelector<TGenotype, Fitness, Goal>>? selectorFactory) { SelectorFactory = selectorFactory; return this; }
  //public GeneticAlgorithmBuilder<TGenotype> WithSelector(ISelector<TGenotype, Fitness, Goal>? selector) { SelectorFactory = selector != null ? OperatorFactory.Create(selector) : null; return this; }
  public IGeneticAlgorithmBuilder<TGenotype> WithSelector(ISelector<TGenotype, Fitness, Goal>? selector) { Parameter.Selector = selector; return this; }
  
  //public GeneticAlgorithmBuilder<TGenotype> WithReplacer(IOperatorFactory<IReplacer<TGenotype, Fitness, Goal>>? replacementFactory) { ReplacerFactory = replacementFactory; return this; }
  //public GeneticAlgorithmBuilder<TGenotype> WithReplacer(IReplacer<TGenotype, Fitness, Goal>? replacer) { ReplacerFactory = replacer != null ? OperatorFactory.Create(replacer) : null; return this; }
  public IGeneticAlgorithmBuilder<TGenotype> WithReplacer(IReplacer<TGenotype, Fitness, Goal>? replacer) { Parameter.Replacer = replacer; return this; }
  
  public IGeneticAlgorithmBuilder<TGenotype> WithEvaluator(IEvaluator<TGenotype, Fitness>? evaluator) { Parameter.Evaluator = evaluator; return this; }
  
  public IGeneticAlgorithmBuilder<TGenotype> WithGoal(Goal? goal) { Parameter.Goal = goal; return this; }
  
  public IGeneticAlgorithmBuilder<TGenotype> WithRandomSource(IRandomSource? randomSource) { Parameter.RandomSource = randomSource; return this; }
  
  public IGeneticAlgorithmBuilder<TGenotype> WithTerminator(ITerminator<PopulationState<TGenotype, Fitness, Goal>>? terminator) { Parameter.Terminator = terminator; return this; }
  
  public IGeneticAlgorithmBuilder<TGenotype> WithInterceptor(IInterceptor<PopulationState<TGenotype, Fitness, Goal>>? interceptor) { Parameter.Interceptor = interceptor; return this; }

  private sealed class RequiredParametersAreSetValidator : AbstractValidator<BuilderParameter> {
    public RequiredParametersAreSetValidator() {
      RuleFor(x => x.PopulationSize).NotNull().WithMessage("Population size must not be null.");
      // RuleFor(x => x.CreatorFactory).NotNull().WithMessage("Creator factory must not be null.");
      // RuleFor(x => x.CrossoverFactory).NotNull().WithMessage("Crossover factory must not be null.");
      // RuleFor(x => x.MutatorFactory).NotNull().WithMessage("Mutator factory must not be null.");
      RuleFor(x => x.Creator).NotNull().WithMessage("Creator must not be null.");
      RuleFor(x => x.Crossover).NotNull().WithMessage("Crossover must not be null.");
      RuleFor(x => x.Mutator).NotNull().WithMessage("Mutator must not be null.");
      RuleFor(x => x.MutationRate).NotNull().WithMessage("Mutation rate must not be null.");
      RuleFor(x => x.Evaluator).NotNull().WithMessage("Evaluator must not be null.");
      RuleFor(x => x.Goal).NotNull().WithMessage("Goal must not be null.");
      // RuleFor(x => x.SelectorFactory).NotNull().WithMessage("Selector factory must not be null.");
      // RuleFor(x => x.ReplacerFactory).NotNull().WithMessage("Replacement factory must not be null.");
      RuleFor(x => x.Selector).NotNull().WithMessage("Selector must not be null.");
      RuleFor(x => x.Replacer).NotNull().WithMessage("Replacer must not be null.");
      RuleFor(x => x.RandomSource).NotNull().WithMessage("Random source must not be null.");
      //RuleFor(x => x.Terminator).NotNull().WithMessage("Termination criterion must not be null.");

      // When(x => x.CreatorFactory is IEncodingDependentOperatorFactory<TParameter>, () => RuleFor(x => x.Encoding).NotNull().WithMessage("Encoding must not be null when CreatorFactory is encoding-dependent"));
      // When(x => x.CreatorFactory is IProblemDependentOperatorFactory<IProblem<TGenotype, Fitness, Goal>>, () => RuleFor(x => x.Problem).NotNull().WithMessage("Problem must not be null when CreatorFactory is problem-dependent"));
      //
      // When(x => x.CrossoverFactory is IEncodingDependentOperatorFactory<TParameter>, () => RuleFor(x => x.Encoding).NotNull().WithMessage("Encoding must not be null when CrossoverFactory is encoding-dependent"));
      // When(x => x.CrossoverFactory is IProblemDependentOperatorFactory<IProblem<TGenotype, Fitness, Goal>>, () => RuleFor(x => x.Problem).NotNull().WithMessage("Problem must not be null when CrossoverFactory is problem-dependent"));
      //
      // When(x => x.MutatorFactory is IEncodingDependentOperatorFactory<TParameter>, () => RuleFor(x => x.Encoding).NotNull().WithMessage("Encoding must not be null when MutatorFactory is encoding-dependent"));
      // When(x => x.MutatorFactory is IProblemDependentOperatorFactory<IProblem<TGenotype, Fitness, Goal>>, () => RuleFor(x => x.Problem).NotNull().WithMessage("Problem must not be null when MutatorFactory is problem-dependent"));
      //
      // When(x => x.SelectorFactory is IEncodingDependentOperatorFactory<TParameter>, () => RuleFor(x => x.Encoding).NotNull().WithMessage("Encoding must not be null when SelectorFactory is encoding-dependent"));
      // When(x => x.SelectorFactory is IProblemDependentOperatorFactory<IProblem<TGenotype, Fitness, Goal>>, () => RuleFor(x => x.Problem).NotNull().WithMessage("Problem must not be null when SelectorFactory is problem-dependent"));
      //
      // When(x => x.ReplacerFactory is IEncodingDependentOperatorFactory<TParameter>, () => RuleFor(x => x.Encoding).NotNull().WithMessage("Encoding must not be null when ReplacerFactory is encoding-dependent"));
      // When(x => x.ReplacerFactory is IProblemDependentOperatorFactory<IProblem<TGenotype, Fitness, Goal>>, () => RuleFor(x => x.Problem).NotNull().WithMessage("Problem must not be null when ReplacerFactory is problem-dependent"));
    }
  }
  
  // public GeneticAlgorithmBuilder<TGenotype> WithGeneticAlgorithmSpec(GeneticAlgorithmSpec gaSpec) {
  //   if (gaSpec.PopulationSize.HasValue) PopulationSize = gaSpec.PopulationSize.Value;
  //   // if (gaSpec.Creator is not null) CreatorFactory = gaSpec.Creator.GetCreatorFactory<TGenotype>();
  //   // if (gaSpec.Crossover is not null) CrossoverFactory = gaSpec.Crossover.GetCrossoverFactory<TGenotype>();
  //   // if (gaSpec.Mutator is not null) MutatorFactory = gaSpec.Mutator.GetMutatorFactory<TGenotype>();
  //   if (gaSpec.MutationRate.HasValue) MutationRate = gaSpec.MutationRate.Value;
  //   // if (gaSpec.Selector is not null) SelectorFactory = gaSpec.Selector.GetSelectorFactory<TGenotype, Fitness, Goal>();
  //   // if (gaSpec.Replacer is not null) ReplacerFactory = gaSpec.Replacer.GetReplacerFactory<TGenotype, Fitness, Goal>();
  //   if (gaSpec.MaximumGenerations.HasValue) Terminator = Operators.Terminator.OnGeneration(gaSpec.MaximumGenerations.Value);
  //   if (gaSpec.RandomSeed.HasValue) RandomSource = new RandomSource(gaSpec.RandomSeed.Value);
  //
  //   return this;
  // }
}

// public static class OperatorFactoryMapping {
//   public static IOperatorFactory<ICreator<TGenotype>> GetCreatorFactory<TGenotype>(this CreatorSpec creatorSpec) {
//     IOperatorFactory operatorFactory = creatorSpec switch {
//       RandomPermutationCreatorSpec => new RandomPermutationCreator.Factory(),
//       UniformRealVectorCreatorSpec spec => new UniformDistributedCreator.Factory(spec.Minimum, spec.Maximum),
//       NormalRealVectorCreatorSpec spec => new NormalDistributedCreator.Factory(spec.Mean != null ? new RealVector(spec.Mean) : null, spec.StandardDeviation != null ? new RealVector(spec.StandardDeviation) : null),
//       _ => throw new ArgumentException($"Unknown creator spec: {creatorSpec}")
//     };
//     if (operatorFactory is IOperatorFactory<ICreator<TGenotype>> creatorFactory)
//       return creatorFactory;
//     throw new InvalidOperationException($"{creatorSpec} is not compatible with Genotype {typeof(TGenotype)}");
//   }
//
//   public static IOperatorFactory<ICrossover<TGenotype>> GetCrossoverFactory<TGenotype>(this CrossoverSpec crossoverSpec) {
//     IOperatorFactory operatorFactory = crossoverSpec switch {
//       OrderCrossoverSpec => new OrderCrossover.Factory(),
//       SinglePointRealVectorCrossoverSpec => new SinglePointCrossover.Factory(),
//       AlphaBetaBlendRealVectorCrossoverSpec spec => new AlphaBetaBlendCrossover.Factory(spec.Alpha, spec.Beta),
//       _ => throw new ArgumentException($"Unknown crossover spec: {crossoverSpec}")
//     };
//     if (operatorFactory is IOperatorFactory<ICrossover<TGenotype>> crossoverFactory)
//       return crossoverFactory;
//     throw new InvalidOperationException($"{crossoverSpec} is not compatible with Genotype {typeof(TGenotype)}");
//   }
//
//   public static IOperatorFactory<IMutator<TGenotype>> GetMutatorFactory<TGenotype>(this MutatorSpec mutatorSpec) {
//     IOperatorFactory operatorFactory = mutatorSpec switch {
//       SwapMutatorSpec => new SwapMutator.Factory(),
//       GaussianRealVectorMutatorSpec spec => new GaussianMutator.Factory(spec.Rate, spec.Strength),
//       InversionMutatorSpec => new InversionMutator.Factory(),
//       _ => throw new ArgumentException($"Unknown mutator spec: {mutatorSpec}")
//     };
//     if (operatorFactory is IOperatorFactory<IMutator<TGenotype>> mutatorFactory)
//       return mutatorFactory;
//     throw new InvalidOperationException($"{mutatorSpec} is not compatible with Genotype {typeof(TGenotype)}");
//   }
//
//   public static IOperatorFactory<ISelector<TGenotype, TFitness, TGoal>> GetSelectorFactory<TGenotype, TFitness, TGoal>(this SelectorSpec selectorSpec) {
//     IOperatorFactory operatorFactory = selectorSpec switch {
//       RandomSelectorSpec => new RandomSelector<TGenotype, TFitness, TGoal>.Factory(),
//       TournamentSelectorSpec spec => new TournamentSelector<TGenotype>.Factory(spec.TournamentSize),
//       ProportionalSelectorSpec spec => new ProportionalSelector<TGenotype>.Factory(spec.Windowing),
//       _ => throw new ArgumentException($"Unknown selector spec: {selectorSpec}")
//     };
//     if (operatorFactory is IOperatorFactory<ISelector<TGenotype, TFitness, TGoal>> selectorFactory)
//       return selectorFactory;
//     throw new InvalidOperationException($"{selectorSpec} is not compatible with Genotype {typeof(TGenotype)}, Fitness {typeof(TFitness)}, Goal {typeof(TGoal)}");
//   }
//
//   public static IOperatorFactory<IReplacer<TGenotype, TFitness, TGoal>> GetReplacerFactory<TGenotype, TFitness, TGoal>(this ReplacerSpec replacerSpec) {
//     IOperatorFactory operatorFactory = replacerSpec switch {
//       ElitistReplacerSpec spec => new ElitismReplacer<TGenotype>.Factory(spec.Elites),
//       PlusSelectionReplacerSpec => new PlusSelectionReplacer<TGenotype>.Factory(),
//       _ => throw new ArgumentException($"Unknown replacer spec: {replacerSpec}")
//     };
//     if (operatorFactory is IOperatorFactory<IReplacer<TGenotype, TFitness, TGoal>> replacerFactory)
//       return replacerFactory;
//     throw new InvalidOperationException($"{replacerSpec} is not compatible with Genotype {typeof(TGenotype)}, Fitness {typeof(TFitness)}, Goal {typeof(TGoal)}");
//   }
// }

public interface IOperatorFactory<out TOperator> where TOperator : IOperator {
  TOperator Create(IRandomSource randomSource);
}

public interface IEncodingParameterDependentOperatorFactory<out TOperator, in TEncodingParameter> 
  where TOperator : IOperator
  where TEncodingParameter : IEncodingParameter {
  TOperator Create(TEncodingParameter parameters, IRandomSource randomSource);
}

public class GeneticAlgorithmBuilderWithEncodingParameters<TGenotype, TEncodingParameter> : IGeneticAlgorithmBuilder<TGenotype>
  where TEncodingParameter : IEncodingParameter<TGenotype>
{
  private readonly IGeneticAlgorithmBuilder<TGenotype> baseBuilder;  
  
  public TEncodingParameter? EncodingParameter { get; private set; }
  public IEncodingParameterDependentOperatorFactory<ICreator<TGenotype>, TEncodingParameter>? CreatorFactory { get; private set; }
  public IEncodingParameterDependentOperatorFactory<ICrossover<TGenotype>, TEncodingParameter>? CrossoverFactory { get; private set; }
  public IEncodingParameterDependentOperatorFactory<IMutator<TGenotype>, TEncodingParameter>? MutatorFactory { get; private set; }
  
  public GeneticAlgorithmBuilderWithEncodingParameters(IGeneticAlgorithmBuilder<TGenotype> baseBuilder) {
    this.baseBuilder = baseBuilder;
  }
  
  public GeneticAlgorithmBuilderWithEncodingParameters<TGenotype, TEncodingParameter> WithEncodingParameter(TEncodingParameter? encodingParameter) {
    EncodingParameter = encodingParameter;
    return this;
  }
  
  public GeneticAlgorithmBuilderWithEncodingParameters<TGenotype, TEncodingParameter> WithCreatorFactory(IEncodingParameterDependentOperatorFactory<ICreator<TGenotype>, TEncodingParameter>? creatorFactory) {
    CreatorFactory = creatorFactory;
    return this;
  }
  
  public GeneticAlgorithmBuilderWithEncodingParameters<TGenotype, TEncodingParameter> WithCrossoverFactory(IEncodingParameterDependentOperatorFactory<ICrossover<TGenotype>, TEncodingParameter>? crossoverFactory) {
    CrossoverFactory = crossoverFactory;
    return this;
  }
  
  public GeneticAlgorithmBuilderWithEncodingParameters<TGenotype, TEncodingParameter> WithMutatorFactory(IEncodingParameterDependentOperatorFactory<IMutator<TGenotype>, TEncodingParameter>? mutatorFactory) {
    MutatorFactory = mutatorFactory;
    return this;
  }

  public GeneticAlgorithm<TGenotype> Build() {
    if (CreatorFactory is not null) 
      baseBuilder.WithCreator(CreatorFactory.Create(EncodingParameter!, baseBuilder.Parameter.RandomSource!));  
    if (CrossoverFactory is not null)
      baseBuilder.WithCrossover(CrossoverFactory.Create(EncodingParameter!, baseBuilder.Parameter.RandomSource!));
    if (MutatorFactory is not null)
      baseBuilder.WithMutator(MutatorFactory.Create(EncodingParameter!, baseBuilder.Parameter.RandomSource!));
    
    return baseBuilder.Build();
  }


  public GeneticAlgorithmBuilder<TGenotype>.BuilderParameter Parameter => baseBuilder.Parameter;
  public IGeneticAlgorithmBuilder<TGenotype> WithPopulationSize(int? populationSize) { baseBuilder.WithPopulationSize(populationSize); return this; }
  public IGeneticAlgorithmBuilder<TGenotype> WithCreator(ICreator<TGenotype>? creator) { baseBuilder.WithCreator(creator); return this; }
  public IGeneticAlgorithmBuilder<TGenotype> WithCrossover(ICrossover<TGenotype>? crossover) { baseBuilder.WithCrossover(crossover); return this; }
  public IGeneticAlgorithmBuilder<TGenotype> WithMutator(IMutator<TGenotype>? mutator) { baseBuilder.WithMutator(mutator); return this; }
  public IGeneticAlgorithmBuilder<TGenotype> WithMutationRate(double? mutationRate) { baseBuilder.WithMutationRate(mutationRate); return this; }
  public IGeneticAlgorithmBuilder<TGenotype> WithSelector(ISelector<TGenotype, Fitness, Goal>? selector) { baseBuilder.WithSelector(selector); return this; }
  public IGeneticAlgorithmBuilder<TGenotype> WithReplacer(IReplacer<TGenotype, Fitness, Goal>? replacer) { baseBuilder.WithReplacer(replacer); return this; }
  public IGeneticAlgorithmBuilder<TGenotype> WithEvaluator(IEvaluator<TGenotype, Fitness>? evaluator) { baseBuilder.WithEvaluator(evaluator); return this; }
  public IGeneticAlgorithmBuilder<TGenotype> WithGoal(Goal? goal) { baseBuilder.WithGoal(goal); return this; }
  public IGeneticAlgorithmBuilder<TGenotype> WithRandomSource(IRandomSource? randomSource) { baseBuilder.WithRandomSource(randomSource); return this; }
  public IGeneticAlgorithmBuilder<TGenotype> WithTerminator(ITerminator<PopulationState<TGenotype, Fitness, Goal>>? terminator) { baseBuilder.WithTerminator(terminator); return this; }
  public IGeneticAlgorithmBuilder<TGenotype> WithInterceptor(IInterceptor<PopulationState<TGenotype, Fitness, Goal>>? interceptor) { baseBuilder.WithInterceptor(interceptor); return this; }
}

public static class GeneticAlgorithmBuilderWithEncodingParametersExtensions {
  public static GeneticAlgorithmBuilderWithEncodingParameters<TGenotype, TEncodingParameter> UsingEncodingParameters<TGenotype, TEncodingParameter>(
    this IGeneticAlgorithmBuilder<TGenotype> builder, TEncodingParameter encodingParameters) 
  where TEncodingParameter : IEncodingParameter<TGenotype> {
    return new GeneticAlgorithmBuilderWithEncodingParameters<TGenotype, TEncodingParameter>(builder).WithEncodingParameter(encodingParameters);
  }
}
