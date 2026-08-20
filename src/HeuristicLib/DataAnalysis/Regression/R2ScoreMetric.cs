using System.Numerics.Tensors;
using HEAL.HeuristicLib.Optimization;

namespace HEAL.HeuristicLib.DataAnalysis.Regression;

public sealed class R2ScoreMetric : IRegressionMetric
{
    public ObjectiveDirection Direction => ObjectiveDirection.Maximize;

    public double Evaluate(ReadOnlySpan<double> predictedValues, ReadOnlySpan<double> targetValues)
    {
        RegressionMetricValidation.Validate(predictedValues, targetValues);

        var residualDistance = TensorPrimitives.Distance(predictedValues, targetValues);
        var residualSumOfSquares = residualDistance * residualDistance;
        var targetSum = TensorPrimitives.Sum(targetValues);
        var totalSumOfSquares = TensorPrimitives.SumOfSquares(targetValues) - targetSum * targetSum / targetValues.Length;

        if (totalSumOfSquares <= 0.0)
            throw new ArgumentException("R2 score is undefined for a constant target series.", nameof(targetValues));

        return 1.0 - (residualSumOfSquares / totalSumOfSquares);
    }
}
