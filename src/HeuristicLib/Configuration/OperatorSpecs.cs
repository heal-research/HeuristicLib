using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Encodings;
using HEAL.HeuristicLib.Operators;

namespace HEAL.HeuristicLib.Configuration;

public abstract record OperatorSpec {
}

public abstract record CreatorSpec<TGenotype, TEncoding> : OperatorSpec where TEncoding : IEncoding<TGenotype, TEncoding> {
  public abstract ICreator<TGenotype, TEncoding> Create(TEncoding encoding, IRandomSource randomSource);
}

public record RandomPermutationCreatorSpec : CreatorSpec<Permutation, PermutationEncoding> {
  public override RandomPermutationCreator Create(PermutationEncoding encoding, IRandomSource randomSource) => new RandomPermutationCreator();
}

public record UniformRealVectorCreatorSpec(double[]? Minimum = null, double[]? Maximum = null) : CreatorSpec<RealVector, RealVectorEncoding> {
  public override UniformDistributedCreator Create(RealVectorEncoding encoding, IRandomSource randomSource) => new UniformDistributedCreator(Minimum != null ? new RealVector(Minimum) : null, Maximum != null ? new RealVector(Maximum) : null);
}

public record NormalRealVectorCreatorSpec(double[]? Mean = null, double[]? StandardDeviation = null) : CreatorSpec<RealVector, RealVectorEncoding> {
  public override NormalDistributedCreator Create(RealVectorEncoding encoding, IRandomSource randomSource) => new NormalDistributedCreator(Mean != null ? new RealVector(Mean) : 0.0, StandardDeviation != null ? new RealVector(StandardDeviation) : 1.0);
}

public abstract record CrossoverSpec<TGenotype, TEncoding> : OperatorSpec where TEncoding : IEncoding<TGenotype, TEncoding> {
  public abstract ICrossover<TGenotype, TEncoding> Create(TEncoding encoding, IRandomSource randomSource);
}

public record OrderCrossoverSpec : CrossoverSpec<Permutation, PermutationEncoding> {
  public override OrderCrossover Create(PermutationEncoding encoding, IRandomSource randomSource) => new OrderCrossover();
}

public record SinglePointRealVectorCrossoverSpec : CrossoverSpec<RealVector, RealVectorEncoding> {
  public override SinglePointCrossover Create(RealVectorEncoding encoding, IRandomSource randomSource) => new SinglePointCrossover();
}

public record AlphaBlendRealVectorCrossoverSpec(double? Alpha = null, double? Beta = null) : CrossoverSpec<RealVector, RealVectorEncoding> {
  public override AlphaBetaBlendCrossover Create(RealVectorEncoding encoding, IRandomSource randomSource) => new AlphaBetaBlendCrossover(Alpha, Beta);
}

public abstract record MutatorSpec<TGenotype, TEncoding> : OperatorSpec where TEncoding : IEncoding<TGenotype, TEncoding>  {
  public abstract IMutator<TGenotype, TEncoding> Create(TEncoding encoding, IRandomSource randomSource);
}

public record SwapMutatorSpec : MutatorSpec<Permutation, PermutationEncoding> {
  public override SwapMutator Create(PermutationEncoding encoding, IRandomSource randomSource) => new SwapMutator();
}

public record GaussianRealVectorMutatorSpec(double? Rate = null, double? Strength = null) : MutatorSpec<RealVector, RealVectorEncoding> {
  public override GaussianMutator Create(RealVectorEncoding encoding, IRandomSource randomSource) => new GaussianMutator(Rate ?? 0.1, Strength ?? 0.1);
}

public record InversionMutatorSpec : MutatorSpec<Permutation, PermutationEncoding> {
  public override InversionMutator Create(PermutationEncoding encoding, IRandomSource randomSource) => new InversionMutator();
}

public abstract record SelectorSpec<TGenotype, TFitness, TGoal> : OperatorSpec {
  public abstract ISelector<TGenotype, TFitness, TGoal> Create(IRandomSource randomSource);
}

public record RandomSelectorSpec<TGenotype, TFitness, TGoal> : SelectorSpec<TGenotype, TFitness, TGoal> {
  public override RandomSelector<TGenotype, TFitness, TGoal> Create(IRandomSource randomSource) => new RandomSelector<TGenotype, TFitness, TGoal>();
}

public record TournamentSelectorSpec<TGenotype>(int? TournamentSize = null) : SelectorSpec<TGenotype, Fitness, Goal> {
  public override TournamentSelector<TGenotype> Create(IRandomSource randomSource) => new TournamentSelector<TGenotype>(TournamentSize ?? 2);
}

public record ProportionalSelectorSpec<TGenotype>(bool Windowing = true) : SelectorSpec<TGenotype, Fitness, Goal> {
  public override ProportionalSelector<TGenotype> Create(IRandomSource randomSource) => new ProportionalSelector<TGenotype>(Windowing);
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

