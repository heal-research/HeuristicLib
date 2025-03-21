using HEAL.HeuristicLib.Encodings;
using HEAL.HeuristicLib.Operators;

namespace HEAL.HeuristicLib.Configuration;

public abstract record OperatorSpec;

public abstract record CreatorSpec : OperatorSpec;
public record RandomPermutationCreatorSpec : CreatorSpec;
public record UniformRealVectorCreatorSpec(double[]? Minimum = null, double[]? Maximum = null) : CreatorSpec;
public record NormalRealVectorCreatorSpec(double[]? Mean = null, double[]? StandardDeviation = null) : CreatorSpec;

public abstract record CrossoverSpec : OperatorSpec;
public record OrderCrossoverSpec : CrossoverSpec;
public record SinglePointRealVectorCrossoverSpec : CrossoverSpec;
public record AlphaBetaBlendRealVectorCrossoverSpec(double? Alpha = null, double? Beta = null) : CrossoverSpec;

public abstract record MutatorSpec : OperatorSpec;
public record SwapMutatorSpec : MutatorSpec;
public record GaussianRealVectorMutatorSpec(double? Rate = null, double? Strength = null) : MutatorSpec;
public record InversionMutatorSpec : MutatorSpec;

public abstract record SelectorSpec : OperatorSpec;
public record RandomSelectorSpec : SelectorSpec;
public record TournamentSelectorSpec(int? TournamentSize = null) : SelectorSpec;
public record ProportionalSelectorSpec(bool Windowing = true) : SelectorSpec;

public abstract record ReplacerSpec : OperatorSpec;
public record ElitistReplacerSpec(int? Elites = null) : ReplacerSpec;
public record PlusSelectionReplacerSpec : ReplacerSpec;


public static class OperatorFactoryMapping {
  
  public static ICreator<TGenotype> CreateCreator<TGenotype, TEncodingParameter>(this CreatorSpec creatorSpec, TEncodingParameter encodingParameter) where TEncodingParameter : IEncodingParameter {
    IOperator @operator = (encodingParameter, creatorSpec) switch {
      (PermutationEncodingParameter enc, RandomPermutationCreatorSpec spec) => new RandomPermutationCreator(enc),
      (RealVectorEncodingParameter enc, UniformRealVectorCreatorSpec spec) => new UniformDistributedCreator(spec.Minimum, spec.Maximum, enc),
      (RealVectorEncodingParameter enc, NormalRealVectorCreatorSpec spec) => new NormalDistributedCreator(spec.Mean ?? [0.0], spec.StandardDeviation ?? [1.0], enc),
      _ => throw new ArgumentException($"Unknown creator spec {creatorSpec} for genotype {typeof(TGenotype)}")
    };
    if (@operator is ICreator<TGenotype> creator) return creator;
    throw new InvalidOperationException($"{creatorSpec} is not compatible with Genotype {typeof(TGenotype)}");
  }
  
  public static ICrossover<TGenotype> CreateCrossover<TGenotype, TEncodingParameter>(this CrossoverSpec crossoverSpec, TEncodingParameter encodingParameter) where TEncodingParameter : IEncodingParameter {
    IOperator @operator = (encodingParameter, crossoverSpec) switch {
      (PermutationEncodingParameter enc, OrderCrossoverSpec spec) => new OrderCrossover(),
      (RealVectorEncodingParameter enc, SinglePointRealVectorCrossoverSpec spec) => new SinglePointCrossover(),
      (RealVectorEncodingParameter enc, AlphaBetaBlendRealVectorCrossoverSpec spec) => new AlphaBetaBlendCrossover(spec.Alpha, spec.Beta),
      _ => throw new ArgumentException($"Unknown crossover spec {crossoverSpec} for genotype {typeof(TGenotype)}")
    };
    if (@operator is ICrossover<TGenotype> crossover) return crossover;
    throw new InvalidOperationException($"{crossoverSpec} is not compatible with Genotype {typeof(TGenotype)}");
  }

  public static IMutator<TGenotype> CreateMutator<TGenotype, TEncodingParameter>(this MutatorSpec mutatorSpec, TEncodingParameter encodingParameter) where TEncodingParameter : IEncodingParameter {
    IOperator @operator = (encodingParameter, mutatorSpec) switch {
      (PermutationEncodingParameter enc, SwapMutatorSpec spec) => new SwapMutator(enc),
      (RealVectorEncodingParameter enc, GaussianRealVectorMutatorSpec spec) => new GaussianMutator(spec.Rate ?? 0.1, spec.Strength ?? 0.1, enc),
      (PermutationEncodingParameter enc, InversionMutatorSpec spec) => new InversionMutator(),
      _ => throw new ArgumentException($"Unknown mutator spec {mutatorSpec} for genotype {typeof(TGenotype)}")
    };
    if (@operator is IMutator<TGenotype> mutator) return mutator;
    throw new InvalidOperationException($"{mutatorSpec} is not compatible with Genotype {typeof(TGenotype)}");
  }

  public static ISingleObjectiveSelector CreateSelector(this SelectorSpec selectorSpec) {
    IOperator @operator = selectorSpec switch {
      RandomSelectorSpec => new RandomSelector(),
      TournamentSelectorSpec spec => new TournamentSelector(spec.TournamentSize ?? 2),
      ProportionalSelectorSpec spec => new ProportionalSelector(spec.Windowing),
      _ => throw new ArgumentException($"Unknown selector spec: {selectorSpec}")
    };
    if (@operator is ISingleObjectiveSelector selector) return selector;
    throw new InvalidOperationException($"{selectorSpec} is not compatible with {typeof(ISingleObjectiveSelector)}");
    //throw new InvalidOperationException($"{selectorSpec} is not compatible with Fitness {typeof(TFitness)}, Goal {typeof(TGoal)}");
  }

  public static ISingleObjectiveReplacer CreateReplacer(this ReplacerSpec replacerSpec) {
    IOperator @operator = replacerSpec switch {
      ElitistReplacerSpec spec => new ElitismReplacer(spec.Elites ?? 1),
      PlusSelectionReplacerSpec => new PlusSelectionReplacer(),
      _ => throw new ArgumentException($"Unknown replacer spec: {replacerSpec}")
    };
    if (@operator is ISingleObjectiveReplacer replacer) return replacer;
    throw new InvalidOperationException($"{replacerSpec} is not compatible with {typeof(ISingleObjectiveReplacer)}");
    //throw new InvalidOperationException($"{replacerSpec} is not compatible with Fitness {typeof(TFitness)}, Goal {typeof(TGoal)}");
  }
}
