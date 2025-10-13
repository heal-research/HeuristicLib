namespace HEAL.HeuristicLib.Optimization;

public interface IMultiObjectiveResult<TGenotype> : IOptimizationResult
  where TGenotype : IEquatable<TGenotype> {
  IReadOnlyList<Solution<TGenotype>> ParetoFront { get; }
}
