using System.Numerics.Tensors;
using HEAL.HeuristicLib.Objectives;

namespace HEAL.HeuristicLib.MachineLearning;

public sealed class PearsonR2Metric : IRegressionMetric
{
    public ObjectiveDirection Direction => ObjectiveDirection.Maximize;

    public double Evaluate(ReadOnlySpan<double> predictedValues, ReadOnlySpan<double> targetValues)
    {
        RegressionMetricValidation.Validate(predictedValues, targetValues);

        var predictedSum = TensorPrimitives.Sum(predictedValues);
        var targetSum = TensorPrimitives.Sum(targetValues);
        var centeredPredictedSumOfSquares = TensorPrimitives.SumOfSquares(predictedValues) - predictedSum * predictedSum / predictedValues.Length;
        var centeredTargetSumOfSquares = TensorPrimitives.SumOfSquares(targetValues) - targetSum * targetSum / targetValues.Length;

        if (centeredPredictedSumOfSquares <= 0.0 || centeredTargetSumOfSquares <= 0.0)
            return 0.0;

        var covarianceSum = TensorPrimitives.Dot(predictedValues, targetValues) - predictedSum * targetSum / predictedValues.Length;
        var correlation = covarianceSum / Math.Sqrt(centeredPredictedSumOfSquares * centeredTargetSumOfSquares);
        correlation = Math.Clamp(correlation, -1.0, 1.0);

        return correlation * correlation;
    }
}
