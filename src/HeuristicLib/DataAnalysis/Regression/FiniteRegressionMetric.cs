using HEAL.HeuristicLib.Optimization;

namespace HEAL.HeuristicLib.DataAnalysis.Regression;

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
        metric.ToFinite(ObjectiveValue.WorstValue(metric.Direction).Value);

    public static FiniteRegressionMetric ToFinite(this IRegressionMetric metric, double nonFiniteValue) =>
        new(metric, nonFiniteValue);
}
