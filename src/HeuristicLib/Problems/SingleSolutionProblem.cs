using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Problems;

public abstract class SingleSolutionProblem<TSolution, TSearchSpace> : Problem<TSolution, TSearchSpace>
    where TSearchSpace : class, ISearchSpace<TSolution>
{
    protected SingleSolutionProblem(Objective objective, TSearchSpace searchSpace) : base(objective, searchSpace) { }
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
    public abstract ObjectiveVector Evaluate(TSolution genotype, IRandomNumberGenerator random);

    public TSearchSpace SearchSpace { get; }
}
