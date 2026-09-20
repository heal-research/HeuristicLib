# Analyzer architecture

An analyzer is a stateful, first-class run component. It owns its collected data and exposes typed reads directly. It implements `IAnalyzer.Install(ResolutionScopeBuilder)` to declare the execution observations it needs. An analyzer is not an execution module, although its installation can create and install any number of modules.

`AlgorithmRun` accepts analyzers and modules while its lifecycle is `Preparing`. Starting it freezes those attachments, installs them, and resolves the execution graph. The run does not own analyzer disposal and does not provide a result lookup service. Reusing one analyzer on several runs intentionally combines its results.

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

## Trace analyzers

`TraceAnalyzer<T>` is the standard tracing subsystem. It combines one or several compatible observation sources, measurement, aggregation, retention, clocks, immutable trace entries and synchronization. A trace requires at least one source and ignores repeated references to the same source.

Named measurements are immutable value strategies. Delegate measurements and scalar projections are runtime-only adapters with identity semantics. Public trace construction never exposes an execution scope.

Aggregation and retention use `IExecutionConfiguration<T>` and `IExecutionInstance`, the same foundation as algorithms and operators. Their configuration objects are reusable and their execution instances own mutable state. A trace privately resolves those strategies, so separate traces always receive separate state. Combining several sources in one trace is the public way to share an accumulator and result sink.

`IAggregation<TValue, TResult>` covers both per-observation summaries and accumulation across observations. `StatelessAggregation<TValue, TResult>` returns itself when resolved. Stateful configurations such as `BestSoFarAggregation` create private mutable execution instances. There is no separate reducer role.

`TraceRetention` configurations similarly create instances for counters and remembered values. Retention happens after measurement and aggregation. It controls publication without skipping computation or evicting history.

## Objective comparison and publication

Observations carry the concrete problem used by the operation. Ranking uses that problem's objective unless the trace was given an `IComparer<ObjectiveVector>`. Comparers and `RequireTotalOrder` belong to the objective system. No implicit lexicographic fallback is supplied.

A trace serializes measurement, aggregation, retention and publication. Built-in traces can safely observe several sources concurrently. Custom analyzers that may be installed on concurrently executing runs are responsible for synchronizing their own state.

Entries contain immutable results. `Latest` and `SampleCount` do not allocate; `Snapshot()` and `By(clock)` return stable copies. Stopping, cancelling or failing a run leaves collected results available. An analyzer has no global completion state because another run may still use it.

## Run lifecycle

Algorithm and experiment runs expose `RunLifecycleState`. An algorithm run moves from `Preparing` to `Running` when its first stream starts. Disposing that stream between yielded root-algorithm states moves it to `Paused`; a later stream continues the same resolved execution and underlying iterator. Natural completion moves it to `Completed`, and a later stream is empty. Setup and execution failures move it to `Failed`.

Each returned execution stream has one consumer, and a run permits only one active stream. Stream cancellation is cooperative at root-algorithm yield boundaries. It pauses the run instead of interrupting a partially executed iteration.

See [Observability and analysis](/guide/execution/observability-and-analysis) for user examples.
