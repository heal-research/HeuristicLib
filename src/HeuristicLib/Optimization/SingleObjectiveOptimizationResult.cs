namespace HEAL.HeuristicLib.Optimization;

public record SingleObjectiveOptimizationResult<TGenotype, TSolutionLayout> : OptimizationResult<TGenotype, TSolutionLayout>, ISingleObjectiveResult<TGenotype>
  where TGenotype : IEquatable<TGenotype>
  where TSolutionLayout : ISolutionLayout<TGenotype> {
  public Solution<TGenotype> BestSolution { get; init; }

  public SingleObjectiveOptimizationResult(TSolutionLayout solutions, Solution<TGenotype> bestSolution)
    : base(solutions) {
    BestSolution = bestSolution;
  }
}
