# Analyzer architecture

An analyzer is an immutable configuration node that creates one typed result per run and names the substitutions that place its collector. Traces use `TraceAnalyzer<TObservation, TValue> : Analyzer<Trace<TValue>>`. Accumulating analyses derive directly from `Analyzer<TResult>` and create their graphs, fronts or histories per run.

Pass analyzers to `algorithm.CreateRun(problem, random, [analyzer])` and read their results with `run.Result(analyzer)`. The run substitutes the whole list in one call, validates the resulting graph and checks the supplied configuration definitions with non-empty expansions for placement. An absent definition fails before execution unless a declared delegate prevents proving it absent. The same definition serves independent runs. Results belong to the root scope and span every Cycle turn; a `Share` does not narrow an analyzer result.

## Observation boundaries

Algorithms and operators remain unaware of analysis. Each operation declares its observation record and its `Invocations` source in files beside the operation. `AlgorithmObservation`, the algorithm's `Iterations` source, `ObservationSource<TObservation>` and `Observing` live in `HEAL.HeuristicLib.Inspection`. `Observing` passes observations to a callback and publishes no result, so content such as a meta-algorithm tracking its contenders can use it.

A source's observed algorithm or operator, which it receives on construction and matches by reference, identifies what it observes. Every operation and every algorithm offers a source; an operator implementing several operations offers each through a view typed to that operation. Operator observations follow successful invocations. Algorithm observations follow each yielded state, after interceptors have transformed it. Each observation names the observed configuration.

Algorithm `Calls` observes the start and outcome of each executor call, including a call that yields no state. A Cycle turn is such a call of an inner algorithm. There is no separate observation-point mechanism.

Each operation's observation record keeps its domain names, such as `Parents` and `Offspring` in `MutationObservation<TCandidate>`. It reads the search space and problem at `ISearchSpace<TCandidate>` and `IProblem<TCandidate, ISearchSpace<TCandidate>>`, so it fits every run over the candidate. An algorithm observation is an `AlgorithmObservation<TCandidate, TSearchState>`, and an interceptor's observation reads the states at the search state its operation names. The observation wrapper therefore checks no run types; resolving an observed operator for a run it was not written for still fails, and the failure names that operator. Every operator and algorithm observation carries the search space and problem its executor was bound to, so a trace's function can rank by that problem's objective. A source of any other configuration may deliver any value; its trace reads it through a function like any other.

The trace machinery names no operation; population traces extend algorithm iteration sources, while `Analyzer.BestSoFar` selects evaluator boundaries. An external operation declares the same observation record and source and is observed and traced exactly like a built-in one.

An operation's observation record makes an executor report it, through `IInvocationObservation`. External operations construct boundary sources through `ObservationSource.Create`; source inheritance is limited to the library. The wrapper keeps no further state, and an algorithm's observation wrapper keeps no counter. Each executor numbers the iterations of its own call from 1, so `Iteration` restarts in each call, including every activation by a meta-algorithm, whatever its scope policy. Invocation-local previous states stay in the iterator.

A `Substitution` is one rewrite, created through its `Of` factories. `ISubstitution.Expand()` returns its single substitutions in application order, with the same instances on every call. `SubstitutionGroup` caches the depth-first expansion of its parts; parts may themselves be groups. Recursive containment is an error. Applying belongs to the call: `Substitute` expands all arguments together, applies a repeated single substitution once at its last position and matches every target against the graph it received. Later parts sit closer to their targets. The caller lists a clock's placement after every analyzer that reads it, and rank lists genealogy after its iteration recording, so shared parts update once and before what reads them.

