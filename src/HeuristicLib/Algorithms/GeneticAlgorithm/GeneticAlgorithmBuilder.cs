using FluentValidation;
using HEAL.HeuristicLib.Encodings;
using HEAL.HeuristicLib.Operators;

namespace HEAL.HeuristicLib.Algorithms.GeneticAlgorithm;

public record GeneticAlgorithmBuilderState {
  public int? PopulationSize { get; init; }
  public double? MutationRate { get; init; }
  public ISelector<Fitness, Goal>? Selector { get; init; }
  public IReplacer<Fitness, Goal>? Replacer { get; init; }
  public Goal? Goal { get; init; }
  public IRandomSource? RandomSource { get; init; }

  public GeneticAlgorithmBuilderState() { }
  public GeneticAlgorithmBuilderState(GeneticAlgorithmBuilderState original) {
    PopulationSize = original.PopulationSize;
    MutationRate = original.MutationRate;
    Selector = original.Selector;
    Replacer = original.Replacer;
    Goal = original.Goal;
    RandomSource = original.RandomSource;
  }
}
public record GeneticAlgorithmBuilderState<TGenotype> : GeneticAlgorithmBuilderState {
  public ICreator<TGenotype>? Creator { get; init; }
  public ICrossover<TGenotype>? Crossover { get; init; }
  public IMutator<TGenotype>? Mutator { get; init; }
  public IEvaluator<TGenotype, Fitness>? Evaluator { get; init; }
  public ITerminator<PopulationState<TGenotype, Fitness, Goal>>? Terminator { get; init; }
  public IInterceptor<PopulationState<TGenotype, Fitness, Goal>>? Interceptor { get; init; }

  public GeneticAlgorithmBuilderState() {}
  public GeneticAlgorithmBuilderState(GeneticAlgorithmBuilderState baseState) : base(baseState) {}
  public GeneticAlgorithmBuilderState(GeneticAlgorithmBuilderState<TGenotype> original) : base(original) {
    Creator = original.Creator;
    Crossover = original.Crossover;
    Mutator = original.Mutator;
    Evaluator = original.Evaluator;
    Terminator = original.Terminator;
    Interceptor = original.Interceptor;
  }
}

public record GeneticAlgorithmBuilderState<TGenotype, TEncodingParameter> : GeneticAlgorithmBuilderState<TGenotype> {
  public TEncodingParameter? EncodingParameter { get; init; }
  public Func<TEncodingParameter, ICreator<TGenotype>>? CreatorFactory { get; init; }
  public Func<TEncodingParameter, ICrossover<TGenotype>>? CrossoverFactory { get; init; }
  public Func<TEncodingParameter, IMutator<TGenotype>>? MutatorFactory { get; init; }
  
  public GeneticAlgorithmBuilderState() {}
  public GeneticAlgorithmBuilderState(GeneticAlgorithmBuilderState<TGenotype> baseState) : base(baseState) {}
  public GeneticAlgorithmBuilderState(GeneticAlgorithmBuilderState<TGenotype, TEncodingParameter> original) : base(original) {
    EncodingParameter = original.EncodingParameter;
    CreatorFactory = original.CreatorFactory;
    CrossoverFactory = original.CrossoverFactory;
    MutatorFactory = original.MutatorFactory;
  }
}

public interface IGeneticAlgorithmBuilder<out TSelf> where TSelf : IGeneticAlgorithmBuilder<TSelf> {
  TSelf WithPopulationSize(int populationSize);
  TSelf WithMutationRate(double mutationRate);
  TSelf WithSelector(ISelector<Fitness, Goal> selector);
  TSelf WithReplacer(IReplacer<Fitness, Goal> replacer);
  TSelf WithGoal(Goal goal);
  TSelf WithRandomSource(IRandomSource randomSource);
}

public interface IGeneticAlgorithmBuilder<TGenotype, out TSelf> : IGeneticAlgorithmBuilder<TSelf>, IBuilder<GeneticAlgorithm<TGenotype>> where TSelf : IGeneticAlgorithmBuilder<TGenotype, TSelf> {
  TSelf WithCreator(ICreator<TGenotype> creator);
  TSelf WithCrossover(ICrossover<TGenotype> crossover);
  TSelf WithMutator(IMutator<TGenotype> mutator);
  TSelf WithEvaluator(IEvaluator<TGenotype, Fitness> evaluator);
  TSelf WithTerminator(ITerminator<PopulationState<TGenotype, Fitness, Goal>> terminator);
  TSelf WithInterceptor(IInterceptor<PopulationState<TGenotype, Fitness, Goal>> interceptor);
}

