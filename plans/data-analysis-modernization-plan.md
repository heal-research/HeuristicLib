# Data-Analysis Modernization Plan

## Purpose

Modernize HeuristicLib's data-analysis APIs incrementally while the legacy
`Dataset`-based workflows remain available. New APIs are designed around
immutable columnar data, familiar predictor and estimator terminology, and
span-based numerical hot paths.

This is a replacement program, not a compatibility wrapper over the legacy
model. Legacy components remain active until a maintained replacement covers
their behavior and tests. No legacy component is deleted without explicit
approval.

## Current Foundation

### General data analysis

The `HEAL.HeuristicLib.DataAnalysis` namespace owns:

- `Series` and `Series<T>` as immutable named columns;
- `DataFrame` as an immutable, insertion-ordered heterogeneous column set;
- `SupervisedData<TTarget>` as the shared input/target pairing;
- `IPredictor` and `IPredictor<TPrediction>` for fitted prediction behavior;
- `IPredictionMetric<TPrediction>` for prediction/target comparison with an
  explicit objective direction;
- `IEstimator<TTrainingData, TPredictor>` for asynchronous fitting with
  explicit randomness.

Names live on series and are the sole semantic source of column names.
`DataFrame` maintains only a derived ordinal name-to-index cache.

Data-domain objects deliberately have no row slicing API. Numerical consumers
work with `ReadOnlyMemory<T>`, `ReadOnlySpan<T>`, and caller-provided
`Span<T>` values.

### Regression

The `HEAL.HeuristicLib.DataAnalysis.Regression` namespace owns:

- `RegressionData`;
- `IRegressor`;
- `IRegressionMetric` and the standard metric implementations;
- `SymbolicRegressor`, the fitted predictor for immutable expression trees;
- `BoundedRegressor`, a bounds decorator for any `IRegressor`.

`RegressionData` does not encode training, validation, or test roles. A
workflow assigns those roles by how it uses separate `RegressionData`
instances.

### Algorithm binding

`HEAL.HeuristicLib.Problems.DataAnalysis.Regression.SymbolicRegressionProblem`
binds one `RegressionData` training set, prediction metrics, expression
metrics, and one `ExpressionTreeSearchSpace` for optimization. Prediction
metrics score one shared prediction vector; expression metrics inspect the
genotype without requiring prediction. Their values form one objective vector,
with prediction metrics first. The simple constructor defaults to MSE.

At least one prediction or expression metric is required. An entirely empty
objective configuration is rejected rather than treated as an implicit
all-equal objective. Multiple objectives use lexicographic total order by
default while retaining their individual directions for dominance comparison.
The problem evaluates only its bound training data.

Prediction on validation, test, or arbitrary inputs belongs to an
`IPredictor`. Metric calculation outside optimization remains an explicit
caller action.

The old mutable symbolic-regression problem remains under the `.Legacy`
namespace while dependent legacy workflows are active.

## Design Constraints

- Data containers use reference identity rather than dataset-wide value
  equality or hashing.
- Column names are ordinal and case-sensitive.
- Typed frame lookup does not perform reflection-based construction or
  conversion.
- Ordinary series construction copies values; `FromOwnedArray` explicitly
  transfers ownership.
- Predictors expose both allocating and caller-provided destination paths.
- Estimators receive explicit randomness and return the fitted predictor
  directly.
- Problems define algorithm-facing evaluation, not general prediction APIs.
- Empty-data behavior is not hardened until concrete use cases establish its
  required semantics.

## Migration Strategy

Migrate one complete behavior at a time:

1. introduce the maintained replacement and its tests;
2. migrate active consumers that fit the new model;
3. compare legacy tests with replacement coverage;
4. keep still-required implementations in their existing domain folders and use
   `.Legacy` namespaces only where type-name collisions require them;
5. request approval before deleting any superseded component.

Do not force classification, clustering, time-series, or Python workflows
through regression-only abstractions merely to reduce the visible legacy
surface.

