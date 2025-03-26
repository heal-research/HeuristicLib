using FluentValidation;
using HEAL.HeuristicLib.Encodings;
using HEAL.HeuristicLib.Operators;

namespace HEAL.HeuristicLib.Algorithms.GeneticAlgorithm;

public record GeneticAlgorithmBuilderState {
  public int? PopulationSize { get; init; }
  public double? MutationRate { get; init; }
  public ISelectorOperator? Selector { get; init; }
  public IExecutableOperatorFactory<ISelectorOperator>? SelectorReference { get; init; }
  public IReplacerOperator? Replacer { get; init; }
  public IExecutableOperatorFactory<IReplacerOperator>? ReplacerReference { get; init; }
  public Objective? Objective { get; init; }
  public IRandomSource? RandomSource { get; init; }

  public GeneticAlgorithmBuilderState() { }
  public GeneticAlgorithmBuilderState(GeneticAlgorithmBuilderState original) {
    PopulationSize = original.PopulationSize;
    MutationRate = original.MutationRate;
    Selector = original.Selector;
    SelectorReference = original.SelectorReference;
    Replacer = original.Replacer;
    ReplacerReference = original.ReplacerReference;
    Objective = original.Objective;
    RandomSource = original.RandomSource;
  }
}
public record GeneticAlgorithmBuilderState<TGenotype> : GeneticAlgorithmBuilderState {
  public ICreatorOperator<TGenotype>? Creator { get; init; }
  public IExecutableOperatorFactory<ICreatorOperator<TGenotype>>? CreatorReference { get; init; }
  public ICrossoverOperator<TGenotype>? Crossover { get; init; }
  public IExecutableOperatorFactory<ICrossoverOperator<TGenotype>>? CrossoverReference { get; init; }
  public IMutatorOperator<TGenotype>? Mutator { get; init; }
  public IExecutableOperatorFactory<IMutatorOperator<TGenotype>>? MutatorReference { get; init; }
  public IEvaluatorOperator<TGenotype>? Evaluator { get; init; }
  public ITerminatorOperator<PopulationState<TGenotype>>? Terminator { get; init; }
  public IInterceptor<PopulationState<TGenotype>>? Interceptor { get; init; }

  public GeneticAlgorithmBuilderState() {}
  public GeneticAlgorithmBuilderState(GeneticAlgorithmBuilderState baseState) : base(baseState) {}
  public GeneticAlgorithmBuilderState(GeneticAlgorithmBuilderState<TGenotype> original) : base(original) {
    Creator = original.Creator;
    CreatorReference = original.CreatorReference;
    Crossover = original.Crossover;
    CrossoverReference = original.CrossoverReference;
    Mutator = original.Mutator;
    MutatorReference = original.MutatorReference;
    Evaluator = original.Evaluator;
    Terminator = original.Terminator;
    Interceptor = original.Interceptor;
  }
}

public record GeneticAlgorithmBuilderState<TGenotype, TEncodingParameter> : GeneticAlgorithmBuilderState<TGenotype> 
  where TEncodingParameter : IEncodingParameter<TGenotype> {
  public TEncodingParameter? EncodingParameter { get; init; }
  public IExecutableEncodingOperatorFactory<ICreatorOperator<TGenotype>, TEncodingParameter>? CreatorEncodingReference { get; init; }
  public IExecutableEncodingOperatorFactory<ICrossoverOperator<TGenotype>, TEncodingParameter>? CrossoverEncodingReference { get; init; }
  public IExecutableEncodingOperatorFactory<IMutatorOperator<TGenotype>, TEncodingParameter>? MutatorEncodingReference { get; init; }
  
  public GeneticAlgorithmBuilderState() {}
  public GeneticAlgorithmBuilderState(GeneticAlgorithmBuilderState<TGenotype> baseState) : base(baseState) {}
  public GeneticAlgorithmBuilderState(GeneticAlgorithmBuilderState<TGenotype, TEncodingParameter> original) : base(original) {
    EncodingParameter = original.EncodingParameter;
    CreatorEncodingReference = original.CreatorEncodingReference;
    CrossoverEncodingReference = original.CrossoverEncodingReference;
    MutatorEncodingReference = original.MutatorEncodingReference;
  }
}

