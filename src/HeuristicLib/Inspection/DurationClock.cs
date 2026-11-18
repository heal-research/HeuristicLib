using HEAL.HeuristicLib.Execution;

namespace HEAL.HeuristicLib.Inspection;

/// <summary>Reads a duration's root accumulator as a clock.</summary>
/// <remarks>
/// Reads the root's accumulator whatever <see cref="ResolutionScope.Share"/> narrows in ordinary resolution.
/// This clock places nothing. Two clocks of the same duration are equal.
/// </remarks>
public sealed record DurationClock(Duration Duration) : IClock<TimeSpan>
{
    public Duration Duration { get; init; } = Duration ?? throw new ArgumentNullException(nameof(Duration));

    public static Parameter<DurationClock, Duration> DurationParameter { get; } =
        new(nameof(Duration), static clock => clock.Duration, static (clock, value) => clock with { Duration = value });

    public static IReadOnlyList<IParameter> Parameters { get; } = [DurationParameter];

    public IReadOnlyList<IParameter> GetParameters() => Parameters;

    /// <summary>Creates a reading of this run's root accumulator.</summary>
    public IClockReading<TimeSpan> CreateExecutionNode(ResolutionScope scope)
    {
        var accumulator = scope.Results.Result(Duration);
        return new Reading(accumulator);
    }

    private sealed class Reading(DurationAccumulator accumulator) : IClockReading<TimeSpan>
    {
        public TimeSpan Read() => accumulator.CurrentDuration;
    }
}
