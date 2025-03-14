using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Encodings;
using HEAL.HeuristicLib.Operators;

namespace HEAL.HeuristicLib.Configuration;

public abstract record OperatorSpec {
}

public abstract record CreatorSpec<TGenotype, TEncoding> : OperatorSpec where TEncoding : IEncoding<TGenotype, TEncoding> {
  public abstract ICreator<TGenotype> Create(TEncoding encoding, IRandomSource randomSource);
}

public record RandomPermutationCreatorSpec : CreatorSpec<Permutation, PermutationEncoding> {
  public override ICreator<Permutation> Create(PermutationEncoding encoding, IRandomSource randomSource) => new RandomPermutationCreator(encoding, randomSource);
}

public record UniformRealVectorCreatorSpec(double[]? Minimum = null, double[]? Maximum = null) : CreatorSpec<RealVector, RealVectorEncoding> {
  public override ICreator<RealVector> Create(RealVectorEncoding encoding, IRandomSource randomSource) => new UniformDistributedCreator(encoding, Minimum != null ? new RealVector(Minimum) : null, Maximum != null ? new RealVector(Maximum) : null, randomSource);
}

public record NormalRealVectorCreatorSpec(double[]? Mean = null, double[]? StandardDeviation = null) : CreatorSpec<RealVector, RealVectorEncoding> {
  public override ICreator<RealVector> Create(RealVectorEncoding encoding, IRandomSource randomSource) => new NormalDistributedCreator(encoding, Mean != null ? new RealVector(Mean) : 0.0, StandardDeviation != null ? new RealVector(StandardDeviation) : 1.0, randomSource);
}

public abstract record CrossoverSpec<TGenotype, TEncoding> : OperatorSpec where TEncoding : IEncoding<TGenotype, TEncoding> {
  public abstract ICrossover<TGenotype> Create(TEncoding encoding, IRandomSource randomSource);
}

public record OrderCrossoverSpec : CrossoverSpec<Permutation, PermutationEncoding> {
  public override ICrossover<Permutation> Create(PermutationEncoding encoding, IRandomSource randomSource) => new OrderCrossover(encoding, randomSource);
}

public record SinglePointRealVectorCrossoverSpec : CrossoverSpec<RealVector, RealVectorEncoding> {
  public override ICrossover<RealVector> Create(RealVectorEncoding encoding, IRandomSource randomSource) => new SinglePointCrossover(encoding, randomSource);
}

public record AlphaBlendRealVectorCrossoverSpec(double? Alpha = null, double? Beta = null) : CrossoverSpec<RealVector, RealVectorEncoding> {
  public override ICrossover<RealVector> Create(RealVectorEncoding encoding, IRandomSource randomSource) => new AlphaBetaBlendCrossover(encoding, Alpha, Beta);
}

public abstract record MutatorSpec<TGenotype, TEncoding> : OperatorSpec {
  public abstract IMutator<TGenotype> Create(TEncoding encoding, IRandomSource randomSource);
}

public record SwapMutatorSpec : MutatorSpec<Permutation, PermutationEncoding> {
  public override IMutator<Permutation> Create(PermutationEncoding encoding, IRandomSource randomSource) => new SwapMutator(encoding, randomSource);
}

public record GaussianRealVectorMutatorSpec(double? Rate = null, double? Strength = null) : MutatorSpec<RealVector, RealVectorEncoding> {
  public override IMutator<RealVector> Create(RealVectorEncoding encoding, IRandomSource randomSource) => new GaussianMutator(encoding, Rate ?? 0.1, Strength ?? 0.1, randomSource);
}

public record InversionMutatorSpec : MutatorSpec<Permutation, PermutationEncoding> {
  public override IMutator<Permutation> Create(PermutationEncoding encoding, IRandomSource randomSource) => new InversionMutator(encoding, randomSource);
}

public abstract record SelectorSpec<TGenotype, TFitness, TGoal> : OperatorSpec {
  public abstract ISelector<TGenotype, TFitness, TGoal> Create(IRandomSource randomSource);
}

public record RandomSelectorSpec<TGenotype, TFitness, TGoal> : SelectorSpec<TGenotype, TFitness, TGoal> {
  public override ISelector<TGenotype, TFitness, TGoal> Create(IRandomSource randomSource) => new RandomSelector<TGenotype, TFitness, TGoal>(randomSource);
}

public record TournamentSelectorSpec<TGenotype>(int? TournamentSize = null) : SelectorSpec<TGenotype, Fitness, Goal> {
  public override ISelector<TGenotype, Fitness, Goal> Create(IRandomSource randomSource) => new TournamentSelector<TGenotype>(TournamentSize ?? 2, randomSource);
}

public record ProportionalSelectorSpec<TGenotype>(bool Windowing = true) : SelectorSpec<TGenotype, Fitness, Goal> {
  public override ISelector<TGenotype, Fitness, Goal> Create(IRandomSource randomSource) => new ProportionalSelector<TGenotype>(randomSource, Windowing);
}

public abstract record ReplacerSpec<TGenotype, TFitness, TGoal> : OperatorSpec {
  public abstract IReplacer<TGenotype, TFitness, TGoal> Create(IRandomSource randomSource);
}

public record ElitistReplacerSpec<TGenotype>(int? Elites = null) : ReplacerSpec<TGenotype, Fitness, Goal> {
  public override IReplacer<TGenotype, Fitness, Goal> Create(IRandomSource randomSource) => new ElitismReplacer<TGenotype>(Elites ?? 1);
}

public record PlusSelectionReplacerSpec<TGenotype> : ReplacerSpec<TGenotype, Fitness, Goal> {
  public override IReplacer<TGenotype, Fitness, Goal> Create(IRandomSource randomSource) => new PlusSelectionReplacer<TGenotype>();
}

