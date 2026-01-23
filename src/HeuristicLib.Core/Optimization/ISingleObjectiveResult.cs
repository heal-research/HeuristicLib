namespace HEAL.HeuristicLib.Optimization;

public interface ISingleObjectiveResult<out TGenotype> : IOptimizationResult
  where TGenotype : IEquatable<TGenotype>
{
  ISolution<TGenotype> BestSolution { get; }
}