public interface IGeneticAlgorithmBuilder<out TSelf> where TSelf : IGeneticAlgorithmBuilder<TSelf> {
  TSelf WithPopulationSize(int populationSize);
  TSelf WithMutationRate(double mutationRate);
  TSelf WithSelector(ISelectorOperator selector);
  TSelf WithSelector(IExecutableOperatorFactory<ISelectorOperator> selectorFactory);
  TSelf WithReplacer(IReplacerOperator replacer);
  TSelf WithReplacer(IExecutableOperatorFactory<IReplacerOperator> replacerFactory);
  TSelf WithObjective(Objective objective);
  TSelf WithRandomSource(IRandomSource randomSource);
}

public interface IGeneticAlgorithmBuilder<TGenotype, out TSelf> : IGeneticAlgorithmBuilder<TSelf>, IBuilder<GeneticAlgorithm<TGenotype>> where TSelf : IGeneticAlgorithmBuilder<TGenotype, TSelf> {
  TSelf WithCreator(ICreatorOperator<TGenotype> creator);
  TSelf WithCreator(IExecutableOperatorFactory<ICreatorOperator<TGenotype>> creatorFactory);
  TSelf WithCrossover(ICrossoverOperator<TGenotype> crossover);
  TSelf WithCrossover(IExecutableOperatorFactory<ICrossoverOperator<TGenotype>> crossoverFactory);
  TSelf WithMutator(IMutatorOperator<TGenotype> mutator);
  TSelf WithMutator(IExecutableOperatorFactory<IMutatorOperator<TGenotype>> mutatorFactory);
  TSelf WithEvaluator(IEvaluatorOperator<TGenotype> evaluator);
  TSelf WithTerminator(ITerminatorOperator<PopulationState<TGenotype>> terminator);
  TSelf WithInterceptor(IInterceptor<PopulationState<TGenotype>> interceptor);
}

public interface IGeneticAlgorithmBuilder<TGenotype, TEncodingParameter, out TSelf> : IGeneticAlgorithmBuilder<TGenotype, TSelf> 
  where TSelf : IGeneticAlgorithmBuilder<TGenotype, TEncodingParameter, TSelf> where TEncodingParameter : IEncodingParameter<TGenotype> {
  TSelf WithEncodingParameter(TEncodingParameter encodingParameter);
  TSelf WithCreator(IExecutableEncodingOperatorFactory<ICreatorOperator<TGenotype>, TEncodingParameter> creatorEncodingFactory);
  TSelf WithCrossover(IExecutableEncodingOperatorFactory<ICrossoverOperator<TGenotype>, TEncodingParameter> crossoverEncodingFactory);
  TSelf WithMutator(IExecutableEncodingOperatorFactory<IMutatorOperator<TGenotype>, TEncodingParameter> mutatorEncodingFactory);
}

public class GeneticAlgorithmBuilder : IGeneticAlgorithmBuilder<GeneticAlgorithmBuilder> {
  public GeneticAlgorithmBuilderState State { get; private set; }
  
  public GeneticAlgorithmBuilder() {
    State = new GeneticAlgorithmBuilderState();
  }
  public GeneticAlgorithmBuilder(GeneticAlgorithmBuilder original) {
    State = new GeneticAlgorithmBuilderState(original.State);
  }
  
  public GeneticAlgorithmBuilder WithPopulationSize(int populationSize) {
    State = State with { PopulationSize = populationSize };
    return this;
  }
  public GeneticAlgorithmBuilder WithMutationRate(double mutationRate) {
    State = State with { MutationRate = mutationRate };
    return this;
  }
  public GeneticAlgorithmBuilder WithSelector(ISelectorOperator selector) {
    State = State with { Selector = selector, SelectorReference = null };
    return this;
  }
  public GeneticAlgorithmBuilder WithSelector(IExecutableOperatorFactory<ISelectorOperator> selectorFactory) {
    State = State with { SelectorReference = selectorFactory, Selector = null };
    return this;
  }
  public GeneticAlgorithmBuilder WithReplacer(IReplacerOperator replacer) {
    State = State with { Replacer = replacer, ReplacerReference = null};
    return this;
  }
  public GeneticAlgorithmBuilder WithReplacer(IExecutableOperatorFactory<IReplacerOperator> replacerFactory) {
    State = State with { ReplacerReference = replacerFactory, Replacer = null };
    return this;
  }
  public GeneticAlgorithmBuilder WithObjective(Objective objective) {
    State = State with { Objective = objective };
    return this;
  }
  public GeneticAlgorithmBuilder WithRandomSource(IRandomSource randomSource) {
    State = State with { RandomSource = randomSource };
    return this;
  }
}

