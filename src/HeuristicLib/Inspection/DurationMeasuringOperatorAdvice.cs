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

    public override OperatorAdviceWrapperFactory<TOperation, TCandidate> CreateWrapperFactory(IOperator<TOperation, TCandidate> source) =>
        new Factory(Duration, TimeProvider);

    private sealed class Factory(DurationAccumulator duration, TimeProvider timeProvider) : OperatorAdviceWrapperFactory<TOperation, TCandidate>
    {
        private readonly CallbackInvocationWrapper<OperatorDurationCallbacks, long> invocationWrapper =
            new(new OperatorDurationCallbacks(duration, timeProvider));

        public override WrapperExecutorFactory GetWrapperFactory<TSearchSpace, TProblem, TRunSearchState>(
            IOperator<TOperation, TCandidate> source,
            OperationExecutorAdapter<TOperation, TCandidate, TSearchSpace, TProblem, TRunSearchState> adapter) =>
            adapter.WrapInvocation(invocationWrapper);
    }
}

internal readonly struct OperatorDurationCallbacks(DurationAccumulator duration, TimeProvider timeProvider) : IInvocationCallbacks<long>
{
    public long OnStarting() => timeProvider.GetTimestamp();
    public void OnSucceeded(long invocationData) { }
    public void OnFailed(long invocationData, Exception failure) { }
    public void OnFinished(long invocationData) => duration.AddDuration(timeProvider.GetElapsedTime(invocationData));
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
