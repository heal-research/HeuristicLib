# Observability and analysis

An analyzer defines the analysis of selected execution boundaries. Attach the definition with `AnalyzedBy`, create a run from the configuration it returns, then read the typed result through `run.Result(analyzer)`. Each run creates independent result data.

<<< ../../../test/HeuristicLib.Tests.Docs/Execution/ObservabilityDocumentationSpecs.cs#observability-imports{csharp}

<<< ../../../test/HeuristicLib.Tests.Docs/Execution/ObservabilityDocumentationSpecs.cs#observability-quick-start{csharp}

An algorithm observation captures each search state it yields, after its interceptors have transformed that state. No placeholder interceptor is needed. For simple progress logging, consuming `algorithm.Stream(problem, random)` directly is also sufficient.

## Choose the right tool

An analyzer is one of several ways to see what a run does. Pick by what you need:

| Need                                                                                        | Use                                                                                                          |
| ------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------ |
| Look at every state the algorithm yields                                                    | `algorithm.Stream(problem, random)`                                                                          |
| Only the final state                                                                        | `algorithm.CompleteAsync(problem, random)`                                                                   |
| Typed data from chosen boundaries, such as a quality curve or an operator's offspring       | An instrument attached with `AnalyzedBy`                                                                     |
| Behavior at a boundary that is not analysis, such as logging or bridging to another runtime | An aspect attached with `Advised`                                                                            |
| A count or duration that stops or limits the run                                            | Inspection such as `Invocations.CountCandidates()` or `LimitedToCandidates`, read by terminators and budgets |
| The same analysis for every experiment trial                                                | A trial analyzer, see [Experiments](/guide/execution/experiments)                                            |

Inspection preserves the observed computation while recording its own observations, counts, durations and clocks. Analyzers derive results from those observations. Observation callbacks must preserve the operation's inputs and outputs; they receive live references rather than isolated deep copies. General aspects may change behavior, and explicit algorithm policies such as budgets may read inspection data to decide when to stop.

Inspection of creation, crossover, mutation, evaluation, selection, refinement and replacement passes on the selected executor's controls. Generic naming and retry wrappers do too, so inspection through those wrappers keeps mutation-strength adaptation. Composites expose their own controls.

## Choose a boundary and a clock

Each operator offers its successful invocations as `Invocations`, and each algorithm offers its yielded states as `Iterations`. These observation sources describe a boundary; creating one installs nothing. An evaluator's `Invocations` observes one completed evaluator invocation, including the whole batch. An algorithm's `Iterations` observes a yielded state. A nested algorithm is a separate observation boundary.

Ready-made traces are extensions on sources, such as `evaluator.Invocations` or `algorithm.Iterations`:

<<< ../../../test/HeuristicLib.Tests.Docs/Execution/ObservabilityDocumentationSpecs.cs#observability-boundary-and-clock{csharp}

A clock gives each recorded entry its time. `algorithm.Iterations.Count()` counts yielded states, `algorithm.Calls.Count()` counts started calls, and `source.Invocations.Count()` or `CountCandidates()` counts successful operator calls or their candidate work. `Clock.Elapsed` measures time spent advancing the root algorithm, excluding pauses and consumer work. Counts accumulate across fresh executor activations within one run.

Several traces may share the same clock in one run. One `AnalyzedBy` call places it once. Use that same clock object to project the trace; constructing another clock does not identify the original axis.

Candidate counts describe the selected boundary. A caching evaluator's outer boundary counts requests while its inner evaluator counts cache misses. An operation without candidate work rejects `Invocations.CountCandidates()` when the definition is created; use `Invocations.Count()` for its calls.

Observation sources match configurations by reference. A copied configuration is observed separately, even if its settings compare equal. A configuration that never participates produces no observations. Create analyzers for the actual configured objects that will execute. An operator implementing several operations offers each operation's source through that operation's typed view.

### Sources and `IObservable<T>`