public class GeneticAlgorithmBuilder<TGenotype> : IGeneticAlgorithmBuilder<TGenotype, GeneticAlgorithmBuilder<TGenotype>> {
  public GeneticAlgorithmBuilderState<TGenotype> State { get; private set; }
  
  public GeneticAlgorithmBuilder() {
    State = new GeneticAlgorithmBuilderState<TGenotype>();
  }
  public GeneticAlgorithmBuilder(GeneticAlgorithmBuilder baseBuilder) {
    State = new GeneticAlgorithmBuilderState<TGenotype>(baseBuilder.State);
  }
  
  public GeneticAlgorithmBuilder(GeneticAlgorithmBuilder<TGenotype> original) {
    State = new GeneticAlgorithmBuilderState<TGenotype>(original.State);
  }
  
  public GeneticAlgorithm<TGenotype> Build() {
    var context = new OperatorCreationContext { RandomSource = State.RandomSource ?? throw new InvalidOperationException("Random Source is not set.") };
    var resolvedState = new GeneticAlgorithmBuilderState<TGenotype> {
      PopulationSize = State.PopulationSize,
      Creator = State.Creator ?? State.CreatorReference?.Create(context),
      Crossover = State.Crossover ?? State.CrossoverReference?.Create(context),
      Mutator = State.Mutator ?? State.MutatorReference?.Create(context),
      MutationRate = State.MutationRate,
      Evaluator = State.Evaluator,
      Objective = State.Objective,
      Selector = State.Selector ?? State.SelectorReference?.Create(context),
      Replacer = State.Replacer ?? State.ReplacerReference?.Create(context),
      RandomSource = State.RandomSource,
      Terminator = State.Terminator,
      Interceptor = State.Interceptor
    };
    
    var parametersAreSetValidator = new GeneticAlgorithmBuilderStateValidator<TGenotype>();
    var parametersAreSetValidationResult = parametersAreSetValidator.Validate(resolvedState);

    if (!parametersAreSetValidationResult.IsValid) throw new ValidationException(parametersAreSetValidationResult.Errors);
    
    return new GeneticAlgorithm<TGenotype>(
      resolvedState.PopulationSize!.Value,
      resolvedState.Creator!,
      resolvedState.Crossover!,
      resolvedState.Mutator!,
      resolvedState.MutationRate!.Value,
      resolvedState.Evaluator!,
      resolvedState.Objective!,
      resolvedState.Selector!,
      resolvedState.Replacer!,
      resolvedState.RandomSource!,
      resolvedState.Terminator,
      resolvedState.Interceptor
    );
  }

