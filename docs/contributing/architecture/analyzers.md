# Analyzer architecture

An analyzer is an immutable configuration node that creates one typed result per run and names the substitutions that place its collector. Traces use `TraceAnalyzer<TObservation, TValue> : Analyzer<Trace<TValue>>`. Accumulating analyses derive directly from `Analyzer<TResult>` and create their graphs, fronts or histories per run. [Observability and analysis](/guide/execution/observability-and-analysis) shows users how to pass analyzers to `CreateRun` and read their results.

`CreateRun` applies the whole substitution list in one call and validates the resulting graph unless the caller passes `validate: false`. It then checks the placement of each supplied configuration node whose expansion is not empty, such as an analyzer or a clock that observes. `AlgorithmRun.RequireHeld` fails a node unless a declared parameter reachable from the root holds it, or a reachable declared delegate could have placed it unseen. The placement check does not depend on graph validation, and `AlgorithmRun.Result` repeats it when a result is read. The same analyzer serves independent runs, including concurrent ones, each with independent state. Results belong to the root scope and span every Cycle turn, and a `Share` does not narrow an analyzer result ([Shared configurations](/contributing/architecture/execution-resolution#shared-configurations) states how recording and lookup treat it).

## Observation boundaries

Algorithms and operators remain unaware of analysis. Each operation declares its observation record and its `Invocations` source in files beside the operation. `AlgorithmObservation`, the algorithm's `Iterations` source, `ObservationSource<TObservation>` and `Observing` live in `HEAL.HeuristicLib.Inspection`. `Observing` passes observations to a callback and publishes no result, so content such as a meta-algorithm tracking its contenders can use it.

A source's observed algorithm or operator, which it receives on construction and matches by reference, identifies what it observes. Every operation and every algorithm offers a source; an operator implementing several operations offers each through a view typed to that operation. Operator observations follow successful invocations. Algorithm observations follow each yielded state, after interceptors have transformed it. Each observation names the observed configuration. Sources create the same observation wrapper for explicit `Observed` placement and for an analyzer's automatic placement.

Algorithm `Calls` observes the start and outcome of each executor call, including a call that yields no state. A Cycle turn is such a call of an inner algorithm, so that algorithm's `Calls` observe the turn. There is no separate observation-point mechanism.

Each operation's observation record keeps its domain names, such as `Parents` and `Offspring` in `MutationObservation<TCandidate>`. Observing executors snapshot each batch as a `ValueArray<T>`. The record reads the search space and problem at `ISearchSpace<TCandidate>` and `IProblem<TCandidate, ISearchSpace<TCandidate>>`, so it fits every run over the candidate. An algorithm observation is an `AlgorithmObservation<TCandidate, TSearchState>`, and an interceptor's observation reads the states at the search state its operation names. The observation wrapper therefore checks no run types; resolving an observed operator for a run it was not written for still fails, and the failure names that operator. Every operator and algorithm observation carries the search space and problem its executor was bound to, so a trace's function can rank by that problem's objective. A source of any other configuration may deliver any value; its trace reads it through a function like any other.

The trace machinery names no operation; population traces extend algorithm iteration sources, while `Analyzer.BestSoFar` selects evaluator boundaries. An external operation declares the same observation record and source and is observed and traced exactly like a built-in one.

An operation's observation record makes an executor report it through `IInvocationObservation.Wrap<TSearchState>`. An algorithm observation uses `IAlgorithmObservation.Wrap`, whose interface already names the search-state type. Both hooks receive the bound problem as `IProblem<TCandidate, ISearchSpace<TCandidate>>`. External operations construct boundary sources through `ObservationSource.Create`; source inheritance is limited to the library. The wrapper keeps no further state, and an algorithm's observation wrapper keeps no counter. Each executor numbers the iterations of its own call from 1, so `Iteration` restarts in each call, including every activation by a meta-algorithm, whether its executor is fresh or reused. Invocation-local previous states stay in the iterator.

Observation reads inputs, outputs and progress while preserving the observed computation. An algorithm that adapts to measured work uses inspection data in its explicit control flow rather than reading an analyzer's result. Wrappers of your own may change behavior; the observation contract applies to inspection and analysis.

## Clocks

Counters and durations are configuration nodes that place nothing. Their `Clock` properties create `CounterClock` and `DurationClock` records that read the root accumulators, even when `Share` narrows ordinary resolution. The placement substitutions are `Counting` on operator, iteration and call sources, `CountingCandidates` on operator sources and `Timing` on operator and iteration sources; the explicit wrappers `Counted`, `CountedAlgorithm`, `Timed` and `TimedAlgorithm` state what is counted or measured. The caller lists a clock's placement after every analyzer that reads it, and rank lists genealogy after its iteration recording, so shared parts update once and before what reads them. [Choose a boundary and a clock](/guide/execution/observability-and-analysis#choose-a-boundary-and-a-clock) shows users how to select, place and read clocks.

`Clock.Elapsed` needs no placement. `AlgorithmRun` measures each call that advances its retained iterator in `try`/`finally`, so a call that fails or is canceled counts too, and `RunResults` retains the elapsed value and stopwatch state for live reads. Resolution measures no time, so direct scope execution leaves `scope.Results.Elapsed` and `Clock.Elapsed` at zero.

A custom clock implements `IClock<TTime>` and writes only the typed `CreateExecutionNode(ResolutionScope)`; the boxed construction delegates to it. Because `IClock<TTime>` has two configuration-node constructions, `results.Result` needs an explicit result type through it. Each instance has one root-owned reading per run, whose `Read()` may be called concurrently and must preserve the observed computation. A clock only reads. A clock that needs placement also implements `ISubstitution`, and the caller lists it after the analyzers that read it. A clock observing calls derives from `SubstitutionGroup`, places an `Observed` wrapper for itself and holds a private `Counter`. Its recorder increments the run's counter accumulator, and its reading reads that root accumulator.

Equal clocks identify one trace axis and are deduplicated: two clocks of one counter or duration are equal, repeated `counter.Clock` reads are equal, as are `new ElapsedClock()` and `Clock.Elapsed`. A class clock without an equality override, such as the epoch clock, matches by reference.

## Trace analyzers

`TraceAnalyzer<TObservation, TValue>` defines tracing over one or several sources of one observation type. Its per-run `Trace<TValue>` owns entries, retention progress, the value function and synchronization. Source extensions such as `evaluator.Invocations.Trace(value)` and multi-source `Analyzer.Trace(sources, value)` construct analyzers. A stateful value uses `Trace(createState, value)`, which creates one class state per run. A repeated boundary is observed once by configuration reference and observation type; separate operations remain separate boundaries.

A general trace composes sources, a function, retention and explicit clocks. It records no clock it was not given.

Trace functions are runtime delegates; they are neither comparable nor serializable values. A caller's delegate may deliberately capture shared mutable state, as developer guidelines § 4.8 allows; that state remains the caller's responsibility. Public trace construction never exposes an execution scope.

Retention is an immutable policy that creates a per-result decider. Best-so-far state is created by the trace's per-run state factory. Neither is an execution configuration, and neither is resolved as a child through a scope.

A best-so-far object keeps the running best across observations: `BestSoFar` the best objective vector, and `BestSoFar<T>` any other value, ranked by the objective vector its selector reads. The first directions it receives fix it; other directions or another dimensionality throw. Accumulation is an analyzer concern, not an operator role: summaries and accumulation are functions of the observation, and accumulation keeps its state in an object the function captures or in an `Aggregate` ([Accumulating analyzers](#accumulating-analyzers)).

A per-result `RetentionDecider<TValue>.Decide` returns `Append`, `ReplaceLatest` or `Skip`. Retention runs after the value function and never skips its computation; an unknown decision fails at collection.

## Accumulating analyzers

A trace keeps a history of immutable values. A mutable summary over one observation type uses `source.Aggregate(create, update)` or `Analyzer.Aggregate(sources, create, update)`. Each run owns an `Aggregate<TState>` whose `Value` is the plain state created by the factory. The result serializes updates, so the update function needs no synchronization. Reading live state during execution is the state type's concern. Retention and clocks do not apply to it: one current state has no moment of its own. These analyzers hold delegates, making configurations that hold them nonserializable. [Accumulating analyzers](/guide/execution/observability-and-analysis#accumulating-analyzers) shows an example.

An `Observed` or `ObservedAlgorithm` wrapper holds its `Receiver`, an `IObservationReceiver<TObservation>`, and binds the recorder during construction. An analyzer implements this interface for every observation type it accepts; other placed nodes, such as clocks that observe, implement it too. A callback-only receiver may publish no result.

An analyzer over several observation types derives from `Analyzer<TResult>` and implements `IObservationReceiver<T>` for each type. Its `CreateExecutionNode(ResolutionScope)` creates an empty result implementing `IRunResult`, and each `CreateRecorder(RunResults)` looks it up with `results.Result(this)`. Experimental's `GenealogyAnalyzer` combines crossover, mutation and algorithm iteration observations to build a `GenealogyGraph`. Rank composes that analyzer, placing genealogy after its own iteration hook so the generation closes before ranks are read. Synchronization belongs to the run-created result or collector. Read a live front, genealogy graph, rank state or performance summary after execution has stopped; copying it on each read is not part of the contract.

## Objective comparison and publication

Ranking uses the observed problem's objective unless the trace was given an `IComparer<ObjectiveVector>`. Comparers and `RequireTotalOrder` belong to the objective system. No implicit lexicographic fallback is supplied.

An observation carries the problem because each observed executor is bound to it during construction. It does not carry the objective or a comparer: what the run optimizes is not part of what happened at the boundary, and a trace handed an objective when it is composed could rank by one the run does not use.

Each trace result serializes value computation, retention and publication. A result collecting several concurrent boundaries must synchronize its updates.

The result lock protects concurrent collection and live reads. Trace storage is an ordinary list, copied when a snapshot or projection is requested. Clock readings are obtained once per result in a fixed column layout shared by every entry; each retained entry stores one row of boxed times.

Entries contain immutable values. Stopping, canceling or failing a run leaves its results available.

Analyzers, results and resolution scopes have no automatic disposal contract. Deferred execution can retain its scope and result table. Dependencies requiring cleanup need an explicit lifecycle owner.

## Naming and placement

An analyzer is named `...Analyzer`; its result is a trace, summary or domain accumulator, never another analyzer. Analyzers are immutable identity-bearing classes; result objects own collected data.

Ready-made analyzers are factories on `Analyzer`. Population traces remain source extensions. Keep the analyzer as the key for `run.Result(analyzer)`.

The main package ships general-purpose traces. Specialized analysis belongs beside the concept it measures, following the package placement rules in § 9.6 of the [developer guidelines](/contributing/developer-guidelines).

## Placement, budgets and persistence

`CreateRun` places analyzers after budgets into the configuration it receives. A budget holds a copy of its algorithm ([Inspection and budgets](/contributing/architecture/execution-resolution#inspection-and-budgets)), so a source matches only what that configuration holds, and [Configurations](/guide/fundamentals/configurations#substituting-for-one-run) states how users name the target. Ready-made default selections need no named target.

An experiment trial does the same. It applies the substitutions once per parameter candidate, validates the result for each problem and parameter candidate unless validation is off, and runs the placement check for each trial. The trial retains the decoded configuration, and its algorithm run uses the resulting graph and owns the results. Reference-based sources must match nodes in the decoded graph. See [Experiments](/guide/execution/experiments#add-analysis).

A substitution is never stored or serialized; what persists is the configuration it produced, an ordinary graph containing the placed nodes. What is placed must be data for persistence: delegates in traces, aggregates and callbacks prevent it (developer guidelines § 4.8). A substitution cannot be undone, so persist the configuration without the placed nodes before substituting if it is also needed. [Configurations](/guide/fundamentals/configurations#what-substitute-guarantees) states what this means for users.

## Best so far

`Analyzer.BestSoFar<TCandidate>()` selects evaluators of that candidate type with no such evaluator anywhere among their declared descendants. It walks declared children only, never settings or delegates. With `Cached(E)` it observes `E` and counts actual evaluations, including evaluators within refiners. The group places observations, then its own candidate counter, and places no clocks. In one list a later part sits closer to its target, so a clock listed after the group includes the call; placed before it, a clock reads before the call. Each point includes its own batch and exposes `Evaluations`. Retention compares the running best candidate, recording improvements only by default. The analyzer's `Evaluations` counter is public, so an explicit placement can count inside the observer and a terminator can read the same count. The curve enumerates a snapshot, as does every general trace. [Best-so-far selection and placement](/guide/execution/observability-and-analysis#best-so-far-selection-and-placement) shows users how to name an evaluator, place the count explicitly and read the points.

## Next

[Observability and analysis](/guide/execution/observability-and-analysis) gives user examples. [Layering](/contributing/architecture/layering#run-ownership) describes the run lifecycle that analyzers read from.
