using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Problems.Dynamic;

/// <summary>
/// The environment version of a dynamic problem, as an axis a trace can record against.
/// </summary>
/// <remarks>
/// The problem applies a deferred update when the next unit of work begins, so between an evaluated batch and the next
/// one this reads the version that scored that batch. An analysis observing the evaluator therefore records the
/// environment its readings were made against, with no ordering to arrange.
/// </remarks>
public sealed class EpochClock<TCandidate, TSearchSpace>(DynamicProblem<TCandidate, TSearchSpace> problem) : Clock<int>
    where TSearchSpace : class, ISearchSpace<TCandidate>
{
    public DynamicProblem<TCandidate, TSearchSpace> Problem { get; } = problem;

    protected override int ReadTime() => Problem.CurrentEpoch;

    public override void Install(ExecutionInstanceResolverBuilder builder)
    {
        // The problem keeps its own environment version current.
    }
}

public static class DynamicClocks
{
    extension(Clock)
    {
        /// <summary>
        /// Creates a clock reading a dynamic problem's environment version.
        /// </summary>
        public static EpochClock<TCandidate, TSearchSpace> FromEpoch<TCandidate, TSearchSpace>(DynamicProblem<TCandidate, TSearchSpace> problem)
            where TSearchSpace : class, ISearchSpace<TCandidate> =>
            new(problem);
    }
}
