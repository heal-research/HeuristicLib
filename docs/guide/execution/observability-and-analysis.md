# Observability and analysis

An analyzer collects typed data from the observations of a run, such as a quality curve, a count of evaluations or the offspring of an operator. This page goes from the simplest use to the most specialized: choose a tool, pass a first analyzer to `CreateRun`, then choose boundaries and clocks, keep traces, accumulate, count and time, observe cycles and rank several objectives, and finally write observations and clocks of your own.

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

Inspection preserves the observed computation while recording its own observations, counts, durations and clocks. Analyzers derive results from those observations. Observation callbacks must preserve the operation's inputs and outputs. Wrappers of your own may change behavior, and explicit algorithm policies such as budgets may read inspection data to decide when to stop. Wrappers forward no controls: a mutator reads its [adaptation](/guide/extending/operator-composition#adaptable-parameters) as a declared node, whichever inspection wrappers surround it.

## A first analyzer

Create an analyzer, pass it in the list of `CreateRun` and read its result from the run. `Analyzer.BestSoFar<RealVector>()` tracks improvements, and `run.Result(best)` enumerates them. Each run owns independent data.

<<< ../../../test/HeuristicLib.Tests.Docs/Execution/ObservabilityDocumentationSpecs.cs#observability-imports{csharp}

<<< ../../../test/HeuristicLib.Tests.Docs/Execution/ObservabilityDocumentationSpecs.cs#observability-simple-best{csharp}