A source resembles a cold `IObservable<T>`: it describes observations without producing any until something subscribes. Here, an analyzer such as `Analyzer.Trace` or `Observing` subscribes when `AnalyzedBy` places it in the configuration. The library implements neither `IObservable<T>` nor Rx-style operators. A callback can feed observations into Rx, for example into a subject:

```csharp
var subject = new Subject<CrossoverObservation<RealVector>>();
var feed = crossover.Invocations.Observing(subject.OnNext);
var run = algorithm.AnalyzedBy(feed).CreateRun(problem, random);
```

The callback runs on the thread that invoked the operator, inside the wrapped invocation. Anything taking a callback produces a configuration that cannot be serialized.

## Select configurations by properties or names

`Pointcut.Predicate<TConfiguration>(predicate)` matches configuration data without a custom subclass. Combine pointcuts through `And`/`Or` or `&`/`|`, and negate them through `Not()` or `!`. Negation stays within the declared configuration type. Predicates are runtime-only delegates, rather than persistable configuration strategies.

Name a configured operator through `.Named(name)` and select that boundary explicitly:

<<< ../../../test/HeuristicLib.Tests.Docs/Execution/ObservabilityDocumentationSpecs.cs#observability-named-pointcuts{csharp}

The receiver here is an `IOperator<Mutation, RealVector>`. Named wrappers exist for creators, crossovers, evaluators, mutators, refiners, replacers, selectors, interceptors, terminators and algorithms. Mutation and evaluation use `Named.Create(child, name)`; other roles retain their role-specific construction companions. A consumer-defined configuration can implement `INamedConfiguration` directly.

Names compare exactly and case-sensitively. Several nodes may share a name, and all match. The wrapper and its child are separate configuration nodes: name matching selects the wrapper, while a broad role pointcut can select both boundaries. Names do not change execution sharing or create caller paths. The child's concrete properties remain on the child configuration.

## Ownership and reads

Attach instruments through `AnalyzedBy` and aspects through `Advised` before creating a run. `AnalyzedBy` places what the instruments need into the configuration it returns, so create the run from that configuration. Instruments are reusable definitions; each run creates a fresh result, clock state and retention state. A definition repeated within one `AnalyzedBy` call is placed once. Separate calls place it again, and every placement collects into that run's single result. Shared advice applies once by reference. Raw aspects retain any caller-owned dependencies supplied to their advice constructors.

`LifecycleState` reports whether a run is `Ready`, `Running`, `Paused`, `Completed`, `Canceled`, `Failed` or `Stopped`. The returned run accepts no further attachments. Each returned execution stream has one consumer. The run itself can continue through a later `Stream()` call after the consumer stops at a yielded root-algorithm state.

Read the trace through `run.Result(trace)`. Its `SampleCount` and `Latest` properties do not allocate; `Latest` is null before an entry is recorded. `Snapshot()` and `By(clock)` allocate stable copies without stopping collection.

Results remain available after completion, cancellation, failure or pausing. A trace supports snapshots during execution. Read accumulating results such as a Pareto front or genealogy graph after execution has stopped. Disposing a stream enumerator pauses the run without disposing its retained algorithm iterator.

## Retention and accumulation

`TracePopulationQuality` records the current best, median, and worst of each observation's objective vectors, such as those of a yielded population. `TraceBestSoFar` accumulates the best objective vector across observations, such as evaluator invocations, and records only improvements by default. Use `TracePopulationCandidates` or `TraceBestCandidateSoFar` when you need to retain the candidates behind those values.

<<< ../../../test/HeuristicLib.Tests.Docs/Execution/ObservabilityDocumentationSpecs.cs#observability-retention{csharp}

`TraceRetention.EveryNth(10)` records observations 10, 20, 30, and so on, with no extra initial or final entry. `OnChange()` records the first value and subsequent changes by equality; `LatestOnly()` replaces the preceding entry. Retention definitions are immutable, and each result creates its own retention progress even when several traces or runs use one definition.

Retention runs after a trace's function reads an observation. It controls stored entries and never skips expensive computations, so best-so-far still sees an improvement between retained observations. `EveryNth` and `OnChange` leave stored entries alone; `LatestOnly` is the one policy that discards what the trace already held.

