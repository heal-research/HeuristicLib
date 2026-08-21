namespace HEAL.HeuristicLib.Numerics;

/// <summary>Accumulates the mean and variance without retaining observations.</summary>
/// <remarks>Instances are mutable and not thread-safe. Independent accumulators can be merged.</remarks>
public sealed class RunningMeanVariance
{
    private long count;
    private double mean;
    private double secondCentralMomentSum;

    public long Count => count;
    public double Mean => count > 0 ? mean : double.NaN;
    public double PopulationVariance => count > 0 ? secondCentralMomentSum / count : double.NaN;
    public double SampleVariance => count > 1 ? secondCentralMomentSum / (count - 1) : double.NaN;
    public double PopulationStandardDeviation => Math.Sqrt(PopulationVariance);
    public double SampleStandardDeviation => Math.Sqrt(SampleVariance);

    public void Add(double value)
    {
        count++;
        var delta = value - mean;
        mean += delta / count;
        secondCentralMomentSum += delta * (value - mean);
    }

    public void AddRange(ReadOnlySpan<double> values)
    {
        if (values.IsEmpty)
            return;

        var accumulator = DescriptiveStatistics.AccumulateMeanVariance(values);
        Merge(accumulator);
    }

    public void Merge(RunningMeanVariance other)
    {
        Merge(new MeanVarianceAccumulator(other.count, other.mean, other.secondCentralMomentSum));
    }

    private void Merge(MeanVarianceAccumulator other)
    {
        if (other.Count <= 0)
            return;

        if (count == 0)
        {
            count = other.Count;
            mean = other.Mean;
            secondCentralMomentSum = other.SecondCentralMomentSum;
            return;
        }

        var combinedCount = count + other.Count;
        var delta = other.Mean - mean;
        secondCentralMomentSum += other.SecondCentralMomentSum + delta * delta * count * other.Count / combinedCount;
        mean += delta * other.Count / combinedCount;
        count = combinedCount;
    }

    public void Reset()
    {
        count = 0;
        mean = 0.0;
        secondCentralMomentSum = 0.0;
    }
}
