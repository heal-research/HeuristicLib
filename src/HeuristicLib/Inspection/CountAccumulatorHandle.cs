using HEAL.HeuristicLib.Execution;

namespace HEAL.HeuristicLib.Inspection;

/// <remarks>Shares atomic accounting only. A separate limit check is not a strict concurrent reservation.</remarks>
public sealed class CountAccumulatorHandle : ResourceHandle<CountAccumulator>
{
    protected internal override CountAccumulator CreateResource() => new();
}
