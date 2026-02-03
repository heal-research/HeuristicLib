namespace HEAL.HeuristicLib.Optimization;

public record MultiObjectiveOptimizationResult<TGenotype, TISolutionLayout> : OptimizationResult<TGenotype, TISolutionLayout>, IMultiObjectiveResult<TGenotype>
  where TGenotype : IEquatable<TGenotype>
  where TISolutionLayout : IISolutionLayout<TGenotype>
{

  public MultiObjectiveOptimizationResult(TISolutionLayout Solutions, IReadOnlyList<ISolution<TGenotype>> paretoFront)
    : base(Solutions) =>
    ParetoFront = paretoFront;
  public IReadOnlyList<ISolution<TGenotype>> ParetoFront { get; init; }
}
