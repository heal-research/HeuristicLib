using System.Numerics.Tensors;
using HEAL.HeuristicLib.Optimization;

namespace HEAL.HeuristicLib.DataAnalysis.Regression;

public sealed class MeanSquaredErrorMetric : IRegressionMetric
{
    public ObjectiveDirection Direction => ObjectiveDirection.Minimize;

    public double Evaluate(ReadOnlySpan<double> predictedValues, ReadOnlySpan<double> targetValues)
    {
        RegressionMetricValidation.Validate(predictedValues, targetValues);

        var euclideanDistance = TensorPrimitives.Distance(predictedValues, targetValues);
        return euclideanDistance * euclideanDistance / predictedValues.Length;
    }
}