## Accumulating analyzers

Some analysis produces one mutable summary rather than a history. Derive the definition from `Instrument<TResult>`, create the accumulator in `CreateResult`, record each observation in `CreateRecorder`, and name the substitutions that place it in `CreateSubstitutions`. Put synchronization on the run-created result or collector:

<<< ../../../test/HeuristicLib.Tests.Docs/Execution/ObservabilityDocumentationSpecs.cs#observability-accumulating-analyzer{csharp}

Read an accumulating analyzer after the run finishes. Clocks and retention apply to trace entries, so they do not apply to a single mutable summary.

## Custom traces

`Analyzer.Trace` reads one value from each observation of a source through a function:

<<< ../../../test/HeuristicLib.Tests.Docs/Execution/ObservabilityDocumentationSpecs.cs#observability-custom-traces{csharp}

Each operation has its own observation record, such as `CrossoverObservation<TCandidate>` with `Parents` and `Offspring`, or `EvaluationObservation<TCandidate>` with `Candidates` and `ObjectiveVectors`. It holds the invocation's inputs and outputs and reads the search space and problem at the candidate's interfaces, so it fits every run over the candidate.

Counting, collecting and single values are plain functions of the observation. Ranking functions such as `Best` and `BestMedianWorst` receive the objective directions they order by, usually the observed invocation's own, `observation.Problem.Objective`, so each invocation is ranked by its own problem. `MinMeanMax` summarizes plain numbers and needs no directions.

A best-so-far object keeps the running best across observations. `BestSoFar` keeps an objective vector, and `BestSoFar<T>` keeps a value ranked by its selected objective vector. Supply a state factory so every run creates a separate accumulator:

<<< ../../../test/HeuristicLib.Tests.Docs/Execution/ObservabilityDocumentationSpecs.cs#observability-best-so-far{csharp}

The first directions a best-so-far accumulator receives fix its ordering. Other directions or dimensionality throw. Ready-made traces create their accumulation state per run. State deliberately captured by a caller's own delegate remains the caller's responsibility.

To collect several sources of one observation type into one accumulator and result sink, create one combined trace:

<<< ../../../test/HeuristicLib.Tests.Docs/Execution/ObservabilityDocumentationSpecs.cs#observability-combined-trace{csharp}

Attach `combined` to the enclosing algorithm to collect several sources into one result within each run. Reusing the definition in another run creates a separate result.

A combined trace observes each boundary once. Giving it the same source twice records each invocation once. The sources of different operations of one operator observe different boundaries, and each is recorded.

Take one snapshot when you want consistent projections onto several axes:

<<< ../../../test/HeuristicLib.Tests.Docs/Execution/ObservabilityDocumentationSpecs.cs#observability-snapshot{csharp}

The snapshot preserves its selected axes even when empty. Selected clocks are read when an entry is recorded. Clocks on independent concurrent boundaries do not promise an atomic global timestamp.

## Multi-objective ranking

Single-objective traces use the observed problem's objective comparer automatically. A multi-objective problem without a total order cannot supply a unique best, median, or worst. Ranking throws when first required, even for a singleton batch. There is no implicit lexicographic fallback.

Select an objective comparer explicitly when the trace needs a total order:

<<< ../../../test/HeuristicLib.Tests.Docs/Execution/ObservabilityDocumentationSpecs.cs#observability-lexicographic{csharp}

This makes dimension priority explicit and affects this trace's comparisons, including best-so-far accumulation. It does not change the problem or algorithm's objective. Analysis accepts the objective system's existing `IComparer<ObjectiveVector>` implementations. The `RequireTotalOrder` helper also serves callers outside analysis. Ranking is checked at observation time because installation has no problem context.

## Cycles and turns

An algorithm observation's `Iteration` numbers the iterations of one call, as the algorithm sees them. A meta-algorithm that runs an inner algorithm again calls it again, so the inner iterations number from 1 again, whether its executor is fresh or reused. A clock over the iterations keeps counting across those calls.

