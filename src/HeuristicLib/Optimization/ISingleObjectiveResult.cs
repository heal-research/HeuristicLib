namespace HEAL.HeuristicLib.Optimization;

public interface ISingleObjectiveResult<TGenotype> : IOptimizationResult
  where TGenotype : IEquatable<TGenotype> {
  Solution<TGenotype> BestSolution { get; }
}
