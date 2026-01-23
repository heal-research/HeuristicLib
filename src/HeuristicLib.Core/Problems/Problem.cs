using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Problems;

public abstract class Problem<TSolution, TSearchSpace>(Objective objective, TSearchSpace searchSpace) : IProblem<TSolution, TSearchSpace>
  where TSearchSpace : class, ISearchSpace<TSolution> {
  public Objective Objective { get; } = objective;
  public TSearchSpace SearchSpace { get; } = searchSpace;

  public abstract ObjectiveVector Evaluate(TSolution solution, IRandomNumberGenerator random);
}
