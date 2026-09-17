using HEAL.HeuristicLib.Execution;

namespace HEAL.HeuristicLib.Analysis;

/// <summary>Reusable trace-retention configuration. Selects which aggregated observations a trace keeps.</summary>
public abstract record TraceRetention : IExecutionInstanceResolvable<ITraceRetentionInstance>
{
    public static TraceRetention EveryObservation() => new EveryObservationRetention();
    public static TraceRetention EveryNth(int interval) => new EveryNthRetention(interval);
    public static TraceRetention OnChange() => new OnChangeRetention();
    public abstract ITraceRetentionInstance CreateExecutionInstance(ExecutionInstanceResolver resolver);
}

/// <summary>Retention state for one trace.</summary>
public interface ITraceRetentionInstance : IExecutionInstance
{
    bool ShouldRetain<T>(T value);
}

public sealed record EveryObservationRetention : TraceRetention, ITraceRetentionInstance
{
    public override ITraceRetentionInstance CreateExecutionInstance(ExecutionInstanceResolver resolver) => this;
    public bool ShouldRetain<T>(T value) => true;
}

/// <summary>Records observations n, 2n, 3n, and so on. No extra initial or final entry is added.</summary>
public sealed record EveryNthRetention : TraceRetention
{
    public int Interval { get; init; }
    public EveryNthRetention(int interval)
    {
        Interval = interval;
    }

    public override ITraceRetentionInstance CreateExecutionInstance(ExecutionInstanceResolver resolver)
    {
        if (Interval <= 0)
            throw new InvalidOperationException("The retention interval must be positive.");
        return new ExecutionInstance(Interval);
    }

    private sealed class ExecutionInstance(int interval) : ITraceRetentionInstance
    {
        private long observed;
        public bool ShouldRetain<T>(T value) => Interlocked.Increment(ref observed) % interval == 0;
    }
}

/// <summary>Records the first value and subsequent changes according to the value's default equality.</summary>
public sealed record OnChangeRetention : TraceRetention
{
    public override ITraceRetentionInstance CreateExecutionInstance(ExecutionInstanceResolver resolver) => new ExecutionInstance();

    private sealed class ExecutionInstance : ITraceRetentionInstance
    {
        private readonly Lock sync = new();
        private object? previous;
        private Type? previousType;

        public bool ShouldRetain<T>(T value)
        {
            lock (sync)
            {
                if (previousType == typeof(T) && EqualityComparer<T>.Default.Equals((T)previous!, value))
                    return false;
                previous = value;
                previousType = typeof(T);
                return true;
            }
        }
    }
}
