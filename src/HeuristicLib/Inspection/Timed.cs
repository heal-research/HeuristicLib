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
        var operation = OperationInfo.For<TOperation, TCandidate, TSearchSpace, TProblem, TSearchState>();
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
}
