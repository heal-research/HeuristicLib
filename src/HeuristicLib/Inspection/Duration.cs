using HEAL.HeuristicLib.Execution;

namespace HEAL.HeuristicLib.Inspection;

/// <summary>Time that wrappers add to and terminators read, such as the time spent evaluating; its executor is a <see cref="DurationAccumulator"/>.</summary>
/// <remarks>
/// It is a configuration node like <see cref="Counter"/>: a link held by the wrappers and terminators that use it, whose
/// time belongs to the run and spans it unless <see cref="ResolutionScope.Share"/> narrows ordinary resolution.
/// <see cref="AlgorithmRun.Result{TResult}"/> and clocks from <see cref="Clock"/> always read the root's accumulator, which can differ
/// from a scope-local shared accumulator. Accumulator reads and additions are thread-safe.
/// Two durations are different durations, and copies of a configuration keep the same one.
/// A duration places nothing. A wrapper measures into it, such as <c>Timed</c>, <c>TimedAlgorithm</c> or the
/// substitution a source's <c>Timing</c> returns. The wrapper says what is measured. A duration placed nowhere reads
/// zero, also when a trace reads it.
/// <para>
/// Its wrappers measure with the run's time provider.
/// Only completed calls add time, including failed calls; concurrent or nested calls can contribute overlapping time.
/// </para>
/// </remarks>
public sealed class Duration : IConfigurationNode<DurationAccumulator>
{
    /// <summary>Creates a duration whose time each run accumulates from zero.</summary>
    public Duration() { }

    /// <summary>A clock reading this node, equal on every read of this property.</summary>
    public DurationClock Clock => new(this);

    /// <summary>Creates a fresh accumulator starting at zero.</summary>
    public DurationAccumulator CreateExecutionNode(ResolutionScope scope) => new();
}
