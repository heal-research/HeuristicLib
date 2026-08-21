using HEAL.HeuristicLib.Data;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.MachineLearning;

public static class FeatureImportance
{
    public static FeatureImportanceResults Permutation(IRegressor predictor, RegressionData data, IRandomNumberGenerator random, IRegressionMetric? metric = null, int repetitions = 5, IReadOnlyList<string>? featureNames = null)
    {
        return Perturbation(predictor, data, random, metric ?? Metrics.MSE, FeaturePerturbations.Permutation, repetitions, featureNames);
    }

    public static FeatureImportanceResults Permutation<T>(IPredictor<T> predictor, SupervisedData<T> data, IRandomNumberGenerator random, IPredictionMetric<T> metric, int repetitions = 5, IReadOnlyList<string>? featureNames = null)
        where T : notnull
    {
        return Perturbation(predictor, data, random, metric, FeaturePerturbations.Permutation, repetitions, featureNames);
    }

    public static ImmutableArray<FeatureImportanceResults> Permutation<T>(IPredictor<T> predictor, SupervisedData<T> data, IRandomNumberGenerator random, IReadOnlyList<IPredictionMetric<T>> metrics, int repetitions = 5, IReadOnlyList<string>? featureNames = null)
        where T : notnull
    {
        return Perturbation(predictor, data, random, metrics, FeaturePerturbations.Permutation, repetitions, featureNames);
    }

    public static FeatureImportanceResults Perturbation(IRegressor predictor, RegressionData data, IRandomNumberGenerator random, IRegressionMetric metric, FeaturePerturbation perturbation, int repetitions = 5, IReadOnlyList<string>? featureNames = null)
    {
        return Perturbation<double>(predictor, data, random, metric, perturbation, repetitions, featureNames);
    }

    public static FeatureImportanceResults Perturbation<T>(IPredictor<T> predictor, SupervisedData<T> data, IRandomNumberGenerator random, IPredictionMetric<T> metric, FeaturePerturbation perturbation, int repetitions = 5, IReadOnlyList<string>? featureNames = null)
        where T : notnull
    {
        return Perturbation(predictor, data, random, [metric], perturbation, repetitions, featureNames)[0];
    }

    public static ImmutableArray<FeatureImportanceResults> Perturbation<T>(IPredictor<T> predictor, SupervisedData<T> data, IRandomNumberGenerator random, IReadOnlyList<IPredictionMetric<T>> metrics, FeaturePerturbation perturbation, int repetitions = 5, IReadOnlyList<string>? featureNames = null)
        where T : notnull
    {
        if (metrics.Count == 0)
            throw new ArgumentException("At least one prediction metric is required.", nameof(metrics));

        if (repetitions < 1)
            throw new ArgumentOutOfRangeException(nameof(repetitions));

        if (data.RowCount == 0)
            throw new ArgumentException("At least one observation is required.", nameof(data));

        var selectedFeatures = new List<(int Index, Series<double> Series)>();
        if (featureNames is null)
        {
            for (var i = 0; i < data.Inputs.ColumnCount; i++)
            {
                if (data.Inputs[i] is Series<double> series)
                    selectedFeatures.Add((i, series));
            }
        }
        else
        {
            var uniqueNames = new HashSet<string>(StringComparer.Ordinal);
            foreach (var featureName in featureNames)
            {
                if (!uniqueNames.Add(featureName))
                    throw new ArgumentException($"Feature '{featureName}' is specified more than once.", nameof(featureNames));

                var series = data.Inputs.Get<double>(featureName);
                for (var i = 0; i < data.Inputs.ColumnCount; i++)
                {
                    if (data.Inputs[i].Name != featureName)
                        continue;

                    selectedFeatures.Add((i, series));
                    break;
                }
            }
        }

        var predictions = new T[data.RowCount];
        predictor.Predict(data.Inputs, predictions);

        var baselineMetricValues = new double[metrics.Count];
        for (var metricIndex = 0; metricIndex < metrics.Count; metricIndex++)
        {
            baselineMetricValues[metricIndex] = metrics[metricIndex].Evaluate(predictions, data.Target.Values.Span);
        }

        var perturbedMetricValues = new double[metrics.Count][][];
        var importanceValues = new double[metrics.Count][][];
        for (var metricIndex = 0; metricIndex < metrics.Count; metricIndex++)
        {
            perturbedMetricValues[metricIndex] = new double[selectedFeatures.Count][];
            importanceValues[metricIndex] = new double[selectedFeatures.Count][];
            for (var featureIndex = 0; featureIndex < selectedFeatures.Count; featureIndex++)
            {
                perturbedMetricValues[metricIndex][featureIndex] = new double[repetitions];
                importanceValues[metricIndex][featureIndex] = new double[repetitions];
            }
        }

        var columns = data.Inputs.Columns.ToArray();
        for (var featureIndex = 0; featureIndex < selectedFeatures.Count; featureIndex++)
        {
            var feature = selectedFeatures[featureIndex];
            for (var repetition = 0; repetition < repetitions; repetition++)
            {
                var perturbedValues = new double[data.RowCount];
                perturbation.Apply(feature.Series.Values.Span, perturbedValues, random);
                columns[feature.Index] = Series<double>.FromOwnedArray(feature.Series.Name, perturbedValues);

                var perturbedInputs = new DataFrame(columns);
                predictor.Predict(perturbedInputs, predictions);

                for (var metricIndex = 0; metricIndex < metrics.Count; metricIndex++)
                {
                    var metric = metrics[metricIndex];
                    var perturbedMetricValue = metric.Evaluate(predictions, data.Target.Values.Span);
                    perturbedMetricValues[metricIndex][featureIndex][repetition] = perturbedMetricValue;
                    importanceValues[metricIndex][featureIndex][repetition] = metric.Direction switch
                    {
                        ObjectiveDirection.Minimize => perturbedMetricValue - baselineMetricValues[metricIndex],
                        ObjectiveDirection.Maximize => baselineMetricValues[metricIndex] - perturbedMetricValue,
                        _ => throw new InvalidOperationException($"Unsupported objective direction '{metric.Direction}'.")
                    };
                }
            }

            columns[feature.Index] = feature.Series;
        }

        var results = ImmutableArray.CreateBuilder<FeatureImportanceResults>(metrics.Count);
        for (var metricIndex = 0; metricIndex < metrics.Count; metricIndex++)
        {
            var features = ImmutableArray.CreateBuilder<FeatureImportanceResult>(selectedFeatures.Count);
            for (var featureIndex = 0; featureIndex < selectedFeatures.Count; featureIndex++)
            {
                features.Add(new FeatureImportanceResult(
                    selectedFeatures[featureIndex].Series.Name,
                    [.. perturbedMetricValues[metricIndex][featureIndex]],
                    [.. importanceValues[metricIndex][featureIndex]]));
            }

            results.Add(new FeatureImportanceResults(metrics[metricIndex], baselineMetricValues[metricIndex], features.MoveToImmutable()));
        }

        return results.MoveToImmutable();
    }
}
