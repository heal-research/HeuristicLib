using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators;

namespace HEAL.HeuristicLib.Inspection;

internal sealed class DurationMeasuringOperatorAdvice(DurationAccumulator duration, TimeProvider timeProvider) : OperatorAdvice
{
    public override IExecutor Wrap(IExecutor executor, OperationInfo operation, IOperator source) =>
        operation.Wrap(executor, new DurationMeasurement(duration, timeProvider));
}

/// <summary>Measures the time spent in each operation call, including calls that fail.</summary>
/// <remarks>The supplied runtime accumulator is shared wherever this advice is reused.</remarks>
public sealed class DurationMeasuringOperatorAdvice<TOperation, TCandidate> : OperatorAdvice<TOperation, TCandidate>
    where TOperation : IOperation<TOperation>
{
    public DurationAccumulator Duration { get; }
    public TimeProvider TimeProvider { get; }

    public DurationMeasuringOperatorAdvice(DurationAccumulator duration) : this(duration, TimeProvider.System) { }

    public DurationMeasuringOperatorAdvice(DurationAccumulator duration, TimeProvider timeProvider)
    {
        Duration = duration;
        TimeProvider = timeProvider;
    }

    public override IExecutor Wrap<TSearchSpace, TProblem, TSearchState>(IExecutor executor, OperationInfo operation,
        IOperator<TOperation, TCandidate> source) =>
        operation.Wrap(executor, new DurationMeasurement(Duration, TimeProvider));
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
