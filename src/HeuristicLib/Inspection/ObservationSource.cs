using HEAL.HeuristicLib.Execution;

namespace HEAL.HeuristicLib.Inspection;

/// <summary>The observations of one operator's invocations, one algorithm's iterations or an observation point.</summary>
/// <remarks>
/// <para>
/// A source describes observations; creating one installs nothing. <c>Aspect.Observe</c> and the traces of
/// <c>Analyzer</c> attach it through an aspect, which resolution weaves into the executors a run builds. An operation
/// offers its source as <c>Invocations</c>, such as <c>crossover.Invocations</c>, and an algorithm as <c>Iterations</c>; an
/// operator implementing several operations offers each through a view typed to that operation.
/// </para>
/// <para>
/// A configuration that decides something inside its executor which no invocation or iteration shows publishes it,
/// such as the cycles and turns of <c>CycleAlgorithm</c>. It declares an <see cref="ObservationPoint{TObservation}"/>
/// and resolves it like a child. The executor reports observations through the resolved point's <c>Report</c> method.
/// </para>
/// <para>
/// An observation may be any value. Operator and algorithm observations are records of the call: its inputs and
/// outputs, and the search space and problem it received. A trace reads what it records from them through a function.
/// </para>
/// <para>
/// Two sources of the same configuration and observation type observe the same boundary, so a trace over both observes
/// it once. Sources of different operations of one configuration observe different boundaries.
/// </para>
/// </remarks>
public abstract class ObservationSource<TObservation>
{
    private protected ObservationSource() { }

    // Configurations match by reference, because a configuration with equal settings is observed separately.
    internal abstract IConfigurationNode Configuration { get; }

    internal abstract Aspect Observe(Action<TObservation> callback);
}

public static class ObservationSource
{
    /// <summary>Creates the source of a configuration's observations, as each operation does for its <c>Invocations</c>.</summary>
    /// <param name="configuration">The observed configuration, matched by reference.</param>
    /// <param name="observe">Creates an aspect that passes each observation of the configuration to the given callback.</param>
    public static ObservationSource<TObservation> Create<TObservation>(IConfigurationNode configuration, Func<Action<TObservation>, Aspect> observe) =>
        new Source<TObservation>(configuration, observe);

    private sealed class Source<TObservation>(IConfigurationNode configuration, Func<Action<TObservation>, Aspect> observe)
        : ObservationSource<TObservation>
    {
        internal override IConfigurationNode Configuration => configuration;

        internal override Aspect Observe(Action<TObservation> callback) => observe(callback);
    }
}

public static class ObservationSourceExtensions
{
    extension(Aspect)
    {
        /// <summary>Passes each observation of the source to the callback.</summary>
        /// <remarks>
        /// The callback runs on the thread that made the observation: after the observed invocation returned, after the
        /// algorithm yielded, or where the configuration published.
        /// </remarks>
        public static Aspect Observe<TObservation>(ObservationSource<TObservation> source, Action<TObservation> observe) =>
            source.Observe(observe);
    }
}
