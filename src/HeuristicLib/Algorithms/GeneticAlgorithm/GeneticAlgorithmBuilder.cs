using FluentValidation;
using HEAL.HeuristicLib.Configuration;
using HEAL.HeuristicLib.Encodings;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems;

namespace HEAL.HeuristicLib.Algorithms.GeneticAlgorithm;


public class GeneticAlgorithmBuilder<TGenotype, TEncoding> : IBuilder<GeneticAlgorithm<TGenotype>>
  where TEncoding : IEncoding<TGenotype, TEncoding> {
  // Genetic Algorithm Params
  public TEncoding? Encoding { get; private set; }
  public int? PopulationSize { get; private set; }
  public IOperatorFactory<ICreator<TGenotype>>? CreatorFactory { get; private set; }
  public IOperatorFactory<ICrossover<TGenotype>>? CrossoverFactory { get; private set; }
  public IOperatorFactory<IMutator<TGenotype>>? MutatorFactory { get; private set; }
  public double? MutationRate { get; private set; }
  public IOperatorFactory<ISelector<TGenotype, Fitness, Goal>>? SelectorFactory { get; private set; }
  public IOperatorFactory<IReplacer<TGenotype, Fitness, Goal>>? ReplacerFactory { get; private set; }

  // Problem Params
  public IProblem<TGenotype, Fitness, Goal>? Problem { get; private set; }
  public IEvaluator<TGenotype, Fitness>? Evaluator { get; private set; }
  public Goal? Goal { get; private set; }

  // Execution Params
  public IRandomSource? RandomSource { get; private set; }
  public ITerminator<PopulationState<TGenotype, Fitness, Goal>>? Terminator { get; private set; }
  public IInterceptor<PopulationState<TGenotype, Fitness, Goal>>? Interceptor { get; private set; }


  public GeneticAlgorithm<TGenotype> Build() {
    var parametersAreSetValidator = new RequiredParametersAreSetValidator();
    var parametersAreSetValidationResult = parametersAreSetValidator.Validate(this);

    if (!parametersAreSetValidationResult.IsValid) throw new ValidationException(parametersAreSetValidationResult.Errors);

    if (CreatorFactory is IStochasticOperatorFactory stochasticOperatorFactory) { stochasticOperatorFactory.SetRandom(RandomSource!); }
    if (CreatorFactory is IEncodingDependentOperatorFactory<TEncoding> encodingDependentCreatorFactory) encodingDependentCreatorFactory.SetEncoding(Encoding!);
    if (CreatorFactory is IProblemDependentOperatorFactory<IProblem<TGenotype, Fitness, Goal>> problemDependentCreatorFactory) problemDependentCreatorFactory.SetProblem(Problem!);

    if (CrossoverFactory is IStochasticOperatorFactory stochasticCrossoverFactory) stochasticCrossoverFactory.SetRandom(RandomSource!);
    if (CrossoverFactory is IEncodingDependentOperatorFactory<TEncoding> encodingDependentCrossoverFactory) encodingDependentCrossoverFactory.SetEncoding(Encoding!);
    if (CrossoverFactory is IProblemDependentOperatorFactory<IProblem<TGenotype, Fitness, Goal>> problemDependentCrossoverFactory) problemDependentCrossoverFactory.SetProblem(Problem!);

    if (MutatorFactory is IStochasticOperatorFactory stochasticMutatorFactory) stochasticMutatorFactory.SetRandom(RandomSource!);
    if (MutatorFactory is IEncodingDependentOperatorFactory<TEncoding> encodingDependentMutatorFactory) encodingDependentMutatorFactory.SetEncoding(Encoding!);
    if (MutatorFactory is IProblemDependentOperatorFactory<IProblem<TGenotype, Fitness, Goal>> problemDependentMutatorFactory) problemDependentMutatorFactory.SetProblem(Problem!);

    if (SelectorFactory is IStochasticOperatorFactory stochasticSelectorFactory) stochasticSelectorFactory.SetRandom(RandomSource!);
    if (SelectorFactory is IEncodingDependentOperatorFactory<TEncoding> encodingDependentSelectorFactory) encodingDependentSelectorFactory.SetEncoding(Encoding!);
    if (SelectorFactory is IProblemDependentOperatorFactory<IProblem<TGenotype, Fitness, Goal>> problemDependentSelectorFactory) problemDependentSelectorFactory.SetProblem(Problem!);

    if (ReplacerFactory is IStochasticOperatorFactory stochasticReplacerFactory) stochasticReplacerFactory.SetRandom(RandomSource!);
    if (ReplacerFactory is IEncodingDependentOperatorFactory<TEncoding> encodingDependentReplacerFactory) encodingDependentReplacerFactory.SetEncoding(Encoding!);
    if (ReplacerFactory is IProblemDependentOperatorFactory<IProblem<TGenotype, Fitness, Goal>> problemDependentReplacerFactory) problemDependentReplacerFactory.SetProblem(Problem!);

    return new GeneticAlgorithm<TGenotype>(
    PopulationSize!.Value,
    CreatorFactory!.Create(),
    CrossoverFactory!.Create(),
    MutatorFactory!.Create(),
    MutationRate!.Value,
    Evaluator!,
    Goal!.Value,
    SelectorFactory!.Create(),
    ReplacerFactory!.Create(),
    RandomSource!,
    Terminator,
    Interceptor
    );

  }

  public GeneticAlgorithmBuilder<TGenotype, TEncoding> WithEncoding(TEncoding? encoding) {
    Encoding = encoding;

    return this;
  }
  public GeneticAlgorithmBuilder<TGenotype, TEncoding> WithPopulationSize(int? populationSize) {
    PopulationSize = populationSize;

    return this;
  }
  public GeneticAlgorithmBuilder<TGenotype, TEncoding> WithCreator(IOperatorFactory<ICreator<TGenotype>>? creatorFactory) {
    CreatorFactory = creatorFactory;

    return this;
  }
  public GeneticAlgorithmBuilder<TGenotype, TEncoding> WithCreator(ICreator<TGenotype>? creator) {
    CreatorFactory = creator != null ? OperatorFactory.Create(creator) : null;

    return this;
  }
  public GeneticAlgorithmBuilder<TGenotype, TEncoding> WithCrossover(IOperatorFactory<ICrossover<TGenotype>>? crossoverFactory) {
    CrossoverFactory = crossoverFactory;

    return this;
  }
  public GeneticAlgorithmBuilder<TGenotype, TEncoding> WithCrossover(ICrossover<TGenotype>? crossover) {
    CrossoverFactory = crossover != null ? OperatorFactory.Create(crossover) : null;

    return this;
  }
  public GeneticAlgorithmBuilder<TGenotype, TEncoding> WithMutator(IOperatorFactory<IMutator<TGenotype>>? mutatorFactory) {
    MutatorFactory = mutatorFactory;

    return this;
  }
  public GeneticAlgorithmBuilder<TGenotype, TEncoding> WithMutator(IMutator<TGenotype>? mutator) {
    MutatorFactory = mutator != null ? OperatorFactory.Create(mutator) : null;

    return this;
  }
  public GeneticAlgorithmBuilder<TGenotype, TEncoding> WithMutationRate(double? mutationRate) {
    MutationRate = mutationRate;

    return this;
  }
  public GeneticAlgorithmBuilder<TGenotype, TEncoding> WithSelector(IOperatorFactory<ISelector<TGenotype, Fitness, Goal>>? selectorFactory) {
    SelectorFactory = selectorFactory;

    return this;
  }
  public GeneticAlgorithmBuilder<TGenotype, TEncoding> WithSelector(ISelector<TGenotype, Fitness, Goal>? selector) {
    SelectorFactory = selector != null ? OperatorFactory.Create(selector) : null;

    return this;
  }
  public GeneticAlgorithmBuilder<TGenotype, TEncoding> WithReplacer(IOperatorFactory<IReplacer<TGenotype, Fitness, Goal>>? replacementFactory) {
    ReplacerFactory = replacementFactory;

    return this;
  }
  public GeneticAlgorithmBuilder<TGenotype, TEncoding> WithReplacer(IReplacer<TGenotype, Fitness, Goal>? replacer) {
    ReplacerFactory = replacer != null ? OperatorFactory.Create(replacer) : null;

    return this;
  }
  public GeneticAlgorithmBuilder<TGenotype, TEncoding> WithProblem(IProblem<TGenotype, Fitness, Goal>? problem) {
    Problem = problem;

    return this;
  }
  public GeneticAlgorithmBuilder<TGenotype, TEncoding> WithEvaluator(IEvaluator<TGenotype, Fitness>? evaluator) {
    Evaluator = evaluator;

    return this;
  }
  public GeneticAlgorithmBuilder<TGenotype, TEncoding> WithGoal(Goal? goal) {
    Goal = goal;

    return this;
  }
  public GeneticAlgorithmBuilder<TGenotype, TEncoding> WithRandomSource(IRandomSource? randomSource) {
    RandomSource = randomSource;

    return this;
  }
  public GeneticAlgorithmBuilder<TGenotype, TEncoding> WithTerminator(ITerminator<PopulationState<TGenotype, Fitness, Goal>>? terminator) {
    Terminator = terminator;

    return this;
  }
  public GeneticAlgorithmBuilder<TGenotype, TEncoding> WithInterceptor(IInterceptor<PopulationState<TGenotype, Fitness, Goal>>? interceptor) {
    Interceptor = interceptor;

    return this;
  }

  private sealed class RequiredParametersAreSetValidator : AbstractValidator<GeneticAlgorithmBuilder<TGenotype, TEncoding>> {
    public RequiredParametersAreSetValidator() {
      RuleFor(x => x.PopulationSize).NotNull().WithMessage("Population size must not be null.");
      RuleFor(x => x.CreatorFactory).NotNull().WithMessage("Creator factory must not be null.");
      RuleFor(x => x.CrossoverFactory).NotNull().WithMessage("Crossover factory must not be null.");
      RuleFor(x => x.MutatorFactory).NotNull().WithMessage("Mutator factory must not be null.");
      RuleFor(x => x.MutationRate).NotNull().WithMessage("Mutation rate must not be null.");
      RuleFor(x => x.Evaluator).NotNull().WithMessage("Evaluator must not be null.");
      RuleFor(x => x.Goal).NotNull().WithMessage("Goal must not be null.");
      RuleFor(x => x.SelectorFactory).NotNull().WithMessage("Selector factory must not be null.");
      RuleFor(x => x.ReplacerFactory).NotNull().WithMessage("Replacement factory must not be null.");
      RuleFor(x => x.RandomSource).NotNull().WithMessage("Random source must not be null.");
      //RuleFor(x => x.Terminator).NotNull().WithMessage("Termination criterion must not be null.");

      When(x => x.CreatorFactory is IEncodingDependentOperatorFactory<TEncoding>, () => RuleFor(x => x.Encoding).NotNull().WithMessage("Encoding must not be null when CreatorFactory is encoding-dependent"));
      When(x => x.CreatorFactory is IProblemDependentOperatorFactory<IProblem<TGenotype, Fitness, Goal>>, () => RuleFor(x => x.Problem).NotNull().WithMessage("Problem must not be null when CreatorFactory is problem-dependent"));

      When(x => x.CrossoverFactory is IEncodingDependentOperatorFactory<TEncoding>, () => RuleFor(x => x.Encoding).NotNull().WithMessage("Encoding must not be null when CrossoverFactory is encoding-dependent"));
      When(x => x.CrossoverFactory is IProblemDependentOperatorFactory<IProblem<TGenotype, Fitness, Goal>>, () => RuleFor(x => x.Problem).NotNull().WithMessage("Problem must not be null when CrossoverFactory is problem-dependent"));

      When(x => x.MutatorFactory is IEncodingDependentOperatorFactory<TEncoding>, () => RuleFor(x => x.Encoding).NotNull().WithMessage("Encoding must not be null when MutatorFactory is encoding-dependent"));
      When(x => x.MutatorFactory is IProblemDependentOperatorFactory<IProblem<TGenotype, Fitness, Goal>>, () => RuleFor(x => x.Problem).NotNull().WithMessage("Problem must not be null when MutatorFactory is problem-dependent"));

      When(x => x.SelectorFactory is IEncodingDependentOperatorFactory<TEncoding>, () => RuleFor(x => x.Encoding).NotNull().WithMessage("Encoding must not be null when SelectorFactory is encoding-dependent"));
      When(x => x.SelectorFactory is IProblemDependentOperatorFactory<IProblem<TGenotype, Fitness, Goal>>, () => RuleFor(x => x.Problem).NotNull().WithMessage("Problem must not be null when SelectorFactory is problem-dependent"));

      When(x => x.ReplacerFactory is IEncodingDependentOperatorFactory<TEncoding>, () => RuleFor(x => x.Encoding).NotNull().WithMessage("Encoding must not be null when ReplacerFactory is encoding-dependent"));
      When(x => x.ReplacerFactory is IProblemDependentOperatorFactory<IProblem<TGenotype, Fitness, Goal>>, () => RuleFor(x => x.Problem).NotNull().WithMessage("Problem must not be null when ReplacerFactory is problem-dependent"));
    }
  }
  
  public GeneticAlgorithmBuilder<TGenotype, TEncoding> WithGeneticAlgorithmSpec(GeneticAlgorithmSpec gaSpec) {
    if (gaSpec.PopulationSize.HasValue) PopulationSize = gaSpec.PopulationSize.Value;
    if (gaSpec.Creator is not null) CreatorFactory = gaSpec.Creator.GetCreatorFactory<TGenotype>();
    if (gaSpec.Crossover is not null) CrossoverFactory = gaSpec.Crossover.GetCrossoverFactory<TGenotype>();
    if (gaSpec.Mutator is not null) MutatorFactory = gaSpec.Mutator.GetMutatorFactory<TGenotype>();
    if (gaSpec.MutationRate.HasValue) MutationRate = gaSpec.MutationRate.Value;
    if (gaSpec.Selector is not null) SelectorFactory = gaSpec.Selector.GetSelectorFactory<TGenotype, Fitness, Goal>();
    if (gaSpec.Replacer is not null) ReplacerFactory = gaSpec.Replacer.GetReplacerFactory<TGenotype, Fitness, Goal>();
    if (gaSpec.MaximumGenerations.HasValue) Terminator = Operators.Terminator.OnGeneration(gaSpec.MaximumGenerations.Value);
    if (gaSpec.RandomSeed.HasValue) RandomSource = new RandomSource(gaSpec.RandomSeed.Value);

    return this;
  }
}

