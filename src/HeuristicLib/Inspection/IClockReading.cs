using HEAL.HeuristicLib.Execution;

namespace HEAL.HeuristicLib.Inspection;

/// <summary>One run's reading of a clock, boxed for mixed clock lists.</summary>
/// <remarks>
/// Reading must not change the observed computation. Every thread recording into a result that reads the clock
/// may call <see cref="Read"/> concurrently. A row of readings is not an atomic snapshot of several clocks.
/// </remarks>
public interface IClockReading : IRunResult
{
    /// <summary>Reads the clock's current value, boxing a value type on each call.</summary>
    object Read();
}

/// <summary>One run's reading of a clock whose time has type <typeparamref name="TTime"/>.</summary>
/// <remarks>
/// A reading implements one <c>IClockReading&lt;TTime&gt;</c> construction.
/// The concurrency and observation guarantees of <see cref="IClockReading"/> apply to typed reads too.
/// </remarks>
public interface IClockReading<out TTime> : IClockReading
{
    /// <summary>Reads the clock's current value without changing the observed computation.</summary>
    new TTime Read();

    object IClockReading.Read() => Read()!;
}
