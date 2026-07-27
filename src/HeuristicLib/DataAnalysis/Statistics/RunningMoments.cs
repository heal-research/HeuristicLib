namespace HEAL.HeuristicLib.DataAnalysis.Statistics;

/// <summary>Accumulates the first four central moments without retaining observations.</summary>
/// <remarks>Instances are mutable and not thread-safe. Independent accumulators can be merged.</remarks>
public sealed class RunningMoments
{
    private long count;
    private double mean;
    private double secondCentralMomentSum;
    private double thirdCentralMomentSum;
    private double fourthCentralMomentSum;

    public long Count => count;
    public double Mean => count > 0 ? mean : double.NaN;
    public double PopulationVariance => count > 0 ? secondCentralMomentSum / count : double.NaN;
    public double SampleVariance => count > 1 ? secondCentralMomentSum / (count - 1) : double.NaN;
    public double PopulationStandardDeviation => Math.Sqrt(PopulationVariance);
    public double SampleStandardDeviation => Math.Sqrt(SampleVariance);

    public double PopulationSkewness => count > 0 && secondCentralMomentSum > 0.0
        ? Math.Sqrt(count) * thirdCentralMomentSum / Math.Pow(secondCentralMomentSum, 1.5)
        : double.NaN;

    public double SampleSkewness => count > 2
        ? Math.Sqrt(count * (count - 1.0)) / (count - 2.0) * PopulationSkewness
        : double.NaN;

    public double PopulationExcessKurtosis => count > 0 && secondCentralMomentSum > 0.0
        ? count * fourthCentralMomentSum / (secondCentralMomentSum * secondCentralMomentSum) - 3.0
        : double.NaN;

    public double SampleExcessKurtosis => count > 3
        ? (count - 1.0) / ((count - 2.0) * (count - 3.0)) *
          ((count + 1.0) * PopulationExcessKurtosis + 6.0)
        : double.NaN;

    public void Add(double value)
    {
        var previousCount = count;
        count++;

        var delta = value - mean;
        var normalizedDelta = delta / count;
        var normalizedDeltaSquared = normalizedDelta * normalizedDelta;
        var term = delta * normalizedDelta * previousCount;
        var currentCount = (double)count;

        fourthCentralMomentSum += term * normalizedDeltaSquared * (currentCount * currentCount - 3.0 * currentCount + 3.0) +
                                  6.0 * normalizedDeltaSquared * secondCentralMomentSum -
                                  4.0 * normalizedDelta * thirdCentralMomentSum;
        thirdCentralMomentSum += term * normalizedDelta * (currentCount - 2.0) - 3.0 * normalizedDelta * secondCentralMomentSum;
        secondCentralMomentSum += term;
        mean += normalizedDelta;
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

        BatchStatistics.CalculateCentralMomentSums(values, out var batchMean, out var batchSecondCentralMomentSum, out var batchThirdCentralMomentSum, out var batchFourthCentralMomentSum);
        Merge(values.Length, batchMean, batchSecondCentralMomentSum, batchThirdCentralMomentSum, batchFourthCentralMomentSum);
    }

    public void Merge(RunningMoments other)
    {
        Merge(other.count, other.mean, other.secondCentralMomentSum, other.thirdCentralMomentSum, other.fourthCentralMomentSum);
    }

    private void Merge(long otherCount, double otherMean, double otherSecondCentralMomentSum, double otherThirdCentralMomentSum, double otherFourthCentralMomentSum)
    {
        if (otherCount <= 0)
            return;

        if (count == 0)
        {
            count = otherCount;
            mean = otherMean;
            secondCentralMomentSum = otherSecondCentralMomentSum;
            thirdCentralMomentSum = otherThirdCentralMomentSum;
            fourthCentralMomentSum = otherFourthCentralMomentSum;
            return;
        }

        var firstCount = (double)count;
        var secondCount = (double)otherCount;
        var combinedCount = firstCount + secondCount;
        var delta = otherMean - mean;
        var deltaSquared = delta * delta;
        var deltaCubed = deltaSquared * delta;
        var deltaFourth = deltaSquared * deltaSquared;
        var firstSecondCentralMomentSum = secondCentralMomentSum;
        var firstThirdCentralMomentSum = thirdCentralMomentSum;

        fourthCentralMomentSum += otherFourthCentralMomentSum +
                                  deltaFourth * firstCount * secondCount *
                                  (firstCount * firstCount - firstCount * secondCount + secondCount * secondCount) /
                                  (combinedCount * combinedCount * combinedCount) +
                                  6.0 * deltaSquared *
                                  (firstCount * firstCount * otherSecondCentralMomentSum + secondCount * secondCount * firstSecondCentralMomentSum) /
                                  (combinedCount * combinedCount) +
                                  4.0 * delta * (firstCount * otherThirdCentralMomentSum - secondCount * firstThirdCentralMomentSum) / combinedCount;

        thirdCentralMomentSum += otherThirdCentralMomentSum +
                                 deltaCubed * firstCount * secondCount * (firstCount - secondCount) /
                                 (combinedCount * combinedCount) +
                                 3.0 * delta * (firstCount * otherSecondCentralMomentSum - secondCount * firstSecondCentralMomentSum) / combinedCount;

        secondCentralMomentSum += otherSecondCentralMomentSum + deltaSquared * firstCount * secondCount / combinedCount;
        mean += delta * secondCount / combinedCount;
        count += otherCount;
    }

    public void Reset()
    {
        count = 0;
        mean = 0.0;
        secondCentralMomentSum = 0.0;
        thirdCentralMomentSum = 0.0;
        fourthCentralMomentSum = 0.0;
    }
}
