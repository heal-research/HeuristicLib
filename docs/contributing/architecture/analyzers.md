# Analyzer architecture

An analyzer is a stateful `Aspect` that owns collected data and exposes typed reads. Its constructor assigns ordered pointcut/advice pairs to `Advices`. An accumulating analyzer derives from `AccumulatingAnalyzer` to share the synchronization convention; a trace uses `TraceAnalyzer<T>`.

`AlgorithmRun.AddAspect` accepts analyzers and other aspects while its lifecycle is `Preparing`. Starting the run freezes its aspect list and resolves the execution graph. The same advice instance applies once by reference, including when several analyzers share a clock. A run owns neither analyzer disposal nor a result lookup service. Reusing an analyzer across runs intentionally combines its results.

## Observation boundaries

Algorithms and operators remain unaware of analysis. Each operation declares its observation record, the advice that builds it and its `Invocations` source in files beside the operation. `AlgorithmObservation`, the algorithm's `Iterations` source, `ObservationSource<TObservation>` and `Aspect.Observe` live in `HEAL.HeuristicLib.Inspection`:

```csharp
public SeparateEvaluatorAnalyzer(IOperator<Evaluation, TCandidate> first, IOperator<Evaluation, TCandidate> second)
{
    Advices =
    [
        .. Aspect.Observe(first.Invocations, RecordFirst),
        .. Aspect.Observe(second.Invocations, RecordSecond)
    ];
}
```

A source's observed algorithm or operator, which it receives on construction and matches by reference, identifies what it observes. Every operation and every algorithm offers a source; an operator implementing several operations offers each through a view typed to that operation. Operator observations follow successful invocations. Algorithm observations follow each yielded state, after interceptors have transformed it. Each observation names the observed configuration.

A configuration can also declare named observation points for progress inside its executor that no boundary shows. A member returns `ObservationPoint.Of<TObservation>(this, nameof(Point))`, and the build resolves it with `scope.Resolve(Point)`. The executor calls the resolved point's `Report` method. A point is both a child configuration and an observation source; its identity distinguishes it from other points, including those reporting the same value type. Repeated access returns the same point, while a record copy gets its own. Ordinary advice wraps the point's executor. `CycleAlgorithm` reports `CycleStart` through `Cycles` and `CycleTurn` through `Turns`, because its inner algorithms cannot tell which cycle or turn they run in. Unobserved points still resolve and receive records through an executor that ignores reports.

Each operation's observation record keeps its domain names, such as `Parents` and `Offspring` in `MutationObservation<TCandidate>`. It reads the search space and problem at `ISearchSpace<TCandidate>` and `IProblem<TCandidate, ISearchSpace<TCandidate>>`, so it fits every run over the candidate. An algorithm observation is an `AlgorithmObservation<TCandidate, TSearchState>`, and an interceptor's observation reads the states at the search state its operation names. Observation advice therefore checks no run types; the observed operator's own check still reports `ExecutionSignature.Mismatch` for a run it was not written for. Every operator and algorithm observation carries the call's search space and problem, so a trace's function can rank by that problem's objective. A source of any other configuration may deliver any value; its trace reads it through a function like any other.

The trace machinery names no operation; a ready-made trace extends the source of the observation type it reads, such as evaluator invocations. An external operation declares the same three parts and is observed and traced exactly like a built-in one.

Operation observation advice is file-local; its source creates it with the callback. External operations construct boundary sources through `ObservationSource.Create<TObservation>(configuration, observe)`; source inheritance is limited to the library. It wraps each executor built for its observed configuration and keeps no further state. Algorithm observation advice keeps no counter. Each executor numbers the iterations of its own call from 1, so `Iteration` restarts in each call, including every activation by a meta-algorithm, whatever its scope policy. Invocation-local previous states stay in the iterator.

Earlier advice wraps closer to the source executor and reports completed operations first. A trace therefore includes its clocks' advice before its own observation advice. Shared clocks keep their first position. A scope with no matching advice adds no observation wrapper.

A counting clock keeps its count on its own advice rather than reading the observations it counts, so it never runs backwards. `ObservationClock<TObservation>` counts the observations of a source, as `Clock.FromIterations`, `Clock.FromCycles` and `Clock.FromTurns` do. `OperatorCountClock<TOperation, TCandidate>` counts an operator's invocations or candidate work through counting advice, as `Clock.FromInvocations` and `Clock.FromCandidates` do; a candidate clock over an operation without candidate work fails before the operator is built.

Observation reads inputs, outputs and progress while preserving the observed computation. An algorithm that adapts to measured work uses inspection data in its explicit control flow rather than reading an analyzer's result. General aspects may change behavior; the observation contract applies to inspection and analysis.

## Trace analyzers

`TraceAnalyzer<T>` is the standard tracing subsystem. It combines one or several sources of one observation type with a function, retention, clocks, immutable trace entries and synchronization. `Analyzer.Trace` reads one value per observation through that function, as in `Analyzer.Trace(crossover.Invocations, observation => observation.Offspring.Count)`, for one source or a list. Ranking is a plain function that receives the objective directions it orders by, usually the observed invocation's own, as in `Analyzer.Trace(evaluator.Invocations, observation => observation.ObjectiveVectors.BestMedianWorst(observation.Problem.Objective))`. Ready-made traces extend the sources they read, such as `TraceBestSoFar` on evaluator calls and `TracePopulationQuality` on algorithm iterations. A trace requires at least one source and observes each boundary once: sources match when their observed configurations are the same reference and their observation types are equal, so the same source given twice counts once, while the sources of two operations of one configuration are each observed.

