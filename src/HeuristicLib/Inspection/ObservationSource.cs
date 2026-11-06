using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators;

namespace HEAL.HeuristicLib.Inspection;

/// <summary>The observations of one operator's invocations, one algorithm's iterations or calls.</summary>
/// <remarks>
/// <para>
/// A source is a configuration and the wrapper that makes each call of that configuration's executor produce an
/// observation; creating one installs nothing. Both ways of collecting use that one wrapper: attaching an instrument
/// with <c>Advised</c> places it through advice, and placing it explicitly, such as <c>evaluator.Observed(trace)</c>,
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

    /// <summary>Creates the substitution that records this source's observations into the instrument, wherever a graph holds the configuration.</summary>
    /// <remarks>Each call creates a new substitution.</remarks>
    public abstract Substitution RecordingInto(IObservationInstrument<TObservation> instrument);

    // Creates an aspect that passes each observation of the configuration to the callback.
    internal abstract Aspect Observe(Action<TObservation> callback);
}

/// <summary>The observations of one operator's successful calls for one operation, such as <c>mutator.Invocations</c>.</summary>
/// <remarks>
/// Besides tracing, the source creates the counters and durations of its operator: <see cref="Count"/>,
/// <see cref="CountCandidates"/> and <see cref="Duration"/>. Each call creates a new instrument with its own result.
/// </remarks>
public sealed class InvocationSource<TOperation, TCandidate, TObservation> : ObservationSource<TObservation>
    where TOperation : IOperation<TOperation>
{
    private readonly Func<Action<TObservation>, OperatorAdvice<TOperation, TCandidate>> advise;
    private readonly Func<IObservationInstrument<TObservation>, Substitution>? recordingInto;

    internal InvocationSource(IOperator<TOperation, TCandidate> source, Func<Action<TObservation>, OperatorAdvice<TOperation, TCandidate>> advise,
        Func<IObservationInstrument<TObservation>, Substitution>? recordingInto = null)
    {
        Operator = source;
        this.advise = advise;
        this.recordingInto = recordingInto;
    }

    /// <summary>The observed operator.</summary>
    public IOperator<TOperation, TCandidate> Operator { get; }

    public override IConfigurationNode Configuration => Operator;

    /// <summary>Creates a counter of the operator's successful calls.</summary>
    public OperatorCounter Count() => new OperatorCounter<TOperation, TCandidate>(Operator, OperatorCountMetric.Invocations);

    /// <summary>Creates a counter of the candidate work of the operator's successful calls, such as the candidates an evaluator evaluated.</summary>
    /// <exception cref="InvalidOperationException">The operation reports no candidate work.</exception>
    public OperatorCounter CountCandidates() =>
        TOperation.SupportsCandidateWork
            ? new OperatorCounter<TOperation, TCandidate>(Operator, OperatorCountMetric.Candidates)
            : throw new InvalidOperationException($"Counting candidates cannot measure candidate work for {Operator.GetType().Name}: " +
                $"{ExecutionSignature.Name(typeof(TOperation))} reports none.");

    /// <summary>Creates a duration adding up the time spent in the operator's calls, including calls that fail.</summary>
    /// <param name="timeProvider">Measures each call. When omitted, the system clock does.</param>
    public OperatorDuration Duration(TimeProvider? timeProvider = null) =>
        new OperatorDuration<TOperation, TCandidate>(Operator, timeProvider ?? TimeProvider.System);

    public override Substitution RecordingInto(IObservationInstrument<TObservation> instrument) =>
        recordingInto is null
            ? throw new InvalidOperationException($"This source of {Operator.GetType().Name} was created from an advice factory, so it can only be attached with Advised. " +
                $"Create it from a record implementing {nameof(IInvocationObservation<,,>)} to place it.")
            : recordingInto(instrument);

    internal override Aspect Observe(Action<TObservation> callback) =>
        Aspect.Create(Pointcut.Reference(Operator), advise(callback));
}

