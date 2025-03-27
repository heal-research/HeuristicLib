using FluentValidation;
using HEAL.HeuristicLib.Core;
using HEAL.HeuristicLib.Encodings;
using HEAL.HeuristicLib.Operators;

namespace HEAL.HeuristicLib.Algorithms.GeneticAlgorithm;

public record GeneticAlgorithmBuilderState {
  public int? PopulationSize { get; init; }
  public double? MutationRate { get; init; }
  public ISelector? Selector { get; init; }
  public IReplacer? Replacer { get; init; }
  public Objective? Objective { get; init; }
  //public IRandomSource? RandomSource { get; init; }

  public GeneticAlgorithmBuilderState() { }
  public GeneticAlgorithmBuilderState(GeneticAlgorithmBuilderState original) {
    PopulationSize = original.PopulationSize;
    MutationRate = original.MutationRate;
    Selector = original.Selector;
    Replacer = original.Replacer;
    Objective = original.Objective;
    //RandomSource = original.RandomSource;
  }
}
public record GeneticAlgorithmBuilderState<TGenotype, TPhenotype, TEncodingParameter> : GeneticAlgorithmBuilderState
  where TEncodingParameter : IEncodingParameter<TGenotype>
{
  public TEncodingParameter? EncodingParameter { get; init; }
  public ICreator<TGenotype,TEncodingParameter>? Creator { get; init; }
  public ICrossover<TGenotype,TEncodingParameter>? Crossover { get; init; }
  public IMutator<TGenotype,TEncodingParameter>? Mutator { get; init; }
  public IEvaluator<TGenotype, TPhenotype>? Evaluator { get; init; }
  //public ITerminator<PopulationState<TGenotype>>? Terminator { get; init; }
  public IInterceptor<EvolutionResult<TGenotype, TPhenotype>>? Interceptor { get; init; }

  public GeneticAlgorithmBuilderState() {}
  public GeneticAlgorithmBuilderState(GeneticAlgorithmBuilderState baseState) : base(baseState) {}
  public GeneticAlgorithmBuilderState(GeneticAlgorithmBuilderState<TGenotype, TPhenotype, TEncodingParameter> original) : base(original) {
    EncodingParameter = original.EncodingParameter;
    Creator = original.Creator;
    Crossover = original.Crossover;
    Mutator = original.Mutator;
    Evaluator = original.Evaluator;
    //Terminator = original.Terminator;
    Interceptor = original.Interceptor;
  }
}

public interface IGeneticAlgorithmBuilder<out TSelf> where TSelf : IGeneticAlgorithmBuilder<TSelf> {
  TSelf WithPopulationSize(int populationSize);
  TSelf WithMutationRate(double mutationRate);
  TSelf WithSelector(ISelector selector);
  TSelf WithReplacer(IReplacer replacer);
  TSelf WithObjective(Objective objective);
  //TSelf WithRandomSource(IRandomSource randomSource);
}

public interface IGeneticAlgorithmBuilder<TGenotype, TPhenotype, TEncodingParameter, out TSelf>
  : IGeneticAlgorithmBuilder<TSelf>,
    IBuilder<GeneticAlgorithm<TGenotype, TPhenotype, TEncodingParameter>>
  where TSelf : IGeneticAlgorithmBuilder<TGenotype, TPhenotype, TEncodingParameter, TSelf>
  where TEncodingParameter : IEncodingParameter<TGenotype>
{
  TSelf WithEncodingParameter(TEncodingParameter encodingParameter);
  TSelf WithCreator(ICreator<TGenotype, TEncodingParameter> creator);
  TSelf WithCrossover(ICrossover<TGenotype, TEncodingParameter> crossover);
  TSelf WithMutator(IMutator<TGenotype, TEncodingParameter> mutator);
  TSelf WithEvaluator(IEvaluator<TGenotype, TPhenotype> evaluator);
  //TSelf WithTerminator(ITerminator<PopulationState<TGenotype>> terminator);
  TSelf WithInterceptor(IInterceptor<EvolutionResult<TGenotype, TPhenotype>> interceptor);
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
  public GeneticAlgorithmBuilder WithSelector(ISelector selector) {
    State = State with { Selector = selector };
    return this;
  }
  public GeneticAlgorithmBuilder WithReplacer(IReplacer replacer) {
    State = State with { Replacer = replacer };
    return this;
  }
  public GeneticAlgorithmBuilder WithObjective(Objective objective) {
    State = State with { Objective = objective };
    return this;
  }
}

