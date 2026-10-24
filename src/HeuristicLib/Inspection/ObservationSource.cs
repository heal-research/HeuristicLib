using HEAL.HeuristicLib.Execution;

namespace HEAL.HeuristicLib.Inspection;

/// <summary>The observations of one operator's calls or one algorithm's iterations.</summary>
/// <remarks>
/// <para>
/// A source describes observations; creating one installs nothing. <c>Aspect.Observe</c> and the traces of
/// <c>Analyzer</c> attach it through an aspect, which resolution weaves into the executors a run builds. An operation
/// offers its source as <c>Calls</c>, such as <c>crossover.Calls</c>, and an algorithm as <c>Iterations</c>; an
/// operator implementing several operations offers each through a view typed to that operation.
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
public sealed class ObservationSource<TObservation>
{
    private readonly Func<Action<TObservation>, Aspect> observe;

    /// <summary>Creates the source of a configuration's observations, as each operation does for its <c>Calls</c>.</summary>
    /// <param name="configuration">The observed configuration, matched by reference.</param>
    /// <param name="observe">Creates an aspect that passes each observation of the configuration to the given callback.</param>
    public ObservationSource(IConfigurationNode configuration, Func<Action<TObservation>, Aspect> observe)
    {
        Configuration = configuration;
        this.observe = observe;
    }

    // Configurations match by reference, because a configuration with equal settings is observed separately.
    internal IConfigurationNode Configuration { get; }

    internal Aspect Observe(Action<TObservation> callback) => observe(callback);
}

public static class ObservationSourceExtensions
{
    extension(Aspect)
    {
        /// <summary>Passes each observation of the source to the callback.</summary>
        /// <remarks>
        /// The callback runs on the thread that made the observation, after the observed call returned or the algorithm
        /// yielded.
        /// </remarks>
        public static Aspect Observe<TObservation>(ObservationSource<TObservation> source, Action<TObservation> observe) =>
            source.Observe(observe);
    }
}
