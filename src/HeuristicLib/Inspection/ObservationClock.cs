using HEAL.HeuristicLib.Execution;

namespace HEAL.HeuristicLib.Inspection;

/// <summary>
/// Counts the observations of one source, such as the iterations of one chosen algorithm.
/// </summary>
/// <remarks>
/// The clock keeps its own count and reads nothing from the observations, so it never runs backwards: an iteration
/// clock over an inner algorithm of a cycle keeps counting while each turn restarts the algorithm's own
/// <c>Iteration</c>. A nested algorithm has its own iteration count, which is why the source is named rather than
/// inferred.
/// </remarks>
public sealed class ObservationClock<TObservation> : Clock<long>
{
    private long count;

    public ObservationClock(ObservationSource<TObservation> source)
    {
        Advices = [.. Aspect.Observe(source, Record)];
    }

    protected override long ReadTime() => Interlocked.Read(ref count);

    private void Record(TObservation observation) => Interlocked.Increment(ref count);
}