public class GeneticAlgorithmBuilder<TGenotype, TPhenotype, TEncodingParameter>
  : IGeneticAlgorithmBuilder<TGenotype, TPhenotype, TEncodingParameter, GeneticAlgorithmBuilder<TGenotype, TPhenotype, TEncodingParameter>>
  where TEncodingParameter : IEncodingParameter<TGenotype>
{
  public GeneticAlgorithmBuilderState<TGenotype, TPhenotype, TEncodingParameter> State { get; private set; }
  
  public GeneticAlgorithmBuilder() {
    State = new GeneticAlgorithmBuilderState<TGenotype, TPhenotype, TEncodingParameter>();
  }
  public GeneticAlgorithmBuilder(GeneticAlgorithmBuilder baseBuilder) {
    State = new GeneticAlgorithmBuilderState<TGenotype, TPhenotype, TEncodingParameter>(baseBuilder.State);
  }
  
  public GeneticAlgorithmBuilder(GeneticAlgorithmBuilder<TGenotype, TPhenotype, TEncodingParameter> original) {
    State = new GeneticAlgorithmBuilderState<TGenotype, TPhenotype, TEncodingParameter>(original.State);
  }
  
  public GeneticAlgorithm<TGenotype, TPhenotype, TEncodingParameter> Build() {
    var resolvedState = new GeneticAlgorithmBuilderState<TGenotype, TPhenotype, TEncodingParameter> {
      EncodingParameter = State.EncodingParameter,
      PopulationSize = State.PopulationSize,
      Creator = State.Creator,
      Crossover = State.Crossover,
      Mutator = State.Mutator,
      MutationRate = State.MutationRate,
      Evaluator = State.Evaluator,
      Objective = State.Objective,
      Selector = State.Selector,
      Replacer = State.Replacer,
      //RandomSource = State.RandomSource,
      //Terminator = State.Terminator,
      Interceptor = State.Interceptor
    };
    
    var parametersAreSetValidator = new GeneticAlgorithmBuilderStateValidator<TGenotype, TPhenotype, TEncodingParameter>();
    var parametersAreSetValidationResult = parametersAreSetValidator.Validate(resolvedState);

    if (!parametersAreSetValidationResult.IsValid) throw new ValidationException(parametersAreSetValidationResult.Errors);
    
    return new GeneticAlgorithm<TGenotype, TPhenotype, TEncodingParameter>(
      resolvedState.EncodingParameter!,
      resolvedState.PopulationSize!.Value,
      resolvedState.Creator!,
      resolvedState.Crossover!,
      resolvedState.Mutator!,
      resolvedState.MutationRate!.Value,
      resolvedState.Evaluator!,
      resolvedState.Objective!,
      resolvedState.Selector!,
      resolvedState.Replacer!,
      //resolvedState.RandomSource!,
      //resolvedState.Terminator,
      resolvedState.Interceptor
    );
  }

  public GeneticAlgorithmBuilder<TGenotype, TPhenotype, TEncodingParameter> WithEncodingParameter(TEncodingParameter encodingParameter) {
    State = State with { EncodingParameter = encodingParameter };
    return this;
  }
  public GeneticAlgorithmBuilder<TGenotype, TPhenotype, TEncodingParameter> WithPopulationSize(int populationSize) {
    State = State with { PopulationSize = populationSize };
    return this;
  }
  public GeneticAlgorithmBuilder<TGenotype, TPhenotype, TEncodingParameter> WithCreator(ICreator<TGenotype, TEncodingParameter> creator) {
    State = State with { Creator = creator };
    return this;
  }
  public GeneticAlgorithmBuilder<TGenotype, TPhenotype, TEncodingParameter> WithCrossover(ICrossover<TGenotype, TEncodingParameter> crossover) {
    State = State with { Crossover = crossover };
    return this;
  }
  public GeneticAlgorithmBuilder<TGenotype, TPhenotype, TEncodingParameter> WithMutator(IMutator<TGenotype, TEncodingParameter> mutator) {
    State = State with { Mutator = mutator };
    return this;
  }
  public GeneticAlgorithmBuilder<TGenotype, TPhenotype, TEncodingParameter> WithMutationRate(double mutationRate) {
    State = State with { MutationRate = mutationRate };
    return this;
  }
  public GeneticAlgorithmBuilder<TGenotype, TPhenotype, TEncodingParameter> WithEvaluator(IEvaluator<TGenotype, TPhenotype> evaluator) {
    State = State with { Evaluator = evaluator };
    return this;
  }
  public GeneticAlgorithmBuilder<TGenotype, TPhenotype, TEncodingParameter> WithObjective(Objective objective) {
    State = State with { Objective = objective };
    return this;
  }
  public GeneticAlgorithmBuilder<TGenotype, TPhenotype, TEncodingParameter> WithSelector(ISelector selector) {
    State = State with { Selector = selector };
    return this;
  }
  public GeneticAlgorithmBuilder<TGenotype, TPhenotype, TEncodingParameter> WithReplacer(IReplacer replacer) {
    State = State with { Replacer = replacer };
    return this;
  }
  // public GeneticAlgorithmBuilder<TGenotype> WithRandomSource(IRandomSource randomSource) {
  //   State = State with { RandomSource = randomSource };
  //   return this;
  // }
  // public GeneticAlgorithmBuilder<TGenotype> WithTerminator(ITerminator<PopulationState<TGenotype>> terminator) {
  //   State = State with { Terminator = terminator };
  //   return this;
  // }
  public GeneticAlgorithmBuilder<TGenotype, TPhenotype, TEncodingParameter> WithInterceptor(IInterceptor<EvolutionResult<TGenotype, TPhenotype>> interceptor) {
    State = State with { Interceptor = interceptor };
    return this;
  }
}

