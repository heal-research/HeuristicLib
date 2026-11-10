using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators;

namespace HEAL.HeuristicLib.Inspection;

/// <summary>A count that wrappers add to and terminators read, such as the calls of a mutator; its executor is a <see cref="CountAccumulator"/>.</summary>
/// <remarks>
/// It stays in the configuration graph, so the wrapper, the terminator and a <c>Sharing</c> entry that hold it show who
/// uses which counter. It is a link rather than a value: two counters are different counters, and copies of a configuration
/// keep the link to the same one. The count itself belongs to the run, as its executor, so runs of such copies count
/// separately. The count spans the run, unless a scope narrows it with <see cref="ResolutionScope.Share"/>, as a budget does
/// to restart it each time it is built. Keep the variable, and read it with <c>run.Result(counter).CurrentCount</c>.
/// </remarks>
public sealed class Counter : IConfigurationNode<CountAccumulator>, IClock<long>
{
    public CountAccumulator CreateExecutor(ResolutionScope scope) => new();

    // The call that created this counter placed its wrapper.
    IReadOnlyList<Substitution> IClock.Substitutions => [];

    Func<object> IClock.Bind(RunResults results)
    {
        var accumulator = results.Result(this);
        return () => accumulator.CurrentCount;
    }
}

/// <summary>Counts the successful calls of one operator, or their candidate work.</summary>
/// <remarks>
/// Attached with <c>AnalyzedBy</c>, it counts every call of its operator's configuration reference. Placed explicitly with
/// <c>Counted</c>, it counts the calls of the configuration it wraps.
/// </remarks>
public abstract class OperatorCounter : Obsolete.Counter
{
    private protected OperatorCounter(OperatorCountMetric metric)
    {
        Metric = metric;
    }

    /// <summary>Whether the counter counts successful calls or their candidate work.</summary>
    public OperatorCountMetric Metric { get; }
}

internal sealed class OperatorCounter<TOperation, TCandidate>(IOperator<TOperation, TCandidate> source, OperatorCountMetric metric)
    : OperatorCounter(metric)
    where TOperation : IOperation<TOperation>
{
    protected override IReadOnlyList<Substitution> CreateSubstitutions() =>
        [Substitution.Of<IOperator<TOperation, TCandidate>>(source, node => node.Counted(this))];
}

/// <summary>A clock that adds up durations, such as the time spent evaluating; its result in each run is a <see cref="DurationAccumulator"/>.</summary>
/// <remarks>
/// Create durations from the source they measure, such as <c>evaluator.Invocations.Duration()</c>. A terminator such as
/// <c>AfterOperatorDurationTerminator</c> checks it.
/// </remarks>
public abstract class Duration : Clock<TimeSpan, DurationAccumulator>
{
    protected Duration(TimeProvider timeProvider)
    {
        TimeProvider = timeProvider;
    }

    /// <summary>Measures the time of each call.</summary>
    public TimeProvider TimeProvider { get; }

    protected sealed override DurationAccumulator CreateResult(RunResults results) => new();

    protected sealed override TimeSpan Read(DurationAccumulator result) => result.CurrentDuration;
}

/// <summary>Adds up the time spent in the calls of one operator, including calls that fail.</summary>
/// <remarks>
/// Attached with <c>AnalyzedBy</c>, it times every call of its operator's configuration reference. Placed explicitly with
/// <c>Timed</c>, it times the calls of the configuration it wraps.
/// </remarks>
public abstract class OperatorDuration : Duration
{
    private protected OperatorDuration(TimeProvider timeProvider) : base(timeProvider) { }
}

internal sealed class OperatorDuration<TOperation, TCandidate>(IOperator<TOperation, TCandidate> source, TimeProvider timeProvider)
    : OperatorDuration(timeProvider)
    where TOperation : IOperation<TOperation>
{
    protected override IReadOnlyList<Substitution> CreateSubstitutions() =>
        [Substitution.Of<IOperator<TOperation, TCandidate>>(source, node => node.Timed(this))];
}

// Counts the observations of one source, such as the cycles an observation point reports.
internal sealed class ObservationCounter<TObservation>(ObservationSource<TObservation> source) : Obsolete.Counter, IObservationInstrument<TObservation>
{
    protected override IReadOnlyList<Substitution> CreateSubstitutions() => [source.RecordingInto(this)];

    public Action<TObservation> CreateRecorder(RunResults results)
    {
        var count = results.Result(this);
        return _ => count.IncrementBy(1);
    }
}
