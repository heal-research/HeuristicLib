using HEAL.HeuristicLib.Optimization;

namespace HEAL.HeuristicLib.Problems.DataAnalysis.Regression;

public sealed class RootMeanSquaredErrorMetric : IRegressionMetric
{
    public ObjectiveDirection Direction => ObjectiveDirection.Minimize;

    public double Evaluate(ReadOnlySpan<double> predictedValues, ReadOnlySpan<double> targetValues)
    {
        if (predictedValues.Length != targetValues.Length)
        {
            throw new ArgumentException(
                $"Prediction count {predictedValues.Length} must match target count {targetValues.Length}.",
                nameof(predictedValues));
        }

        if (predictedValues.IsEmpty)
            throw new ArgumentException("At least one prediction-target pair is required.", nameof(predictedValues));

        var squaredErrorSum = 0.0;
        for (var i = 0; i < predictedValues.Length; i++)
        {
            var error = predictedValues[i] - targetValues[i];
            squaredErrorSum += error * error;
        }

        return Math.Sqrt(squaredErrorSum / predictedValues.Length);
    }
}
