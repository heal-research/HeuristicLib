using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators;

namespace HEAL.HeuristicLib.Inspection;

/// <summary>Adds the time spent in each call of its child, including calls that fail, to a duration.</summary>
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
        return operation.Wrap(scope.ResolveOperator(ChildOperator), new DurationMeasurement(duration, Duration.TimeProvider));
    }
}

public static class TimedOperatorExtensions
{
    extension<TOperation, TCandidate>(IOperator<TOperation, TCandidate> source)
        where TOperation : IOperation<TOperation>
    {
        /// <summary>Adds the time spent in this operator's calls, including calls that fail, to the duration.</summary>
        public Timed<TOperation, TCandidate> Timed(Duration duration) => new(source, duration);
    }

    extension<TRoot>(TRoot root) where TRoot : class, IConfigurationNode
    {
        /// <summary>Measures the operator's calls, including calls that fail, wherever this configuration holds it.</summary>
        /// <param name="target">The operator to measure, matched by reference. It is an error when this configuration does not hold it.</param>
        /// <param name="duration">Receives the new duration, which <c>run.Result(duration).CurrentDuration</c> reads and a terminator can check.</param>
        /// <param name="timeProvider">Measures each call. When omitted, the system clock does.</param>
        public TRoot Timing<TOperation, TCandidate>(IOperator<TOperation, TCandidate> target, out Duration duration, TimeProvider? timeProvider = null)
            where TOperation : IOperation<TOperation> =>
            root.Substitute(Substitution.Timing(target, out duration, timeProvider));

        /// <summary>Adds the time of the operator's calls, including calls that fail, to an existing duration wherever this configuration holds it.</summary>
        /// <remarks>Timing the same target twice into one duration places two wrappers, which add every call twice.</remarks>
        /// <param name="target">The operator to measure, matched by reference. It is an error when this configuration does not hold it.</param>
        /// <param name="duration">The duration to add to, such as the one <c>LimitedToDuration</c> hands to its placement. Its time provider measures each call.</param>
        public TRoot Timing<TOperation, TCandidate>(IOperator<TOperation, TCandidate> target, Duration duration)
            where TOperation : IOperation<TOperation> =>
            root.Substitute(Substitution.Timing(target, duration));
    }

    extension(Substitution)
    {
        /// <summary>The substitution behind <c>Timing</c>, for one call with others.</summary>
        /// <param name="target">The operator to measure, matched by reference. It is an error when the configuration does not hold it.</param>
        /// <param name="duration">Receives the new duration.</param>
        /// <param name="timeProvider">Measures each call. When omitted, the system clock does.</param>
        public static Substitution Timing<TOperation, TCandidate>(IOperator<TOperation, TCandidate> target, out Duration duration, TimeProvider? timeProvider = null)
            where TOperation : IOperation<TOperation>
        {
            var created = new Duration(timeProvider);
            duration = created;
            return Substitution.Timing(target, created);
        }

        /// <summary>The substitution behind <c>Timing</c> into an existing duration, for one call with others.</summary>
        /// <param name="target">The operator to measure, matched by reference. It is an error when the configuration does not hold it.</param>
        /// <param name="duration">The duration to add to.</param>
        public static Substitution Timing<TOperation, TCandidate>(IOperator<TOperation, TCandidate> target, Duration duration)
            where TOperation : IOperation<TOperation> =>
            Substitution.Of(target, node => node.Timed(duration));
    }
}

/// <summary>Adds the elapsed time of each call, including calls that fail, to the accumulator.</summary>
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
