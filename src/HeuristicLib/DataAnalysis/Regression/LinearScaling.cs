using System.Numerics.Tensors;

namespace HEAL.HeuristicLib.DataAnalysis.Regression;

public readonly record struct LinearScalingParameters(double Slope, double Intercept);

public static class LinearScaling
{
    public static LinearScalingParameters Fit(ReadOnlySpan<double> predictedValues, ReadOnlySpan<double> targetValues)
    {
        if (predictedValues.Length != targetValues.Length)
            throw new ArgumentException("The predicted and target value spans must have the same length.");
        if (predictedValues.IsEmpty)
            throw new ArgumentException("Linear scaling requires at least one predicted and target value.");

        var statistics = Statistics.Covariance(predictedValues, targetValues);

        var predictionVariance = statistics.PopulationVarianceX;
        if (predictionVariance <= 0.0 || double.IsNaN(predictionVariance))
            return new LinearScalingParameters(0.0, statistics.MeanY);

        var slope = statistics.PopulationCovariance / predictionVariance;
        var intercept = statistics.MeanY - slope * statistics.MeanX;
        return new LinearScalingParameters(slope, intercept);
    }

    public static void Apply(ReadOnlySpan<double> values, LinearScalingParameters parameters, Span<double> destination)
    {
        if (values.Length != destination.Length)
            throw new ArgumentException("The source and destination spans must have the same length.", nameof(destination));

        if (values.Overlaps(destination, out var elementOffset))
        {
            if (elementOffset != 0)
                throw new ArgumentException("The source and destination spans may only overlap when they begin at the same location.", nameof(destination));

            TensorPrimitives.Multiply(values, parameters.Slope, destination);
            TensorPrimitives.Add(destination, parameters.Intercept, destination);
            return;
        }

        destination.Fill(parameters.Intercept);
        TensorPrimitives.MultiplyAdd(values, parameters.Slope, destination, destination);
    }
}
