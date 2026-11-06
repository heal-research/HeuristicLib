using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators;

namespace HEAL.HeuristicLib.Inspection;

/// <summary>Adds the time spent in each call of its child, including calls that fail, to the run's result of a duration.</summary>
/// <remarks>
/// This is the explicit placement of a duration: it measures what the duration's advice measures, through the same
/// wrapper, but at the configuration it wraps rather than at the duration's source.
/// </remarks>
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

    protected override IExecutor CreateExecutor<TSearchSpace, TProblem, TSearchState>(ResolutionScope<TCandidate, TSearchSpace, TProblem, TSearchState> scope) =>
        ExplicitPlacement.Apply(scope, ChildOperator, new DurationMeasuringOperatorAdvice<TOperation, TCandidate>(scope.Results.Result(Duration), Duration.TimeProvider));
}

public static class TimedOperatorExtensions
{
    extension<TOperation, TCandidate>(IOperator<TOperation, TCandidate> source)
        where TOperation : IOperation<TOperation>
    {
        /// <summary>Adds the time spent in this operator's calls to the run's result of the duration.</summary>
        public Timed<TOperation, TCandidate> Timed(Duration duration) => new(source, duration);
    }
}
