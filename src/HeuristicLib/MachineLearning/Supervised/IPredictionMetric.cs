using HEAL.HeuristicLib.Data;
using HEAL.HeuristicLib.Objectives;

namespace HEAL.HeuristicLib.MachineLearning;

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
    extension<T>(IPredictionMetric<T> metric)
        where T : notnull
    {
        public double Evaluate(Series<T> predictedValues, Series<T> targetValues)
        {
            return metric.Evaluate(predictedValues.Values.Span, targetValues.Values.Span);
        }
    }
}