public interface IGeneticAlgorithmBuilder<TGenotype, TEncodingParameter, out TSelf> : IGeneticAlgorithmBuilder<TGenotype, TSelf> where TSelf : IGeneticAlgorithmBuilder<TGenotype, TEncodingParameter, TSelf> {
  TSelf WithEncodingParameter(TEncodingParameter encodingParameter);
  TSelf WithCreator(Func<TEncodingParameter, ICreator<TGenotype>> creatorFactory);
  TSelf WithCrossover(Func<TEncodingParameter, ICrossover<TGenotype>> crossoverFactory);
  TSelf WithMutator(Func<TEncodingParameter, IMutator<TGenotype>> mutatorFactory);
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
  public GeneticAlgorithmBuilder WithSelector(ISelector<Fitness, Goal> selector) {
    State = State with { Selector = selector };
    return this;
  }
  public GeneticAlgorithmBuilder WithReplacer(IReplacer<Fitness, Goal> replacer) {
    State = State with { Replacer = replacer };
    return this;
  }
  public GeneticAlgorithmBuilder WithGoal(Goal goal) {
    State = State with { Goal = goal };
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
    var parametersAreSetValidator = new GeneticAlgorithmBuilderStateValidator<TGenotype>();
    var parametersAreSetValidationResult = parametersAreSetValidator.Validate(State);

    if (!parametersAreSetValidationResult.IsValid) throw new ValidationException(parametersAreSetValidationResult.Errors);
    
    return new GeneticAlgorithm<TGenotype>(
      State.PopulationSize!.Value,
      State.Creator!,
      State.Crossover!,
      State.Mutator!,
      State.MutationRate!.Value,
      State.Evaluator!,
      State.Goal!.Value,
      State.Selector!,
      State.Replacer!,
      State.RandomSource!,
      State.Terminator,
      State.Interceptor
    );
  }

  public GeneticAlgorithmBuilder<TGenotype> WithPopulationSize(int populationSize) {
    State = State with { PopulationSize = populationSize };
    return this;
  }
  public GeneticAlgorithmBuilder<TGenotype> WithCreator(ICreator<TGenotype> creator) {
    State = State with { Creator = creator };
    return this;
  }
  public GeneticAlgorithmBuilder<TGenotype> WithCrossover(ICrossover<TGenotype> crossover) {
    State = State with { Crossover = crossover };
    return this;
  }
  public GeneticAlgorithmBuilder<TGenotype> WithMutator(IMutator<TGenotype> mutator) {
    State = State with { Mutator = mutator };
    return this;
  }
  public GeneticAlgorithmBuilder<TGenotype> WithMutationRate(double mutationRate) {
    State = State with { MutationRate = mutationRate };
    return this;
  }
  public GeneticAlgorithmBuilder<TGenotype> WithEvaluator(IEvaluator<TGenotype, Fitness> evaluator) {
    State = State with { Evaluator = evaluator };
    return this;
  }
  public GeneticAlgorithmBuilder<TGenotype> WithGoal(Goal goal) {
    State = State with { Goal = goal };
    return this;
  }
  public GeneticAlgorithmBuilder<TGenotype> WithSelector(ISelector<Fitness, Goal> selector) {
    State = State with { Selector = selector };
    return this;
  }
  public GeneticAlgorithmBuilder<TGenotype> WithReplacer(IReplacer<Fitness, Goal> replacer) {
    State = State with { Replacer = replacer };
    return this;
  }
  public GeneticAlgorithmBuilder<TGenotype> WithRandomSource(IRandomSource randomSource) {
    State = State with { RandomSource = randomSource };
    return this;
  }
  public GeneticAlgorithmBuilder<TGenotype> WithTerminator(ITerminator<PopulationState<TGenotype, Fitness, Goal>> terminator) {
    State = State with { Terminator = terminator };
    return this;
  }
  public GeneticAlgorithmBuilder<TGenotype> WithInterceptor(IInterceptor<PopulationState<TGenotype, Fitness, Goal>> interceptor) {
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
    RuleFor(x => x.Goal).NotNull().WithMessage("Goal must not be null.");
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
    if (State.EncodingParameter is not null) {
      if (State.CreatorFactory is not null) State = State with { Creator = State.CreatorFactory(State.EncodingParameter) };
      if (State.CrossoverFactory is not null) State = State with { Crossover = State.CrossoverFactory(State.EncodingParameter) };
      if (State.MutatorFactory is not null) State = State with { Mutator = State.MutatorFactory(State.EncodingParameter) };
    }
    
    var parametersAreSetValidator = new GeneticAlgorithmBuilderStateValidator<TGenotype>();
    var parametersAreSetValidationResult = parametersAreSetValidator.Validate(State);

    if (!parametersAreSetValidationResult.IsValid) throw new ValidationException(parametersAreSetValidationResult.Errors);
    
    return new GeneticAlgorithm<TGenotype>(
      State.PopulationSize!.Value,
      State.Creator!,
      State.Crossover!,
      State.Mutator!,
      State.MutationRate!.Value,
      State.Evaluator!,
      State.Goal!.Value,
      State.Selector!,
      State.Replacer!,
      State.RandomSource!,
      State.Terminator,
      State.Interceptor
    );
  }
  
