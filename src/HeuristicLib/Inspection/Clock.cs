using HEAL.HeuristicLib.Execution;

namespace HEAL.HeuristicLib.Inspection;

/// <summary>The clocks supplied beside counters and durations.</summary>
public static class Clock
{
    /// <summary>The time the run spends advancing its algorithm, read from <see cref="RunResults.Elapsed"/>.</summary>
    /// <remarks>
    /// Includes a call in progress and deferred construction during it. Initial construction, pauses and consumer
    /// work are excluded. Direct scope execution leaves it at zero; use <c>algorithm.Timed(duration)</c> there.
    /// Failed and canceled advances remain included. This clock needs no placement. Every elapsed clock equals every other.
    /// </remarks>
    public static ElapsedClock Elapsed { get; } = new();
}