The old classification and clustering packages had no consumers or tests
outside their own source folders and were removed with explicit approval because
neither domain is currently maintained. Classification-specific online
calculators were removed with that subsystem. Future classification or
clustering support will be designed against the modern data-analysis foundation
instead of reviving those APIs.

The unused legacy `DatasetUtil` was also removed. Its `ShuffleLists` extension
had no consumers or tests, and its static initialization produced delegates
that were immediately discarded, so there was no behavior to retain on
`Dataset` or `ModifiableDataset`.

The remaining `Dataset`-based support group stays in its established
`Problems/DataAnalysis` domain folders while preserving its existing behavior.
This group includes `Dataset`, `ModifiableDataset`, their data-analysis problem
and regression-data abstractions, the old regression-model contracts, CSV
loading, and related consumers. Active AutoDiff conversion and
symbolic-regression parameter optimization continue to use these types until
maintained replacements exist.

The duplicate empty `IProblemData` marker interfaces were removed. Neither
participated in generic constraints or behavior, so retaining two unrelated
markers only made the problem-data hierarchy less clear.

## Follow-Up Work

### Data semantics

- define empty-frame and empty-supervised-data behavior;
- design missing-value representation and validation;
- decide whether arbitrary row views are needed for sampling and
  cross-validation;
- add serialization and schema inspection;
- add preprocessing and transformation pipelines.

### Learning domains

- design classification data, predictors, and metrics;
- decide categorical and encoded target representations;
- migrate clustering and time-series data independently;
- add concrete estimator APIs and fitted symbolic-model types.

### Prediction lifecycle

- decide whether fitted symbolic regressors retain compiled expressions;
- define predictor serialization and reproducibility metadata;
- design general bounded-regressor composition if bounds prove useful beyond
  symbolic expressions;
- add fit results only when workflows require diagnostics beyond the returned
  predictor.

### Model inspection

`HEAL.HeuristicLib.DataAnalysis.Inspection` provides the first maintained model
inspection workflow through the `FeatureImportance` facade. `Permutation` is
the familiar shortcut, while `Perturbation` accepts an explicit replacement
policy. The simple permutation entry point uses MSE, five repetitions, and
every numeric input feature. Generic overloads support any matching prediction
and target type through `IPredictionMetric<T>`. Multi-metric calculation reuses
one baseline prediction and every perturbed prediction across all metrics.

`FeatureImportanceResults` contains the baseline and all per-feature results
for one metric. Each `FeatureImportanceResult` retains the raw perturbed metric
and importance values plus their mean and population standard deviation. These
are working names and may change as the inspection API develops.

Feature replacement is an explicit `FeaturePerturbation` policy. The first
numeric policies are permutation, mean, median, and distribution resampling.
Positive importance consistently means that perturbing the feature worsened
the metric, regardless of whether the metric is minimized or maximized. Raw
metric and importance values remain available alongside their mean and
population standard deviation.

Distribution resampling accepts an optional `IDistribution<double>`. An
explicit distribution is sampled directly and may be uniform, normal, a
mixture, or another reusable distribution implementation. Without an explicit
distribution, the policy derives a separate normal distribution from each
feature's mean and population standard deviation.

Row subsampling is intentionally absent. Callers that need sampled importance
construct the desired `DataFrame` and supervised-data object before invoking
the calculator. Population weights, parallel execution, grouped features, and
categorical feature perturbation remain follow-up work.

The legacy variable-impact calculator and its dedicated tests were removed
after numeric permutation, mean, median, and noise replacement had maintained
equivalents. Its categorical replacement modes are intentionally not retained
in the current data-analysis scope.

### Performance

- benchmark frame construction and typed lookup on realistic schemas;
- benchmark allocating versus destination-span prediction;
- design row sampling/views only with measured workloads;
- keep compiled-expression reuse explicit for repeated symbolic prediction.

## Deferred Topics

- mutable frames or series;
- row objects and general table manipulation;
- built-in statistics;
- implicit type conversion;
- classifier contracts;
- a general `IModel`;
- training/validation/test labels embedded in data objects;
- automatic preprocessing.
