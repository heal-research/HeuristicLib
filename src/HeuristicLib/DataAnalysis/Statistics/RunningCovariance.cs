namespace HEAL.HeuristicLib.DataAnalysis;

/// <summary>Accumulates paired means, variances, covariance, and correlation without retaining observations.</summary>
/// <remarks>Instances are mutable and not thread-safe. Independent accumulators can be merged.</remarks>
public sealed class RunningCovariance
{
    private long count;
    private double meanX;
    private double meanY;
    private double secondCentralMomentSumX;
    private double secondCentralMomentSumY;
    private double crossDeviationSum;

    public long Count => count;
    public double MeanX => count > 0 ? meanX : double.NaN;
    public double MeanY => count > 0 ? meanY : double.NaN;
    public double PopulationVarianceX => count > 0 ? secondCentralMomentSumX / count : double.NaN;
    public double PopulationVarianceY => count > 0 ? secondCentralMomentSumY / count : double.NaN;
    public double SampleVarianceX => count > 1 ? secondCentralMomentSumX / (count - 1) : double.NaN;
    public double SampleVarianceY => count > 1 ? secondCentralMomentSumY / (count - 1) : double.NaN;
    public double PopulationCovariance => count > 0 ? crossDeviationSum / count : double.NaN;
    public double SampleCovariance => count > 1 ? crossDeviationSum / (count - 1) : double.NaN;

    public double Correlation => count > 1 && secondCentralMomentSumX > 0.0 && secondCentralMomentSumY > 0.0
        ? Math.Clamp(crossDeviationSum / Math.Sqrt(secondCentralMomentSumX * secondCentralMomentSumY), -1.0, 1.0)
        : double.NaN;

    public void Add(double x, double y)
    {
        count++;
        var deltaX = x - meanX;
        var deltaY = y - meanY;
        meanX += deltaX / count;
        meanY += deltaY / count;
        secondCentralMomentSumX += deltaX * (x - meanX);
        secondCentralMomentSumY += deltaY * (y - meanY);
        crossDeviationSum += deltaX * (y - meanY);
    }

    public void AddRange(ReadOnlySpan<double> xValues, ReadOnlySpan<double> yValues)
    {
        if (xValues.Length != yValues.Length)
            throw new ArgumentException("The paired value spans must have the same length.");

        if (xValues.IsEmpty)
            return;

        var accumulator = Statistics.AccumulateCovariance(xValues, yValues);
        Merge(accumulator);
    }

    public void Merge(RunningCovariance other)
    {
        Merge(new CovarianceAccumulator(other.count, other.meanX, other.meanY, other.secondCentralMomentSumX, other.secondCentralMomentSumY, other.crossDeviationSum));
    }

    private void Merge(CovarianceAccumulator other)
    {
        if (other.Count <= 0)
            return;

        if (count == 0)
        {
            count = other.Count;
            meanX = other.MeanX;
            meanY = other.MeanY;
            secondCentralMomentSumX = other.SecondCentralMomentSumX;
            secondCentralMomentSumY = other.SecondCentralMomentSumY;
            crossDeviationSum = other.CrossDeviationSum;
            return;
        }

        var combinedCount = count + other.Count;
        var factor = (double)count * other.Count / combinedCount;
        var deltaX = other.MeanX - meanX;
        var deltaY = other.MeanY - meanY;
        secondCentralMomentSumX += other.SecondCentralMomentSumX + deltaX * deltaX * factor;
        secondCentralMomentSumY += other.SecondCentralMomentSumY + deltaY * deltaY * factor;
        crossDeviationSum += other.CrossDeviationSum + deltaX * deltaY * factor;
        meanX += deltaX * other.Count / combinedCount;
        meanY += deltaY * other.Count / combinedCount;
        count = combinedCount;
    }

    public void Reset()
    {
        count = 0;
        meanX = 0.0;
        meanY = 0.0;
        secondCentralMomentSumX = 0.0;
        secondCentralMomentSumY = 0.0;
        crossDeviationSum = 0.0;
    }
}
