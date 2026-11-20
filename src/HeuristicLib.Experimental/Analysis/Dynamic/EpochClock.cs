using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Inspection;
using HEAL.HeuristicLib.Problems.Dynamic;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Analysis;

/// <summary>
/// The environment version of a dynamic problem, as an axis a trace can record against.
/// </summary>
/// <remarks>
/// The problem applies a deferred update when the next unit of work begins, so between an evaluated batch and the next
/// one this reads the version that scored that batch. An analysis observing the evaluator therefore records the
/// environment its observations were made against, with no ordering to arrange. Under
/// <see cref="UpdatePolicy.AfterEachEvaluation"/> one batch can span several versions, and this reads the one that
/// scored its last candidate; the versions of earlier candidates are not recorded.
/// This clock places nothing and reads the supplied problem, so reusing it across runs does not
/// create independent problem state. Synchronization follows the problem's contract.
/// It holds a live problem, so a configuration holding it is not serializable. This clock matches by reference.
/// </remarks>
public sealed class EpochClock<TCandidate, TSearchSpace>(IDynamicProblem<TCandidate, TSearchSpace> problem)
    : IClock<int>
    where TSearchSpace : class, ISearchSpace<TCandidate>
{
    public IDynamicProblem<TCandidate, TSearchSpace> Problem { get; } = problem;

    /// <summary>Creates a reading of the supplied problem's current epoch.</summary>
    public IClockReading<int> CreateExecutionNode(ResolutionScope scope) => new Reading(Problem);

    private sealed class Reading(IDynamicProblem<TCandidate, TSearchSpace> problem) : IClockReading<int>
    {
        public int Read() => problem.CurrentEpoch;
    }
}

public static class DynamicClocks
{
    extension(Clock)
    {
        /// <summary>
        /// Creates a clock reading a dynamic problem's environment version.
        /// </summary>
        public static EpochClock<TCandidate, TSearchSpace> FromEpoch<TCandidate, TSearchSpace>(IDynamicProblem<TCandidate, TSearchSpace> problem)
            where TSearchSpace : class, ISearchSpace<TCandidate> =>
            new(problem);
    }
}
