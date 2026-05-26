using HEAL.HeuristicLib.Optimization;

namespace HEAL.HeuristicLib.Problems.DataAnalysis.Regression;

public sealed class R2ScoreMetric : IRegressionMetric
{
    public ObjectiveDirection Direction => ObjectiveDirection.Maximize;

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

        var mean = 0.0;
        for (var i = 0; i < targetValues.Length; i++)
        {
            mean += targetValues[i];
        }

        mean /= targetValues.Length;

        var residualSumOfSquares = 0.0;
        var totalSumOfSquares = 0.0;
        for (var i = 0; i < predictedValues.Length; i++)
        {
            var residual = targetValues[i] - predictedValues[i];
            residualSumOfSquares += residual * residual;

            var centeredTarget = targetValues[i] - mean;
            totalSumOfSquares += centeredTarget * centeredTarget;
        }

        if (totalSumOfSquares <= 0.0)
            throw new ArgumentException("R2 score is undefined for a constant target series.", nameof(targetValues));

        return 1.0 - (residualSumOfSquares / totalSumOfSquares);
    }
}