  public GeneticAlgorithmBuilder<TGenotype, TEncodingParameter> WithEncodingParameter(TEncodingParameter encodingParameter) {
    State = State with { EncodingParameter = encodingParameter };
    return this;
  }
  public GeneticAlgorithmBuilder<TGenotype, TEncodingParameter> WithCreator(Func<TEncodingParameter, ICreator<TGenotype>> creatorFactory) {
    State = State with { CreatorFactory = creatorFactory };
    return this;
  }
  public GeneticAlgorithmBuilder<TGenotype, TEncodingParameter> WithCrossover(Func<TEncodingParameter, ICrossover<TGenotype>> crossoverFactory) {
    State = State with { CrossoverFactory = crossoverFactory };
    return this;
  }
  public GeneticAlgorithmBuilder<TGenotype, TEncodingParameter> WithMutator(Func<TEncodingParameter, IMutator<TGenotype>> mutatorFactory) {
    State = State with { MutatorFactory = mutatorFactory };
    return this;
  }
  
  public GeneticAlgorithmBuilder<TGenotype, TEncodingParameter> WithPopulationSize(int populationSize) {
    State = State with { PopulationSize = populationSize };
    return this;
  }
  public GeneticAlgorithmBuilder<TGenotype, TEncodingParameter> WithCreator(ICreator<TGenotype> creator) {
    State = State with { Creator = creator };
    return this;
  }
  public GeneticAlgorithmBuilder<TGenotype, TEncodingParameter> WithCrossover(ICrossover<TGenotype> crossover) {
    State = State with { Crossover = crossover };
    return this;
  }
  public GeneticAlgorithmBuilder<TGenotype, TEncodingParameter> WithMutator(IMutator<TGenotype> mutator) {
    State = State with { Mutator = mutator };
    return this;
  }
  public GeneticAlgorithmBuilder<TGenotype, TEncodingParameter> WithMutationRate(double mutationRate) {
    State = State with { MutationRate = mutationRate };
    return this;
  }
  public GeneticAlgorithmBuilder<TGenotype, TEncodingParameter> WithEvaluator(IEvaluator<TGenotype, Fitness> evaluator) {
    State = State with { Evaluator = evaluator };
    return this;
  }
  public GeneticAlgorithmBuilder<TGenotype, TEncodingParameter> WithGoal(Goal goal) {
    State = State with { Goal = goal };
    return this;
  }
  public GeneticAlgorithmBuilder<TGenotype, TEncodingParameter> WithSelector(ISelector<Fitness, Goal> selector) {
    State = State with { Selector = selector };
    return this;
  }
  public GeneticAlgorithmBuilder<TGenotype, TEncodingParameter> WithReplacer(IReplacer<Fitness, Goal> replacer) {
    State = State with { Replacer = replacer };
    return this;
  }
  public GeneticAlgorithmBuilder<TGenotype, TEncodingParameter> WithRandomSource(IRandomSource randomSource) {
    State = State with { RandomSource = randomSource };
    return this;
  }
  public GeneticAlgorithmBuilder<TGenotype, TEncodingParameter> WithTerminator(ITerminator<PopulationState<TGenotype, Fitness, Goal>> terminator) {
    State = State with { Terminator = terminator };
    return this;
  }
  public GeneticAlgorithmBuilder<TGenotype, TEncodingParameter> WithInterceptor(IInterceptor<PopulationState<TGenotype, Fitness, Goal>> interceptor) {
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
