using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators;

namespace HEAL.HeuristicLib.Inspection;

/// <summary>Names a configuration boundary and creates the substitution that observes it.</summary>
/// <remarks>
/// <para>
/// A source names a configuration and the wrapper that makes each call of that configuration's executor produce an
/// observation; creating one installs nothing. Both ways of collecting use that one wrapper: attaching an analyzer
/// with <c>CreateRun</c> places it through a substitution, and placing it explicitly, such as <c>evaluator.Observed(trace)</c>,
/// wraps the configuration where it is written. An operation offers its source as <c>Invocations</c>, such as
/// <c>crossover.Invocations</c>, and an algorithm as <c>Iterations</c> and <c>Calls</c>; an operator implementing several
/// operations offers each through a view typed to that operation.
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

    /// <summary>The observed configuration, matched by reference: a configuration with equal settings is observed separately.</summary>
    public abstract IConfigurationNode Configuration { get; }

    /// <summary>Creates the substitution that records this source's observations into the receiver, wherever a graph holds the configuration.</summary>
    /// <remarks>Each call creates a new substitution.</remarks>
    /// <param name="receiver">Receives observations through a recorder bound to the run's results.</param>
    /// <returns>A rewrite matching <see cref="Configuration"/> by reference; apply it to place the observation wrapper.</returns>
    public abstract Substitution RecordingInto(IObservationReceiver<TObservation> receiver);
}

/// <summary>The observations of one operator's successful calls for one operation, such as <c>mutator.Invocations</c>.</summary>
/// <remarks>
/// Besides tracing, the source creates substitutions that place counting and timing wrappers for a counter or duration
/// supplied by the caller. Creating a source installs nothing.
/// </remarks>
public sealed class InvocationSource<TOperation, TCandidate, TObservation> : ObservationSource<TObservation>
    where TOperation : IOperation<TOperation>
    where TObservation : IInvocationObservation<TOperation, TCandidate, TObservation>
{
    internal InvocationSource(IOperator<TOperation, TCandidate> source)
    {
        Operator = source;
    }

    /// <summary>The observed operator.</summary>
    public IOperator<TOperation, TCandidate> Operator { get; }

    public override IConfigurationNode Configuration => Operator;

    /// <summary>Places a wrapper counting the operator's successful calls into the counter.</summary>
    /// <remarks>
    /// In one substitution list, a later part sits closer to its target. List this placement after every analyzer that
    /// reads the counter so its reading includes the call; listed before them, it reads before the call.
    /// </remarks>
    public Substitution Counting(Counter counter) => Substitution.Of(Operator, node => node.Counted(counter));

    /// <summary>Places a wrapper counting the candidate work of the operator's successful calls into the counter.</summary>
    /// <remarks>
    /// In one substitution list, a later part sits closer to its target. List this placement after every analyzer that
    /// reads the counter so its reading includes the call; listed before them, it reads before the call.
    /// </remarks>
    /// <exception cref="InvalidOperationException">The operation reports no candidate work.</exception>
    public Substitution CountingCandidates(Counter counter) =>
        TOperation.SupportsCandidateWork
            ? Substitution.Of(Operator, node => node.Counted(counter, OperatorCountMetric.Candidates))
            : throw new InvalidOperationException($"Counting candidates cannot measure candidate work for {Operator.GetType().Name}: " +
                $"{ExecutionSignature.Name(typeof(TOperation))} reports none.");

    /// <summary>Places a wrapper adding the time spent in the operator's calls, including failed calls, to the duration. Uses the run's time provider.</summary>
    /// <remarks>
    /// In one substitution list, a later part sits closer to its target. List this placement after every analyzer that
    /// reads the duration so its reading includes the call; listed before them, it reads before the call.
    /// </remarks>
    public Substitution Timing(Duration duration) => Substitution.Of(Operator, node => node.Timed(duration));

    /// <inheritdoc/>
    public override Substitution RecordingInto(IObservationReceiver<TObservation> receiver) =>
        Substitution.Of(Operator, node => new Observed<TOperation, TCandidate, TObservation>(node, receiver));
}

