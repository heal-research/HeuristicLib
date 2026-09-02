using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Problems.Partial;

public interface IPartialSolutionProblem<TCandidate, out TSearchSpace>
    : IProblem<TCandidate, TSearchSpace>
    where TSearchSpace : class, ISearchSpace<TCandidate>
{
    IReadOnlyList<bool> IsTerminal(IReadOnlyList<TCandidate> candidates, IRandomNumberGenerator random);

    IReadOnlyList<ObjectiveVector?> EvaluatePartial(IReadOnlyList<TCandidate> candidates, IRandomNumberGenerator random);
}

public static class PartialSolutionProblemExtensions
{
    public static bool IsTerminal<TCandidate, TSearchSpace>(this IPartialSolutionProblem<TCandidate, TSearchSpace> problem, TCandidate candidate, IRandomNumberGenerator random)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        => problem.IsTerminal([candidate], random)[0];

    public static ObjectiveVector? EvaluatePartial<TCandidate, TSearchSpace>(this IPartialSolutionProblem<TCandidate, TSearchSpace> problem, TCandidate candidate, IRandomNumberGenerator random)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        => problem.EvaluatePartial([candidate], random)[0];
}

public interface IBoundedProblem<TCandidate, out TSearchSpace>
    : IProblem<TCandidate, TSearchSpace>
    where TSearchSpace : class, ISearchSpace<TCandidate>
{
    IReadOnlyList<ObjectiveVector> Bound(IReadOnlyList<TCandidate> candidates, IRandomNumberGenerator random);
}

public static class BoundedProblemExtensions
{
    public static ObjectiveVector Bound<TCandidate, TSearchSpace>(this IBoundedProblem<TCandidate, TSearchSpace> problem, TCandidate candidate, IRandomNumberGenerator random)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        => problem.Bound([candidate], random)[0];
}

public abstract class SingleSolutionBoundedProblem<TSelf, TCandidate, TSearchSpace>
    : SingleSolutionProblem<TSelf, TCandidate, TSearchSpace>, IBoundedProblem<TCandidate, TSearchSpace>
    where TSelf : Problem<TSelf, TCandidate, TSearchSpace>
    where TSearchSpace : class, ISearchSpace<TCandidate>
{
    protected SingleSolutionBoundedProblem(ObjectiveDirections objective, TSearchSpace searchSpace)
        : base(objective, searchSpace)
    { }

    public IReadOnlyList<ObjectiveVector> Bound(IReadOnlyList<TCandidate> candidates, IRandomNumberGenerator random) =>
        BatchExecution.Execute(candidates, Bound, random, Concurrency);

    public abstract ObjectiveVector Bound(TCandidate candidate, IRandomNumberGenerator random);
}

public abstract class SingleSolutionPartialProblem<TSelf, TCandidate, TSearchSpace>
    : SingleSolutionProblem<TSelf, TCandidate, TSearchSpace>, IPartialSolutionProblem<TCandidate, TSearchSpace>
    where TSelf : Problem<TSelf, TCandidate, TSearchSpace>
    where TSearchSpace : class, ISearchSpace<TCandidate>
{
    protected SingleSolutionPartialProblem(ObjectiveDirections objective, TSearchSpace searchSpace)
        : base(objective, searchSpace)
    { }

    public IReadOnlyList<bool> IsTerminal(IReadOnlyList<TCandidate> candidates, IRandomNumberGenerator random) =>
        BatchExecution.Execute(candidates, IsTerminal, random, Concurrency);

    public IReadOnlyList<ObjectiveVector?> EvaluatePartial(IReadOnlyList<TCandidate> candidates, IRandomNumberGenerator random) =>
        BatchExecution.Execute(candidates, EvaluatePartial, random, Concurrency);

    public abstract bool IsTerminal(TCandidate candidate, IRandomNumberGenerator random);

    public abstract ObjectiveVector? EvaluatePartial(TCandidate candidate, IRandomNumberGenerator random);
}

public abstract class SingleSolutionPartialBoundedProblem<TSelf, TCandidate, TSearchSpace>
    : SingleSolutionPartialProblem<TSelf, TCandidate, TSearchSpace>, IBoundedProblem<TCandidate, TSearchSpace>
    where TSelf : Problem<TSelf, TCandidate, TSearchSpace>
    where TSearchSpace : class, ISearchSpace<TCandidate>
{
    protected SingleSolutionPartialBoundedProblem(ObjectiveDirections objective, TSearchSpace searchSpace)
        : base(objective, searchSpace)
    { }

    public IReadOnlyList<ObjectiveVector> Bound(IReadOnlyList<TCandidate> candidates, IRandomNumberGenerator random)
        => BatchExecution.Execute(candidates, Bound, random, Concurrency);

    public abstract ObjectiveVector Bound(TCandidate candidate, IRandomNumberGenerator random);
}
