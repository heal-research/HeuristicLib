using System.Numerics.Tensors;
using HEAL.HeuristicLib.Optimization;

namespace HEAL.HeuristicLib.DataAnalysis.Regression;

public sealed class NormalizedMeanSquaredErrorMetric : IRegressionMetric
{
    public ObjectiveDirection Direction => ObjectiveDirection.Minimize;

    public double Evaluate(ReadOnlySpan<double> predictedValues, ReadOnlySpan<double> targetValues)
    {
        RegressionMetricValidation.Validate(predictedValues, targetValues);

        var targetSum = TensorPrimitives.Sum(targetValues);
        var targetSumOfSquares = TensorPrimitives.SumOfSquares(targetValues);
        var centeredTargetSumOfSquares = targetSumOfSquares - targetSum * targetSum / targetValues.Length;

        if (centeredTargetSumOfSquares <= 0.0)
            return 0.0;

        var residualDistance = TensorPrimitives.Distance(predictedValues, targetValues);
        return residualDistance * residualDistance / centeredTargetSumOfSquares;
    }
}
