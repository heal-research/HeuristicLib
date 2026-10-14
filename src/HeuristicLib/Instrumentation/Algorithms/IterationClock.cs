using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Instrumentation;

/// <summary>
/// Counts the iterations of one chosen algorithm.
/// </summary>
/// <remarks>
/// A nested algorithm has its own iteration count, which is why the algorithm is named rather than inferred.
/// </remarks>
public sealed class IterationClock<TCandidate, TSearchState>
    : Clock<long>
    where TSearchState : class, ISearchState
{
    private long latest;

    public IterationClock(IAlgorithm<TCandidate, TSearchState> algorithm)
    {
        Advices = Aspect.Observe(algorithm, Record).Advices;
    }

    protected override long ReadTime() => Interlocked.Read(ref latest);


    private void Record(AlgorithmObservation<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>, TSearchState> observation) =>
        Interlocked.Exchange(ref latest, observation.Iteration);
}

/// <summary>
/// Creates the clocks the library ships.
/// </summary>
/// <remarks>
/// A clock of your own belongs beside the thing it reads, and is offered the same way: an extension on
/// <see cref="Clock"/> named for the time it reads, so every clock is reached through one name.
/// </remarks>
public static class AlgorithmClockExtensions
{
    extension(Clock)
    {
        /// <summary>
        /// Creates a clock counting the iterations of one chosen algorithm.
        /// </summary>
        public static IterationClock<TCandidate, TSearchState> FromIterations<TCandidate, TSearchState>(IAlgorithm<TCandidate, TSearchState> algorithm)
            where TSearchState : class, ISearchState =>
            new(algorithm);
    }
}
