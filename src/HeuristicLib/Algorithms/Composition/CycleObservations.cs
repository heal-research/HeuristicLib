using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Inspection;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Algorithms;

/// <summary>The start of one cycle, in which a cycle runs every inner algorithm once.</summary>
/// <remarks>
/// The cycle publishes it before the cycle's first turn. <see cref="Cycle"/> counts from 1 in each call of the cycle.
/// <see cref="State"/> is the state the cycle continues from, or <see langword="null"/> before the first turn of a run
/// without an initial state. The search space and problem are read at the candidate's interfaces, so an observation
/// fits every run of the cycle.
/// </remarks>
public sealed record CycleStart<TCandidate, TSearchState>(
    long Cycle,
    TSearchState? State,
    ISearchSpace<TCandidate> SearchSpace,
    IProblem<TCandidate, ISearchSpace<TCandidate>> Problem)
    where TSearchState : class, ISearchState;

/// <summary>The start of one turn, an activation of one inner algorithm of a cycle.</summary>
/// <remarks>
/// The cycle publishes it after building the inner algorithm's executor and before running it. <see cref="Turn"/>
/// counts the turns of one call of the cycle from 1, across its cycles; <see cref="Index"/> is the inner algorithm's
/// position in the cycle, which tells two occurrences of one algorithm apart. <see cref="State"/> is the state the inner
/// algorithm starts from, or <see langword="null"/> before the first turn of a run without an initial state.
/// </remarks>
public sealed record CycleTurn<TCandidate, TSearchState>(
    long Cycle,
    long Turn,
    IAlgorithm<TCandidate, TSearchState> Algorithm,
    int Index,
    TSearchState? State,
    ISearchSpace<TCandidate> SearchSpace,
    IProblem<TCandidate, ISearchSpace<TCandidate>> Problem)
    where TSearchState : class, ISearchState;

public static class CycleObservations
{
    extension(Clock)
    {
        /// <summary>
        /// Creates a clock counting the cycles one chosen cycle has started.
        /// </summary>
        public static ObservationClock<CycleStart<TCandidate, TSearchState>> FromCycles<TAlgorithm, TCandidate, TSearchState>(
            CycleAlgorithm<TAlgorithm, TCandidate, TSearchState> cycle)
            where TAlgorithm : IAlgorithm<TCandidate, TSearchState>
            where TSearchState : class, ISearchState =>
            new(cycle.Cycles);

        /// <summary>
        /// Creates a clock counting the turns of inner algorithms one chosen cycle has started.
        /// </summary>
        public static ObservationClock<CycleTurn<TCandidate, TSearchState>> FromTurns<TAlgorithm, TCandidate, TSearchState>(
            CycleAlgorithm<TAlgorithm, TCandidate, TSearchState> cycle)
            where TAlgorithm : IAlgorithm<TCandidate, TSearchState>
            where TSearchState : class, ISearchState =>
            new(cycle.Turns);
    }
}
