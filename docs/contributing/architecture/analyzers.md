# Analyzer architecture

An analyzer is a stateful, first-class run component. It owns its collected data and exposes typed reads directly. It implements `IAnalyzer.Install(ResolutionScopeBuilder)` to declare the execution observations it needs. An analyzer is not an execution module, although its installation can create and install any number of modules. The run installs it as it would a module, so a decoration an analyzer declares directly, without creating a module for it, still has module origin.

`AlgorithmRun` accepts analyzers and modules while its lifecycle is `Preparing`. Starting it freezes those attachments, installs them, analyzers before modules and each in the order attached, and resolves the execution graph. The run does not own analyzer disposal and does not provide a result lookup service. Reusing one analyzer on several runs intentionally combines its results.

## Observation boundaries

Algorithms and operators remain unaware of analysis. The analysis layer turns a selected configuration into an observable runtime wrapper through typed `ResolutionScopeBuilder.Observe` overloads:

```csharp
public void Install(ResolutionScopeBuilder builder)
{
    var typed = builder.For<TCandidate, TSearchSpace, TProblem>();
    typed.Observe(firstEvaluator, RecordFirst);
    typed.Observe(secondEvaluator, RecordSecond);
}
```

The selected algorithm or operator is the boundary identity; no separate anchor object is needed. Built-in overloads cover algorithms, evaluators, crossovers, mutators and interceptors. Each receives a typed observation value after the underlying operation completes. Different sources may use different callbacks, and several sources may feed one analyzer.

A configuration such as `IEvaluator<TCandidate>` names only its candidate, while the observation hands the callback the search space and problem the operation ran with. Those types are named in one of three spellings of the same mechanism:

- `builder.For<TCandidate, TSearchSpace, TProblem>()` names them once for an analyzer, as above, and its `Observe` accepts method groups typed at them. `For<TCandidate, TSearchSpace, TProblem, TSearchState>()` also names the search state, which interceptor observations need.
- `builder.Observe<TCandidate, TSearchSpace, TProblem>(source, callback)` names them at one call.
- `builder.Observe(source, observation => …)` names nothing. An implicitly typed lambda binds to the overload typed at `ISearchSpace<TCandidate>` and `IProblem<TCandidate, ISearchSpace<TCandidate>>`, and at `ISearchState` for an interceptor, which suits callbacks that read only candidates, objective vectors or states.

Named types are checked against the run when the observation is resolved, applying the same rule as the operator authoring bases, so a run over other types fails with `ExecutionSignature.Mismatch` instead of casting. The interface-typed overload fits every run over the candidate.

Each `Observe` call creates a private runtime module. These module and wrapper classes may retain delegates because they are runtime identity objects rather than records or serializable configuration. Installing the same exact module object twice in one scope has no additional effect. Distinct modules compose in declaration order.

Configuration decorations remain inside module decorations. Among modules, earlier declarations observe completed operations first. A trace therefore installs its clocks before its own observation modules.

An operator that nothing observes is resolved without a wrapper, so a run without analyzers takes the ordinary operator path unchanged. Observation only reads, and analysis never steers the search. An operator that adapts to its own measured success is control flow: it reads instrumentation, as budgets and terminators read an operator counter, rather than an analyzer.

## Trace analyzers

`TraceAnalyzer<T>` is the standard tracing subsystem. It combines one or several compatible observation sources, measurement, aggregation, retention, clocks, immutable trace entries and synchronization. A trace requires at least one source and ignores repeated references to the same source.

Almost every analysis users ask for is the same act: at a boundary, read something, summarize it, and file the summary under the clocks of that moment. A trace therefore composes independent choices instead of shipping one analyzer type per metric. HeuristicLab ended up with `BestAverageWorstQualityAnalyzer`, `QualityPerEvaluationsAnalyzer` and `QualityPerClockAnalyzer` for one metric because the time axis was part of each type. Here every clock a trace selects is recorded on every entry, so one trace is read against iterations, evaluations or elapsed time without a second run. No clock is added automatically, because observing a source costs something and a run with nested algorithms or several evaluators has no single obvious iteration or evaluation count.

Named measurements are immutable value strategies. Delegate measurements and scalar projections are runtime-only adapters with identity semantics. Public trace construction never exposes an execution scope.

Aggregation and retention are plain objects, not execution configurations. They are never resolved through a `ResolutionScope`, because nothing about them depends on a run: an aggregation's state is its own, it is built when the trace is built, and it is never rebuilt. A trace does reach the resolution system for the one thing that needs it, installing its observation modules and clocks into the run's scope.

