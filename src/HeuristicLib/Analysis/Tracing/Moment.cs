using HEAL.HeuristicLib.Instrumentation;

namespace HEAL.HeuristicLib.Analysis;

/// <summary>The times a selected set of clocks read together, recorded with one entry or one update.</summary>
internal sealed class Moment
{
    private readonly ImmutableDictionary<Clock, object> times;

    private Moment(ImmutableDictionary<Clock, object> times)
    {
        this.times = times;
    }

    internal TTime At<TTime>(Clock<TTime> clock)
    {
        if (!times.TryGetValue(clock, out var time))
            throw new InvalidOperationException("The requested clock is not one of the selected clocks.");
        return (TTime)time;
    }

    internal static Moment Read(IEnumerable<Clock> clocks)
    {
        var times = ImmutableDictionary.CreateBuilder<Clock, object>(ReferenceEqualityComparer.Instance);
        foreach (var clock in clocks)
            times.Add(clock, clock.Read());
        return new Moment(times.ToImmutable());
    }
}
