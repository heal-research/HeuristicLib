using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Problems;

public abstract class SingleSolutionProblem<TSolution, TSearchSpace> : Problem<TSolution, TSearchSpace>
  where TSearchSpace : class, ISearchSpace<TSolution>
{
  public int DegreeOfParallelism { get; init; } = 1;

  protected SingleSolutionProblem(Objective objective, TSearchSpace searchSpace) : base(objective, searchSpace) { }

  public sealed override IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TSolution> genotypes, IRandomNumberGenerator random) => BatchExecution.Parallel(genotypes, Evaluate, random, DegreeOfParallelism);

  public abstract ObjectiveVector Evaluate(TSolution solution, IRandomNumberGenerator random);
}

public abstract class Problem<TSolution, TSearchSpace> : IProblem<TSolution, TSearchSpace>
  where TSearchSpace : class, ISearchSpace<TSolution>
{
  protected Problem(Objective objective, TSearchSpace searchSpace)
  {
    Objective = objective;
    SearchSpace = searchSpace;
  }

  public Objective Objective { get; }
  public abstract IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TSolution> genotypes, IRandomNumberGenerator random);

  public TSearchSpace SearchSpace { get; }
}
