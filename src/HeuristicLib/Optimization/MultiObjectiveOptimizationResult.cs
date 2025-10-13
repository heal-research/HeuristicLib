namespace HEAL.HeuristicLib.Optimization;

public record MultiObjectiveOptimizationResult<TGenotype, TSolutionLayout> : OptimizationResult<TGenotype, TSolutionLayout>, IMultiObjectiveResult<TGenotype>
  where TGenotype : IEquatable<TGenotype>
  where TSolutionLayout : ISolutionLayout<TGenotype> {
  public IReadOnlyList<Solution<TGenotype>> ParetoFront { get; init; }

  public MultiObjectiveOptimizationResult(TSolutionLayout solutions, IReadOnlyList<Solution<TGenotype>> paretoFront)
    : base(solutions) {
    ParetoFront = paretoFront;
  }
}
