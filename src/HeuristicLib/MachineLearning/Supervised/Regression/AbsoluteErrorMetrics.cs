using System.Numerics.Tensors;
using HEAL.HeuristicLib.Objectives;

namespace HEAL.HeuristicLib.MachineLearning;

public sealed class MeanAbsoluteErrorMetric : IRegressionMetric
{
    public ObjectiveDirection Direction => ObjectiveDirection.Minimize;

    public double Evaluate(ReadOnlySpan<double> predictedValues, ReadOnlySpan<double> targetValues)
    {
        RegressionMetricValidation.Validate(predictedValues, targetValues);

        var differences = new double[predictedValues.Length];
        TensorPrimitives.Subtract(predictedValues, targetValues, differences);
        return TensorPrimitives.SumOfMagnitudes(differences) / predictedValues.Length;
    }
}

public sealed class MaximumAbsoluteErrorMetric : IRegressionMetric
{
    public ObjectiveDirection Direction => ObjectiveDirection.Minimize;

    public double Evaluate(ReadOnlySpan<double> predictedValues, ReadOnlySpan<double> targetValues)
    {
        RegressionMetricValidation.Validate(predictedValues, targetValues);

        var differences = new double[predictedValues.Length];
        TensorPrimitives.Subtract(predictedValues, targetValues, differences);
        return Math.Abs(TensorPrimitives.MaxMagnitude(differences));
    }
}

public sealed class MeanLogErrorMetric : IRegressionMetric
{
    public ObjectiveDirection Direction => ObjectiveDirection.Minimize;

    public double Evaluate(ReadOnlySpan<double> predictedValues, ReadOnlySpan<double> targetValues)
    {
        RegressionMetricValidation.Validate(predictedValues, targetValues);

        var errors = new double[predictedValues.Length];
        TensorPrimitives.Subtract(predictedValues, targetValues, errors);
        TensorPrimitives.Abs(errors, errors);
        TensorPrimitives.LogP1(errors, errors);
        return TensorPrimitives.Sum(errors) / predictedValues.Length;
    }
}

public sealed class MeanRelativeErrorMetric : IRegressionMetric
{
    public ObjectiveDirection Direction => ObjectiveDirection.Minimize;

    public double Evaluate(ReadOnlySpan<double> predictedValues, ReadOnlySpan<double> targetValues)
    {
        RegressionMetricValidation.Validate(predictedValues, targetValues);

        var errors = new double[predictedValues.Length];
        var denominators = new double[targetValues.Length];
        TensorPrimitives.Subtract(predictedValues, targetValues, errors);
        TensorPrimitives.Abs(errors, errors);
        TensorPrimitives.Abs(targetValues, denominators);
        TensorPrimitives.Add(denominators, 1.0, denominators);
        TensorPrimitives.Divide(errors, denominators, errors);
        return TensorPrimitives.Sum(errors) / predictedValues.Length;
    }
}
