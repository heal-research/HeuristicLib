namespace HEAL.HeuristicLib.Optimization;

public record MultiObjectiveOptimizationResult<TGenotype, TSolutionLayout> : OptimizationResult<TGenotype, TSolutionLayout>, IMultiObjectiveResult<TGenotype>
  where TGenotype : IEquatable<TGenotype>
  where TSolutionLayout : IISolutionLayout<TGenotype> {
  public IReadOnlyList<ISolution<TGenotype>> ParetoFront { get; init; }

  public MultiObjectiveOptimizationResult(TSolutionLayout Solutions, IReadOnlyList<ISolution<TGenotype>> paretoFront)
    : base(Solutions) {
    ParetoFront = paretoFront;
  }
}
