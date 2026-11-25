using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Problems;

public abstract class SingleSolutionProblem<TSelf, TCandidate, TSearchSpace> : Problem<TSelf, TCandidate, TSearchSpace>
    where TSelf : Problem<TSelf, TCandidate, TSearchSpace>
    where TSearchSpace : class, ISearchSpace<TCandidate>
{
    public ExecutionConcurrency Concurrency { get; init; } = ExecutionConcurrency.Sequential();

    protected SingleSolutionProblem(ObjectiveDirections objective, TSearchSpace searchSpace) : base(objective, searchSpace) { }

    public sealed override IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TCandidate> candidates, IRandomNumberGenerator random) =>
        BatchExecution.Execute(candidates, Evaluate, random, Concurrency);

    public abstract ObjectiveVector Evaluate(TCandidate candidate, IRandomNumberGenerator random);
}
