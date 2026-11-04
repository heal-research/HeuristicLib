using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators;

namespace HEAL.HeuristicLib.Inspection;

/// <summary>A clock that counts, such as the evaluations of a run; its result in each run is a <see cref="CountAccumulator"/>.</summary>
/// <remarks>
/// Create counters from the source they count, such as <c>evaluator.Invocations.Count()</c>. Each call creates a new
/// counter with its own result, so keep the variable: <c>run.Result(counter).CurrentCount</c> reads it, and a terminator
/// such as <c>AfterOperatorCountTerminator</c> checks it. A counter of your own derives from this type and places its
/// advice through <see cref="Instrument{TResult}.CreateAspect"/>.
/// </remarks>
public abstract class Counter : Clock<long, CountAccumulator>
{
    protected Counter() { }

    protected sealed override CountAccumulator CreateResult(RunResults results) => new();

    protected sealed override long Read(CountAccumulator result) => result.CurrentCount;
}

/// <summary>Counts the successful calls of one operator, or their candidate work.</summary>
/// <remarks>
/// Attached with <c>Advised</c>, it counts every call of its operator's configuration reference. Placed explicitly with
/// <c>Counted</c>, it counts the calls of the configuration it wraps.
/// </remarks>
public abstract class OperatorCounter : Counter
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
    protected override Aspect CreateAspect(CountAccumulator result, RunResults results) =>
        Aspect.Create(Pointcut.Reference(source), new CountingOperatorAdvice<TOperation, TCandidate>(result, Metric));
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
/// Attached with <c>Advised</c>, it times every call of its operator's configuration reference. Placed explicitly with
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
    protected override Aspect CreateAspect(DurationAccumulator result, RunResults results) =>
        Aspect.Create(Pointcut.Reference(source), new DurationMeasuringOperatorAdvice<TOperation, TCandidate>(result, TimeProvider));
}

// Counts the observations of one source, such as the cycles an observation point reports.
internal sealed class ObservationCounter<TObservation>(ObservationSource<TObservation> source) : Counter
{
    protected override Aspect CreateAspect(CountAccumulator result, RunResults results) =>
        Aspect.Observe(source, _ => result.IncrementBy(1));
}
