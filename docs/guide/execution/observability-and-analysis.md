# Observability and analysis

An analyzer collects data from the execution boundaries you select. Create it, attach it to a prepared run, then read its typed properties or take a snapshot.

```csharp
using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Instrumentation;
using HEAL.HeuristicLib.Objectives;

var run = algorithm.CreateRun(problem, random)
    .TracePopulationCandidates(out var quality);
await run.CompleteAsync();

var best = quality.RequireLatestValue().Best;
```

An algorithm observation captures each search state it yields, after its interceptors have transformed that state. No placeholder interceptor is needed. For simple progress logging, consuming `algorithm.Stream(problem, random)` directly is also sufficient.

## Choose the right tool

An analyzer is one of several ways to see what a run does. Pick by what you need:

| Need                                                                                        | Use                                                                                                          |
| ------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------ |
| Look at every state the algorithm yields                                                    | `algorithm.Stream(problem, random)`                                                                          |
| Only the final state                                                                        | `algorithm.CompleteAsync(problem, random)`                                                                   |
| Typed data from chosen boundaries, such as a quality curve or an operator's offspring       | An analyzer attached with `AddAspect`                                                                        |
| Behavior at a boundary that is not analysis, such as logging or bridging to another runtime | An aspect added with `AddAspect`                                                                             |
| A count or duration that stops or limits the run                                            | Instrumentation such as `CountCandidates` or `LimitedToEvaluatedCandidates`, read by terminators and budgets |
| The same analysis for every experiment trial                                                | A trial analyzer, see [Experiments](/guide/execution/experiments)                                            |

Analyzers only read. Nothing an analyzer records changes what the algorithm does next.

## Choose a boundary and a clock

An evaluator observation captures one completed evaluator call, including the whole batch. An algorithm observation captures a yielded state. A nested algorithm is a separate observation boundary.

```csharp
var evaluations = Clock.FromEvaluations(algorithm.Evaluator);
var best = algorithm.Evaluator.TraceBestSoFar(clocks: [evaluations]);
var population = algorithm.TracePopulationQuality(clocks: [evaluations]);
var run = algorithm.CreateRun(problem, random)
    .AddAspect(best)
    .AddAspect(population);
await run.CompleteAsync();
```

`Clock.FromElapsedTime(timeProvider)` measures time since the clock was constructed. Create it when that interval should begin.

Several traces may share the same clock in one run. Its advice applies once. Use that same clock object to project the trace; constructing another clock does not identify the original axis.

Evaluation counts describe the selected evaluator boundary. A caching evaluator's outer boundary can count requests while its inner evaluator counts cache misses. Choose the boundary whose work you intend to compare.

Observation sources match configurations by reference. A copied configuration is a different source, even if its settings compare equal. A source that never participates produces no observations. Create analyzers for the actual configured objects that will execute.

## Ownership and reads

A run accepts analyzers and other aspects through `AddAspect` while its lifecycle is `Preparing`. It collects them in the supplied order; resolution weaves their advice into the execution graph when the run starts. Adding the same aspect again does not apply its advice twice. Create fresh analyzers and clocks for independent results. You may reuse them across runs when combined history or cumulative counts are intentional. Adding an aspect does not reset its collected data.

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

Some analysis produces one mutable summary rather than a history. Derive from `AccumulatingAnalyzer`, assign observation advice in the constructor, and update the summary under `Sync`:

```csharp
public CandidateSummaryAnalyzer(IEvaluator<TCandidate> evaluator)
{
    Advices = Aspect.Observe(evaluator, Record).Advices;
}

private void Record(EvaluatorObservation<TCandidate, TSearchSpace, TProblem> observation)
{
    lock (Sync)
        accumulator.Add(observation.Candidates);
}
```

Read an accumulating analyzer after the run finishes. Clocks and retention apply to trace entries, so they do not apply to a single mutable summary.

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

Attach `combined` to both runs to intentionally accumulate shared history. Separate traces have independent strategy state when each receives its own aggregation and retention objects.

Take one snapshot when you want consistent projections onto several axes:

```csharp
var snapshot = population.Snapshot();
var byEvaluations = snapshot.By(evaluations);
```

