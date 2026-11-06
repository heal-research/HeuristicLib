# Analyzer architecture

An analyzer is an immutable instrument definition that creates one typed result and its collecting aspect per run. Traces use `TraceAnalyzer<TObservation, TValue> : Instrument<Trace<TObservation, TValue>>`. Accumulating analyses derive directly from `Instrument<TResult>` and create their graphs, fronts or histories per run.

Attach definitions through `algorithm.Advised(definition)` and read them through `run.Result(definition)`. The same definition may be reused across runs; each run owns independent result and collector state. Within one run, placements share its result and aspect by definition reference. Plain aspects may still retain caller-owned runtime data.

## Observation boundaries

Algorithms and operators remain unaware of analysis. Each operation declares its observation record, the advice that builds it and its `Invocations` source in files beside the operation. `AlgorithmObservation`, the algorithm's `Iterations` source, `ObservationSource<TObservation>` and `Aspect.Observe` live in `HEAL.HeuristicLib.Inspection`:

<<< ../../../test/HeuristicLib.Tests.Docs/Execution/ObservabilityDocumentationSpecs.cs#observability-accumulating-analyzer{csharp}

A source's observed algorithm or operator, which it receives on construction and matches by reference, identifies what it observes. Every operation and every algorithm offers a source; an operator implementing several operations offers each through a view typed to that operation. Operator observations follow successful invocations. Algorithm observations follow each yielded state, after interceptors have transformed it. Each observation names the observed configuration.

Algorithm `Calls` observes the start and outcome of each executor call, including a call that yields no state. A Cycle turn is such a call of an inner algorithm. There is no separate observation-point mechanism.

Each operation's observation record keeps its domain names, such as `Parents` and `Offspring` in `MutationObservation<TCandidate>`. It reads the search space and problem at `ISearchSpace<TCandidate>` and `IProblem<TCandidate, ISearchSpace<TCandidate>>`, so it fits every run over the candidate. An algorithm observation is an `AlgorithmObservation<TCandidate, TSearchState>`, and an interceptor's observation reads the states at the search state its operation names. Observation advice therefore checks no run types; the observed operator's own check still reports `ExecutionSignature.Mismatch` for a run it was not written for. Every operator and algorithm observation carries the call's search space and problem, so a trace's function can rank by that problem's objective. A source of any other configuration may deliver any value; its trace reads it through a function like any other.

The trace machinery names no operation; a ready-made trace extends the source of the observation type it reads, such as evaluator invocations. An external operation declares the same three parts and is observed and traced exactly like a built-in one.

An operation's observation record makes an executor report it, through `IInvocationObservation`. External operations construct boundary sources through `ObservationSource.Create`; source inheritance is limited to the library. The wrapper keeps no further state. Algorithm observation advice keeps no counter. Each executor numbers the iterations of its own call from 1, so `Iteration` restarts in each call, including every activation by a meta-algorithm, whatever its scope policy. Invocation-local previous states stay in the iterator.

Within an aspect, earlier pairs wrap closer to the executor. Within a scope, later added aspects wrap closer, and child-scope advice precedes inherited advice. Matching happens before reference deduplication; the closest occurrence wins. A trace lists the run aspects of its clocks before its own observation advice so shared clocks update once before collection.

Counters are instruments and clocks: operator sources offer `Count()` and `CountCandidates()`, and algorithm iteration or call sources offer `Count()`. Durations from operator sources measure failed calls too. `Clock.Elapsed` reads the root scope's active execution time, excluding pauses and consumer work. Custom clocks derive from `Clock<TTime, TResult>`.

Observation reads inputs, outputs and progress while preserving the observed computation. An algorithm that adapts to measured work uses inspection data in its explicit control flow rather than reading an analyzer's result. General aspects may change behavior; the observation contract applies to inspection and analysis.

## Trace analyzers

`TraceAnalyzer<TObservation, TValue>` defines tracing over one or several sources of one observation type. Its per-run `Trace<TObservation, TValue>` owns entries, retention progress, the value function and synchronization. Source extensions such as `evaluator.Invocations.Trace(value)` and multi-source `Analyzer.Trace(sources, value)` construct definitions. A stateful value uses `Trace(createState, value)`, which creates one class state per run. Ready-made traces are source extensions. A repeated boundary is observed once by configuration reference and observation type; separate operations remain separate boundaries.

