namespace HEAL.HeuristicLib.Numerics;

/// <summary>Accumulates the arithmetic mean without retaining observations.</summary>
/// <remarks>Instances are mutable and not thread-safe. Independent accumulators can be merged.</remarks>
public sealed class RunningMean
{
    private long count;
    private double mean;

    public long Count => count;
    public double Mean => count > 0 ? mean : double.NaN;

    public void Add(double value)
    {
        count++;
        mean += (value - mean) / count;
    }

    public void AddRange(ReadOnlySpan<double> values)
    {
        if (values.IsEmpty)
            return;

        Merge(values.Length, DescriptiveStatistics.Mean(values));
    }

    public void Merge(RunningMean other)
    {
        Merge(other.count, other.mean);
    }

    private void Merge(long otherCount, double otherMean)
    {
        if (otherCount <= 0)
            return;

        if (count == 0)
        {
            count = otherCount;
            mean = otherMean;
            return;
        }

        var combinedCount = count + otherCount;
        mean += (otherMean - mean) * otherCount / combinedCount;
        count = combinedCount;
    }

    public void Reset()
    {
        count = 0;
        mean = 0.0;
    }
}
