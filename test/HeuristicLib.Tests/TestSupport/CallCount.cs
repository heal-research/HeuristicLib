using HEAL.HeuristicLib.Inspection;

namespace HEAL.HeuristicLib.Tests.TestSupport;

internal sealed class CallCount(CountAccumulator calls) : IClockReading<long>
{
    public long Read() => calls.CurrentCount;
}
