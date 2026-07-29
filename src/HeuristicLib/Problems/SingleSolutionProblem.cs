using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Problems;

public abstract class SingleSolutionProblem<TSolution, TSearchSpace> : Problem<TSolution, TSearchSpace>
    where TSearchSpace : class, ISearchSpace<TSolution>
{
    public ExecutionConcurrency Concurrency { get; init; } = ExecutionConcurrency.Sequential();

    protected SingleSolutionProblem(ObjectiveDirections objective, TSearchSpace searchSpace) : base(objective, searchSpace) { }

    public sealed override IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TSolution> candidates, IRandomNumberGenerator random) =>
        BatchExecution.Execute(candidates, Evaluate, random, Concurrency);

    public abstract ObjectiveVector Evaluate(TSolution solution, IRandomNumberGenerator random);
}

public abstract class Problem<TSolution, TSearchSpace> : IProblem<TSolution, TSearchSpace>
    where TSearchSpace : class, ISearchSpace<TSolution>
{
    protected Problem(ObjectiveDirections objective, TSearchSpace searchSpace)
    {
        Objective = objective;
        SearchSpace = searchSpace;
    }

    public ObjectiveDirections Objective { get; }
    public abstract IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TSolution> candidates, IRandomNumberGenerator random);

    public TSearchSpace SearchSpace { get; }
}