[Running algorithms](/guide/execution/running-algorithms#read-run-bound-results) covers how an analyzer is attached to a run, the lookup rules of `run.Result` and when a result can be read. To keep an instrumented configuration instead of instrumenting one run, use `Substitute`. [Configurations](/guide/fundamentals/configurations#rewriting-a-graph-with-substitute) explains it, including the type a receiver that is itself wrapped needs, and [when to use which](/guide/fundamentals/configurations#substituting-for-one-run).

## Choose a boundary and a clock

Each operator offers its successful invocations as `Invocations`, and each algorithm offers its yielded states as `Iterations`. These observation sources describe a boundary; creating one installs nothing. An evaluator's `Invocations` observes one completed evaluator invocation, including the whole batch. An algorithm's `Iterations` observes a yielded state, after its interceptors have transformed that state, and needs no placeholder interceptor. A nested algorithm is a separate observation boundary.

Observation sources match configurations by reference, as [Configurations](/guide/fundamentals/configurations#the-configuration-graph) describes. A configuration that never participates produces no observations. Create analyzers for the actual configured objects that will execute. An operator implementing several operations offers each operation's source through that operation's typed view.

A clock gives each recorded entry its time. A `Counter` counts what its placement observes and a `Duration` accumulates the time its placement measures; `counter.Clock` and `duration.Clock` read them as trace axes. [Count, time and observe an operator](#count-time-and-observe-an-operator) covers counters and durations. Ready-made traces name their boundary: `Analyzer.BestSoFar(algorithm.Evaluator, ...)` observes the evaluator, and `algorithm.Iterations.TracePopulationQuality(...)` observes yielded states. The example records both against one evaluation counter:

<<< ../../../test/HeuristicLib.Tests.Docs/Execution/ObservabilityDocumentationSpecs.cs#observability-boundary-and-clock{csharp}

Counters and durations place nothing: the caller supplies their placement in the `substitutions` list of `CreateRun` or in `Substitute`, as the example does with `CountingCandidates(evaluations)`. A later entry in one list sits closer to its target ([Configurations](/guide/fundamentals/configurations#what-substitute-guarantees)). List a counter's or duration's placement after every analyzer that reads it so the reading includes the call; listed before them, it reads before the call. A counter or duration placed nowhere reads zero, including when a trace reads it. Several traces may share one clock: place its counting or timing wrapper once, after all those analyzers, to update it once per call before their readings. Separate trace placements add no clock wrappers, so they do not duplicate the count.

`algorithm.Iterations.Counting(counter)` counts yielded states, `algorithm.Calls.Counting(counter)` counts started calls, and `source.Invocations.Counting(counter)` or `CountingCandidates(counter)` counts successful operator calls or their candidate work. `Clock.Elapsed` reads the time a run spends advancing its algorithm, including a call in progress. Executor construction before the first advance, pauses and consumer work are excluded. Deferred child construction during an advance, such as a fresh Cycle turn, is included. Direct scope execution leaves it and `scope.Results.Elapsed` at zero; use `algorithm.Timed(duration)` for explicit measurement there. [Sharing executors](/guide/fundamentals/configurations#sharing-executors) explains how counters and durations accumulate across a cycle's turns.

Equal clocks identify one column of every entry, including when a clock is supplied twice. Repeated `counter.Clock` reads give equal clocks, and `new ElapsedClock()` reads the column selected with `Clock.Elapsed`. Clocks of two distinct counters identify two columns. A class clock without an equality override, such as the epoch clock, matches by reference; keep that instance to project its column. `TraceEntry.At(clock)` and `BestSoFarPoint.At(clock)` read the column of the equal clock the analyzer was given, and `By(clock)` on a trace result or snapshot projects it for every entry. A clock the analyzer was not given throws `InvalidOperationException`.

Read a trace through `run.Result(trace)`. Its `SampleCount` and `Latest` properties do not allocate; `Latest` is null before an entry is recorded. `Snapshot()` and `By(clock)` allocate stable copies without stopping collection, so a trace can also be read during execution. Take one snapshot when you want consistent projections onto several axes. The example projects it onto a single axis, the evaluation counter. A trace created with `clocks: [evaluations.Clock, Clock.Elapsed]` would also offer `snapshot.By(Clock.Elapsed)`, a second axis over the same entries:

<<< ../../../test/HeuristicLib.Tests.Docs/Execution/ObservabilityDocumentationSpecs.cs#observability-snapshot{csharp}

A snapshot preserves its selected axes even when empty. Selected clocks are read when an entry is recorded. Clocks on independent concurrent boundaries do not promise an atomic global timestamp.

Candidate counts describe the selected boundary. A caching evaluator's outer boundary counts requests while its inner evaluator counts cache misses. An operation without candidate work rejects `Invocations.CountingCandidates(counter)` when the substitution is created; use `Invocations.Counting(counter)` for its calls.

## Traces and retention

`TracePopulationQuality` records the current best, median, and worst of each observation's objective vectors, such as those of a yielded population. Use `TracePopulationCandidates` for population candidates.

<<< ../../../test/HeuristicLib.Tests.Docs/Execution/ObservabilityDocumentationSpecs.cs#observability-retention{csharp}

`TraceRetention.EveryObservation()` keeps each observation, so a best-so-far analyzer records one point for every non-empty evaluation batch and not only its improvements; a batch that evaluates nothing records no point. `TraceRetention.EveryNth(10)` records observations 10, 20, 30, and so on, with no extra initial or final entry. `OnChange()` records the first value and subsequent changes by equality; `LatestOnly()` replaces the preceding entry. Retention policies are immutable, and each result creates its own retention progress even when several traces or runs use one policy.

Retention runs after a trace's function reads an observation. It controls stored entries and never skips expensive computations, so best-so-far still sees an improvement between retained observations. `EveryNth` and `OnChange` leave stored entries alone; `LatestOnly` is the one policy that discards what the trace already held.

### Custom traces

`Analyzer.Trace` reads one value from each observation of a source through a function:

<<< ../../../test/HeuristicLib.Tests.Docs/Execution/ObservabilityDocumentationSpecs.cs#observability-custom-traces{csharp}

Each operation has its own observation record, such as `CrossoverObservation<TCandidate>` with `Parents` and `Offspring`, or `EvaluationObservation<TCandidate>` with `Candidates` and `ObjectiveVectors`. It holds the invocation's inputs and outputs and reads the search space and problem at the candidate's interfaces, so it fits every run over the candidate. Each batch is an immutable `ValueArray<T>` snapshot, reusing existing immutable storage where possible, so an observer never sees a later change to a list an operator reuses. The candidates in a batch are live references, not isolated deep copies.

Counting, collecting and single values are plain functions of the observation. Ranking functions such as `Best` and `BestMedianWorst` receive the objective directions they order by, usually the observed invocation's own, `observation.Problem.Objective`, so each invocation is ranked by its own problem. `MinMeanMax` summarizes plain numbers and needs no directions.

## Accumulating analyzers

Some analysis produces one mutable summary rather than a history. Use `source.Aggregate(create, update)` to create plain state for each run and update it from observations. Read the state with `run.Result(summary).Value`. Updates are serialized per run; reading the state during execution is the state type's concern:

<<< ../../../test/HeuristicLib.Tests.Docs/Execution/ObservabilityDocumentationSpecs.cs#observability-accumulating-analyzer{csharp}

<<< ../../../test/HeuristicLib.Tests.Docs/Execution/ObservabilityDocumentationSpecs.cs#observability-summary-state{csharp}

Read an accumulating analyzer after the run finishes, as for accumulating results such as a Pareto front or a genealogy graph. Its result spans the whole run, including Cycle turns; sharing does not narrow the result that recording and `run.Result` use. Share counters and durations, not analyzers: resolving a shared analyzer directly gives a separate result that nothing records into. For several observation types, use the class form described in [Analyzer architecture](/contributing/architecture/analyzers). Clocks and retention apply to trace entries, so they do not apply to a single mutable summary.

## Best-so-far selection and placement

`Analyzer.BestSoFar` keeps the best candidate across evaluator observations and records only improvements by default. Each point retains its candidate directly and gives `Evaluations`, `Value` and `Candidate`.

Without a named evaluator, `Analyzer.BestSoFar<TCandidate>()` observes evaluators of that candidate type holding no such evaluator among their declared descendants. A cache therefore counts misses at its inner evaluator. Evaluators inside refiners count too. For composite evaluations or refinement against another problem, name the intended evaluator with `Analyzer.BestSoFar(evaluator)`. Explicit placement counts inside the observer with `evaluator.Counted(best.Evaluations, OperatorCountMetric.Candidates).Observed(best)`. Time is optional: pass `clocks: [Clock.Elapsed]` and read `point.At(Clock.Elapsed)`.

### Keep a best-so-far of your own

A best-so-far object keeps the running best across observations inside a custom trace. `BestSoFar` keeps an objective vector, and `BestSoFar<T>` keeps a value ranked by its selected objective vector. Supply a state factory so every run creates a separate accumulator:

<<< ../../../test/HeuristicLib.Tests.Docs/Execution/ObservabilityDocumentationSpecs.cs#observability-best-so-far{csharp}

The first directions a best-so-far accumulator receives fix its ordering. Other directions or dimensionality throw. Ready-made traces create their accumulation state per run. State deliberately captured by a caller's own delegate remains the caller's responsibility.

### Combine several sources in one trace

To collect several sources of one observation type into one accumulator and result sink, create one combined trace:

<<< ../../../test/HeuristicLib.Tests.Docs/Execution/ObservabilityDocumentationSpecs.cs#observability-combined-trace{csharp}

Attach `combined` to the enclosing algorithm to collect several sources into one result within each run. Reusing the analyzer in another run creates a separate result.

A combined trace observes each boundary once. Giving it the same source twice records each invocation once. The sources of different operations of one operator observe different boundaries, and each is recorded.

## Count, time and observe an operator

`CreateRun` or `Substitute` applies counting and timing substitutions alongside observation callbacks:

<<< ../../../test/HeuristicLib.Tests.Docs/Execution/ObservabilityDocumentationSpecs.cs#observability-analyzers{csharp}

Pass everything in one `CreateRun` substitutions list or one `Substitute` call; [Configurations](/guide/fundamentals/configurations#what-substitute-guarantees) explains how the substitutions of one call combine. The example lists the timing last, so it sits closest to the operator and finishes before the observation callback runs. Counting updates after success; timing also records failed calls.

Create counters with `new Counter()` and durations with `new Duration()`. `Invocations.Counting(counter)`, `CountingCandidates(counter)` and `Timing(duration)` return substitutions placing their wrappers; `algorithm.Iterations.Timing(duration)` times an algorithm's calls the same way. Read their accumulators through the run, as [Configurations](/guide/fundamentals/configurations#substituting-for-one-run) shows. `Counted` and `Timed` place the same wrappers directly around a selected configuration. The wrapper decides what is counted, so pass `OperatorCountMetric.Candidates` to `Counted` to count candidate work.

### Share accounting with a terminator

A count terminator holds the counter whose result it reads. Place a counting wrapper for that counter so successful calls update the same run-bound result:

<<< ../../../test/HeuristicLib.Tests.Docs/Execution/ObservabilityDocumentationSpecs.cs#observability-shared-accounting{csharp}

The run stops after two successful mutation invocations and `run.Result(mutations)` returns the measured accumulator. A terminator stops only its own stream, so place it on the root algorithm to stop the run. A counter placed nowhere stays at zero and never stops a positive-limit terminator.

When the algorithm carries a budget, name the targets of an analyzer from the configuration you pass to `CreateRun`, as [Configurations](/guide/fundamentals/configurations#substituting-for-one-run) states.

## Select configurations by properties or names

`Substitute` places a wrapper wherever a selection matches, as [Configurations](/guide/fundamentals/configurations#select-by-type-or-predicate) describes. Select by the type of the lambda's parameter to wrap every configuration of a role:

<<< ../../../test/HeuristicLib.Tests.Docs/Execution/ObservabilityDocumentationSpecs.cs#observability-type-selection{csharp}

Pass a predicate before the replacement to select by properties. Combine conditions with `&&`, `||` and `!`:

<<< ../../../test/HeuristicLib.Tests.Docs/Execution/ObservabilityDocumentationSpecs.cs#observability-predicate-selection{csharp}

Apply the type selection with `algorithm.Substitute(counting)` and the predicate selection with `algorithm.Substitute(smallMutations)`, and read the counter through the run. [Configurations](/guide/fundamentals/configurations#rewriting-a-graph-with-substitute) states what a selection reaches, when it matches nothing and which order substitutions apply in.

### Select by name

Name a configured operator through `.Named(name)` and select that boundary explicitly:

<<< ../../../test/HeuristicLib.Tests.Docs/Execution/ObservabilityDocumentationSpecs.cs#observability-named-selection{csharp}

The receiver here is an `IOperator<Mutation, RealVector>`, which fixes the operation the wrapper names. One `Named<TOperation, TCandidate>` serves every operator role: creators, crossovers, evaluators, mutators, refiners, replacers, selectors, interceptors and terminators. Create it with `.Named(name)` or `Named.Create(child, name)`. Algorithms use `NamedAlgorithm.Create(child, name)` or `algorithm.Named(name)`. A consumer-defined configuration can implement `INamedConfiguration` directly.

Names compare exactly and case-sensitively. Several nodes may share a name, and all match. The wrapper and its child are separate configuration nodes: a name selects the wrapper, while a selection by role type can select both boundaries. Names do not change execution sharing or create caller paths. The child's concrete properties remain on the child configuration.

## Cycles and turns

An algorithm observation's `Iteration` numbers the iterations of one call, as the algorithm sees them. A meta-algorithm that runs an inner algorithm again calls it again, so the inner iterations number from 1 again, whether its executor is fresh or reused. A clock over the iterations keeps counting across those calls.

A turn is one call of an inner algorithm. Its `Calls` source reports `Started`, then `Completed`, `Failed` or `Stopped`, including calls that yield no states. There is no separate cycle-start observation source: observe a cycle through the turns of its inner algorithms. The example counts calls of `exploitation` and combines both inner algorithms' call events:

<<< ../../../test/HeuristicLib.Tests.Docs/Execution/ObservabilityDocumentationSpecs.cs#observability-cycle{csharp}

`exploitation` runs two iterations per call. Its iteration trace holds (1, 1), (1, 2), (2, 1) and (2, 2) against its call counter. A `Started` observation reads that counter before its own call is counted; yielded states and the call's end read the updated count. The combined event trace records both inner algorithms. Call observations provide no cycle number or slot index.

## Multi-objective ranking

Single-objective traces use the observed problem's objective comparer automatically. A multi-objective problem without a total order cannot supply a unique best, median, or worst. Ranking throws when first required, even for a singleton batch. There is no implicit lexicographic fallback.

Select an objective comparer explicitly when the trace needs a total order:

<<< ../../../test/HeuristicLib.Tests.Docs/Execution/ObservabilityDocumentationSpecs.cs#observability-lexicographic{csharp}

This makes dimension priority explicit and affects this trace's comparisons, including best-so-far accumulation. It does not change the problem or algorithm's objective. Analysis accepts the objective system's existing `IComparer<ObjectiveVector>` implementations. The `RequireTotalOrder` helper also serves callers outside analysis. Ranking is checked at observation time because installation has no problem context.

## Experiments

An experiment runs the parameter candidates of its grids on each problem and repetition. Pass the same analyzers in the list of its `CreateRun` and read each trial's result through `trial.Run.Result(analyzer)`. [Experiments](/guide/execution/experiments#add-analysis) shows how placement works when parameter candidates vary.

## Observations, clocks and sources of your own

### An operation of your own

An operation you define becomes observable when it declares an observation record and an `Invocations` source, as each built-in operation does. Its source then works with `Analyzer.Trace` and `Observing` like any other. An observation that carries the problem of the invocation it observed lets a trace rank by that problem's objective. See [Make the operation observable](/contributing/architecture/operator-implementation#make-the-operation-observable) for a complete example.

### A clock of your own

A custom clock implements `IClock<TTime>`. Its typed `CreateExecutionNode(ResolutionScope)` creates a reading for the run; the boxed construction delegates to it. The root owns one reading per clock instance and run. Its `Read()` may be called concurrently and must preserve the observed computation.

Typed reads such as `At(clock)` and `By(clock)` infer `TTime` through `IClock<TTime>`. Mixed clock lists use `IReadOnlyList<IClock>`, and `results.Result(clock)` infers its type through `IClock`. Through `IClock<TTime>` it needs an explicit result type because the typed interface has two configuration-node constructions.

A clock only reads. If it also needs wrappers to update its value, it implements `ISubstitution`, and the caller lists it after the analyzers that read it. A clock that observes calls holds a private `Counter`: its recorder increments the run's accumulator, and its reading reads that same root accumulator. It places an `Observed` wrapper for itself through a `SubstitutionGroup`. Traces and best-so-far analyzers place no clocks. `Clock.Elapsed` needs no placement.

### Sources and `IObservable<T>`

A source resembles a cold `IObservable<T>`: it describes observations without producing any until something subscribes. Here, a trace from `Analyzer.Trace` or a callback from `Observing` subscribes when `Substitute` places it in the configuration. The library implements neither `IObservable<T>` nor Rx-style operators. A callback can feed observations into Rx, for example into a subject:

```csharp
var subject = new Subject<CrossoverObservation<RealVector>>();
var feed = crossover.Invocations.Observing(subject.OnNext);
var run = algorithm.CreateRun(problem, random, [feed]);
```

The callback runs on the thread that invoked the operator, inside the wrapped invocation. [Configurations](/guide/fundamentals/configurations#what-substitute-guarantees) covers what a callback means for persisting the configuration and who owns the state it captures.

## Next steps

- [Experiments](/guide/execution/experiments) continues the Execution path with parameter grids, repetitions and concurrency.
- [Writing wrappers](/guide/extending/writing-wrappers) shows complete typed and general wrapper examples and their placement.
