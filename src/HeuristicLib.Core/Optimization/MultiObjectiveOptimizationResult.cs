namespace HEAL.HeuristicLib.Optimization;

public record MultiObjectiveOptimizationResult<TGenotype, TISolutionLayout> : OptimizationResult<TGenotype, TISolutionLayout>, IMultiObjectiveResult<TGenotype>
  where TGenotype : IEquatable<TGenotype>
  where TISolutionLayout : IISolutionLayout<TGenotype> {
  public IReadOnlyList<ISolution<TGenotype>> ParetoFront { get; init; }

  public MultiObjectiveOptimizationResult(TISolutionLayout Solutions, IReadOnlyList<ISolution<TGenotype>> paretoFront)
    : base(Solutions) {
    ParetoFront = paretoFront;
  }
}