public static class OperatorFactoryMapping {
  public static IOperatorFactory<ICreator<TGenotype>> GetCreatorFactory<TGenotype>(this CreatorSpec creatorSpec) {
    IOperatorFactory operatorFactory = creatorSpec switch {
      RandomPermutationCreatorSpec => new RandomPermutationCreator.Factory(),
      UniformRealVectorCreatorSpec spec => new UniformDistributedCreator.Factory(spec.Minimum, spec.Maximum),
      NormalRealVectorCreatorSpec spec => new NormalDistributedCreator.Factory(spec.Mean != null ? new RealVector(spec.Mean) : null, spec.StandardDeviation != null ? new RealVector(spec.StandardDeviation) : null),
      _ => throw new ArgumentException($"Unknown creator spec: {creatorSpec}")
    };
    if (operatorFactory is IOperatorFactory<ICreator<TGenotype>> creatorFactory)
      return creatorFactory;
    throw new InvalidOperationException($"{creatorSpec} is not compatible with Genotype {typeof(TGenotype)}");
  }

  public static IOperatorFactory<ICrossover<TGenotype>> GetCrossoverFactory<TGenotype>(this CrossoverSpec crossoverSpec) {
    IOperatorFactory operatorFactory = crossoverSpec switch {
      OrderCrossoverSpec => new OrderCrossover.Factory(),
      SinglePointRealVectorCrossoverSpec => new SinglePointCrossover.Factory(),
      AlphaBetaBlendRealVectorCrossoverSpec spec => new AlphaBetaBlendCrossover.Factory(spec.Alpha, spec.Beta),
      _ => throw new ArgumentException($"Unknown crossover spec: {crossoverSpec}")
    };
    if (operatorFactory is IOperatorFactory<ICrossover<TGenotype>> crossoverFactory)
      return crossoverFactory;
    throw new InvalidOperationException($"{crossoverSpec} is not compatible with Genotype {typeof(TGenotype)}");
  }

