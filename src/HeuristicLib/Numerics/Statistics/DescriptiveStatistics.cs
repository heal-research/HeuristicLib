using System.Numerics;

namespace HEAL.HeuristicLib.Numerics;

public static class DescriptiveStatistics
{
    public static double Mean(ReadOnlySpan<double> values)
    {
        if (values.IsEmpty)
            return double.NaN;
        if (values.Length == 1)
            return values[0];

        var origin = values[0];
        var originVector = new Vector<double>(origin);
        var sum = Vector<double>.Zero;
        var index = 0;

        for (; index <= values.Length - Vector<double>.Count; index += Vector<double>.Count)
            sum += new Vector<double>(values[index..]) - originVector;

        var deviationSum = Vector.Sum(sum);
        for (; index < values.Length; index++)
            deviationSum += values[index] - origin;

        return origin + deviationSum / values.Length;
    }

    public static MeanVarianceStatistics MeanVariance(ReadOnlySpan<double> values) => new(AccumulateMeanVariance(values));

    public static MomentStatistics Moments(ReadOnlySpan<double> values) => new(AccumulateMoments(values));

    public static CovarianceStatistics Covariance(ReadOnlySpan<double> xValues, ReadOnlySpan<double> yValues) => new(AccumulateCovariance(xValues, yValues));

    internal static MeanVarianceAccumulator AccumulateMeanVariance(ReadOnlySpan<double> values)
    {
        if (values.IsEmpty)
            return default;

        var mean = DescriptiveStatistics.Mean(values);
        var meanVector = new Vector<double>(mean);
        var secondCentralMomentSumVector = Vector<double>.Zero;
        var index = 0;

        for (; index <= values.Length - Vector<double>.Count; index += Vector<double>.Count)
        {
            var deviation = new Vector<double>(values[index..]) - meanVector;
            secondCentralMomentSumVector += deviation * deviation;
        }

        var secondCentralMomentSum = Vector.Sum(secondCentralMomentSumVector);
        for (; index < values.Length; index++)
        {
            var deviation = values[index] - mean;
            secondCentralMomentSum += deviation * deviation;
        }

        return new MeanVarianceAccumulator(values.Length, mean, secondCentralMomentSum);
    }

    internal static MomentAccumulator AccumulateMoments(ReadOnlySpan<double> values)
    {
        if (values.IsEmpty)
            return default;

        var mean = DescriptiveStatistics.Mean(values);
        var meanVector = new Vector<double>(mean);
        var secondCentralMomentSumVector = Vector<double>.Zero;
        var thirdCentralMomentSumVector = Vector<double>.Zero;
        var fourthCentralMomentSumVector = Vector<double>.Zero;
        var index = 0;

        for (; index <= values.Length - Vector<double>.Count; index += Vector<double>.Count)
        {
            var deviation = new Vector<double>(values[index..]) - meanVector;
            var deviationSquared = deviation * deviation;
            secondCentralMomentSumVector += deviationSquared;
            thirdCentralMomentSumVector += deviationSquared * deviation;
            fourthCentralMomentSumVector += deviationSquared * deviationSquared;
        }

        var secondCentralMomentSum = Vector.Sum(secondCentralMomentSumVector);
        var thirdCentralMomentSum = Vector.Sum(thirdCentralMomentSumVector);
        var fourthCentralMomentSum = Vector.Sum(fourthCentralMomentSumVector);

        for (; index < values.Length; index++)
        {
            var deviation = values[index] - mean;
            var deviationSquared = deviation * deviation;
            secondCentralMomentSum += deviationSquared;
            thirdCentralMomentSum += deviationSquared * deviation;
            fourthCentralMomentSum += deviationSquared * deviationSquared;
        }

        return new MomentAccumulator(values.Length, mean, secondCentralMomentSum, thirdCentralMomentSum, fourthCentralMomentSum);
    }

    internal static CovarianceAccumulator AccumulateCovariance(ReadOnlySpan<double> xValues, ReadOnlySpan<double> yValues)
    {
        if (xValues.Length != yValues.Length)
            throw new ArgumentException("The paired value spans must have the same length.");
        if (xValues.IsEmpty)
            return default;

        var meanX = DescriptiveStatistics.Mean(xValues);
        var meanY = DescriptiveStatistics.Mean(yValues);
        var meanXVector = new Vector<double>(meanX);
        var meanYVector = new Vector<double>(meanY);
        var secondCentralMomentSumXVector = Vector<double>.Zero;
        var secondCentralMomentSumYVector = Vector<double>.Zero;
        var crossDeviationSumVector = Vector<double>.Zero;
        var index = 0;

        for (; index <= xValues.Length - Vector<double>.Count; index += Vector<double>.Count)
        {
            var deviationX = new Vector<double>(xValues[index..]) - meanXVector;
            var deviationY = new Vector<double>(yValues[index..]) - meanYVector;
            secondCentralMomentSumXVector += deviationX * deviationX;
            secondCentralMomentSumYVector += deviationY * deviationY;
            crossDeviationSumVector += deviationX * deviationY;
        }

        var secondCentralMomentSumX = Vector.Sum(secondCentralMomentSumXVector);
        var secondCentralMomentSumY = Vector.Sum(secondCentralMomentSumYVector);
        var crossDeviationSum = Vector.Sum(crossDeviationSumVector);

        for (; index < xValues.Length; index++)
        {
            var deviationX = xValues[index] - meanX;
            var deviationY = yValues[index] - meanY;
            secondCentralMomentSumX += deviationX * deviationX;
            secondCentralMomentSumY += deviationY * deviationY;
            crossDeviationSum += deviationX * deviationY;
        }

        return new CovarianceAccumulator(xValues.Length, meanX, meanY, secondCentralMomentSumX, secondCentralMomentSumY, crossDeviationSum);
    }
}

