namespace HEAL.HeuristicLib.Optimization;

public record SingleObjectiveOptimizationResult<TGenotype, TISolutionLayout> : OptimizationResult<TGenotype, TISolutionLayout>, ISingleObjectiveResult<TGenotype>
  where TGenotype : IEquatable<TGenotype>
  where TISolutionLayout : IISolutionLayout<TGenotype>
{

  public SingleObjectiveOptimizationResult(TISolutionLayout Solutions, ISolution<TGenotype> bestSolution)
    : base(Solutions) =>
    BestSolution = bestSolution;
  public ISolution<TGenotype> BestSolution { get; init; }
}
