using HEAL.HeuristicLib.Inspection;

namespace HEAL.HeuristicLib.Analysis;

/// <summary>The times a selected set of clocks read together, recorded with one entry or one update.</summary>
internal sealed class Moment
{
    private readonly ImmutableDictionary<IClock, object> times;

    private Moment(ImmutableDictionary<IClock, object> times)
    {
        this.times = times;
    }

    internal TTime At<TTime>(IClock<TTime> clock)
    {
        if (!times.TryGetValue(clock, out var time))
            throw new InvalidOperationException("The requested clock is not one of the selected clocks.");
        return (TTime)time;
    }

    // Reads each clock bound to the run.
    internal static Moment Read(IEnumerable<(IClock Clock, Func<object> Read)> clocks)
    {
        var times = ImmutableDictionary.CreateBuilder<IClock, object>(ReferenceEqualityComparer.Instance);
        foreach (var (clock, read) in clocks)
            times.Add(clock, read());
        return new Moment(times.ToImmutable());
    }
}
