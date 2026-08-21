namespace HEAL.HeuristicLib.MachineLearning;

public interface IRegressionMetric : IPredictionMetric<double>;

internal static class RegressionMetricValidation
{
    internal static void Validate(ReadOnlySpan<double> predictedValues, ReadOnlySpan<double> targetValues)
    {
        if (predictedValues.Length != targetValues.Length)
            throw new ArgumentException($"Prediction count {predictedValues.Length} must match target count {targetValues.Length}.", nameof(predictedValues));

        if (predictedValues.IsEmpty)
            throw new ArgumentException("At least one prediction-target pair is required.", nameof(predictedValues));
    }
}