  public GeneticAlgorithmBuilder<TGenotype> WithPopulationSize(int populationSize) {
    State = State with { PopulationSize = populationSize };
    return this;
  }
  public GeneticAlgorithmBuilder<TGenotype> WithCreator(ICreatorOperator<TGenotype> creator) {
    State = State with { Creator = creator, CreatorReference = null };
    return this;
  }
  public GeneticAlgorithmBuilder<TGenotype> WithCreator(IExecutableOperatorFactory<ICreatorOperator<TGenotype>> creatorFactory) {
    State = State with { CreatorReference = creatorFactory, Creator = null };
    return this;
  }
  public GeneticAlgorithmBuilder<TGenotype> WithCrossover(ICrossoverOperator<TGenotype> crossover) {
    State = State with { Crossover = crossover, CrossoverReference = null };
    return this;
  }
  public GeneticAlgorithmBuilder<TGenotype> WithCrossover(IExecutableOperatorFactory<ICrossoverOperator<TGenotype>> crossoverFactory) {
    State = State with { CrossoverReference = crossoverFactory, Crossover = null };
    return this;
  }
  public GeneticAlgorithmBuilder<TGenotype> WithMutator(IMutatorOperator<TGenotype> mutator) {
    State = State with { Mutator = mutator, MutatorReference = null };
    return this;
  }
  public GeneticAlgorithmBuilder<TGenotype> WithMutator(IExecutableOperatorFactory<IMutatorOperator<TGenotype>> mutatorFactory) {
    State = State with { MutatorReference = mutatorFactory, Mutator = null };
    return this;
  }
  public GeneticAlgorithmBuilder<TGenotype> WithMutationRate(double mutationRate) {
    State = State with { MutationRate = mutationRate };
    return this;
  }
  public GeneticAlgorithmBuilder<TGenotype> WithEvaluator(IEvaluatorOperator<TGenotype> evaluator) {
    State = State with { Evaluator = evaluator };
    return this;
  }
  public GeneticAlgorithmBuilder<TGenotype> WithObjective(Objective objective) {
    State = State with { Objective = objective };
    return this;
  }
  public GeneticAlgorithmBuilder<TGenotype> WithSelector(ISelectorOperator selector) {
    State = State with { Selector = selector, SelectorReference = null };
    return this;
  }
  public GeneticAlgorithmBuilder<TGenotype> WithSelector(IExecutableOperatorFactory<ISelectorOperator> selectorFactory) {
    State = State with { SelectorReference = selectorFactory, Selector = null };
    return this;
  }
  public GeneticAlgorithmBuilder<TGenotype> WithReplacer(IReplacerOperator replacer) {
    State = State with { Replacer = replacer, ReplacerReference = null};
    return this;
  }
  public GeneticAlgorithmBuilder<TGenotype> WithReplacer(IExecutableOperatorFactory<IReplacerOperator> replacerFactory) {
    State = State with { ReplacerReference = replacerFactory, Replacer = null };
    return this;
  }
  public GeneticAlgorithmBuilder<TGenotype> WithRandomSource(IRandomSource randomSource) {
    State = State with { RandomSource = randomSource };
    return this;
  }
  public GeneticAlgorithmBuilder<TGenotype> WithTerminator(ITerminatorOperator<PopulationState<TGenotype>> terminator) {
    State = State with { Terminator = terminator };
    return this;
  }
  public GeneticAlgorithmBuilder<TGenotype> WithInterceptor(IInterceptor<PopulationState<TGenotype>> interceptor) {
    State = State with { Interceptor = interceptor };
    return this;
  }
}

internal sealed class GeneticAlgorithmBuilderStateValidator<TGenotype> : AbstractValidator<GeneticAlgorithmBuilderState<TGenotype>> {
  public GeneticAlgorithmBuilderStateValidator() {
    RuleFor(x => x.PopulationSize).NotNull().WithMessage("Population size must not be null.");
    RuleFor(x => x.Creator).NotNull().WithMessage("Creator must not be null.");
    RuleFor(x => x.Crossover).NotNull().WithMessage("Crossover must not be null.");
    RuleFor(x => x.Mutator).NotNull().WithMessage("Mutator must not be null.");
    RuleFor(x => x.MutationRate).NotNull().WithMessage("Mutation rate must not be null.");
    RuleFor(x => x.Evaluator).NotNull().WithMessage("Evaluator must not be null.");
    RuleFor(x => x.Objective).NotNull().WithMessage("Objective must not be null.");
    RuleFor(x => x.Selector).NotNull().WithMessage("Selector must not be null.");
    RuleFor(x => x.Replacer).NotNull().WithMessage("Replacer must not be null.");
    RuleFor(x => x.RandomSource).NotNull().WithMessage("Random source must not be null.");
  }
}