A trace composes its sources, a function, retention and explicit clocks. Every selected clock is recorded on each entry, so the same trace can be projected by iterations, evaluations or elapsed time. No clock is added automatically: a nested run can contain several algorithm and evaluator boundaries.

Trace functions are runtime delegates; they are neither comparable nor serializable values. Public trace construction never exposes an execution scope.

Retention is an immutable policy that creates a per-result decider. Best-so-far state is created by the trace's per-run state factory. Neither is an execution configuration, and neither is resolved as a child through a scope.

Definitions may be reused without coupling their results. A caller's delegate may deliberately capture shared mutable state under the documented developer-guidelines § 4.8 exception; that state remains the caller's responsibility.

A best-so-far object keeps the running best across observations: `BestSoFar` the best objective vector, and `BestSoFar<T>` any other value, ranked by the objective vector its selector reads. The first directions it receives fix it; other directions or another dimensionality throw. There is no aggregation role: summaries and accumulation are functions of the observation, and accumulation keeps its state in an object the function captures.

A per-result `RetentionDecider<TValue>.Decide` returns `Append`, `ReplaceLatest` or `Skip`. Retention runs after the value function and never skips its computation; an unknown decision fails at collection.

## Accumulating analyzers

A trace keeps a history of immutable values. A mutable summary instead uses `Instrument<TResult>` directly. Its `CreateResult` creates one accumulator and its `CreateAspect` closes over that result. Synchronization belongs to the run-created result or collector. Read a live front, genealogy graph, rank state or performance summary after execution has stopped; copying it on each read is not part of the contract.

Neither retention nor clocks apply to it. There is nothing to store apart from what was computed, and one current state has no moment of its own.

## Objective comparison and publication

Observations carry the concrete problem used by the operation. Ranking uses that problem's objective unless the trace was given an `IComparer<ObjectiveVector>`. Comparers and `RequireTotalOrder` belong to the objective system. No implicit lexicographic fallback is supplied.

An observation carries the problem because every observed operation is called with it. It does not carry the objective or a comparer: what the run optimizes is not part of what happened at the boundary, and a trace handed an objective when it is composed could rank by one the run does not use.

Each trace result serializes value computation, retention and publication. The same definition can serve concurrent runs with independent state. A result collecting several concurrent boundaries must synchronize its updates.

The result lock protects concurrent collection and live reads. Trace storage is an ordinary list, copied when a snapshot or projection is requested.

Entries contain immutable values. A trace result's `Latest` and `SampleCount` do not allocate; `Snapshot()` and `By(clock)` return stable copies. Stopping, canceling or failing a run leaves its results available.

Instruments, results and resolution scopes have no automatic disposal contract. Deferred execution can retain its scope and result table. Dependencies requiring cleanup need an explicit lifecycle owner.

## Naming and placement

An analysis definition is named `...Analyzer`; its result is a trace, summary or domain accumulator, never another analyzer. Definitions are immutable identity-bearing classes; result objects own collected data.

Ready-made traces are source extension members named `Trace...`, grouped in a static class ending in `Traces`. Keep the definition in a variable, attach it before creating the run, and obtain its result through `run.Result(definition)`.

The main package ships general-purpose traces. Specialized analysis belongs beside the concept it measures, following the package placement rules in § 9.6 of the [developer guidelines](/contributing/developer-guidelines).

## Run lifecycle

`AlgorithmRun`, `ExecutionStream` and `RunLifecycleState` belong to the `HEAL.HeuristicLib.Execution` namespace, with their source files grouped under `Execution/Runs`. Experiment runs remain in `HEAL.HeuristicLib.Experiments` and share `RunLifecycleState`.

An algorithm run is returned in `Ready`, resolves its executor graph on first start and retains that graph and its iterator across pauses. Completion or failure is terminal; results remain readable. See [running algorithms](/guide/execution/running-algorithms) for the lifecycle contract.

Each returned execution stream has one consumer, and a run permits only one active stream. Stream cancellation is cooperative at root-algorithm yield boundaries. It pauses the run instead of interrupting a partially executed iteration.

See [Observability and analysis](/guide/execution/observability-and-analysis) for user examples.
