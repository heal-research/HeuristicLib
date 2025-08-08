namespace HEAL.HeuristicLib.Optimization;

public interface IOptimizationResult {
}

public interface IOptimizationResult<TGenotype, out TSolutionLayout> : IOptimizationResult
  where TGenotype : IEquatable<TGenotype>
  where TSolutionLayout : ISolutionLayout<TGenotype>
{
  TSolutionLayout Solutions { get; }
}

public interface ISingleObjectiveResult<TGenotype> : IOptimizationResult 
  where TGenotype : IEquatable<TGenotype> 
{
  Solution<TGenotype> BestSolution { get; }
}

public interface IMultiObjectiveResult<TGenotype> : IOptimizationResult 
  where TGenotype : IEquatable<TGenotype> 
{
  IReadOnlyList<Solution<TGenotype>> ParetoFront { get; }
}

public abstract record class OptimizationResult<TGenotype, TSolutionLayout> : IOptimizationResult<TGenotype, TSolutionLayout>
  where TGenotype : IEquatable<TGenotype>
  where TSolutionLayout : ISolutionLayout<TGenotype>

{
  public TSolutionLayout Solutions { get; init; }
  
  protected OptimizationResult(TSolutionLayout solutions) {
    Solutions = solutions;
  }
}

public record class SingleObjectiveOptimizationResult<TGenotype, TSolutionLayout> : OptimizationResult<TGenotype, TSolutionLayout>, ISingleObjectiveResult<TGenotype>
  where TGenotype : IEquatable<TGenotype>
  where TSolutionLayout : ISolutionLayout<TGenotype>
{
  public Solution<TGenotype> BestSolution { get; init; }

  public SingleObjectiveOptimizationResult(TSolutionLayout solutions, Solution<TGenotype> bestSolution) 
    : base(solutions)
  {
    BestSolution = bestSolution;
  }
}

public record class MultiObjectiveOptimizationResult<TGenotype, TSolutionLayout> : OptimizationResult<TGenotype, TSolutionLayout>, IMultiObjectiveResult<TGenotype>
  where TGenotype : IEquatable<TGenotype>
  where TSolutionLayout : ISolutionLayout<TGenotype>
{
  public IReadOnlyList<Solution<TGenotype>> ParetoFront { get; init; }

  public MultiObjectiveOptimizationResult(TSolutionLayout solutions, IReadOnlyList<Solution<TGenotype>> paretoFront) 
    : base(solutions)
  {
    ParetoFront = paretoFront;
  }
}
