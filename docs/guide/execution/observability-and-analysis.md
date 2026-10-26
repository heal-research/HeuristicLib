# Observability and analysis

An analyzer collects data from the execution boundaries you select. Create it, attach it to a prepared run, then read its typed properties or take a snapshot.

<<< ../../../test/HeuristicLib.Tests.Docs/Execution/ObservabilityDocumentationSpecs.cs#observability-imports{csharp}

<<< ../../../test/HeuristicLib.Tests.Docs/Execution/ObservabilityDocumentationSpecs.cs#observability-quick-start{csharp}

An algorithm observation captures each search state it yields, after its interceptors have transformed that state. No placeholder interceptor is needed. For simple progress logging, consuming `algorithm.Stream(problem, random)` directly is also sufficient.

## Choose the right tool

An analyzer is one of several ways to see what a run does. Pick by what you need:

| Need                                                                                        | Use                                                                                            |
| ------------------------------------------------------------------------------------------- | ---------------------------------------------------------------------------------------------- |
| Look at every state the algorithm yields                                                    | `algorithm.Stream(problem, random)`                                                            |
| Only the final state                                                                        | `algorithm.CompleteAsync(problem, random)`                                                     |
| Typed data from chosen boundaries, such as a quality curve or an operator's offspring       | An analyzer attached with `AddAspect`                                                          |
| Behavior at a boundary that is not analysis, such as logging or bridging to another runtime | An aspect added with `AddAspect`                                                               |
| A count or duration that stops or limits the run                                            | Inspection such as `CountCandidates` or `LimitedToCandidates`, read by terminators and budgets |
| The same analysis for every experiment trial                                                | A trial analyzer, see [Experiments](/guide/execution/experiments)                              |

Inspection preserves the observed computation while recording its own observations, counts, durations and clocks. Analyzers derive results from those observations. Observation callbacks must preserve the operation's inputs and outputs; they receive live references rather than isolated deep copies. General aspects may change behavior, and explicit algorithm policies such as budgets may read inspection data to decide when to stop.

Inspection of creation, crossover, mutation, evaluation, selection, refinement and replacement passes on the selected executor's controls. Generic naming and retry wrappers do too, so inspection through those wrappers keeps mutation-strength adaptation. Composites expose their own controls.

## Choose a boundary and a clock

Each operator offers its successful invocations as `Invocations`, and each algorithm offers its yielded states as `Iterations`. These observation sources describe a boundary; creating one installs nothing. An evaluator's `Invocations` observes one completed evaluator invocation, including the whole batch. An algorithm's `Iterations` observes a yielded state. A nested algorithm is a separate observation boundary.

Ready-made traces are extensions on sources, such as `evaluator.Invocations` or `algorithm.Iterations`:

<<< ../../../test/HeuristicLib.Tests.Docs/Execution/ObservabilityDocumentationSpecs.cs#observability-boundary-and-clock{csharp}

A clock gives each recorded entry its time. `Clock.FromIterations(algorithm)` counts an algorithm's iterations, `Clock.FromInvocations(source)` an operator's invocations and `Clock.FromCandidates(source)` the candidates its invocations produced or evaluated. A counting clock keeps its own count and never runs backwards, even where the observed number restarts, as an algorithm's `Iteration` does in each call; see [Cycles and turns](#cycles-and-turns). `Clock.FromElapsedTime(timeProvider)` measures time since the clock was constructed. Create it when that interval should begin.

Several traces may share the same clock in one run. Its advice applies once. Use that same clock object to project the trace; constructing another clock does not identify the original axis.

Candidate counts describe the selected boundary. A caching evaluator's outer boundary can count requests while its inner evaluator counts cache misses. Choose the boundary whose work you intend to compare. An operation without candidate work cannot have a candidate clock; resolution rejects it before the run starts, and `Clock.FromInvocations` counts it instead.

