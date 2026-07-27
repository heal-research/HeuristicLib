using HEAL.HeuristicLib.Optimization;

namespace HEAL.HeuristicLib.DataAnalysis;

public interface IPredictionMetric
{
    ObjectiveDirection Direction { get; }
}

public interface IPredictionMetric<T> : IPredictionMetric
    where T : notnull
{
    double Evaluate(ReadOnlySpan<T> predictedValues, ReadOnlySpan<T> targetValues);
}

public static class PredictionMetricExtensions
{
    public static double Evaluate<T>(this IPredictionMetric<T> metric, Series<T> predictedValues, Series<T> targetValues)
        where T : notnull
    {
        return metric.Evaluate(predictedValues.Values.Span, targetValues.Values.Span);
    }
}