One consequence is deliberate: an aggregation or retention that holds state owns it, so handing one object to two traces couples them. The factories on `Aggregate` and `TraceRetention` return a fresh object per call, which is what makes ordinary inline use independent. Combining several sources in one trace remains the supported way to share an accumulator and result sink.

`IAggregation<TValue, TResult>` has one method and covers both per-observation summaries and accumulation across observations, such as `BestSoFarAggregation`. There is no separate reducer role and no stateless base class: a stateless aggregation is a class with no fields.

`TraceRetention.Decide` returns `RetentionDecision.Append`, `ReplaceLatest` or `Skip`, so a policy controls publication and may keep the trace at its most recent entry. An unknown decision fails at the trace. Retention happens after measurement and aggregation and never skips that computation.

## Accumulating analyzers

`TraceAnalyzer<T>` keeps a history of immutable values. An analyzer whose data is one object updated in place, such as a Pareto front or a descent graph, derives from `AccumulatingAnalyzer` instead. It owns that object, mutates it under the inherited `Sync` and publishes it as it is. Copying it per read would cost the whole accumulator, and every reader of one today reads after the run finished, so a live view is what they get and reading during a run is not supported.

Neither retention nor clocks apply to it. There is nothing to store apart from what was computed, and one current state has no moment of its own.

## Objective comparison and publication

Observations carry the concrete problem used by the operation. Ranking uses that problem's objective unless the trace was given an `IComparer<ObjectiveVector>`. Comparers and `RequireTotalOrder` belong to the objective system. No implicit lexicographic fallback is supplied.

An observation carries the problem because every observed operation is called with it. It does not carry the objective or a comparer: what the run optimizes is not part of what happened at the boundary, and a trace handed an objective when it is composed could rank by one the run does not use.

A trace serializes measurement, aggregation, retention and publication. Built-in traces can safely observe several sources concurrently. Custom analyzers that may be installed on concurrently executing runs are responsible for synchronizing their own state.

The lock is not there for batch parallelism: an observation of one source arrives on the thread that called the operator, so it is never delivered twice. It is what lets a caller read a trace while the run writes, and what keeps one analyzer consistent when runs sharing it execute at the same time. The store is an ordinary list under that lock, copied when a snapshot or projection is taken. The alternatives measured against it are recorded in the developer backlog.

Entries contain immutable results. `Latest` and `SampleCount` do not allocate; `Snapshot()` and `By(clock)` return stable copies. Stopping, cancelling or failing a run leaves collected results available, so there is no separate result type for an unfinished run. An analyzer has no global completion state because another run may still use it.

Analyzers have no disposal contract. What an analyzer installs lives in the run's resolution scope, so nothing it acquires outlives the run.

## Naming and placement

A stateful object that collects data is named `...Analyzer`, such as `GenealogyAnalyzer` or `ParetoFrontAnalyzer`. The immutable values it publishes are entries, snapshots or result records, never `...Analyzer`.

Ready-made traces are extension methods on the configuration they observe, named `Trace...`, as in `algorithm.TracePopulationQuality()` or `algorithm.Evaluator.TraceBestSoFar()`, and grouped in a static class ending in `Traces`. User-facing shortcuts take the observed configuration itself. A run-level shortcut such as `run.TracePopulationCandidates(out var analyzer)` observes the run's root algorithm unless a nested one is named.

The main package ships only the traces every algorithm can use. A trace specific to an encoding or a problem family lives beside what it measures, and one whose semantics are still unsettled, such as Pareto front and hypervolume analysis awaiting the objective-system rework, stays in the Experimental package under the rules of § 9.6 of the [developer guidelines](/contributing/developer-guidelines).

## Run lifecycle

Algorithm and experiment runs expose `RunLifecycleState`. An algorithm run moves from `Preparing` to `Running` when its first stream starts. Disposing that stream between yielded root-algorithm states moves it to `Paused`; a later stream continues the same resolved execution and underlying iterator. Natural completion moves it to `Completed`, and a later stream is empty. Setup and execution failures move it to `Failed`.

Each returned execution stream has one consumer, and a run permits only one active stream. Stream cancellation is cooperative at root-algorithm yield boundaries. It pauses the run instead of interrupting a partially executed iteration.

See [Observability and analysis](/guide/execution/observability-and-analysis) for user examples.