Observation sources match configurations by reference. A copied configuration is observed separately, even if its settings compare equal. A configuration that never participates produces no observations. Create analyzers for the actual configured objects that will execute. An operator implementing several operations offers each operation's source through that operation's typed view.

### Sources and `IObservable<T>`

A source resembles a cold `IObservable<T>`: it describes observations without producing any until something subscribes. Here, `Analyzer.Trace` and `Aspect.Observe` subscribe by creating an aspect, and adding that aspect to a run or scope attaches it. The library implements neither `IObservable<T>` nor Rx-style operators. A callback can feed observations into Rx, for example into a subject:

```csharp
var subject = new Subject<CrossoverObservation<RealVector>>();
var feed = Aspect.Observe(crossover.Invocations, subject.OnNext);
var run = algorithm.CreateRun(problem, random).AddAspect(feed);
```

The callback runs on the thread that invoked the operator, inside the wrapped invocation.

## Select configurations by properties or names

`Pointcut.Predicate<TConfiguration>(predicate)` matches configuration data without a custom subclass. Combine pointcuts through `And`/`Or` or `&`/`|`, and negate them through `Not()` or `!`. Negation stays within the declared configuration type. Predicates are runtime-only delegates, rather than persistable configuration strategies.

Name a configured operator through `.Named(name)` and select that boundary explicitly:

<<< ../../../test/HeuristicLib.Tests.Docs/Execution/ObservabilityDocumentationSpecs.cs#observability-named-pointcuts{csharp}

The receiver here is an `IOperator<Mutation, RealVector>`. Named wrappers exist for creators, crossovers, evaluators, mutators, refiners, replacers, selectors, interceptors, terminators and algorithms. Mutation and evaluation use `Named.Create(child, name)`; other roles retain their role-specific construction companions. A consumer-defined configuration can implement `INamedConfiguration` directly.

Names compare exactly and case-sensitively. Several nodes may share a name, and all match. The wrapper and its child are separate configuration nodes: name matching selects the wrapper, while a broad role pointcut can select both boundaries. Names do not change execution sharing or create caller paths. The child's concrete properties remain on the child configuration.

## Ownership and reads

A run accepts analyzers and other aspects through `AddAspect` while its lifecycle is `Preparing`. It collects them in the supplied order; resolution weaves their advice into the execution graph when the run starts. Adding the same aspect again does not apply its advice twice. Create fresh analyzers and clocks for independent results. You may reuse them across runs when combined history or cumulative counts are intentional. Adding an aspect does not reset its collected data.

`LifecycleState` reports whether a run is `Preparing`, `Running`, `Paused`, `Completed`, `Canceled`, `Failed` or `Stopped`. Calling an execution entry point freezes its attachments. Each returned execution stream has one consumer. The run itself can continue through a later `Stream()` call after the consumer stops at a yielded root-algorithm state.

`SampleCount` counts recorded entries. `Latest` is the latest recorded entry, or null before any entry is recorded. These properties do not allocate. `Snapshot()` and `By(clock)` allocate stable copies. Taking a snapshot does not stop collection.

Results remain available after completion, cancellation or pausing. An analyzer has no completion flag because another run may still be using it. Disposing a stream enumerator pauses the run without disposing its underlying algorithm iterator.

## Retention and accumulation

`TracePopulationQuality` records the current best, median, and worst of each observation's objective vectors, such as those of a yielded population. `TraceBestSoFar` accumulates the best objective vector across observations, such as evaluator invocations, and records only improvements by default. Use `TracePopulationCandidates` or `TraceBestCandidateSoFar` when you need to retain the candidates behind those values.

<<< ../../../test/HeuristicLib.Tests.Docs/Execution/ObservabilityDocumentationSpecs.cs#observability-retention{csharp}

`TraceRetention.EveryNth(10)` records observations 10, 20, 30, and so on. It adds no extra initial or final entry. `TraceRetention.OnChange()` records the first value and subsequent changes by value equality, and `TraceRetention.OnChange(comparer)` decides sameness with a comparer of your own. `TraceRetention.LatestOnly()` stores each value over the one before it, so the trace holds a single entry carrying the moment of the observation that produced it. Use it when you want what a trace says now rather than how it moved, and read it with `Latest` or `RequireLatestValue()`. A retention counts for the object it is, so give each trace its own, which the factories do.