public readonly record struct MeanVarianceStatistics
{
    private readonly MeanVarianceAccumulator accumulator;

    internal MeanVarianceStatistics(MeanVarianceAccumulator accumulator)
    {
        this.accumulator = accumulator;
    }

    public long Count => accumulator.Count;
    public double Mean => Count > 0 ? accumulator.Mean : double.NaN;
    public double PopulationVariance => Count > 0 ? accumulator.SecondCentralMomentSum / Count : double.NaN;
    public double SampleVariance => Count > 1 ? accumulator.SecondCentralMomentSum / (Count - 1) : double.NaN;
    public double PopulationStandardDeviation => Math.Sqrt(PopulationVariance);
    public double SampleStandardDeviation => Math.Sqrt(SampleVariance);
}

public readonly record struct MomentStatistics
{
    private readonly MomentAccumulator accumulator;

    internal MomentStatistics(MomentAccumulator accumulator)
    {
        this.accumulator = accumulator;
    }

    public long Count => accumulator.Count;
    public double Mean => Count > 0 ? accumulator.Mean : double.NaN;
    public double PopulationVariance => Count > 0 ? accumulator.SecondCentralMomentSum / Count : double.NaN;
    public double SampleVariance => Count > 1 ? accumulator.SecondCentralMomentSum / (Count - 1) : double.NaN;
    public double PopulationStandardDeviation => Math.Sqrt(PopulationVariance);
    public double SampleStandardDeviation => Math.Sqrt(SampleVariance);

    public double PopulationSkewness => Count > 0 && accumulator.SecondCentralMomentSum > 0.0
        ? Math.Sqrt(Count) * accumulator.ThirdCentralMomentSum / Math.Pow(accumulator.SecondCentralMomentSum, 1.5)
        : double.NaN;

    public double SampleSkewness => Count > 2
        ? Math.Sqrt(Count * (Count - 1.0)) / (Count - 2.0) * PopulationSkewness
        : double.NaN;

    public double PopulationExcessKurtosis => Count > 0 && accumulator.SecondCentralMomentSum > 0.0
        ? Count * accumulator.FourthCentralMomentSum / (accumulator.SecondCentralMomentSum * accumulator.SecondCentralMomentSum) - 3.0
        : double.NaN;

    public double SampleExcessKurtosis => Count > 3
        ? (Count - 1.0) / ((Count - 2.0) * (Count - 3.0)) * ((Count + 1.0) * PopulationExcessKurtosis + 6.0)
        : double.NaN;
}

public readonly record struct CovarianceStatistics
{
    private readonly CovarianceAccumulator accumulator;

    internal CovarianceStatistics(CovarianceAccumulator accumulator)
    {
        this.accumulator = accumulator;
    }

    public long Count => accumulator.Count;
    public double MeanX => Count > 0 ? accumulator.MeanX : double.NaN;
    public double MeanY => Count > 0 ? accumulator.MeanY : double.NaN;
    public double PopulationVarianceX => Count > 0 ? accumulator.SecondCentralMomentSumX / Count : double.NaN;
    public double PopulationVarianceY => Count > 0 ? accumulator.SecondCentralMomentSumY / Count : double.NaN;
    public double SampleVarianceX => Count > 1 ? accumulator.SecondCentralMomentSumX / (Count - 1) : double.NaN;
    public double SampleVarianceY => Count > 1 ? accumulator.SecondCentralMomentSumY / (Count - 1) : double.NaN;
    public double PopulationCovariance => Count > 0 ? accumulator.CrossDeviationSum / Count : double.NaN;
    public double SampleCovariance => Count > 1 ? accumulator.CrossDeviationSum / (Count - 1) : double.NaN;

    public double Correlation => Count > 1 && accumulator.SecondCentralMomentSumX > 0.0 && accumulator.SecondCentralMomentSumY > 0.0
        ? Math.Clamp(accumulator.CrossDeviationSum / Math.Sqrt(accumulator.SecondCentralMomentSumX * accumulator.SecondCentralMomentSumY), -1.0, 1.0)
        : double.NaN;
}

internal readonly record struct MeanVarianceAccumulator(
    long Count,
    double Mean,
    double SecondCentralMomentSum);

internal readonly record struct MomentAccumulator(
    long Count,
    double Mean,
    double SecondCentralMomentSum,
    double ThirdCentralMomentSum,
    double FourthCentralMomentSum);

internal readonly record struct CovarianceAccumulator(
    long Count,
    double MeanX,
    double MeanY,
    double SecondCentralMomentSumX,
    double SecondCentralMomentSumY,
    double CrossDeviationSum);
