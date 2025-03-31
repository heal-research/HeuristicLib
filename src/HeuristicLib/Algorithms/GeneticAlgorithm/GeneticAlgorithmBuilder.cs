using FluentValidation;
using HEAL.HeuristicLib.Core;
using HEAL.HeuristicLib.Encodings;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Algorithms.GeneticAlgorithm;

public record GeneticAlgorithmBuilderState {
  public int? PopulationSize { get; init; }
  public double? MutationRate { get; init; }
  public ISelector? Selector { get; init; }
  public IReplacer? Replacer { get; init; }
  public Objective? Objective { get; init; }
  public IRandomSource? RandomSource { get; init; }

  public GeneticAlgorithmBuilderState() { }
  public GeneticAlgorithmBuilderState(GeneticAlgorithmBuilderState original) {
    PopulationSize = original.PopulationSize;
    MutationRate = original.MutationRate;
    Selector = original.Selector;
    Replacer = original.Replacer;
    Objective = original.Objective;
    RandomSource = original.RandomSource;
  }
}
public record GeneticAlgorithmBuilderState<TGenotype, TPhenotype, TEncoding> : GeneticAlgorithmBuilderState
  where TEncoding : IEncoding<TGenotype>
{
  public TEncoding? Encoding { get; init; }
  public ICreator<TGenotype,TEncoding>? Creator { get; init; }
  public ICrossover<TGenotype,TEncoding>? Crossover { get; init; }
  public IMutator<TGenotype, TEncoding>? Mutator { get; init; }
  public IDecoder<TGenotype, TPhenotype>? Decoder { get; init; }
  public IEvaluator<TPhenotype>? Evaluator { get; init; }
  public IInterceptor<EvolutionResult<TGenotype, TPhenotype>>? Interceptor { get; init; }
  //public ITerminator<PopulationState<TGenotype>>? Terminator { get; init; }

  public GeneticAlgorithmBuilderState() {}
  public GeneticAlgorithmBuilderState(GeneticAlgorithmBuilderState baseState) : base(baseState) {}
  public GeneticAlgorithmBuilderState(GeneticAlgorithmBuilderState<TGenotype, TPhenotype, TEncoding> original) : base(original) {
    Encoding = original.Encoding;
    Creator = original.Creator;
    Crossover = original.Crossover;
    Mutator = original.Mutator;
    Decoder = original.Decoder;
    Evaluator = original.Evaluator;
    Interceptor = original.Interceptor;
    //Terminator = original.Terminator;
  }
}

public interface IGeneticAlgorithmBuilder<out TSelf> where TSelf : IGeneticAlgorithmBuilder<TSelf> {
  TSelf WithPopulationSize(int populationSize);
  TSelf WithMutationRate(double mutationRate);
  TSelf WithSelector(ISelector selector);
  TSelf WithReplacer(IReplacer replacer);
  TSelf WithObjective(Objective objective);
  TSelf WithRandomSource(IRandomSource randomSource);
}

public interface IGeneticAlgorithmBuilder<TGenotype, TPhenotype, TEncoding, out TSelf>
  : IGeneticAlgorithmBuilder<TSelf>,
    IBuilder<GeneticAlgorithm<TGenotype, TPhenotype, TEncoding>>
  where TSelf : IGeneticAlgorithmBuilder<TGenotype, TPhenotype, TEncoding, TSelf>
  where TEncoding : IEncoding<TGenotype>
{
  TSelf WithEncoding(TEncoding encoding);
  TSelf WithCreator(ICreator<TGenotype, TEncoding> creator);
  TSelf WithCrossover(ICrossover<TGenotype, TEncoding> crossover);
  TSelf WithMutator(IMutator<TGenotype, TEncoding> mutator);
  TSelf WithDecoder(IDecoder<TGenotype, TPhenotype> decoder);
  TSelf WithEvaluator(IEvaluator<TPhenotype> evaluator);
  TSelf WithInterceptor(IInterceptor<EvolutionResult<TGenotype, TPhenotype>> interceptor);
  //TSelf WithTerminator(ITerminator<PopulationState<TGenotype>> terminator);
}

