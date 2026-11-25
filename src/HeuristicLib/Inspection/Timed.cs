using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators;

namespace HEAL.HeuristicLib.Inspection;

/// <summary>Adds the time spent in each call of its child, including calls that fail, to a duration. Uses the run's time provider.</summary>
/// <remarks>A terminator reading the same duration sees this time wherever the wrapper is placed.</remarks>
public sealed record Timed<TOperation, TCandidate> : WrappingOperator<TOperation, TCandidate>
    where TOperation : IOperation<TOperation>
{
    public Timed(IOperator<TOperation, TCandidate> childOperator, Duration duration) : base(childOperator)
    {
        Duration = duration;
    }

    public Duration Duration { get; init; }

    public static Parameter<Timed<TOperation, TCandidate>, Duration> DurationParameter { get; } =
        new(nameof(Duration), static timed => timed.Duration, static (timed, value) => timed with { Duration = value });

    public new static IReadOnlyList<IParameter> Parameters { get; } = [DurationParameter];

    public override IReadOnlyList<IParameter> GetParameters() => [.. base.GetParameters(), .. Parameters];

    protected override IExecutor CreateExecutor<TSearchSpace, TProblem, TSearchState>(ResolutionScope<TCandidate, TSearchSpace, TProblem, TSearchState> scope)
    {
        var operation = OperationInfo.For<TOperation, TCandidate, TSearchState>();
        var duration = scope.Resolve(Duration);
        return operation.Wrap(scope.ResolveOperator(ChildOperator), new DurationMeasurement(duration, scope.Results.TimeProvider));
    }
}

public static class TimedOperatorExtensions
{
    extension<TOperation, TCandidate>(IOperator<TOperation, TCandidate> source)
        where TOperation : IOperation<TOperation>
    {
        /// <summary>Adds the time spent in this operator's calls, including calls that fail, to the duration. Uses the run's time provider.</summary>
        public Timed<TOperation, TCandidate> Timed(Duration duration) => new(source, duration);
    }

    extension<TRoot>(TRoot root) where TRoot : class, IConfigurationNode
    {
        /// <summary>Measures the operator's calls, including calls that fail, wherever this configuration holds it. Uses the run's time provider.</summary>
        /// <param name="target">The operator to measure, matched by reference. It is an error when this configuration does not hold it.</param>
        /// <param name="duration">Receives the new duration, which <c>run.Result(duration).CurrentDuration</c> reads and a terminator can check.</param>
        public TRoot Timing<TOperation, TCandidate>(IOperator<TOperation, TCandidate> target, out Duration duration)
            where TOperation : IOperation<TOperation>
        {
            var created = new Duration();
            duration = created;
            return root.Substitute(Substitution.Of(target, node => node.Timed(created)));
        }
    }
}

/// <summary>Adds the elapsed time of each call, including calls that fail, to the accumulator. Uses the run's time provider.</summary>
internal readonly struct DurationMeasurement(DurationAccumulator duration, TimeProvider timeProvider) : IInvocationWrapper
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