  public static IOperatorFactory<IMutator<TGenotype>> GetMutatorFactory<TGenotype>(this MutatorSpec mutatorSpec) {
    IOperatorFactory operatorFactory = mutatorSpec switch {
      SwapMutatorSpec => new SwapMutator.Factory(),
      GaussianRealVectorMutatorSpec spec => new GaussianMutator.Factory(spec.Rate, spec.Strength),
      InversionMutatorSpec => new InversionMutator.Factory(),
      _ => throw new ArgumentException($"Unknown mutator spec: {mutatorSpec}")
    };
    if (operatorFactory is IOperatorFactory<IMutator<TGenotype>> mutatorFactory)
      return mutatorFactory;
    throw new InvalidOperationException($"{mutatorSpec} is not compatible with Genotype {typeof(TGenotype)}");
  }

  public static IOperatorFactory<ISelector<TGenotype, TFitness, TGoal>> GetSelectorFactory<TGenotype, TFitness, TGoal>(this SelectorSpec selectorSpec) {
    IOperatorFactory operatorFactory = selectorSpec switch {
      RandomSelectorSpec => new RandomSelector<TGenotype, TFitness, TGoal>.Factory(),
      TournamentSelectorSpec spec => new TournamentSelector<TGenotype>.Factory(spec.TournamentSize),
      ProportionalSelectorSpec spec => new ProportionalSelector<TGenotype>.Factory(spec.Windowing),
      _ => throw new ArgumentException($"Unknown selector spec: {selectorSpec}")
    };
    if (operatorFactory is IOperatorFactory<ISelector<TGenotype, TFitness, TGoal>> selectorFactory)
      return selectorFactory;
    throw new InvalidOperationException($"{selectorSpec} is not compatible with Genotype {typeof(TGenotype)}, Fitness {typeof(TFitness)}, Goal {typeof(TGoal)}");
  }

  public static IOperatorFactory<IReplacer<TGenotype, TFitness, TGoal>> GetReplacerFactory<TGenotype, TFitness, TGoal>(this ReplacerSpec replacerSpec) {
    IOperatorFactory operatorFactory = replacerSpec switch {
      ElitistReplacerSpec spec => new ElitismReplacer<TGenotype>.Factory(spec.Elites),
      PlusSelectionReplacerSpec => new PlusSelectionReplacer<TGenotype>.Factory(),
      _ => throw new ArgumentException($"Unknown replacer spec: {replacerSpec}")
    };
    if (operatorFactory is IOperatorFactory<IReplacer<TGenotype, TFitness, TGoal>> replacerFactory)
      return replacerFactory;
    throw new InvalidOperationException($"{replacerSpec} is not compatible with Genotype {typeof(TGenotype)}, Fitness {typeof(TFitness)}, Goal {typeof(TGoal)}");
  }
}