public class GeneticAlgorithmBuilder : IGeneticAlgorithmBuilder<GeneticAlgorithmBuilder> {
  public GeneticAlgorithmBuilderState State { get; private set; }
  
  public GeneticAlgorithmBuilder() {
    State = new GeneticAlgorithmBuilderState();
  }
  
  public GeneticAlgorithmBuilder(GeneticAlgorithmBuilder original) {
    State = new GeneticAlgorithmBuilderState(original.State);
  }
  
  internal GeneticAlgorithmBuilder(GeneticAlgorithmBuilderState state) {
    State = state;
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
  public GeneticAlgorithmBuilder WithRandomSource(IRandomSource randomSource) {
    State = State with { RandomSource = randomSource };
    return this;
  }

  public GeneticAlgorithmBuilder<TGenotype, TPhenotype, TEncoding> FromAlgorithm<TGenotype, TPhenotype, TEncoding>(GeneticAlgorithm<TGenotype, TPhenotype, TEncoding> algorithm) 
    where TEncoding : IEncoding<TGenotype>
  {
    var state = new GeneticAlgorithmBuilderState<TGenotype, TPhenotype, TEncoding> {
      Encoding = algorithm.Encoding,
      PopulationSize = algorithm.PopulationSize,
      Creator = algorithm.Creator,
      Crossover = algorithm.Crossover,
      Mutator = algorithm.Mutator,
      MutationRate = algorithm.MutationRate,
      Decoder = algorithm.Decoder,
      Evaluator = algorithm.Evaluator,
      Objective = algorithm.Objective,
      Selector = algorithm.Selector,
      Replacer = algorithm.Replacer,
      RandomSource = algorithm.RandomSource
    };

    return new GeneticAlgorithmBuilder<TGenotype, TPhenotype, TEncoding>(state);
  }
}

public class GeneticAlgorithmBuilder<TGenotype, TPhenotype, TEncoding>
  : IGeneticAlgorithmBuilder<TGenotype, TPhenotype, TEncoding, GeneticAlgorithmBuilder<TGenotype, TPhenotype, TEncoding>>
  where TEncoding : IEncoding<TGenotype>
{
  public GeneticAlgorithmBuilderState<TGenotype, TPhenotype, TEncoding> State { get; private set; }
  
  public GeneticAlgorithmBuilder() {
    State = new GeneticAlgorithmBuilderState<TGenotype, TPhenotype, TEncoding>();
  }
  
  public GeneticAlgorithmBuilder(GeneticAlgorithmBuilder baseBuilder) {
    State = new GeneticAlgorithmBuilderState<TGenotype, TPhenotype, TEncoding>(baseBuilder.State);
  }
  
  public GeneticAlgorithmBuilder(GeneticAlgorithmBuilder<TGenotype, TPhenotype, TEncoding> original) {
    State = new GeneticAlgorithmBuilderState<TGenotype, TPhenotype, TEncoding>(original.State);
  }
  
  internal GeneticAlgorithmBuilder(GeneticAlgorithmBuilderState<TGenotype, TPhenotype, TEncoding> state) {
    State = state;
  }

  public GeneticAlgorithm<TGenotype, TPhenotype, TEncoding> Build() {
    var resolvedState = new GeneticAlgorithmBuilderState<TGenotype, TPhenotype, TEncoding> {
      Encoding = State.Encoding,
      PopulationSize = State.PopulationSize,
      Creator = State.Creator,
      Crossover = State.Crossover,
      Mutator = State.Mutator,
      MutationRate = State.MutationRate,
      Decoder = State.Decoder,
      Evaluator = State.Evaluator,
      Objective = State.Objective,
      Selector = State.Selector,
      Replacer = State.Replacer,
      RandomSource = State.RandomSource,
      Interceptor = State.Interceptor
      //Terminator = State.Terminator,
    };
    var parametersAreSetValidator = new GeneticAlgorithmBuilderStateValidator<TGenotype, TPhenotype, TEncoding>();
    var parametersAreSetValidationResult = parametersAreSetValidator.Validate(resolvedState);

    if (!parametersAreSetValidationResult.IsValid) throw new ValidationException(parametersAreSetValidationResult.Errors);
    
    var ga = new GeneticAlgorithm<TGenotype, TPhenotype, TEncoding> {
      Encoding = resolvedState.Encoding!,
      PopulationSize = resolvedState.PopulationSize!.Value,
      Creator = resolvedState.Creator!,
      Crossover = resolvedState.Crossover!,
      Mutator = resolvedState.Mutator!,
      MutationRate = resolvedState.MutationRate!.Value,
      Decoder = resolvedState.Decoder!,
      Evaluator = resolvedState.Evaluator!,
      Objective = resolvedState.Objective!,
      Selector = resolvedState.Selector!,
      Replacer = resolvedState.Replacer!,
      RandomSource = resolvedState.RandomSource!
      //resolvedState.Terminator,
      //Interceptor = resolvedState.Interceptor
    };
    
    if (resolvedState.Interceptor is not null) {
      ga = ga with { Interceptor = resolvedState.Interceptor };
    }

    return ga;
  }

  public GeneticAlgorithmBuilder<TGenotype, TPhenotype, TEncoding> WithEncoding(TEncoding encoding) {
    State = State with { Encoding = encoding };
    return this;
  }
  public GeneticAlgorithmBuilder<TGenotype, TPhenotype, TEncoding> WithPopulationSize(int populationSize) {
    State = State with { PopulationSize = populationSize };
    return this;
  }
  public GeneticAlgorithmBuilder<TGenotype, TPhenotype, TEncoding> WithCreator(ICreator<TGenotype, TEncoding> creator) {
    State = State with { Creator = creator };
    return this;
  }
  public GeneticAlgorithmBuilder<TGenotype, TPhenotype, TEncoding> WithCrossover(ICrossover<TGenotype, TEncoding> crossover) {
    State = State with { Crossover = crossover };
    return this;
  }
  public GeneticAlgorithmBuilder<TGenotype, TPhenotype, TEncoding> WithMutator(IMutator<TGenotype, TEncoding> mutator) {
    State = State with { Mutator = mutator };
    return this;
  }
  public GeneticAlgorithmBuilder<TGenotype, TPhenotype, TEncoding> WithMutationRate(double mutationRate) {
    State = State with { MutationRate = mutationRate };
    return this;
  }
  public GeneticAlgorithmBuilder<TGenotype, TPhenotype, TEncoding> WithDecoder(IDecoder<TGenotype, TPhenotype> decoder) {
    State = State with { Decoder = decoder };
    return this;
  }
  public GeneticAlgorithmBuilder<TGenotype, TPhenotype, TEncoding> WithEvaluator(IEvaluator<TPhenotype> evaluator) {
    State = State with { Evaluator = evaluator };
    return this;
  }
  public GeneticAlgorithmBuilder<TGenotype, TPhenotype, TEncoding> WithObjective(Objective objective) {
    State = State with { Objective = objective };
    return this;
  }
  public GeneticAlgorithmBuilder<TGenotype, TPhenotype, TEncoding> WithSelector(ISelector selector) {
    State = State with { Selector = selector };
    return this;
  }
  public GeneticAlgorithmBuilder<TGenotype, TPhenotype, TEncoding> WithReplacer(IReplacer replacer) {
    State = State with { Replacer = replacer };
    return this;
  }
  public GeneticAlgorithmBuilder<TGenotype, TPhenotype, TEncoding> WithRandomSource(IRandomSource randomSource) {
    State = State with { RandomSource = randomSource };
    return this;
  }
  // public GeneticAlgorithmBuilder<TGenotype> WithTerminator(ITerminator<PopulationState<TGenotype>> terminator) {
  //   State = State with { Terminator = terminator };
  //   return this;
  // }
  public GeneticAlgorithmBuilder<TGenotype, TPhenotype, TEncoding> WithInterceptor(IInterceptor<EvolutionResult<TGenotype, TPhenotype>> interceptor) {
    State = State with { Interceptor = interceptor };
    return this;
  }
}

internal sealed class GeneticAlgorithmBuilderStateValidator<TGenotype, TPhenotype, TEncoding> 
  : AbstractValidator<GeneticAlgorithmBuilderState<TGenotype, TPhenotype, TEncoding>>
  where TEncoding : IEncoding<TGenotype>
{
  public GeneticAlgorithmBuilderStateValidator() {
    RuleFor(x => x.Encoding).NotNull().WithMessage("Encoding parameter must not be null.");
    RuleFor(x => x.PopulationSize).NotNull().WithMessage("Population size must not be null.");
    RuleFor(x => x.Creator).NotNull().WithMessage("Creator must not be null.");
    RuleFor(x => x.Crossover).NotNull().WithMessage("Crossover must not be null.");
    RuleFor(x => x.Mutator).NotNull().WithMessage("Mutator must not be null.");
    RuleFor(x => x.MutationRate).NotNull().WithMessage("Mutation rate must not be null.");
    RuleFor(x => x.Decoder).NotNull().WithMessage("Decoder must not be null.");
    RuleFor(x => x.Evaluator).NotNull().WithMessage("Evaluator must not be null.");
    RuleFor(x => x.Objective).NotNull().WithMessage("Objective must not be null.");
    RuleFor(x => x.Selector).NotNull().WithMessage("Selector must not be null.");
    RuleFor(x => x.Replacer).NotNull().WithMessage("Replacer must not be null.");
    RuleFor(x => x.RandomSource).NotNull().WithMessage("Random source must not be null.");
  }
}

public static class GeneticAlgorithmBuilderExtensions {
  