Counters and durations are configuration nodes that place nothing. Their `Clock` properties create `CounterClock` and `DurationClock` records reading the root accumulators, even when `Share` narrows ordinary resolution. Two clocks of one counter or duration are equal. Create them with `new Counter()` or `new Duration()`. Operator sources return their placement substitutions through `Counting(counter)`, `CountingCandidates(counter)` and `Timing(duration)`. Algorithm iteration and call sources offer `Counting(counter)`, and iteration sources offer `Timing(duration)`. The explicit wrappers `Counted`, `CountedAlgorithm`, `Timed` and `TimedAlgorithm` state what is counted or measured. A counter or duration placed nowhere reads zero, including through its clock in a trace. Durations measure failed calls too. `Clock.Elapsed` reads the time `AlgorithmRun` spends advancing its retained iterator, including a call in progress or one that fails or is canceled. Executor construction before the first advance, pauses and consumer work are excluded. Deferred child construction during an advance, such as a fresh Cycle turn, is included. `RunResults` retains the elapsed value and stopwatch state for live reads. Direct scope execution leaves `scope.Results.Elapsed` and `Clock.Elapsed` at zero; use `algorithm.Timed(duration)` to measure that execution explicitly. A custom clock implements `IClock<TTime>` and writes only the typed `CreateExecutionNode(ResolutionScope)`; the boxed construction delegates to it. Typed reads such as `At` and `By` infer `TTime`. Mixed clock lists use `IReadOnlyList<IClock>`, through which `results.Result(clock)` infers its type; an `IClock<TTime>` needs an explicit result type because it has two configuration-node constructions. Each instance has one root-owned reading per run, whose `Read()` may be called concurrently and must preserve the observed computation. A clock only reads. A clock that needs placement also implements `ISubstitution`, and the caller lists it after the analyzers that read it. A clock observing calls derives from `SubstitutionGroup`, places an `Observed` wrapper for itself and holds a private `Counter`. Its recorder increments the run's counter accumulator, and its reading reads that root accumulator. `Clock.Elapsed` needs no placement. Equal clocks identify one trace axis and are deduplicated: repeated `counter.Clock` reads are equal, as are `new ElapsedClock()` and `Clock.Elapsed`. A class clock without an equality override, such as the epoch clock, matches by reference.

Observation reads inputs, outputs and progress while preserving the observed computation. An algorithm that adapts to measured work uses inspection data in its explicit control flow rather than reading an analyzer's result. Wrappers of your own may change behavior; the observation contract applies to inspection and analysis.

## Trace analyzers

`TraceAnalyzer<TObservation, TValue>` defines tracing over one or several sources of one observation type. Its per-run `Trace<TValue>` owns entries, retention progress, the value function and synchronization. Source extensions such as `evaluator.Invocations.Trace(value)` and multi-source `Analyzer.Trace(sources, value)` construct definitions. A stateful value uses `Trace(createState, value)`, which creates one class state per run. A repeated boundary is observed once by configuration reference and observation type; separate operations remain separate boundaries.

A general trace composes sources, a function, retention and explicit clocks. It records no clock it was not given. A ready-made analyzer may place its own counter: `Analyzer.BestSoFar<TCandidate>()` counts evaluated candidates and exposes `Evaluations` on every point. Extra clocks, including `Clock.Elapsed`, are supplied explicitly and read with `point.At(clock)`.

Trace functions are runtime delegates; they are neither comparable nor serializable values. Public trace construction never exposes an execution scope.

Retention is an immutable policy that creates a per-result decider. Best-so-far state is created by the trace's per-run state factory. Neither is an execution configuration, and neither is resolved as a child through a scope.

Definitions may be reused without coupling their results. A caller's delegate may deliberately capture shared mutable state under the documented developer-guidelines § 4.8 exception; that state remains the caller's responsibility.

A best-so-far object keeps the running best across observations: `BestSoFar` the best objective vector, and `BestSoFar<T>` any other value, ranked by the objective vector its selector reads. The first directions it receives fix it; other directions or another dimensionality throw. There is no aggregation role: summaries and accumulation are functions of the observation, and accumulation keeps its state in an object the function captures.

A per-result `RetentionDecider<TValue>.Decide` returns `Append`, `ReplaceLatest` or `Skip`. Retention runs after the value function and never skips its computation; an unknown decision fails at collection.

## Accumulating analyzers

A trace keeps a history of immutable values. A mutable summary over one observation type uses `source.Aggregate(create, update)` or `Analyzer.Aggregate(sources, create, update)`. Each run owns an `Aggregate<TState>` whose `Value` is the plain state created by the factory. The result serializes updates, so the update function needs no synchronization. Reading live state during execution is the state type's concern. These analyzers hold delegates, making configurations that hold them nonserializable.

<<< ../../../test/HeuristicLib.Tests.Docs/Execution/ObservabilityDocumentationSpecs.cs#observability-accumulating-analyzer{csharp}

An `Observed` or `ObservedAlgorithm` wrapper holds its `Receiver`, an `IObservationReceiver<TObservation>`, and binds the recorder during construction. An analyzer implements this interface for every observation type it accepts; other placed nodes, such as clocks that observe, implement it too. A callback-only receiver may publish no result.

An analyzer over several observation types derives from `Analyzer<TResult>` and implements `IObservationReceiver<T>` for each type. Its `CreateExecutionNode(ResolutionScope)` creates an empty result implementing `IRunResult`, and each `CreateRecorder(RunResults)` looks it up with `results.Result(this)`. Experimental's `GenealogyAnalyzer` combines crossover, mutation and algorithm iteration observations to build a `GenealogyGraph`. Rank composes that analyzer, placing genealogy after its own iteration hook so the generation closes before ranks are read. Synchronization belongs to the run-created result or collector. Read a live front, genealogy graph, rank state or performance summary after execution has stopped; copying it on each read is not part of the contract.

