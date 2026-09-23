using HEAL.HeuristicLib.Execution;

namespace HEAL.HeuristicLib.Analysis;

/// <summary>
/// An analyzer that keeps one accumulator and updates it in place, instead of a history of values.
/// </summary>
/// <remarks>
/// <para>
/// A Pareto front, a descent graph and a fitted model are accumulators: every observation changes one object, and
/// what a reader wants is that object as it stands. Use <see cref="TraceAnalyzer{TResult}"/> instead when the values
/// are immutable and the history is the point. Retention and clocks have no meaning here: there is nothing to store
/// apart from what was computed, and one current state has no moment of its own.
/// </para>
/// <para>
/// A derived analyzer owns its accumulator, updates it while holding <see cref="Sync"/> and publishes the accumulator
/// itself rather than a copy. Copying it per read would cost the whole accumulator, so it is read once the run has
/// finished.
/// </para>
/// </remarks>
public abstract class AccumulatingAnalyzer : IAnalyzer
{
    /// <summary>Guards the accumulator. Hold it while updating.</summary>
    protected Lock Sync { get; } = new();

    /// <summary>Installs the observations that feed the accumulator.</summary>
    public abstract void Install(ResolutionScopeBuilder builder);
}
