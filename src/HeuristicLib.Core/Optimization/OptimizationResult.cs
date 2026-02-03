namespace HEAL.HeuristicLib.Optimization;

public interface IOptimizationResult<TGenotype, out TISolutionLayout> : IOptimizationResult
  where TGenotype : IEquatable<TGenotype>
  where TISolutionLayout : IISolutionLayout<TGenotype>
{
  TISolutionLayout Solutions { get; }
}

public abstract record OptimizationResult<TGenotype, TISolutionLayout>(TISolutionLayout Solutions) : IOptimizationResult<TGenotype, TISolutionLayout>
  where TGenotype : IEquatable<TGenotype>
  where TISolutionLayout : IISolutionLayout<TGenotype>;