Retention runs after a trace's function reads an observation. It controls stored entries and never skips expensive computations, so best-so-far still sees an improvement between retained observations. `EveryNth` and `OnChange` leave stored entries alone; `LatestOnly` is the one policy that discards what the trace already held.

## Accumulating analyzers

Some analysis produces one mutable summary rather than a history. Derive from `AccumulatingAnalyzer`, assign observation advice in the constructor, and update the summary under `Sync`:

<<< ../../../test/HeuristicLib.Tests.Docs/Execution/ObservabilityDocumentationSpecs.cs#observability-accumulating-analyzer{csharp}

Read an accumulating analyzer after the run finishes. Clocks and retention apply to trace entries, so they do not apply to a single mutable summary.

## Custom traces

`Analyzer.Trace` reads one value from each observation of a source through a function:

<<< ../../../test/HeuristicLib.Tests.Docs/Execution/ObservabilityDocumentationSpecs.cs#observability-custom-traces{csharp}

Each operation has its own observation record, such as `CrossoverObservation<TCandidate>` with `Parents` and `Offspring`, or `EvaluationObservation<TCandidate>` with `Candidates` and `ObjectiveVectors`. It holds the invocation's inputs and outputs and reads the search space and problem at the candidate's interfaces, so it fits every run over the candidate.

Counting, collecting and single values are plain functions of the observation. Ranking functions such as `Best` and `BestMedianWorst` receive the objective directions they order by, usually the observed invocation's own, `observation.Problem.Objective`, so each invocation is ranked by its own problem. `MinMeanMax` summarizes plain numbers and needs no directions.

A best-so-far object keeps the running best across observations. `BestSoFar` keeps the best objective vector; `BestSoFar<T>` keeps any other value, ranked by the objective vector its selector reads, such as `new BestSoFar<EvaluatedCandidate<RealVector>>(candidate => candidate.ObjectiveVector)`. Create one per trace and update it from the trace's function:

<<< ../../../test/HeuristicLib.Tests.Docs/Execution/ObservabilityDocumentationSpecs.cs#observability-best-so-far{csharp}

The first directions it receives fix it; other directions or another dimensionality throw. The ready-made traces create their own best-so-far and retention objects, as `TraceRetention.EveryNth(10)` returns a fresh object every call. Hoisting one into a variable and passing it to two traces makes them share its state.

To collect several sources of one observation type into one accumulator and result sink, create one combined trace:

<<< ../../../test/HeuristicLib.Tests.Docs/Execution/ObservabilityDocumentationSpecs.cs#observability-combined-trace{csharp}

Attach `combined` to both runs to intentionally accumulate shared history. Separate traces have independent state when each receives its own best-so-far and retention objects.

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

An algorithm observation's `Iteration` numbers the iterations of one call, as the algorithm sees them. A meta-algorithm that runs an inner algorithm again calls it again, so the inner iterations number from 1 again, whatever the child scope policy. A clock over the iterations keeps counting across those calls.

An inner algorithm does not know it runs inside a cycle, so the cycle algorithm reports its own progress. `cycle.Cycles` observes the start of each cycle as a `CycleStart`, and `cycle.Turns` the start of each turn, one run of an inner algorithm, as a `CycleTurn` with the cycle and turn numbers, the inner algorithm, its position and the state it starts from. Both number from 1 in each call of the cycle, and `Clock.FromCycles` and `Clock.FromTurns` count them:

<<< ../../../test/HeuristicLib.Tests.Docs/Execution/ObservabilityDocumentationSpecs.cs#observability-cycle{csharp}

