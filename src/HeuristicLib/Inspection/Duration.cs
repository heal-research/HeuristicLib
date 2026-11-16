using HEAL.HeuristicLib.Execution;

namespace HEAL.HeuristicLib.Inspection;

/// <summary>Time that wrappers add to and terminators read, such as the time spent evaluating; its executor is a <see cref="DurationAccumulator"/>.</summary>
/// <remarks>
/// It is a configuration node like <see cref="Counter"/>: a link held by the wrappers and terminators that use it, whose
/// time belongs to the run and spans it unless a scope narrows it. Read it with <c>run.Result(duration).CurrentDuration</c>.
/// A duration created from a source, such as <c>evaluator.Invocations.Duration()</c>, holds the substitution that places
/// its wrapper, so <c>AnalyzedBy</c> attaches it. One created with <c>new Duration()</c> holds none and measures wherever it
/// is placed explicitly, such as with <c>Timed</c>.
/// <para>
/// The time provider is part of the node rather than taken from the run, so every wrapper of one duration measures with
/// the same provider.
/// </para>
/// </remarks>
public sealed class Duration : IConfigurationNode<DurationAccumulator>, IClock<TimeSpan>
{
    private readonly IReadOnlyList<Substitution> substitutions;

    /// <param name="timeProvider">Measures each call. When omitted, the system clock does.</param>
    public Duration(TimeProvider? timeProvider = null)
    {
        TimeProvider = timeProvider ?? TimeProvider.System;
        substitutions = [];
    }

    // A duration of a source, which holds its placement outside its declared parameters.
    internal Duration(TimeProvider? timeProvider, Func<Duration, Substitution> placement)
        : this(timeProvider)
    {
        substitutions = [placement(this)];
    }

    /// <summary>Measures the time of each call.</summary>
    public TimeProvider TimeProvider { get; }

    /// <summary>The substitution that places this duration's wrapper, or none for a duration created with <c>new Duration()</c>.</summary>
    public IReadOnlyList<Substitution> Substitutions => substitutions;

    public DurationAccumulator CreateExecutionNode(ResolutionScope scope) => new();

    Func<object> IClock.Bind(RunResults results)
    {
        var accumulator = results.Result(this);
        return () => accumulator.CurrentDuration;
    }
}
