using HEAL.HeuristicLib.Execution;

namespace HEAL.HeuristicLib.Inspection;

/// <summary>Reads a counter's root accumulator as a clock.</summary>
/// <remarks>
/// Reads the root's accumulator whatever <see cref="ResolutionScope.Share"/> narrows in ordinary resolution.
/// This clock places nothing. Two clocks of the same counter are equal.
/// </remarks>
public sealed record CounterClock(Counter Counter) : IClock<long>
{
    public Counter Counter { get; init; } = Counter;

    public static Parameter<CounterClock, Counter> CounterParameter { get; } =
        new(nameof(Counter), static clock => clock.Counter, static (clock, value) => clock with { Counter = value });

    public static IReadOnlyList<IParameter> Parameters { get; } = [CounterParameter];

    public IReadOnlyList<IParameter> GetParameters() => Parameters;

    /// <summary>Creates a reading of this run's root accumulator.</summary>
    public IClockReading<long> CreateExecutionNode(ResolutionScope scope)
    {
        var accumulator = scope.Results.Result(Counter);
        return new Reading(accumulator);
    }

    private sealed class Reading(CountAccumulator accumulator) : IClockReading<long>
    {
        public long Read() => accumulator.CurrentCount;
    }
}
