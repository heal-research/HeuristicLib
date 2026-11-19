# Observability and analysis

Track improvements with `Analyzer.BestSoFar<RealVector>()`, pass the analyzer to `CreateRun` and enumerate `run.Result(best)`. Each run owns independent data.

<<< ../../../test/HeuristicLib.Tests.Docs/Execution/ObservabilityDocumentationSpecs.cs#observability-imports{csharp}

<<< ../../../test/HeuristicLib.Tests.Docs/Execution/ObservabilityDocumentationSpecs.cs#observability-simple-best{csharp}

An algorithm observation captures each search state it yields, after its interceptors have transformed that state. No placeholder interceptor is needed. For simple progress logging, consuming `algorithm.RunStreaming(problem, random)` directly is also sufficient.

## Choose the right tool

An analyzer is one of several ways to see what a run does. Pick by what you need:

| Need                                                                                        | Use                                                                                                                    |
| ------------------------------------------------------------------------------------------- | ---------------------------------------------------------------------------------------------------------------------- |
| Look at every state the algorithm yields                                                    | `algorithm.RunStreaming(problem, random)`                                                                              |
| Only the final state                                                                        | `algorithm.RunToCompletionAsync(problem, random)`                                                                      |
| Typed data from chosen boundaries, such as a quality curve or an operator's offspring       | An analyzer passed to `CreateRun`                                                                                      |
| Behavior at a boundary that is not analysis, such as logging or bridging to another runtime | A [wrapper](/guide/extending/writing-wrappers) placed with `Substitute`                                                |
| A count or duration that stops or limits the run                                            | Inspection such as `Invocations.CountingCandidates(counter)` or `LimitedToCandidates`, read by terminators and budgets |
| The same analysis for every experiment trial                                                | Pass the analyzer to [experiment run creation](/guide/execution/experiments#add-analysis)                              |

Inspection preserves the observed computation while recording its own observations, counts, durations and clocks. Analyzers derive results from those observations. Observation callbacks must preserve the operation's inputs and outputs; they receive live references rather than isolated deep copies. Wrappers of your own may change behavior, and explicit algorithm policies such as budgets may read inspection data to decide when to stop.

Mutation-strength adaptation uses a declared adaptation node shared by the rule and mutator. Inspection, naming, retry and composition wrappers need no control forwarding; the mutator reads its resolved adaptation.

## Choose a boundary and a clock

Each operator offers its successful invocations as `Invocations`, and each algorithm offers its yielded states as `Iterations`. These observation sources describe a boundary; creating one installs nothing. An evaluator's `Invocations` observes one completed evaluator invocation, including the whole batch. An algorithm's `Iterations` observes a yielded state. A nested algorithm is a separate observation boundary.

Custom traces use source extensions such as `evaluator.Invocations.Trace(...)`; population traces extend `algorithm.Iterations`:

<<< ../../../test/HeuristicLib.Tests.Docs/Execution/ObservabilityDocumentationSpecs.cs#observability-boundary-and-clock{csharp}

A clock gives each recorded entry its time. Use `counter.Clock` or `duration.Clock` to read an accumulator as a trace axis. Counters and durations place nothing: the caller supplies their placement in the `substitutions` list of `CreateRun` or in `Substitute`. In one list a later entry sits closer to its target. List a counter's or duration's placement after every analyzer that reads it so the reading includes the call; listed before them, it reads before the call. A counter or duration placed nowhere reads zero, including when a trace reads it.

`algorithm.Iterations.Counting(counter)` counts yielded states, `algorithm.Calls.Counting(counter)` counts started calls, and `source.Invocations.Counting(counter)` or `CountingCandidates(counter)` counts successful operator calls or their candidate work. `Clock.Elapsed` reads the time a run spends advancing its algorithm, including a call in progress. Executor construction before the first advance, pauses and consumer work are excluded. Deferred child construction during an advance, such as a fresh Cycle turn, is included. Direct scope execution leaves it and `scope.Results.Elapsed` at zero; use `algorithm.Timed(duration)` for explicit measurement there. Counts accumulate across fresh executor activations within one run unless `Sharing` narrows their accumulator.

A custom clock implements `IClock<TTime>`. Its typed `CreateExecutionNode(ResolutionScope)` creates a reading for the run; the boxed construction delegates to it. The root owns one reading per clock instance and run. Its `Read()` may be called concurrently and must preserve the observed computation. Typed reads such as `At(clock)` and `By(clock)` infer `TTime` through `IClock<TTime>`. Mixed clock lists use `IReadOnlyList<IClock>`, and `results.Result(clock)` infers its type through `IClock`. Through `IClock<TTime>` it needs an explicit result type because the typed interface has two configuration-node constructions. A clock only reads. If it also needs wrappers to update its value, it implements `ISubstitution`, and the caller lists it after the analyzers that read it. A clock that observes calls holds a private `Counter`: its recorder increments the run's accumulator, and its reading reads that same root accumulator. It places an `Observed` wrapper for itself through a `SubstitutionGroup`. Traces and best-so-far analyzers place no clocks. `Clock.Elapsed` needs no placement.

Several traces may share the same clock in one run. Place its counting or timing wrapper once, after all those analyzers, to update it once per call before their readings. Separate trace placements add no clock wrappers, so they do not duplicate the count. Equal clocks identify one axis, including when supplied twice. Repeated `counter.Clock` reads give equal clocks, and `new ElapsedClock()` reads the column selected with `Clock.Elapsed`. Clocks of two distinct counters identify two axes. A class clock without an equality override, such as the epoch clock, matches by reference; keep that instance to project its axis.

Candidate counts describe the selected boundary. A caching evaluator's outer boundary counts requests while its inner evaluator counts cache misses. An operation without candidate work rejects `Invocations.CountingCandidates(counter)` when the substitution is created; use `Invocations.Counting(counter)` for its calls.

Observation sources match configurations by reference. A copied configuration is observed separately, even if its settings compare equal. A configuration that never participates produces no observations. Create analyzers for the actual configured objects that will execute. An operator implementing several operations offers each operation's source through that operation's typed view.

### Sources and `IObservable<T>`

A source resembles a cold `IObservable<T>`: it describes observations without producing any until something subscribes. Here, a trace from `Analyzer.Trace` or a callback from `Observing` subscribes when `Substitute` places it in the configuration. The library implements neither `IObservable<T>` nor Rx-style operators. A callback can feed observations into Rx, for example into a subject:

```csharp
var subject = new Subject<CrossoverObservation<RealVector>>();
var feed = crossover.Invocations.Observing(subject.OnNext);
var run = algorithm.CreateRun(problem, random, [feed]);
```

The callback runs on the thread that invoked the operator, inside the wrapped invocation. Anything taking a callback produces a configuration that cannot be serialized.

## Select configurations by properties or names

A predicate passed to `Substitute` or `Substitution.Of` selects configurations by their data, and the type of its parameter selects the role. Combine conditions with `&&`, `||` and `!`. Predicates run during substitution and are not stored; persistence concerns the nodes placed in the returned graph.

Name a configured operator through `.Named(name)` and select that boundary explicitly:

<<< ../../../test/HeuristicLib.Tests.Docs/Execution/ObservabilityDocumentationSpecs.cs#observability-named-selection{csharp}

The receiver here is an `IOperator<Mutation, RealVector>`. Named wrappers exist for creators, crossovers, evaluators, mutators, refiners, replacers, selectors, interceptors, terminators and algorithms. Mutation and evaluation use `Named.Create(child, name)`; other roles retain their role-specific construction companions. A consumer-defined configuration can implement `INamedConfiguration` directly.

Names compare exactly and case-sensitively. Several nodes may share a name, and all match. The wrapper and its child are separate configuration nodes: a name selects the wrapper, while a selection by role type can select both boundaries. Names do not change execution sharing or create caller paths. The child's concrete properties remain on the child configuration.

## Ownership and reads

Pass analyzers to `CreateRun(problem, random, [analyzer])`. To keep an instrumented configuration, use `Substitute` on a receiver typed by its interface when the receiver itself is wrapped. A call places a repeated substitution once at its last position; separate calls place it again. All placements collect into one result per run.

`LifecycleState` reports whether a run is `Ready`, `Running`, `Paused`, `Completed`, `Canceled`, `Failed` or `Stopped`. The returned run accepts no further attachments. Each returned execution stream has one consumer. The run itself can continue through a later `Stream()` call after the consumer stops at a yielded root-algorithm state.

Read the trace through `run.Result(trace)`. Its `SampleCount` and `Latest` properties do not allocate; `Latest` is null before an entry is recorded. `Snapshot()` and `By(clock)` allocate stable copies without stopping collection.

Results remain available after completion, cancellation, failure or pausing. A trace supports snapshots during execution. Read accumulating results such as a Pareto front or genealogy graph after execution has stopped. Disposing a stream enumerator pauses the run without disposing its retained algorithm iterator.

## Retention and accumulation

`TracePopulationQuality` records the current best, median, and worst of each observation's objective vectors, such as those of a yielded population. `Analyzer.BestSoFar` keeps the best candidate across evaluator observations and records only improvements by default. Use `TracePopulationCandidates` for population candidates. Best-so-far points retain their candidate directly.

<<< ../../../test/HeuristicLib.Tests.Docs/Execution/ObservabilityDocumentationSpecs.cs#observability-retention{csharp}

`TraceRetention.EveryNth(10)` records observations 10, 20, 30, and so on, with no extra initial or final entry. `OnChange()` records the first value and subsequent changes by equality; `LatestOnly()` replaces the preceding entry. Retention definitions are immutable, and each result creates its own retention progress even when several traces or runs use one definition.

Retention runs after a trace's function reads an observation. It controls stored entries and never skips expensive computations, so best-so-far still sees an improvement between retained observations. `EveryNth` and `OnChange` leave stored entries alone; `LatestOnly` is the one policy that discards what the trace already held.

## Accumulating analyzers

Some analysis produces one mutable summary rather than a history. Use `source.Aggregate(create, update)` to create plain state for each run and update it from observations. Read the state with `run.Result(summary).Value`. Updates are serialized per run; reading the state during execution is the state type's concern:

<<< ../../../test/HeuristicLib.Tests.Docs/Execution/ObservabilityDocumentationSpecs.cs#observability-accumulating-analyzer{csharp}

<<< ../../../test/HeuristicLib.Tests.Docs/Execution/ObservabilityDocumentationSpecs.cs#observability-summary-state{csharp}

Read an accumulating analyzer after the run finishes. Its result spans the whole run, including Cycle turns; a `Share` does not narrow the result that recording and `run.Result` use. Share counters and durations, not analyzers: resolving a shared analyzer directly gives a separate result that nothing records into. For several observation types, use the class form described in [Analyzer architecture](/contributing/architecture/analyzers). Clocks and retention apply to trace entries, so they do not apply to a single mutable summary.

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

A parameter grid declares variation on a prototype with a descriptor, a top-level parameter name or a typed configurator. Each dimension takes a nonempty value list. An experiment combines grids through `Experiment.Create` and sets repetitions and stream sharing. The experiment accepts analysis definitions in `CreateRun(problem, random, [definition])`. The run applies them after variation once per parameter candidate, validates once per problem and parameter candidate by default, and checks placement for every trial. Pass `validate: false` to skip configuration validation.

Descriptor and name dimensions label `trial.Settings` in declaration order. Read each trial's result through `trial.Run.Result(definition)`. A reference-based source on the prototype root only matches a parameter candidate that retains that original instance, such as in `algorithm.Repeat(n)`. For varied parameter candidates, use a default-selecting analyzer or name a child that all parameter candidates retain. See [Experiments](/guide/execution/experiments#add-analysis) for examples.

## Observe an operation of your own

An operation you define becomes observable when it declares an observation record and an `Invocations` source, as each built-in operation does. Its source then works with `Analyzer.Trace` and `Observing` like any other. An observation that carries the problem of the invocation it observed lets a trace rank by that problem's objective. See [Make the operation observable](/contributing/architecture/operator-implementation#make-the-operation-observable) for a complete example.

## Count, time and observe an operator

`CreateRun` or `Substitute` applies counting and timing substitutions alongside observation callbacks:

<<< ../../../test/HeuristicLib.Tests.Docs/Execution/ObservabilityDocumentationSpecs.cs#observability-analyzers{csharp}

Pass everything in one `CreateRun` substitutions list or one `Substitute` call, which resolves targets against that call's input. With `CreateRun`, apply budgets first and name targets from the configuration it receives. Later entries sit closer to the operator, so in the example the timing finishes before the observation callback runs. Place a shared clock's counting or timing wrapper once, after every analyzer that reads it. Counting updates after success; timing also records failed calls.

Create counters with `new Counter()` and durations with `new Duration()`. `Invocations.Counting(counter)`, `CountingCandidates(counter)` and `Timing(duration)` return substitutions placing their wrappers. Read their accumulators through the run. `Counted` and `Timed` place the same wrappers directly around a selected configuration. The wrapper decides what is counted, so pass `OperatorCountMetric.Candidates` to `Counted` to count candidate work.

### Share accounting with a terminator

A count terminator holds the counter definition whose result it reads. Place a counting wrapper for that counter so successful calls update the same run-bound result:

<<< ../../../test/HeuristicLib.Tests.Docs/Execution/ObservabilityDocumentationSpecs.cs#observability-shared-accounting{csharp}

The run stops after two successful mutation invocations and `run.Result(mutations)` returns the measured accumulator. A run-wide counter accumulates throughout the run, but a terminator stops only its own stream. Place it on the root algorithm to stop the run. A counter placed nowhere stays at zero and never stops a positive-limit terminator.

### Select several configurations

`Substitute` places a wrapper wherever a selection matches. Select by the type of the lambda's parameter to wrap every configuration of a role:

<<< ../../../test/HeuristicLib.Tests.Docs/Execution/ObservabilityDocumentationSpecs.cs#observability-type-selection{csharp}

Pass a predicate before the replacement to select by properties:

<<< ../../../test/HeuristicLib.Tests.Docs/Execution/ObservabilityDocumentationSpecs.cs#observability-predicate-selection{csharp}

Apply either with `algorithm.Substitute(counting)` and read the counter through the run. A selection by type or predicate that matches nothing changes nothing, while a target given by reference must be held by the configuration. A selection reaches the configurations the graph declares, including a cycle's inner algorithms, but not those a delegate creates at run time.

Substitutions apply one after another, and a later one sits closer to the node it matches. Analyzer results remain run-bound across child activations, while a wrapper of your own may intentionally retain caller-owned runtime dependencies. An observation's own numbers, such as `Iteration`, describe the call it observes.

Continue with [Writing wrappers](/guide/extending/writing-wrappers) for complete typed and general wrapper examples and their placement.

## Best-so-far selection and placement

Without a named evaluator, `Analyzer.BestSoFar<TCandidate>()` observes evaluators of that candidate type holding no such evaluator among their declared descendants. A cache therefore counts misses at its inner evaluator. Evaluators inside refiners count too. For composite evaluations or refinement against another problem, name the intended evaluator with `Analyzer.BestSoFar(evaluator)`. Each point gives `Evaluations`, `Value` and `Candidate`. Explicit placement counts inside the observer with `evaluator.Counted(best.Evaluations, OperatorCountMetric.Candidates).Observed(best)`. Time is optional: pass `clocks: [Clock.Elapsed]` and read `point.At(Clock.Elapsed)`.

`CreateRun` places analyzers after budgets. Name targets from the budgeted configuration, such as `limited.Algorithm.Iterations`, rather than from its original algorithm. `Substitute` keeps the placed analysis in the returned configuration; the `substitutions` list of `CreateRun` applies only to that run. Substitutions themselves are never persisted. Persist the produced graph, whose placed nodes must be data; delegate-bearing analysis cannot be serialized.