public static class ObservationSource
{
    /// <summary>Creates the source of an operator's observations for one operation, as each operation does for its <c>Invocations</c>.</summary>
    /// <param name="configuration">The observed operator, matched by reference.</param>
    public static InvocationSource<TOperation, TCandidate, TObservation> Create<TOperation, TCandidate, TObservation>(IOperator<TOperation, TCandidate> configuration)
        where TOperation : IOperation<TOperation>
        where TObservation : IInvocationObservation<TOperation, TCandidate, TObservation> =>
        new(configuration, static observe => new ObservationAdvice<TOperation, TCandidate, TObservation>(observe),
            instrument => Substitution.Of<IOperator<TOperation, TCandidate>>(configuration, node => new Observed<TOperation, TCandidate, TObservation>(node, instrument)));

    /// <summary>Creates a source that only attaching with <c>Advised</c> can use, observing any value.</summary>
    /// <param name="configuration">The observed operator, matched by reference.</param>
    /// <param name="observe">
    /// Creates the advice whose wrapper passes each observation of a call to the given callback. Observation through
    /// <c>Advised</c> and the operation's <c>Observed</c> wrapper both apply it.
    /// </param>
    public static InvocationSource<TOperation, TCandidate, TObservation> Create<TOperation, TCandidate, TObservation>(
        IOperator<TOperation, TCandidate> configuration, Func<Action<TObservation>, OperatorAdvice<TOperation, TCandidate>> observe)
        where TOperation : IOperation<TOperation> =>
        new(configuration, observe);

    /// <summary>Creates the source of the observations of a role of your own, such as a cooling schedule.</summary>
    /// <param name="configuration">The observed configuration, matched by reference.</param>
    /// <param name="observe">
    /// Creates the advice whose wrapper passes each observation of a call to the given callback. An explicit wrapper of
    /// the role applies the same advice to the executor it wraps, recording into
    /// <see cref="IObservationInstrument{TObservation}.CreateRecorder"/>.
    /// </param>
    public static ObservationSource<TObservation> Create<TConfiguration, TExecutor, TObservation>(
        TConfiguration configuration, Func<Action<TObservation>, Advice<TConfiguration, TExecutor>> observe)
        where TConfiguration : class, IConfigurationNode<TExecutor>
        where TExecutor : class, IExecutor =>
        new RoleSource<TConfiguration, TExecutor, TObservation>(configuration, observe);

    private sealed class RoleSource<TConfiguration, TExecutor, TObservation>(
        TConfiguration configuration, Func<Action<TObservation>, Advice<TConfiguration, TExecutor>> observe)
        : ObservationSource<TObservation>
        where TConfiguration : class, IConfigurationNode<TExecutor>
        where TExecutor : class, IExecutor
    {
        public override IConfigurationNode Configuration => configuration;

        // An own role has no wrapper the library could create; phase 3 settles how its author supplies one.
        public override Substitution RecordingInto(IObservationInstrument<TObservation> instrument) =>
            throw new InvalidOperationException($"A source of the role {typeof(TConfiguration).Name} can only be attached with Advised for now.");

        internal override Aspect Observe(Action<TObservation> callback) =>
            Aspect.Create(Pointcut.Reference(configuration), observe(callback));
    }
}

/// <summary>An instrument that records observations, such as a trace; what <c>Observed</c> wrappers record into.</summary>
/// <remarks>
/// Implement it on an instrument of your own, together with <see cref="Instrument{TResult}"/>, so that both ways of
/// collecting reach it: its aspect observes the source through <c>Aspect.Observe</c>, and explicit placement records
/// through the callback this creates.
/// </remarks>
public interface IObservationInstrument<in TObservation>
{
    /// <summary>Creates the callback that records one observation into this run's result.</summary>
    Action<TObservation> CreateRecorder(RunResults results);
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
