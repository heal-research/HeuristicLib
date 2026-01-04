namespace HEAL.HeuristicLib.Optimization;

public record SingleObjectiveOptimizationResult<TGenotype, TSolutionLayout> : OptimizationResult<TGenotype, TSolutionLayout>, ISingleObjectiveResult<TGenotype>
  where TGenotype : IEquatable<TGenotype>
  where TSolutionLayout : IISolutionLayout<TGenotype> {
  public ISolution<TGenotype> BestSolution { get; init; }

  public SingleObjectiveOptimizationResult(TSolutionLayout Solutions, ISolution<TGenotype> bestSolution)
    : base(Solutions) {
    BestSolution = bestSolution;
  }
}