public static class ObservationSource
{
    /// <summary>Creates the source of an operator's observations for one operation, as each operation does for its <c>Invocations</c>.</summary>
    /// <param name="configuration">The observed operator, matched by reference.</param>
    public static InvocationSource<TOperation, TCandidate, TObservation> Create<TOperation, TCandidate, TObservation>(IOperator<TOperation, TCandidate> configuration)
        where TOperation : IOperation<TOperation>
        where TObservation : IInvocationObservation<TOperation, TCandidate, TObservation> =>
        new(configuration);

    /// <summary>Creates the source of the observations of a role of your own, such as a cooling schedule.</summary>
    /// <remarks>
    /// An operation is the recommended form of a new role, because it gets <c>Invocations</c>, <c>Observed</c>,
    /// <c>Counted</c>, <c>Timed</c> and <c>Retry</c>. A trace over this source holds the placement function, so a
    /// configuration it is placed in is not serializable.
    /// </remarks>
    /// <typeparam name="TConfiguration">
    /// The type of the slots holding the configuration. Use the role's interface, because the wrapper must fit the slot.
    /// </typeparam>
    /// <typeparam name="TObservation">What the role's wrapper records.</typeparam>
    /// <param name="configuration">The observed configuration, matched by reference.</param>
    /// <param name="place">
    /// Creates the role's own observation wrapper around a configuration, recording into the receiver through
    /// <see cref="IObservationReceiver{TObservation}.CreateRecorder"/>.
    /// </param>
    public static ObservationSource<TObservation> Create<TConfiguration, TObservation>(
        TConfiguration configuration, Func<TConfiguration, IObservationReceiver<TObservation>, TConfiguration> place)
        where TConfiguration : class, IConfigurationNode =>
        new RoleSource<TConfiguration, TObservation>(configuration, place);

    private sealed class RoleSource<TConfiguration, TObservation>(
        TConfiguration configuration, Func<TConfiguration, IObservationReceiver<TObservation>, TConfiguration> place)
        : ObservationSource<TObservation>
        where TConfiguration : class, IConfigurationNode
    {
        public override IConfigurationNode Configuration => configuration;

        public override Substitution RecordingInto(IObservationReceiver<TObservation> receiver) =>
            Substitution.Of(configuration, node => place(node, receiver));
    }
}

/// <summary>Passes the observations of a source to a callback.</summary>
/// <remarks>
/// It belongs to inspection rather than analysis, because it derives and publishes nothing, so content such as a meta
/// algorithm that tracks its contenders can use it.
/// </remarks>
public static class ObservationCallbacks
{
    extension<TObservation>(ObservationSource<TObservation> source)
    {
        /// <summary>Creates an analyzer that passes each observation of this source to the callback.</summary>
        /// <remarks>
        /// Place it with <c>CreateRun</c> or <c>Substitute</c>. The callback runs on the thread that made the observation.
        /// It is not synchronized and can be called concurrently, including from independent runs. The caller owns any
        /// captured state and its synchronization. Exceptions propagate into execution. A configuration holding it is
        /// not serializable, as with every analyzer that holds a delegate. No run result is created.
        /// </remarks>
        public ISubstitution Observing(Action<TObservation> callback) => new CallbackAnalyzer<TObservation>(source, callback);
    }

    // Passes each observation of a source to a callback; it collects no result.
    private sealed class CallbackAnalyzer<TObservation>(ObservationSource<TObservation> source, Action<TObservation> callback)
        : SubstitutionGroup, IObservationReceiver<TObservation>
    {
        protected override IReadOnlyList<ISubstitution> CreateSubstitutions() => [source.RecordingInto(this)];

        public Action<TObservation> CreateRecorder(RunResults results) => callback;
    }
}
