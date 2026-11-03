using HEAL.HeuristicLib.Execution;

namespace HEAL.HeuristicLib.Inspection;

/// <summary>Identifies a duration accumulator shared within each independent run.</summary>
public sealed class DurationAccumulatorHandle : ResourceHandle<DurationAccumulator>
{
    protected internal override DurationAccumulator CreateResource() => new();
}