The snapshot preserves its selected axes even when empty. Selected clocks are read when an entry is recorded. Clocks on independent concurrent boundaries do not promise an atomic global timestamp.

## Multi-objective ranking

Single-objective traces use the observed problem's objective comparer automatically. A multi-objective problem without a total order cannot supply a unique best, median, or worst. Ranking throws when first required, even for a singleton batch. There is no implicit lexicographic fallback.

Select an objective comparer explicitly when the trace needs a total order:

```csharp
var best = algorithm.Evaluator.TraceBestSoFar(objectiveComparer: new LexicographicComparer(problem.Objective.Directions));
```

This makes dimension priority explicit and affects this trace's comparisons, including best-so-far accumulation. It does not change the problem or algorithm's objective. Analysis accepts the objective system's existing `IComparer<ObjectiveVector>` implementations. The `RequireTotalOrder` helper also serves callers outside analysis. Ranking is checked at observation time because installation has no problem context.

## Experiments

An experiment uses a trial analyzer factory to create fresh analyzers for every trial. Retrieve the typed trial/analyzer pairs and read each analyzer directly. See [Experiments](/guide/execution/experiments).

## Build a trace for a custom observation role

`TraceAnalyzer<TResult>.Create` accepts sources, a factory connecting each source to a recording callback, and a measurement function. Consumer-defined roles use the same trace storage, retention and clocks as the built-in `Analyzer.Trace` overloads. Sources are deduplicated by reference.

For example, with a consumer-defined cooling-schedule role and its `RecordedCoolingAdvice`:

```csharp
var temperatures = TraceAnalyzer<double>.Create<ICoolingSchedule, double>(
    [warmup, main],
    static (schedule, record) => Aspect.Create(
        Pointcut.Reference(schedule), new RecordedCoolingAdvice(record)),
    static temperature => temperature);
var execution = ResolutionScope.Create(temperatures).Resolve(plan);
execution.Temperature(step: 0);
var values = temperatures.Snapshot().Values;
```

The factory initializes trace storage before creating its observation callbacks and installs clocks before observation aspects. Reading a snapshot does not change the trace.

## Use an aspect directly

`Aspect.Create` pairs a pointcut with advice. Pass it to `ResolutionScope.Create`, `With` or `CreateChildScope`, or add it to a preparing run with `AddAspect`.

```csharp
var counter = new CountAccumulator();
var counting = Aspect.CountCalls(algorithm.Mutator, counter);
var duration = new DurationAccumulator();
var timing = Aspect.MeasureDuration(algorithm.Mutator, duration, TimeProvider.System);
var observation = Aspect.Observe(algorithm.Mutator,
    value => Console.WriteLine(value.Offspring.Count));
var run = algorithm.CreateRun(problem, random)
    .AddAspect(counting)
    .AddAspect(timing)
    .AddAspect(observation);
await run.CompleteAsync();
```

Earlier advice wraps closer to the source execution. Here timing finishes before the observation callback, so logging is outside the measurement. Counting updates only after success; timing uses `finally` and records failed calls too.

To select several configurations, pair a pointcut with role advice:

```csharp
var counting = Aspect.Create(Pointcut.OfType<IMutator<RealVector>>(),
    new CountingMutatorAdvice<RealVector>(counter, OperatorCountMetric.Candidates));
```

Custom selection derives a small class:

```csharp
sealed class SmallMutations : Pointcut<IMutator<RealVector>>
{
    public override bool Matches(IMutator<RealVector> mutator) =>
        mutator is GaussianMutator { MutationStrength: < 1.0 };
}
```

Reference, type and custom pointcuts of the same configuration type combine with `And`/`&` and `Or`/`|`. Matching short-circuits from left to right. The pointcut sees configurations that resolution reaches, including those created later by a meta-algorithm.

An aspect subclass assigns several pairs directly to `Advices` in its constructor. It can include a clock's pairs before its own observations. Resolution applies each advice instance once, at its first position; sharing a clock across several traces therefore counts each call once. If the same advice is paired with two pointcuts, the first pairing wins. Use separate advice instances for separate selections.

A new scope layer precedes inherited layers. Prepared advice state belongs to the state owning each selected configuration. Reusing an aspect across independent scopes does not share that prepared state; reusing its caller-owned accumulator intentionally combines measurements.
