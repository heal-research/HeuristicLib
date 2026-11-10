using HEAL.HeuristicLib.Execution;

namespace HEAL.HeuristicLib.Inspection;

/// <summary>A notion of time a trace records each entry against, such as a count of evaluations or the elapsed time.</summary>
/// <remarks>
/// A clock is read as a time. A trace binds each of its clocks to the run, reads them at every entry and lists the
/// clocks' own substitutions, which keep them current, after its observations, so an entry reads clocks that already
/// include its call. Mixed lists, such as <c>[evaluations, Clock.Elapsed]</c>, use this type. Derive a clock of your own
/// from <see cref="Clock{TTime, TResult}"/>.
/// </remarks>
public interface IClock : ISubstitutionBundle
{
    // Binds the clock to one run: the reader a trace samples at each entry.
    internal Func<object> Bind(RunResults results);
}

/// <summary>A clock whose time has the type <typeparamref name="TTime"/>, which is what a trace is read back by.</summary>
public interface IClock<out TTime> : IClock;

/// <summary>An instrument whose result in each run is read as a time of the type <typeparamref name="TTime"/>.</summary>
/// <remarks>
/// This is the type a custom clock derives from, such as one over a domain quantity the library knows nothing about. Its
/// result is created once per run, as for every instrument, and a clock that needs keeping current names the wrapper
/// that does so in its substitutions. A clock belongs beside the thing it reads. The clock object is also the key a trace
/// is read back with, so one instance means one axis: two clocks of the same kind stay distinguishable.
/// </remarks>
public abstract class Clock<TTime, TResult> : Instrument<TResult>, IClock<TTime> where TResult : class
{
    protected Clock() { }

    /// <summary>Reads the time from this run's result. Called once per recorded trace entry.</summary>
    protected abstract TTime Read(TResult result);

    Func<object> IClock.Bind(RunResults results)
    {
        var result = results.Result(this);
        return () => Read(result)!;
    }
}

/// <summary>The clocks the library supplies beside its counters and durations.</summary>
/// <remarks>
/// Counters and durations are clocks too, created from the source they measure, such as
/// <c>evaluator.Invocations.Count()</c> or <c>evaluator.Invocations.Duration()</c>.
/// </remarks>
public static class Clock
{
    /// <summary>The time the run's root algorithm has spent executing, read from <see cref="RunResults.Elapsed"/>.</summary>
    /// <remarks>
    /// Like a stopwatch, it stops while the run is paused and while a consumer handles a streamed state. It needs no
    /// placement, so attaching it does nothing.
    /// </remarks>
    public static IClock<TimeSpan> Elapsed { get; } = new ElapsedClock();

    private sealed class ElapsedClock : Clock<TimeSpan, RunResults>
    {
        protected override IReadOnlyList<Substitution> CreateSubstitutions() => [];

        protected override RunResults CreateResult(RunResults results) => results;

        protected override TimeSpan Read(RunResults result) => result.Elapsed;
    }
}
