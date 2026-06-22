using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Problems;

public abstract class Problem<TSolution, TSearchSpace> : IProblem<TSolution, TSearchSpace>
    where TSearchSpace : class, ISearchSpace<TSolution>
{
    protected Problem(Objective objective, TSearchSpace searchSpace)
    {
        Objective = objective;
        SearchSpace = searchSpace;
    }

    public Objective Objective { get; }
    public TSearchSpace SearchSpace { get; }

    public abstract IReadOnlyList<ObjectiveVector> Evaluate(
        IReadOnlyList<TSolution> genotypes,
        IRandomNumberGenerator random);
}
