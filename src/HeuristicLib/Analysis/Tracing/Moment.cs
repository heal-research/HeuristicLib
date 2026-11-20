using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Inspection;

namespace HEAL.HeuristicLib.Analysis;

/// <summary>A row of clock readings using the trace's shared column layout.</summary>
internal sealed class Moment(IClock[] columns, object[] times)
{
    internal TTime At<TTime>(IClock<TTime> clock)
    {
        for (var column = 0; column < columns.Length; column++)
            if (Equals(columns[column], clock))
                return (TTime)times[column];
        throw new InvalidOperationException("The trace does not use the requested clock.");
    }

    internal static Moment Read(IClock[] columns, IClockReading[] readings)
    {
        var times = new object[readings.Length];
        for (var column = 0; column < readings.Length; column++)
            times[column] = readings[column].Read();
        return new Moment(columns, times);
    }
}
