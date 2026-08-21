using System.Numerics.Tensors;

namespace HEAL.HeuristicLib.MachineLearning;

public sealed class FeatureImportanceResults
{
    internal FeatureImportanceResults(IPredictionMetric metric, double baselineMetricValue, ImmutableArray<FeatureImportanceResult> features)
    {
        Metric = metric;
        BaselineMetricValue = baselineMetricValue;
        Features = features;
    }

    public IPredictionMetric Metric { get; }
    public double BaselineMetricValue { get; }
    public ImmutableArray<FeatureImportanceResult> Features { get; }
}

public sealed class FeatureImportanceResult
{
    internal FeatureImportanceResult(string featureName, ImmutableArray<double> perturbedMetricValues, ImmutableArray<double> importanceValues)
    {
        FeatureName = featureName;
        PerturbedMetricValues = perturbedMetricValues;
        ImportanceValues = importanceValues;

        MeanImportance = TensorPrimitives.Sum(importanceValues.AsSpan()) / importanceValues.Length;
        var variance = (TensorPrimitives.SumOfSquares(importanceValues.AsSpan()) / importanceValues.Length) - (MeanImportance * MeanImportance);
        StandardDeviation = Math.Sqrt(Math.Max(0.0, variance));
    }

    public string FeatureName { get; }
    public ImmutableArray<double> PerturbedMetricValues { get; }
    public ImmutableArray<double> ImportanceValues { get; }
    public double MeanImportance { get; }
    public double StandardDeviation { get; }
}