A trace composes its sources, a function, retention and explicit clocks. Every selected clock is recorded on each entry, so the same trace can be projected by iterations, evaluations or elapsed time. No clock is added automatically: a nested run can contain several algorithm and evaluator boundaries.

Trace functions are runtime delegates; they are neither comparable nor serializable values. Public trace construction never exposes an execution scope.

Retention and best-so-far objects are plain objects, not execution configurations. They are never resolved through a `ResolutionScope`, because nothing about them depends on a run: their state is their own, they are built when the trace is built, and they are never rebuilt. A trace contributes its observation and clock advice to the run's scope.

One consequence is deliberate: a retention or best-so-far object that holds state owns it, so handing one object to two traces couples them. The factories on `TraceRetention` and the ready-made traces create a fresh object per call, which is what makes ordinary inline use independent. Combining several sources in one trace remains the supported way to share an accumulator and result sink.

A best-so-far object keeps the running best across observations: `BestSoFar` the best objective vector, and `BestSoFar<T>` any other value, ranked by the objective vector its selector reads. The first directions it receives fix it; other directions or another dimensionality throw. There is no aggregation role: summaries and accumulation are functions of the observation, and accumulation keeps its state in an object the function captures.

`TraceRetention.Decide` returns `RetentionDecision.Append`, `ReplaceLatest` or `Skip`, so a policy controls publication and may keep the trace at its most recent entry. An unknown decision fails at the trace. Retention happens after the trace's function reads an observation and never skips that computation.

## Accumulating analyzers

`TraceAnalyzer<T>` keeps a history of immutable values. An analyzer whose data is one object updated in place, such as a custom summary object, derives from `AccumulatingAnalyzer` instead. It owns that object, mutates it under the inherited `Sync` and publishes it as it is. Copying it per read would cost the whole accumulator, and every reader of one today reads after the run finished, so a live view is what they get and reading during a run is not supported.

Neither retention nor clocks apply to it. There is nothing to store apart from what was computed, and one current state has no moment of its own.

## Objective comparison and publication

Observations carry the concrete problem used by the operation. Ranking uses that problem's objective unless the trace was given an `IComparer<ObjectiveVector>`. Comparers and `RequireTotalOrder` belong to the objective system. No implicit lexicographic fallback is supplied.

An observation carries the problem because every observed operation is called with it. It does not carry the objective or a comparer: what the run optimizes is not part of what happened at the boundary, and a trace handed an objective when it is composed could rank by one the run does not use.

A trace serializes its function, retention and publication. Built-in traces can safely observe several sources concurrently. Custom analyzers that may be installed on concurrently executing runs are responsible for synchronizing their own state.

The lock is not there for batch parallelism: an observation of one source arrives on the thread that called the operator, so it is never delivered twice. It is what lets a caller read a trace while the run writes, and what keeps one analyzer consistent when runs sharing it execute at the same time. The store is an ordinary list under that lock, copied when a snapshot or projection is taken.

Entries contain immutable results. `Latest` and `SampleCount` do not allocate; `Snapshot()` and `By(clock)` return stable copies. Stopping, cancelling or failing a run leaves collected results available, so there is no separate result type for an unfinished run. An analyzer has no global completion state because another run may still use it.

Analyzers and resolution scopes have no disposal contract. Reusing an analyzer or clock across runs intentionally retains its collected data, and a live deferred execution can retain its construction scope and advice. Resources requiring deterministic cleanup need an explicit lifecycle owner.

## Naming and placement

A stateful object that collects data is named `...Analyzer`, such as a consumer-authored `CandidateSummaryAnalyzer`. The immutable values it publishes are entries, snapshots or result records, never `...Analyzer`.

Ready-made traces are extension members on the observation sources they read, named `Trace...`, as in `algorithm.Iterations.TracePopulationQuality()` or `algorithm.Evaluator.Invocations.TraceBestSoFar()`, and grouped in a static class ending in `Traces`. Extending the source of an observation type rather than a configuration lets every configuration offering that source use them. A run-level shortcut such as `run.TracePopulationCandidates(out var analyzer)` observes the run's root algorithm unless a nested one is named.

The main package ships general-purpose traces. Specialized analysis belongs beside the concept it measures, following the package placement rules in § 9.6 of the [developer guidelines](/contributing/developer-guidelines).

## Run lifecycle

`AlgorithmRun`, `ExecutionStream` and `RunLifecycleState` belong to the `HEAL.HeuristicLib.Execution` namespace, with their source files grouped under `Execution/Runs`. Experiment runs remain in `HEAL.HeuristicLib.Experiments` and share `RunLifecycleState`.

An algorithm run moves from `Preparing` to `Running` when its first stream starts. Disposing that stream between yielded root-algorithm states moves it to `Paused`; a later stream continues the same resolved executor and underlying iterator. Natural completion moves it to `Completed`, and a later stream is empty. Setup and execution failures move it to `Failed`.

Each returned execution stream has one consumer, and a run permits only one active stream. Stream cancellation is cooperative at root-algorithm yield boundaries. It pauses the run instead of interrupting a partially executed iteration.

See [Observability and analysis](/guide/execution/observability-and-analysis) for user examples.
