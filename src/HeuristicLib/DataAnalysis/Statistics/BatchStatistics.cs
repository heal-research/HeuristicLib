using System.Numerics;

namespace HEAL.HeuristicLib.DataAnalysis.Statistics;

internal static class BatchStatistics
{
    public static bool IsBeneficial(int count) => count >= Vector<double>.Count * 2;

    public static double CalculateMean(ReadOnlySpan<double> values)
    {
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

    public static void CalculateMeanAndSecondCentralMomentSum(ReadOnlySpan<double> values, out double mean, out double secondCentralMomentSum)
    {
        mean = CalculateMean(values);

        var meanVector = new Vector<double>(mean);
        var secondCentralMomentSumVector = Vector<double>.Zero;
        var index = 0;

        for (; index <= values.Length - Vector<double>.Count; index += Vector<double>.Count)
        {
            var deviation = new Vector<double>(values[index..]) - meanVector;
            secondCentralMomentSumVector += deviation * deviation;
        }

        secondCentralMomentSum = Vector.Sum(secondCentralMomentSumVector);
        for (; index < values.Length; index++)
        {
            var deviation = values[index] - mean;
            secondCentralMomentSum += deviation * deviation;
        }
    }

    public static void CalculateCentralMomentSums(ReadOnlySpan<double> values, out double mean, out double secondCentralMomentSum, out double thirdCentralMomentSum, out double fourthCentralMomentSum)
    {
        mean = CalculateMean(values);

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

        secondCentralMomentSum = Vector.Sum(secondCentralMomentSumVector);
        thirdCentralMomentSum = Vector.Sum(thirdCentralMomentSumVector);
        fourthCentralMomentSum = Vector.Sum(fourthCentralMomentSumVector);

        for (; index < values.Length; index++)
        {
            var deviation = values[index] - mean;
            var deviationSquared = deviation * deviation;
            secondCentralMomentSum += deviationSquared;
            thirdCentralMomentSum += deviationSquared * deviation;
            fourthCentralMomentSum += deviationSquared * deviationSquared;
        }
    }

    public static void CalculateCovarianceSums(ReadOnlySpan<double> xValues, ReadOnlySpan<double> yValues, out double meanX, out double meanY, out double secondCentralMomentSumX, out double secondCentralMomentSumY, out double crossDeviationSum)
    {
        meanX = CalculateMean(xValues);
        meanY = CalculateMean(yValues);

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

        secondCentralMomentSumX = Vector.Sum(secondCentralMomentSumXVector);
        secondCentralMomentSumY = Vector.Sum(secondCentralMomentSumYVector);
        crossDeviationSum = Vector.Sum(crossDeviationSumVector);

        for (; index < xValues.Length; index++)
        {
            var deviationX = xValues[index] - meanX;
            var deviationY = yValues[index] - meanY;
            secondCentralMomentSumX += deviationX * deviationX;
            secondCentralMomentSumY += deviationY * deviationY;
            crossDeviationSum += deviationX * deviationY;
        }
    }
}
