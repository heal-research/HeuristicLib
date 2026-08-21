using System.Numerics.Tensors;
using HEAL.HeuristicLib.Objectives;

namespace HEAL.HeuristicLib.MachineLearning;

public sealed class RootMeanSquaredErrorMetric : IRegressionMetric
{
    public ObjectiveDirection Direction => ObjectiveDirection.Minimize;

    public double Evaluate(ReadOnlySpan<double> predictedValues, ReadOnlySpan<double> targetValues)
    {
        RegressionMetricValidation.Validate(predictedValues, targetValues);

        var euclideanDistance = TensorPrimitives.Distance(predictedValues, targetValues);
        var meanSquaredError = euclideanDistance * euclideanDistance / predictedValues.Length;
        return Math.Sqrt(meanSquaredError);
    }
}