  public static GeneticAlgorithmBuilder<TGenotype, TPhenotype, TEncoding> UsingAlgorithm<TGenotype, TPhenotype, TEncoding>(
    this GeneticAlgorithmBuilder builder,
    GeneticAlgorithm<TGenotype, TPhenotype, TEncoding> algorithm
  )
    where TEncoding : IEncoding<TGenotype>
  {
    return new GeneticAlgorithmBuilder<TGenotype, TPhenotype, TEncoding>(builder)
      .WithPopulationSize(algorithm.PopulationSize)
      .WithMutationRate(algorithm.MutationRate)
      .WithSelector(algorithm.Selector)
      .WithReplacer(algorithm.Replacer)
      .WithObjective(algorithm.Objective)
      .WithRandomSource(algorithm.RandomSource);
  }
  
  public static GeneticAlgorithmBuilder<TGenotype, TPhenotype, TEncoding> UsingEncodingType<TGenotype, TPhenotype, TEncoding>(this GeneticAlgorithmBuilder builder) 
    where TEncoding : IEncoding<TGenotype>
  {
     return new GeneticAlgorithmBuilder<TGenotype, TPhenotype, TEncoding>(builder);
  }
  
  // public static GeneticAlgorithmBuilder<TGenotype, TPhenotype, TEncoding> UsingEncoding<TGenotype, TPhenotype, TEncoding>(
  //   this GeneticAlgorithmBuilder builder, TEncoding encoding)
  //   where TEncoding : IEncoding<TGenotype> 
  // {
  //   return builder
  //     .UsingEncodingType<TGenotype, TPhenotype, TEncoding>()
  //     .UsingEncoding(encoding);
  // }
  // public static GeneticAlgorithmBuilder<TGenotype, TPhenotype, TEncoding> UsingEncoding<TGenotype, TPhenotype, TEncoding>(
  //   this GeneticAlgorithmBuilder<TGenotype, TPhenotype, TEncoding> builder, TEncoding encoding)
  //   where TEncoding : IEncoding<TGenotype> 
  // {
  //   return builder
  //     .WithEncoding(encoding);
  // }
}
