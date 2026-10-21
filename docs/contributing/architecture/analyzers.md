# Analyzer architecture

An analyzer is a stateful `Aspect` that owns collected data and exposes typed reads. Its constructor assigns ordered pointcut/advice pairs to `Advices`. An accumulating analyzer derives from `AccumulatingAnalyzer` to share the synchronization convention; a trace uses `TraceAnalyzer<T>`.

`AlgorithmRun.AddAspect` accepts analyzers and other aspects while its lifecycle is `Preparing`. Starting the run freezes its aspect list and resolves the execution graph. The same advice instance applies once by reference, including when several analyzers share a clock. A run owns neither analyzer disposal nor a result lookup service. Reusing an analyzer across runs intentionally combines its results.

## Observation boundaries

Algorithms and operators remain unaware of analysis. Predefined observation records, advice and `Aspect.Observe` factories live in `HEAL.HeuristicLib.Inspection`:

```csharp
public SeparateEvaluatorAnalyzer(IOperator<Evaluation, TCandidate> first, IOperator<Evaluation, TCandidate> second)
{
    Advices =
    [
        .. Aspect.Observe(first, RecordFirst).Advices,
        .. Aspect.Observe(second, RecordSecond).Advices
    ];
}
```

The selected algorithm or operator is the observation's source identity. Factories cover algorithms and every operation. Operator observations follow successful calls. Algorithm observations follow each yielded state, after interceptors have transformed it. Each observation reports the selected configuration, including when advice is applied through a pointcut covering several sources.

Every operator observation is an `OperatorObservation<TOperation, TCandidate>`. It reads the search space and problem at `ISearchSpace<TCandidate>` and `IProblem<TCandidate, ISearchSpace<TCandidate>>`, so it fits every run over the candidate, and its operation-specific members, such as `Parents` and `Offspring` for mutation, are extension properties over the call's arguments and result. An algorithm observation is an `AlgorithmObservation<TCandidate, TSearchState>`, and an interceptor's observation reads the states at the search state its operation names. Observation advice therefore checks no run types; the observed operator's own check still reports `ExecutionSignature.Mismatch` for a run it was not written for.

Observation advice is a runtime class and may retain its callback. Operator observation wraps each executor built for its source and keeps no further state. Algorithm observation prepares its iteration counter once per advice, configuration and owner state, so rebuilding contextual nodes does not reset the count. Invocation-local previous states stay in the iterator.

Earlier advice wraps closer to the source executor and reports completed operations first. A trace therefore includes its clocks' advice before its own observation advice. Shared clocks keep their first position. A scope with no matching advice adds no observation wrapper.

Observation reads inputs, outputs and progress while preserving the observed computation. An algorithm that adapts to measured work uses inspection data in its explicit control flow rather than reading an analyzer's result. General aspects may change behavior; the observation contract applies to inspection and analysis.

## Trace analyzers

`TraceAnalyzer<T>` is the standard tracing subsystem. It combines one or several compatible observation sources, measurement, aggregation, retention, clocks, immutable trace entries and synchronization. An operator trace reads `OperatorObservation<TOperation, TCandidate>`, so one `Analyzer.Trace` serves every operation, as in `Analyzer.Trace(crossover, observation => observation.Offspring.Count)`. An algorithm trace reads `AlgorithmObservation<TCandidate, TSearchState>`, and a measurement of search states also traces the states an interceptor returns. Every trace aggregates against the observed run's objective. A trace requires at least one source and ignores repeated references to the same source.

A trace composes observation sources, measurement, aggregation, retention and explicit clocks. Every selected clock is recorded on each entry, so the same trace can be projected by iterations, evaluations or elapsed time. No clock is added automatically: a nested run can contain several algorithm and evaluator boundaries.

Named measurements are immutable value strategies. Delegate measurements and scalar projections are runtime-only adapters with identity semantics. Public trace construction never exposes an execution scope.

Aggregation and retention are plain objects, not execution configurations. They are never resolved through a `ResolutionScope`, because nothing about them depends on a run: an aggregation's state is its own, it is built when the trace is built, and it is never rebuilt. A trace contributes its observation and clock advice to the run's scope.

