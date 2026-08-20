# Data-Analysis Modernization Plan

Open program for replacing the legacy `Dataset`-based data-analysis APIs with the
immutable columnar model.

What already exists — `Series<T>`, `DataFrame`, `SupervisedData<T>`, `RegressionData`,
the predictor and metric contracts, statistics, feature importance, and linear scaling
— is documented in [`docs/guide/domains/data-analysis.md`](../docs/guide/domains/data-analysis.md). Symbolic
regression's use of it is in [`docs/guide/domains/symbolic-regression.md`](../docs/guide/domains/symbolic-regression.md).

This is a replacement program, not a compatibility wrapper. Legacy components stay
active until a maintained replacement covers their behavior and tests, and none is
deleted without explicit approval; the retirement rules in
[symbolic-regression-decisions.md](symbolic-regression-decisions.md#retirement-rules)
apply here too.

## Durable Constraints

Rules for anything added to this area:

- Data containers use reference identity, not dataset-wide value equality or hashing.
- Column names are ordinal and case-sensitive, and live on the series. `DataFrame`
  keeps only a derived name-to-index cache.
- Typed frame lookup performs no reflection-based construction or conversion.
- Ordinary series construction copies; `FromOwnedArray` explicitly transfers ownership.
- Data objects expose no row-slicing API. Numerical consumers work with
  `ReadOnlyMemory<T>`, `ReadOnlySpan<T>`, and caller-provided `Span<T>`.
- Predictors expose both allocating and caller-provided destination paths.
- Estimators receive explicit randomness and return the fitted predictor directly.
- Problems define algorithm-facing evaluation, not general prediction APIs.
- `RegressionData` does not encode training, validation, or test roles; a workflow
  assigns those by how it uses separate instances.
- Do not force classification, clustering, time-series, or Python workflows through
  regression-only abstractions merely to shrink the visible legacy surface.

## Open Work

**Data semantics.** Empty-frame and empty-supervised-data behavior; missing-value
representation and validation; whether arbitrary row views are needed for sampling and
cross-validation; serialization and schema inspection; preprocessing and transformation
pipelines.

**Learning domains.** Classification data, predictors, and metrics; categorical and
encoded target representations; clustering and time-series data, migrated
independently; concrete estimator APIs and fitted symbolic-model types.

The old classification and clustering packages were removed rather than migrated —
they had no consumers or tests, and neither domain is maintained. Future support gets
designed against the modern foundation instead of reviving them.

**Prediction lifecycle.** Whether fitted symbolic regressors retain compiled
expressions; predictor serialization and reproducibility metadata; general
bounded-regressor composition if bounds prove useful beyond symbolic expressions; fit
results only when a workflow needs diagnostics beyond the returned predictor.

**Feature importance.** Population weights, parallel execution, grouped features, and
categorical feature perturbation. Categorical replacement modes from the removed legacy
calculator were intentionally not retained.

**Performance.** Benchmark frame construction and typed lookup on realistic schemas,
and allocating versus destination-span prediction. Design row sampling and views only
against measured workloads.

## Deferred

Mutable frames or series; row objects and general table manipulation; implicit type
conversion; classifier contracts; a general `IModel`; training/validation/test labels
embedded in data objects; automatic preprocessing.
