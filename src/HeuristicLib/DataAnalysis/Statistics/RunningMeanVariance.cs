namespace HEAL.HeuristicLib.DataAnalysis.Statistics;

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
        if (!BatchStatistics.IsBeneficial(values.Length))
        {
            foreach (var value in values)
                Add(value);
            return;
        }

        BatchStatistics.CalculateMeanAndSecondCentralMomentSum(values, out var batchMean, out var batchSecondCentralMomentSum);
        Merge(values.Length, batchMean, batchSecondCentralMomentSum);
    }

    public void Merge(RunningMeanVariance other)
    {
        Merge(other.count, other.mean, other.secondCentralMomentSum);
    }

    private void Merge(long otherCount, double otherMean, double otherSecondCentralMomentSum)
    {
        if (otherCount <= 0)
            return;

        if (count == 0)
        {
            count = otherCount;
            mean = otherMean;
            secondCentralMomentSum = otherSecondCentralMomentSum;
            return;
        }

        var combinedCount = count + otherCount;
        var delta = otherMean - mean;
        secondCentralMomentSum += otherSecondCentralMomentSum + delta * delta * count * otherCount / combinedCount;
        mean += delta * otherCount / combinedCount;
        count = combinedCount;
    }

    public void Reset()
    {
        count = 0;
        mean = 0.0;
        secondCentralMomentSum = 0.0;
    }
}