A turn is one call of an inner algorithm. Its `Calls` source reports `Started`, then `Completed`, `Failed` or `Stopped`, including calls that yield no states. The example counts calls of `exploitation` and combines both inner algorithms' call events:

<<< ../../../test/HeuristicLib.Tests.Docs/Execution/ObservabilityDocumentationSpecs.cs#observability-cycle{csharp}

`exploitation` runs two iterations per call. Its iteration trace holds (1, 1), (1, 2), (2, 1) and (2, 2) against its call counter. A `Started` observation reads that counter before its own call is counted; yielded states and the call's end read the updated count. The combined event trace records both inner algorithms. Call observations provide no cycle number or slot index.

## Experiments

Attach analysis definitions to the algorithms before constructing an experiment, then read each trial's result through `trial.Run.Result(definition)`. See [Experiments](/guide/execution/experiments) for advised grids and repetition.

## Observe an operation of your own

An operation you define becomes observable when it declares an observation record, the advice that builds it, and an `Invocations` source, as each built-in operation does. Its source then works with `Analyzer.Trace` and `Observing` like any other. An observation that carries the problem of the invocation it observed lets a trace rank by that problem's objective. See [Make the operation observable](/contributing/architecture/operator-implementation#make-the-operation-observable) for a complete example.

## Count, time and observe an operator

`AnalyzedBy` attaches counters, durations and callbacks:

<<< ../../../test/HeuristicLib.Tests.Docs/Execution/ObservabilityDocumentationSpecs.cs#observability-aspects{csharp}

Pass everything in one `AnalyzedBy` call and apply budgets afterwards, because one call resolves every target against the same configuration. Later entries sit closer to the operator, so in the example the timing finishes before the observation callback runs. A clock shared by several traces of one call ticks once and before them. Each call places what it lists, so a clock shared across separate `AnalyzedBy` calls ticks twice. Counting updates after success; timing also records failed calls.

`Invocations.Count()`, `CountCandidates()` and `Duration()` create counters and durations. Read their accumulators through the run. `Counted` and `Timed` place a counter or duration explicitly around a selected configuration. The wrapper decides what is counted, so pass `OperatorCountMetric.Candidates` to `Counted` to count candidate work.

### Share accounting with a terminator

A count terminator holds the counter definition whose result it reads. Attach that definition so observations update the same run-bound counter:

<<< ../../../test/HeuristicLib.Tests.Docs/Execution/ObservabilityDocumentationSpecs.cs#observability-shared-accounting{csharp}

The run stops after two successful mutation invocations and `run.Result(mutations)` returns the measured accumulator. A run-wide counter accumulates throughout the run, but a terminator stops only its own stream. Place it on the root algorithm to stop the run. A counter attached nowhere stays at zero and never stops a positive-limit terminator.

### Select several configurations

`Aspect.Create` pairs a pointcut with advice. Attach aspects with `algorithm.Advised(aspect)` or call `scope.AddAspect(aspect)` before direct resolution. To select several configurations, pair a pointcut with role advice:

<<< ../../../test/HeuristicLib.Tests.Docs/Execution/ObservabilityDocumentationSpecs.cs#observability-role-advice{csharp}

Custom selection derives a small class:

<<< ../../../test/HeuristicLib.Tests.Docs/Execution/ObservabilityDocumentationSpecs.cs#observability-custom-pointcut{csharp}

Reference, type and custom pointcuts of the same configuration type combine with `And`/`&` and `Or`/`|`. Matching short-circuits from left to right. The pointcut sees configurations that resolution reaches, including those created later by a meta-algorithm.

An aspect stores its pointcut/advice pairs in `Advices`. Within an aspect, earlier pairs wrap closer to the executor. Matching precedes reference deduplication, and the closest effective occurrence of an advice instance wins.

Child-scope advice wraps closer than inherited advice. A scope receives later added aspects closer to the executor. Instrument results remain run-bound across child activations, while raw advice may intentionally retain caller-owned runtime dependencies. An observation's own numbers, such as `Iteration`, describe the call it observes.

Continue with [Writing custom advice](/guide/extending/writing-advice) for complete role-specific and general advice examples and run attachment.
