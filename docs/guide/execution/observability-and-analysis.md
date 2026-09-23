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

## Choose the right tool

An analyzer is one of several ways to see what a run does. Pick by what you need:

| Need | Use |
| --- | --- |
| Look at every state the algorithm yields | `algorithm.Stream(problem, random)` |
| Only the final state | `algorithm.CompleteAsync(problem, random)` |
| Typed data from chosen boundaries, such as a quality curve or an operator's offspring | An analyzer attached with `AddAnalyzer` |
| Behavior at a boundary that is not analysis, such as logging or bridging to another runtime | An execution module attached with `AddExecutionModule` |
| A count or duration that stops or limits the run | Instrumentation such as `CountCandidates` or `LimitedToEvaluatedCandidates`, read by terminators and budgets |
| The same analysis for every experiment trial | A trial analyzer, see [Experiments](/guide/execution/experiments) |

Analyzers only read. Nothing an analyzer records changes what the algorithm does next.

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

A run accepts analyzers through `AddAnalyzer` and independent execution behavior through `AddExecutionModule` while its lifecycle is `Preparing`. It installs both when execution starts, before resolving the execution graph. Create fresh analyzers and clocks for independent results. You may reuse them across runs when combined history or cumulative counts are intentional. Installation does not reset their state.

`LifecycleState` reports whether a run is `Preparing`, `Running`, `Paused`, `Completed`, `Canceled`, `Failed` or `Stopped`. Calling an execution entry point freezes its attachments. Each returned execution stream has one consumer. The run itself can continue through a later `Stream()` call after the consumer stops at a yielded root-algorithm state.

`SampleCount` counts recorded entries. `Latest` is the latest recorded entry, or null before any entry is recorded. These properties do not allocate. `Snapshot()` and `By(clock)` allocate stable copies. Taking a snapshot does not stop collection.

Results remain available after completion, cancellation or pausing. An analyzer has no completion flag because another run may still be using it. Disposing a stream enumerator pauses the run without disposing its underlying algorithm iterator.

## Retention and accumulation

`TracePopulationQuality` records current best, median, and worst objective vectors from each population. `TraceBestSoFar` accumulates the best objective vector across evaluator calls and records only improvements by default. Use `TracePopulationCandidates` or `TraceBestCandidateSoFar` when you need to retain the candidates behind those values.

```csharp
var quality = algorithm.TracePopulationQuality(clocks: [iterations], retention: TraceRetention.EveryNth(10));
var bestAtEveryEvaluation = algorithm.Evaluator.TraceBestSoFar(clocks: [evaluations], retention: TraceRetention.EveryObservation());
```

`TraceRetention.EveryNth(10)` records observations 10, 20, 30, and so on. It adds no extra initial or final entry. `TraceRetention.OnChange()` records the first value and subsequent changes by value equality, and `TraceRetention.OnChange(comparer)` decides sameness with a comparer of your own. `TraceRetention.LatestOnly()` stores each value over the one before it, so the trace holds a single entry carrying the moment of the observation that produced it. Use it when you want what a measurement says now rather than how it moved, and read it with `Latest` or `RequireLatestValue()`. A retention counts for the object it is, so give each trace its own, which the factories do.

Retention runs after measurement and aggregation. It controls stored entries and never skips expensive computations, so best-so-far still sees an improvement between retained observations. `EveryNth` and `OnChange` leave stored entries alone; `LatestOnly` is the one policy that discards what the trace already held.

## Accumulating analyzers

Some analysis is not a series. A Pareto front, a genealogy graph and a fitted model are each one object that every observation changes, and what you want back is that object as it stands. Those analyzers derive from `AccumulatingAnalyzer`:

```csharp
var front = new ParetoFrontAnalyzer<RealVector, TSearchSpace, TProblem>(
    problem.Objective, referencePoint, algorithm.Evaluator);

var points = front.Front.Points;   // the front itself, still growing while the run runs
```

An accumulator is one mutable object, so what it publishes is a live view of it rather than a copy. Read it once the run has finished. Clocks and retention do not apply either: it holds the current state, not a history, so there is no entry for a moment to belong to.

To write one, derive from `AccumulatingAnalyzer`, install your observations, and update under `Sync`:

```csharp
public override void Install(ResolutionScopeBuilder builder) =>
    builder.Observe<TCandidate, TSearchSpace, TProblem>(evaluator, Record);

private void Record(EvaluatorObservation<TCandidate, TSearchSpace, TProblem> observation)
{
    lock (Sync)
        accumulator.Add(observation.Candidates);
}
```

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

`IAggregation<TValue, TResult>` has a single `Aggregate` method. `Aggregate.Best()` summarizes each observation; `Aggregate.BestSoFar()` accumulates across observations, keeping the running best as its own state. A custom aggregation is an ordinary class implementing that one method, and it must publish immutable results.

An aggregation or retention that holds state owns it, so build one per trace. The factories do that for you: `TraceRetention.EveryNth(10)` and `Aggregate.BestSoFar()` return a fresh object every call. Hoisting one into a variable and passing it to two traces makes them share its counting.

To collect several compatible sources into one accumulator and result sink, create one combined trace:

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