internal sealed class GeneticAlgorithmBuilderStateValidator<TGenotype, TPhenotype, TEncodingParameter> 
  : AbstractValidator<GeneticAlgorithmBuilderState<TGenotype, TPhenotype, TEncodingParameter>>
  where TEncodingParameter : IEncodingParameter<TGenotype>
{
  public GeneticAlgorithmBuilderStateValidator() {
    RuleFor(x => x.EncodingParameter).NotNull().WithMessage("Encoding parameter must not be null.");
    RuleFor(x => x.PopulationSize).NotNull().WithMessage("Population size must not be null.");
    RuleFor(x => x.Creator).NotNull().WithMessage("Creator must not be null.");
    RuleFor(x => x.Crossover).NotNull().WithMessage("Crossover must not be null.");
    RuleFor(x => x.Mutator).NotNull().WithMessage("Mutator must not be null.");
    RuleFor(x => x.MutationRate).NotNull().WithMessage("Mutation rate must not be null.");
    RuleFor(x => x.Evaluator).NotNull().WithMessage("Evaluator must not be null.");
    RuleFor(x => x.Objective).NotNull().WithMessage("Objective must not be null.");
    RuleFor(x => x.Selector).NotNull().WithMessage("Selector must not be null.");
    RuleFor(x => x.Replacer).NotNull().WithMessage("Replacer must not be null.");
    //RuleFor(x => x.RandomSource).NotNull().WithMessage("Random source must not be null.");
  }
}

public static class GeneticAlgorithmBuilderExtensions {
  public static GeneticAlgorithmBuilder<TGenotype, TPhenotype, TEncodingParameter> UsingGenotype<TGenotype, TPhenotype, TEncodingParameter>(this GeneticAlgorithmBuilder builder) 
    where TEncodingParameter : IEncodingParameter<TGenotype>
  {
     return new GeneticAlgorithmBuilder<TGenotype, TPhenotype, TEncodingParameter>(builder);
  }
  
  public static GeneticAlgorithmBuilder<TGenotype, TPhenotype, TEncodingParameter> UsingEncodingParameters<TGenotype, TPhenotype, TEncodingParameter>(
    this GeneticAlgorithmBuilder builder) where TEncodingParameter : IEncodingParameter<TGenotype> {
    return new GeneticAlgorithmBuilder<TGenotype, TPhenotype, TEncodingParameter>(builder);
  }
  public static GeneticAlgorithmBuilder<TGenotype, TPhenotype, TEncodingParameter> UsingEncodingParameters<TGenotype, TPhenotype, TEncodingParameter>(
    this GeneticAlgorithmBuilder builder, TEncodingParameter encodingParameter) where TEncodingParameter : IEncodingParameter<TGenotype> {
    return builder
      .UsingEncodingParameters<TGenotype,TPhenotype, TEncodingParameter>()
      .WithEncodingParameter(encodingParameter);
    }
}