public class GeneticAlgorithmBuilder<TGenotype, TEncodingParameter> : 
  IGeneticAlgorithmBuilder<TGenotype, TEncodingParameter, GeneticAlgorithmBuilder<TGenotype, TEncodingParameter>> 
  where TEncodingParameter : IEncodingParameter<TGenotype> {
  public GeneticAlgorithmBuilderState<TGenotype, TEncodingParameter> State { get; private set; }
  
  public GeneticAlgorithmBuilder() {
    State = new GeneticAlgorithmBuilderState<TGenotype, TEncodingParameter>();
  }
  public GeneticAlgorithmBuilder(GeneticAlgorithmBuilder<TGenotype> baseBuilder) {
    State = new GeneticAlgorithmBuilderState<TGenotype, TEncodingParameter>(baseBuilder.State);
  }
  public GeneticAlgorithmBuilder(GeneticAlgorithmBuilder<TGenotype, TEncodingParameter> original) {
    State = new GeneticAlgorithmBuilderState<TGenotype, TEncodingParameter>(original.State);
  }

  public GeneticAlgorithm<TGenotype> Build() {
    var context = new OperatorCreationContext { RandomSource = State.RandomSource ?? throw new InvalidOperationException("Random Source is not set.") };
    var encodingContext = new EncodingOperatorCreationContext<TEncodingParameter> { RandomSource = context.RandomSource, EncodingParameter = State.EncodingParameter ?? throw new InvalidOperationException("Encoding Parameter is not set.") };
    var resolvedState = new GeneticAlgorithmBuilderState<TGenotype> {
      PopulationSize = State.PopulationSize,
      Creator = State.Creator ?? State.CreatorReference?.Create(context) ?? State.CreatorEncodingReference?.Create(encodingContext),
      Crossover = State.Crossover ?? State.CrossoverReference?.Create(context) ?? State.CrossoverEncodingReference?.Create(encodingContext),
      Mutator = State.Mutator ?? State.MutatorReference?.Create(context) ?? State.MutatorEncodingReference?.Create(encodingContext),
      MutationRate = State.MutationRate,
      Evaluator = State.Evaluator,
      Objective = State.Objective,
      Selector = State.Selector ?? State.SelectorReference?.Create(context),
      Replacer = State.Replacer ?? State.ReplacerReference?.Create(context),
      RandomSource = State.RandomSource,
      Terminator = State.Terminator,
      Interceptor = State.Interceptor
    };
    
    var parametersAreSetValidator = new GeneticAlgorithmBuilderStateValidator<TGenotype>();
    var parametersAreSetValidationResult = parametersAreSetValidator.Validate(resolvedState);

    if (!parametersAreSetValidationResult.IsValid) throw new ValidationException(parametersAreSetValidationResult.Errors);
    
    return new GeneticAlgorithm<TGenotype>(
    resolvedState.PopulationSize!.Value,
    resolvedState.Creator!,
    resolvedState.Crossover!,
    resolvedState.Mutator!,
    resolvedState.MutationRate!.Value,
    resolvedState.Evaluator!,
    resolvedState.Objective!,
    resolvedState.Selector!,
    resolvedState.Replacer!,
    resolvedState.RandomSource!,
    resolvedState.Terminator,
    resolvedState.Interceptor
    );
  }
  
  public GeneticAlgorithmBuilder<TGenotype, TEncodingParameter> WithEncodingParameter(TEncodingParameter encodingParameter) {
    State = State with { EncodingParameter = encodingParameter };
    return this;
  }
  public GeneticAlgorithmBuilder<TGenotype, TEncodingParameter> WithPopulationSize(int populationSize) {
    State = State with { PopulationSize = populationSize };
    return this;
  }
  public GeneticAlgorithmBuilder<TGenotype, TEncodingParameter> WithCreator(ICreatorOperator<TGenotype> creator) {
    State = State with { Creator = creator, CreatorReference = null, CreatorEncodingReference = null};
    return this;
  }
  public GeneticAlgorithmBuilder<TGenotype, TEncodingParameter> WithCreator(IExecutableOperatorFactory<ICreatorOperator<TGenotype>> creatorFactory) {
    State = State with { CreatorReference = creatorFactory, Creator = null, CreatorEncodingReference = null };
    return this;
  }
  public GeneticAlgorithmBuilder<TGenotype, TEncodingParameter> WithCreator(IExecutableEncodingOperatorFactory<ICreatorOperator<TGenotype>, TEncodingParameter> creatorEncodingFactory) {
    State = State with { CreatorEncodingReference = creatorEncodingFactory, Creator = null, CreatorReference = null };
    return this;
  }
  public GeneticAlgorithmBuilder<TGenotype, TEncodingParameter> WithCrossover(ICrossoverOperator<TGenotype> crossover) {
    State = State with { Crossover = crossover, CrossoverReference = null, CreatorEncodingReference = null };
    return this;
  }
  public GeneticAlgorithmBuilder<TGenotype, TEncodingParameter> WithCrossover(IExecutableOperatorFactory<ICrossoverOperator<TGenotype>> crossoverFactory) {
    State = State with { CrossoverReference = crossoverFactory, Crossover = null, CrossoverEncodingReference = null };
    return this;
  }
  public GeneticAlgorithmBuilder<TGenotype, TEncodingParameter> WithCrossover(IExecutableEncodingOperatorFactory<ICrossoverOperator<TGenotype>, TEncodingParameter> crossoverEncodingFactory) {
    State = State with { CrossoverEncodingReference = crossoverEncodingFactory };
    return this;
  }
  public GeneticAlgorithmBuilder<TGenotype, TEncodingParameter> WithMutator(IMutatorOperator<TGenotype> mutator) {
    State = State with { Mutator = mutator, MutatorReference = null, MutatorEncodingReference = null };
    return this;
  }
  public GeneticAlgorithmBuilder<TGenotype, TEncodingParameter> WithMutator(IExecutableOperatorFactory<IMutatorOperator<TGenotype>> mutatorFactory) {
    State = State with { MutatorReference = mutatorFactory, Mutator = null, MutatorEncodingReference = null};
    return this;
  }
  public GeneticAlgorithmBuilder<TGenotype, TEncodingParameter> WithMutator(IExecutableEncodingOperatorFactory<IMutatorOperator<TGenotype>, TEncodingParameter> mutatorEncodingFactory) {
    State = State with { MutatorEncodingReference = mutatorEncodingFactory, Mutator = null, MutatorReference = null };
    return this;
  }
  public GeneticAlgorithmBuilder<TGenotype, TEncodingParameter> WithMutationRate(double mutationRate) {
    State = State with { MutationRate = mutationRate };
    return this;
  }
  public GeneticAlgorithmBuilder<TGenotype, TEncodingParameter> WithEvaluator(IEvaluatorOperator<TGenotype> evaluator) {
    State = State with { Evaluator = evaluator };
    return this;
  }
  public GeneticAlgorithmBuilder<TGenotype, TEncodingParameter> WithObjective(Objective objective) {
    State = State with { Objective = objective };
    return this;
  }
  public GeneticAlgorithmBuilder<TGenotype, TEncodingParameter> WithSelector(ISelectorOperator selector) {
    State = State with { Selector = selector, SelectorReference = null };
    return this;
  }
  public GeneticAlgorithmBuilder<TGenotype, TEncodingParameter> WithSelector(IExecutableOperatorFactory<ISelectorOperator> selectorFactory) {
    State = State with { SelectorReference = selectorFactory, Selector = null };
    return this;
  }
  public GeneticAlgorithmBuilder<TGenotype, TEncodingParameter> WithReplacer(IReplacerOperator replacer) {
    State = State with { Replacer = replacer, ReplacerReference = null };
    return this;
  }
  public GeneticAlgorithmBuilder<TGenotype, TEncodingParameter> WithReplacer(IExecutableOperatorFactory<IReplacerOperator> replacerFactory) {
    State = State with { ReplacerReference = replacerFactory, Replacer = null };
    return this;
  }
  public GeneticAlgorithmBuilder<TGenotype, TEncodingParameter> WithRandomSource(IRandomSource randomSource) {
    State = State with { RandomSource = randomSource };
    return this;
  }
  public GeneticAlgorithmBuilder<TGenotype, TEncodingParameter> WithTerminator(ITerminatorOperator<PopulationState<TGenotype>> terminator) {
    State = State with { Terminator = terminator };
    return this;
  }
  public GeneticAlgorithmBuilder<TGenotype, TEncodingParameter> WithInterceptor(IInterceptor<PopulationState<TGenotype>> interceptor) {
    State = State with { Interceptor = interceptor };
    return this;
  }
}

public static class GeneticAlgorithmBuilderExtensions {
  public static GeneticAlgorithmBuilder<TGenotype> UsingGenotype<TGenotype>(this GeneticAlgorithmBuilder builder) {
     return new GeneticAlgorithmBuilder<TGenotype>(builder);
  }
  
  public static GeneticAlgorithmBuilder<TGenotype, TEncodingParameter> UsingEncodingParameters<TGenotype, TEncodingParameter>(
    this GeneticAlgorithmBuilder<TGenotype> builder) where TEncodingParameter : IEncodingParameter<TGenotype> {
    return new GeneticAlgorithmBuilder<TGenotype, TEncodingParameter>(builder);
  }
  public static GeneticAlgorithmBuilder<TGenotype, TEncodingParameter> UsingEncodingParameters<TGenotype, TEncodingParameter>(
    this GeneticAlgorithmBuilder<TGenotype> builder, TEncodingParameter encodingParameter) where TEncodingParameter : IEncodingParameter<TGenotype> {
    return builder
      .UsingEncodingParameters<TGenotype, TEncodingParameter>()
      .WithEncodingParameter(encodingParameter);
    }
}
