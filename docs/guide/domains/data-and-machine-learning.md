# Data and machine learning

Use these APIs when an optimization problem works with tabular observations, regression targets or prediction metrics. They are small domain building blocks, not a general replacement for a dataframe library.

The types on this page live in `HEAL.HeuristicLib.Data` for tabular values, `HEAL.HeuristicLib.MachineLearning` for predictors, metrics and scaling, and `HEAL.HeuristicLib.Numerics` for descriptive and running statistics.

## Represent regression data

HeuristicLib represents tabular inputs with `DataFrame` and typed columns with
`Series<T>`. Regression data binds one input frame to a named `Series<double>`
target:

```csharp
var data = new RegressionData(
    DataFrame.FromMatrix(
        ["x0", "x1"],
        new double[,]
        {
            { 1.0, 2.0 },
            { 3.0, 4.0 }
        }),
    new Series<double>("target", [5.0, 11.0]));
```

## Descriptive statistics

`DescriptiveStatistics` calculates one-shot summaries directly from spans:

```csharp
ReadOnlySpan<double> values = [2.0, 4.0, 4.0, 6.0];
ReadOnlySpan<double> otherValues = [1.0, 2.0, 3.0, 4.0];

double mean = DescriptiveStatistics.Mean(values);
MeanVarianceStatistics variance = DescriptiveStatistics.MeanVariance(values);
MomentStatistics moments = DescriptiveStatistics.Moments(values);
CovarianceStatistics covariance = DescriptiveStatistics.Covariance(values, otherValues);

Console.WriteLine($"mean                {mean}");
Console.WriteLine($"population variance {variance.PopulationVariance}");
Console.WriteLine($"sample variance     {variance.SampleVariance}");
Console.WriteLine($"population skewness {moments.PopulationSkewness:F4}");
Console.WriteLine($"sample covariance   {covariance.SampleCovariance:F4}");
```

```
mean                4
population variance 2
sample variance     2.6666666666666665
population skewness 0.0000
sample covariance   2.0000
```

The two variances differ because one divides by `Count` and the other by `Count - 1`, which is the distinction the property names carry. Every composite result exposes both, so you choose at the point of use instead of at the point of calculation.

Composite results expose `Count` so they remain meaningful after the source
span is no longer available. `Count` is a `long` because running statistics can
merge multiple batches even though one span is limited to `int` length.

Population properties divide by `Count`. Sample variance and covariance divide
by `Count - 1`; sample skewness and kurtosis apply their corresponding
finite-sample corrections. A statistic returns `double.NaN` when it is not
defined, including empty input and sample statistics with too few observations.
Paired calculations reject spans with different lengths.

Use `RunningMean`, `RunningMeanVariance`, `RunningMoments`, and
`RunningCovariance` when observations arrive incrementally. These mutable,
non-thread-safe accumulators support individual observations, span batches,
merging independent accumulators, and resetting. Batch updates use the same
centered calculations as the one-shot API and do not retain observations.

## Feature importance

`FeatureImportance` measures how much a fitted predictor relies on each input feature
by perturbing one feature at a time and observing how the metric changes:

```csharp
FeatureImportanceResults results = FeatureImportance.Permutation(regressor, data, random);
```

The simple entry point uses MSE, five repetitions, and every numeric input feature.
Generic overloads accept any `IPredictor<T>` with a matching `IPredictionMetric<T>`,
and a multi-metric overload reuses one baseline prediction and each perturbed
prediction across all metrics.

`Perturbation` takes an explicit replacement policy instead of assuming permutation:

```csharp
var results = FeatureImportance.Perturbation(
    regressor, data, random, Metrics.MSE, FeaturePerturbations.Median);
```

`FeaturePerturbations` offers `Permutation`, `Mean`, `Median`, and
`Resampling(distribution)`. Resampling without an explicit distribution derives a
normal distribution from each feature's own mean and population standard deviation.

**Positive importance always means perturbing the feature made the metric worse**,
whether the metric is minimized or maximized. `FeatureImportanceResults` holds the
baseline plus one `FeatureImportanceResult` per feature, each retaining the raw
perturbed metric and importance values alongside their mean and population standard
deviation.

Row subsampling is deliberately absent: construct the `DataFrame` you want before
calling. Population weights, parallel execution, grouped features, and categorical
perturbation are not implemented.

## Linear scaling

Least-squares linear scaling fits an affine transformation of raw predictions:

```text
scaled = slope * prediction + intercept
```

Use the span API when predictions are already available:

```csharp
LinearScalingParameters parameters = LinearScaling.Fit(predictions, targets);
LinearScaling.Apply(predictions, parameters, scaledPredictions);
```

`Fit` minimizes squared error on the supplied observations. If every raw
prediction is equal, it returns a zero slope and maps all predictions to the
target mean. `Apply` accepts a separate destination or the same span for an
in-place transformation. Partially overlapping spans are rejected.

To retain fitted scaling for later prediction, decorate an `IRegressor`:

```csharp
LinearlyScaledRegressor scaled = regressor.FitLinearScaling(trainingData);
Series<double> predictions = scaled.Predict(testInputs);
```

Enabling `useLinearScaling` on `SymbolicRegressionProblem` fits one
least-squares transformation for each candidate evaluation and applies the same
scaled predictions to every configured prediction metric. Scaling does not
modify the expression genotype and therefore does not affect expression
identity, length, containment, mutation, or crossover.

Least-squares scaling is authoritative when enabled even if the problem also
uses metrics such as mean absolute error. It minimizes squared error and is not
guaranteed to improve every other metric. Non-finite observations follow normal
IEEE floating-point propagation; a broader invalid-prediction policy remains a
separate problem-evaluation concern.

Continue with [Symbolic regression](/guide/domains/symbolic-regression) to train expressions from data or [Symbolic expressions](/guide/domains/symbolic-expressions) to build and inspect expression candidates directly.