`exploitation` runs two iterations per turn. By the iteration clock, the trace holds (1, 1), (2, 2), (3, 1) and (4, 2): the clock continues while the observed iteration restarts. By the cycle clock, it holds (1, 1), (1, 2), (2, 1) and (2, 2). A cycle or turn is observed at its start, before the inner algorithm runs, so its clock has counted it when the turn's iterations are recorded.

## Experiments

An experiment creates fresh analyzers for every trial from a function of the trial's algorithm. `AddAspectPerTrial` returns the typed trial/analyzer pairs; read each analyzer directly. See [Experiments](/guide/execution/experiments).

## Observe an operation of your own

An operation you define becomes observable when it declares an observation record, the advice that builds it, and an `Invocations` source, as each built-in operation does. Its source then works with `Analyzer.Trace` and `Aspect.Observe` like any other. An observation that carries the problem of the invocation it observed lets a trace rank by that problem's objective. See [Make the operation observable](/contributing/architecture/operator-implementation#make-the-operation-observable) for a complete example.

A configuration of your own can report observations from inside its executor, at points that no boundary shows. Declare an observation record and a named child with `ObservationPoint.Of<TObservation>(this, nameof(Point))`. Resolve it in the configuration's build with `scope.Resolve(Point)` and call its executor's `Report` method. The point is an observation source, so traces use it directly. For example, a cooling schedule of your own can report each temperature it computes:

<<< ../../../test/HeuristicLib.Tests.Docs/Execution/ObservabilityDocumentationSpecs.cs#observability-point-type{csharp}

<<< ../../../test/HeuristicLib.Tests.Docs/Execution/ObservabilityDocumentationSpecs.cs#observability-point-use{csharp}

Each named point has its own identity, so several points of one configuration may report the same value type. The configuration resolves only its own declared points. A cooling step carries no objective, so this trace reads plain values and ranks nothing. Unobserved points still resolve and receive records through an executor that ignores reports.

## Use an aspect directly

`Aspect.Create` pairs a pointcut with advice. Pass it to `ResolutionScope.Create`, `With` or `CreateChildScope`, or add it to a preparing run with `AddAspect`.

<<< ../../../test/HeuristicLib.Tests.Docs/Execution/ObservabilityDocumentationSpecs.cs#observability-aspects{csharp}

Earlier advice wraps closer to the source executor. Here timing finishes before the observation callback, so the callback runs outside the timed span. Counting updates only after success; timing uses `finally` and records failed invocations too.

To select several configurations, pair a pointcut with role advice:

<<< ../../../test/HeuristicLib.Tests.Docs/Execution/ObservabilityDocumentationSpecs.cs#observability-role-advice{csharp}

Custom selection derives a small class:

<<< ../../../test/HeuristicLib.Tests.Docs/Execution/ObservabilityDocumentationSpecs.cs#observability-custom-pointcut{csharp}

Reference, type and custom pointcuts of the same configuration type combine with `And`/`&` and `Or`/`|`. Matching short-circuits from left to right. The pointcut sees configurations that resolution reaches, including those created later by a meta-algorithm.

An aspect is the ordered list of its pointcut/advice pairs. An aspect subclass assigns several pairs to `Advices` in its constructor and can spread a clock's pairs before its own observations, as in `Advices = [.. clock, .. Aspect.Observe(evaluator.Invocations, Record)]`. Resolution applies each advice instance once, at its first position; sharing a clock across several traces therefore counts each invocation once. If the same advice is paired with two pointcuts, the first pairing wins. Use separate advice instances for separate selections.

A new scope layer precedes inherited layers. Data that outlives one executor belongs to the advice object, so a clock keeps counting across every execution of the configuration it observes, including a meta-algorithm's fresh activations, and reusing it across runs continues it too. An observation's own numbers, such as an algorithm's `Iteration`, belong to the call they observe. Reusing operator advice, or its caller-owned accumulator, intentionally combines its counts or durations.

Continue with [Writing custom advice](/guide/extending/writing-advice) for complete role-specific and general advice examples and run attachment.
