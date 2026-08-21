using HEAL.HeuristicLib.Objectives;

namespace HEAL.HeuristicLib.MachineLearning;

public sealed class FiniteRegressionMetric : IRegressionMetric
{
    public FiniteRegressionMetric(IRegressionMetric metric, double nonFiniteValue)
    {
        if (!double.IsFinite(nonFiniteValue))
            throw new ArgumentOutOfRangeException(nameof(nonFiniteValue), "The replacement value must be finite.");

        Metric = metric;
        NonFiniteValue = nonFiniteValue;
    }

    public IRegressionMetric Metric { get; }
    public double NonFiniteValue { get; }
    public ObjectiveDirection Direction => Metric.Direction;

    public double Evaluate(ReadOnlySpan<double> predictedValues, ReadOnlySpan<double> targetValues)
    {
        var value = Metric.Evaluate(predictedValues, targetValues);
        return double.IsFinite(value) ? value : NonFiniteValue;
    }
}

public static class RegressionMetricExtensions
{
    /// <summary>
    /// Replaces non-finite results with the worst finite value for the metric's objective direction.
    /// </summary>
    public static FiniteRegressionMetric ToFinite(this IRegressionMetric metric) =>
        metric.ToFinite(WorstFiniteValue(metric.Direction));

    /// <summary>
    /// The worst finite value for the direction. <see cref="ObjectiveValue.WorstValue"/> is an infinity and cannot
    /// serve as the replacement, which has to be finite.
    /// </summary>
    private static double WorstFiniteValue(ObjectiveDirection objectiveDirection) => objectiveDirection switch
    {
        ObjectiveDirection.Minimize => double.MaxValue,
        ObjectiveDirection.Maximize => double.MinValue,
        _ => throw new InvalidOperationException($"Unsupported objective direction: {objectiveDirection}.")
    };

    public static FiniteRegressionMetric ToFinite(this IRegressionMetric metric, double nonFiniteValue) =>
        new(metric, nonFiniteValue);
}
