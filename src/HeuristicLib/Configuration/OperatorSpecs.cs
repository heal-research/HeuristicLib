namespace HEAL.HeuristicLib.Configuration;

public abstract record OperatorSpec();

public abstract record CreatorSpec() : OperatorSpec();
public record RandomPermutationCreatorSpec : CreatorSpec;
public record UniformRealVectorCreatorSpec(double[]? Minimum = null, double[]? Maximum = null) : CreatorSpec;
public record NormalRealVectorCreatorSpec(double[]? Mean = null, double[]? StandardDeviation = null) : CreatorSpec;

public abstract record CrossoverSpec() : OperatorSpec();
public record OrderCrossoverSpec : CrossoverSpec;
public record SinglePointRealVectorCrossoverSpec : CrossoverSpec;
public record AlphaBlendRealVectorCrossoverSpec(double? Alpha = null, double? Beta = null) : CrossoverSpec;

public abstract record MutatorSpec(): OperatorSpec();
public record SwapMutatorSpec : MutatorSpec;
public record GaussianRealVectorMutatorSpec(double? Rate = null, double? Strength = null) : MutatorSpec;
public record InversionMutatorSpec() : MutatorSpec;

public abstract record SelectorSpec(): OperatorSpec();
public record RandomSelectorSpec : SelectorSpec;
public record TournamentSelectorSpec(int? TournamentSize = null) : SelectorSpec;
public record ProportionalSelectorSpec(bool Windowing = true) : SelectorSpec;

public abstract record ReplacerSpec(): OperatorSpec();
public record ElitistReplacerSpec(int? Elites = null) : ReplacerSpec;
public record PlusSelectionReplacerSpec : ReplacerSpec;

