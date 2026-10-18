using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators;

namespace HEAL.HeuristicLib.Inspection;

/// <summary>Measures the time spent in each operation call, including calls that fail.</summary>
public sealed class DurationMeasuringOperatorAdvice<TOperation, TCandidate> : OperatorAdvice<TOperation, TCandidate>
    where TOperation : IOperationDescriptor<TOperation>
{
    public DurationAccumulator Duration { get; }
    public TimeProvider TimeProvider { get; }

    public DurationMeasuringOperatorAdvice(DurationAccumulator duration) : this(duration, TimeProvider.System) { }

    public DurationMeasuringOperatorAdvice(DurationAccumulator duration, TimeProvider timeProvider)
    {
        Duration = duration;
        TimeProvider = timeProvider;
    }

    public override IExecutor Wrap<TSearchSpace, TProblem, TSearchState>(IExecutor executor, OperationExecutorAdapter operation,
        IOperator<TOperation, TCandidate> source) =>
        operation.WrapInvocation(executor, new DurationMeasurement(Duration, TimeProvider));
}

/// <summary>Adds the elapsed time of each call, including calls that fail, to the accumulator.</summary>
file readonly struct DurationMeasurement(DurationAccumulator duration, TimeProvider timeProvider) : IInvocationWrapper
{
    public TResult Invoke<TInvocation, TResult>(TInvocation invocation) where TInvocation : struct, IInvocation<TResult>
    {
        var start = timeProvider.GetTimestamp();
        try
        {
            return invocation.Proceed();
        }
        finally
        {
            duration.AddDuration(timeProvider.GetElapsedTime(start));
        }
    }
}

public sealed record OperatorDurationMeasuring<TOperation, TCandidate> : IOperatorDurationMeasuring<IOperator<TOperation, TCandidate>>
    where TOperation : IOperationDescriptor<TOperation>
{
    public Advice<IOperator<TOperation, TCandidate>> CreateAdvice(DurationAccumulator duration, TimeProvider timeProvider) =>
        new DurationMeasuringOperatorAdvice<TOperation, TCandidate>(duration, timeProvider);
}

public static class OperatorDurationExtensions
{
    extension(Aspect)
    {
        public static Aspect MeasureDuration<TOperation, TCandidate>(IOperator<TOperation, TCandidate> source, DurationAccumulator duration)
            where TOperation : IOperationDescriptor<TOperation> => Aspect.MeasureDuration(source, duration, TimeProvider.System);

        public static Aspect MeasureDuration<TOperation, TCandidate>(IOperator<TOperation, TCandidate> source, DurationAccumulator duration, TimeProvider timeProvider)
            where TOperation : IOperationDescriptor<TOperation> =>
            Aspect.Create(Pointcut.Reference(source), new DurationMeasuringOperatorAdvice<TOperation, TCandidate>(duration, timeProvider));
    }
}