Neither retention nor clocks apply to it. There is nothing to store apart from what was computed, and one current state has no moment of its own.

## Objective comparison and publication

Observations carry the concrete problem used by the operation. Ranking uses that problem's objective unless the trace was given an `IComparer<ObjectiveVector>`. Comparers and `RequireTotalOrder` belong to the objective system. No implicit lexicographic fallback is supplied.

An observation carries the problem because each observed executor is bound to it during construction. It does not carry the objective or a comparer: what the run optimizes is not part of what happened at the boundary, and a trace handed an objective when it is composed could rank by one the run does not use.

Each trace result serializes value computation, retention and publication. The same definition can serve concurrent runs with independent state. A result collecting several concurrent boundaries must synchronize its updates.

The result lock protects concurrent collection and live reads. Trace storage is an ordinary list, copied when a snapshot or projection is requested. Clock readings are obtained once per result in a fixed column layout shared by every entry; each retained entry stores one row of boxed times.

Entries contain immutable values. A trace result's `Latest` and `SampleCount` do not allocate; `Snapshot()` and `By(clock)` return stable copies. Stopping, canceling or failing a run leaves its results available.

Analyzers, results and resolution scopes have no automatic disposal contract. Deferred execution can retain its scope and result table. Dependencies requiring cleanup need an explicit lifecycle owner.

## Naming and placement

An analysis definition is named `...Analyzer`; its result is a trace, summary or domain accumulator, never another analyzer. Definitions are immutable identity-bearing classes; result objects own collected data.

Ready-made analyzers are factories on `Analyzer`. Population traces remain source extensions. Keep the definition as the key for `run.Result(definition)`.

The main package ships general-purpose traces. Specialized analysis belongs beside the concept it measures, following the package placement rules in § 9.6 of the [developer guidelines](/contributing/developer-guidelines).

## Run lifecycle

`AlgorithmRun`, `ExecutionStream` and `RunLifecycleState` belong to the `HEAL.HeuristicLib.Execution` namespace, with their source files grouped under `Execution/Runs`. Experiment runs remain in `HEAL.HeuristicLib.Experiments` and share `RunLifecycleState`.

An algorithm run is returned in `Ready`, resolves its executor graph on first start and retains that graph and its iterator across pauses. Completion or failure is terminal; results remain readable. See [running algorithms](/guide/execution/running-algorithms) for the lifecycle contract.

Each returned execution stream has one consumer, and a run permits only one active stream. Stream cancellation is cooperative at root-algorithm yield boundaries. It pauses the run instead of interrupting a partially executed iteration.

See [Observability and analysis](/guide/execution/observability-and-analysis) for user examples.

## Placement, budgets and persistence

`CreateRun` places analyzers after budgets into the configuration it receives. A budget holds a copy of its algorithm, so name an iteration target from that configuration, such as `limited.Algorithm.Iterations`. A source from the original algorithm no longer matches. Ready-made default selections need no named target.

A substitution is a rewrite applied while building a configuration. It is never stored or serialized. Persist the configuration it produced, an ordinary graph containing the placed nodes. `ga.Substitute(best)` keeps analysis in that graph; `ga.CreateRun(problem, random, [best])` places it only for the run. What is placed must be data for persistence: delegates in traces, aggregates and callbacks prevent it. A substitution cannot be undone; persist an uninstrumented configuration before substituting if it is also needed.

## Best so far

`Analyzer.BestSoFar<TCandidate>()` selects evaluators of that candidate type with no such evaluator anywhere among their declared descendants. It walks declared children only, never settings or delegates. With `Cached(E)` it observes `E` and counts actual evaluations, including evaluators within refiners. Name an evaluator to override the default, for example for a composite evaluator or an inner problem whose objective should not join the curve. The group places observations, then its own candidate counter. It places no clocks; the caller lists a clock's placement after every analyzer that reads it. In one list a later part sits closer to its target, so this reading includes the call; placed before them, it reads before the call. Each point includes its own batch. Retention compares the running best candidate, recording improvements only by default. Points expose `Evaluations`, `Value`, `Candidate` and `At(clock)`. The analyzer's `Evaluations` counter is public, so an explicit placement counts inside the observer with `evaluator.Counted(best.Evaluations, OperatorCountMetric.Candidates).Observed(best)`, and a terminator may read the same count. The curve enumerates a snapshot, as does every general trace.
