namespace HEAL.HeuristicLib.Optimization;

public interface IOptimizationResult<TGenotype, out TSolutionLayout> : IOptimizationResult
  where TGenotype : IEquatable<TGenotype>
  where TSolutionLayout : ISolutionLayout<TGenotype> {
  TSolutionLayout Solutions { get; }
}

public abstract record OptimizationResult<TGenotype, TSolutionLayout>(TSolutionLayout Solutions) : IOptimizationResult<TGenotype, TSolutionLayout>
  where TGenotype : IEquatable<TGenotype>
  where TSolutionLayout : ISolutionLayout<TGenotype>;