One consequence is deliberate: an aggregation or retention that holds state owns it, so handing one object to two traces couples them. The factories on `Aggregate` and `TraceRetention` return a fresh object per call, which is what makes ordinary inline use independent. Combining several sources in one trace remains the supported way to share an accumulator and result sink.

`IAggregation<TValue, TResult>` has one method and covers both per-observation summaries and accumulation across observations, such as `BestSoFarAggregation`. There is no separate reducer role and no stateless base class: a stateless aggregation is a class with no fields.

`TraceRetention.Decide` returns `RetentionDecision.Append`, `ReplaceLatest` or `Skip`, so a policy controls publication and may keep the trace at its most recent entry. An unknown decision fails at the trace. Retention happens after measurement and aggregation and never skips that computation.

## Accumulating analyzers

`TraceAnalyzer<T>` keeps a history of immutable values. An analyzer whose data is one object updated in place, such as a custom summary object, derives from `AccumulatingAnalyzer` instead. It owns that object, mutates it under the inherited `Sync` and publishes it as it is. Copying it per read would cost the whole accumulator, and every reader of one today reads after the run finished, so a live view is what they get and reading during a run is not supported.

Neither retention nor clocks apply to it. There is nothing to store apart from what was computed, and one current state has no moment of its own.

## Objective comparison and publication

Observations carry the concrete problem used by the operation. Ranking uses that problem's objective unless the trace was given an `IComparer<ObjectiveVector>`. Comparers and `RequireTotalOrder` belong to the objective system. No implicit lexicographic fallback is supplied.

An observation carries the problem because every observed operation is called with it. It does not carry the objective or a comparer: what the run optimizes is not part of what happened at the boundary, and a trace handed an objective when it is composed could rank by one the run does not use.

A trace serializes measurement, aggregation, retention and publication. Built-in traces can safely observe several sources concurrently. Custom analyzers that may be installed on concurrently executing runs are responsible for synchronizing their own state.

The lock is not there for batch parallelism: an observation of one source arrives on the thread that called the operator, so it is never delivered twice. It is what lets a caller read a trace while the run writes, and what keeps one analyzer consistent when runs sharing it execute at the same time. The store is an ordinary list under that lock, copied when a snapshot or projection is taken.

Entries contain immutable results. `Latest` and `SampleCount` do not allocate; `Snapshot()` and `By(clock)` return stable copies. Stopping, cancelling or failing a run leaves collected results available, so there is no separate result type for an unfinished run. An analyzer has no global completion state because another run may still use it.

Analyzers and resolution scopes have no disposal contract. Reusing an analyzer or clock across runs intentionally retains its collected data, and a live deferred execution can retain its construction scope and advice. Resources requiring deterministic cleanup need an explicit lifecycle owner.

## Naming and placement

A stateful object that collects data is named `...Analyzer`, such as a consumer-authored `CandidateSummaryAnalyzer`. The immutable values it publishes are entries, snapshots or result records, never `...Analyzer`.

Ready-made traces are extension methods on the configuration they observe, named `Trace...`, as in `algorithm.TracePopulationQuality()` or `algorithm.Evaluator.TraceBestSoFar()`, and grouped in a static class ending in `Traces`. User-facing shortcuts take the observed configuration itself. A run-level shortcut such as `run.TracePopulationCandidates(out var analyzer)` observes the run's root algorithm unless a nested one is named.

The main package ships general-purpose traces. Specialized analysis belongs beside the concept it measures, following the package placement rules in § 9.6 of the [developer guidelines](/contributing/developer-guidelines).

## Run lifecycle

`AlgorithmRun`, `ExecutionStream` and `RunLifecycleState` belong to the `HEAL.HeuristicLib.Execution` namespace, with their source files grouped under `Execution/Runs`. Experiment runs remain in `HEAL.HeuristicLib.Experiments` and share `RunLifecycleState`.

An algorithm run moves from `Preparing` to `Running` when its first stream starts. Disposing that stream between yielded root-algorithm states moves it to `Paused`; a later stream continues the same resolved executor and underlying iterator. Natural completion moves it to `Completed`, and a later stream is empty. Setup and execution failures move it to `Failed`.

Each returned execution stream has one consumer, and a run permits only one active stream. Stream cancellation is cooperative at root-algorithm yield boundaries. It pauses the run instead of interrupting a partially executed iteration.

See [Observability and analysis](/guide/execution/observability-and-analysis) for user examples.
