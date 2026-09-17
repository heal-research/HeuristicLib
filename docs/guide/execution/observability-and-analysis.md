# Observability and analysis

An analyzer collects data from the execution boundaries you select. Create it, attach it to a prepared run, then read its typed properties or take a snapshot.

```csharp
using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Objectives;

var run = algorithm.CreateRun(problem, random)
    .TracePopulationCandidates(out var quality);
await run.CompleteAsync();

var best = quality.RequireLatestValue().Best;
```

An algorithm observation captures each search state it yields, after its interceptors have transformed that state. No placeholder interceptor is needed. For simple progress logging, consuming `algorithm.Stream(problem, random)` directly is also sufficient.

## Choose a boundary and a clock

An evaluator observation captures one completed evaluator call, including the whole batch. An algorithm observation captures a yielded state. A nested algorithm is a separate observation boundary.

```csharp
var evaluations = Clock.FromEvaluations(algorithm.Evaluator);
var best = algorithm.Evaluator.TraceBestSoFar(clocks: [evaluations]);
var population = algorithm.TracePopulationQuality(clocks: [evaluations]);
var run = algorithm.CreateRun(problem, random)
    .AddAnalyzer(best)
    .AddAnalyzer(population);
await run.CompleteAsync();
```

Several traces may share the same clock in one run. Its observations are installed once. Use that same clock object to project the trace; constructing another clock does not identify the original axis.

Evaluation counts describe the selected evaluator boundary. A caching evaluator's outer boundary can count requests while its inner evaluator counts cache misses. Choose the boundary whose work you intend to compare.

Observation sources match configurations by reference. A copied configuration is a different source, even if its settings compare equal. A source that never participates produces no observations. Create analyzers for the actual configured objects that will execute.

## Ownership and reads

A run accepts analyzers through `AddAnalyzer` and independent execution behavior through `AddExecutionHook` while its lifecycle is `Preparing`. It installs both when execution starts, before resolving the execution graph. Create fresh analyzers and clocks for independent results. You may reuse them across runs when combined history or cumulative counts are intentional. Installation does not reset their state.

`LifecycleState` reports whether a run is `Preparing`, `Running`, `Paused`, `Completed`, `Canceled`, `Failed` or `Stopped`. Calling an execution entry point freezes its attachments. Each returned execution stream has one consumer. The run itself can continue through a later `Stream()` call after the consumer stops at a yielded root-algorithm state.

`SampleCount` counts recorded entries. `Latest` is the latest recorded entry, or null before any entry is recorded. These properties do not allocate. `Snapshot()` and `By(clock)` allocate stable copies. Taking a snapshot does not stop collection.

Results remain available after completion, cancellation or pausing. An analyzer has no completion flag because another run may still be using it. Disposing a stream enumerator pauses the run without disposing its underlying algorithm iterator.

## Retention and accumulation

`TracePopulationQuality` records current best, median, and worst objective vectors from each population. `TraceBestSoFar` accumulates the best objective vector across evaluator calls and records only improvements by default. Use `TracePopulationCandidates` or `TraceBestCandidateSoFar` when you need to retain the candidates behind those values.

```csharp
var quality = algorithm.TracePopulationQuality(clocks: [iterations], retention: TraceRetention.EveryNth(10));
var bestAtEveryEvaluation = algorithm.Evaluator.TraceBestSoFar(clocks: [evaluations], retention: TraceRetention.EveryObservation());
```

`TraceRetention.EveryNth(10)` records observations 10, 20, 30, and so on. It adds no extra initial or final entry. `TraceRetention.OnChange()` records the first value and subsequent changes by value equality. Trace-retention settings are immutable and can be reused across traces. Their counters are private to each trace by default.

Retention runs after measurement and aggregation. It controls stored entries and does not skip expensive computations or discard old history. Best-so-far therefore still sees an improvement between retained observations.

## Custom traces

Use a scalar projection when the observation already provides one value:

```csharp
var size = Analyzer.Trace(algorithm,
    value: observation => observation.State.Population.EvaluatedCandidates.Count);
var quality = Analyzer.Trace(
    algorithm,
    Measurement.ObjectiveVectors(algorithm),
    Aggregate.BestMedianWorst());
```

Named measurements provide reusable value strategies with type inference from the source. Runtime-only delegates provide local projections and have no value-equality or serialization contract.

`IAggregation<TValue, TResult>` is a configuration that resolves to `IAggregationInstance<TValue, TResult>`, using the same execution-instance system as algorithms and operators. `Aggregate.Best()` summarizes each observation; `Aggregate.BestSoFar()` accumulates across observations. Custom aggregations implement `CreateExecutionInstance(resolver)` and put mutable state on their execution instance. They must publish immutable results.

A trace privately resolves its aggregation and retention strategies. To collect several compatible sources into one accumulator and result sink, create one combined trace:

```csharp
var evaluators = new[] { firstAlgorithm.Evaluator, secondAlgorithm.Evaluator };
var combined = evaluators.TraceBestSoFar();
```

Attach `combined` to both runs to intentionally accumulate shared history. Separate traces have separate strategy state, even when they use the same aggregation or retention configuration object.

Take one snapshot when you want consistent projections onto several axes:

```csharp
var snapshot = population.Snapshot();
var byEvaluations = snapshot.By(evaluations);
```

The snapshot preserves its selected axes even when empty. Selected clocks are read when an entry is recorded. Clocks on independent concurrent boundaries do not promise an atomic global timestamp.

## Multi-objective ranking

Single-objective traces use the observed problem's objective comparer automatically. A multi-objective problem without a total order cannot supply a unique best, median, or worst. Ranking throws when first required, even for a singleton batch. There is no implicit lexicographic fallback.

Use Pareto analysis, such as the experimental `TraceHyperVolume`, or explicitly select an objective comparer for the analysis:

```csharp
var best = algorithm.Evaluator.TraceBestSoFar(objectiveComparer: new LexicographicComparer(problem.Objective.Directions));
```

This makes dimension priority explicit and affects this trace's comparisons, including best-so-far accumulation. It does not change the problem or algorithm's objective. Analysis accepts the objective system's existing `IComparer<ObjectiveVector>` implementations. The `RequireTotalOrder` helper also serves callers outside analysis. Genealogy and rank factories accept the same objective comparer. The correlation NSGA-II helper uses a lexicographic comparer for reports while leaving optimization Pareto-based. Ranking is checked at observation time because installation has no problem context.

## Experiments

An experiment uses a trial analyzer factory to create fresh analyzers for every trial. Retrieve the typed trial/analyzer pairs and read each analyzer directly. See [Experiments](/guide/execution/experiments).
