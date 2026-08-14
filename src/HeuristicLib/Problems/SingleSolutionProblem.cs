using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Problems;

public abstract class SingleSolutionProblem<TCandidate, TSearchSpace> : Problem<TCandidate, TSearchSpace>
    where TSearchSpace : class, ISearchSpace<TCandidate>
{
    public ExecutionConcurrency Concurrency { get; init; } = ExecutionConcurrency.Sequential();

    protected SingleSolutionProblem(ObjectiveDirections objective, TSearchSpace searchSpace) : base(objective, searchSpace) { }

    public sealed override IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TCandidate> candidates, IRandomNumberGenerator random) =>
        BatchExecution.Execute(candidates, Evaluate, random, Concurrency);

    public abstract ObjectiveVector Evaluate(TCandidate candidate, IRandomNumberGenerator random);
}

public abstract class Problem<TCandidate, TSearchSpace> : IProblem<TCandidate, TSearchSpace>
    where TSearchSpace : class, ISearchSpace<TCandidate>
{
    protected Problem(ObjectiveDirections objective, TSearchSpace searchSpace)
    {
        Objective = objective;
        SearchSpace = searchSpace;
    }

    public ObjectiveDirections Objective { get; }
    public abstract IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TCandidate> candidates, IRandomNumberGenerator random);

    public TSearchSpace SearchSpace { get; }
}
