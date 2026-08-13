# Execution model

This page explains how algorithm execution works in HeuristicLib.

## The user-facing model

At the user level, the important idea is simple:

- an algorithm configuration is reusable
- a run executes that configuration on a problem
- execution produces a stream of search states

The public streaming shape is:

```csharp
ExecutionStream<TSearchState> Stream(TProblem problem, IRandomNumberGenerator random, TSearchState? initialState = null, CancellationToken ct = default);
```

`Stream(...)`, `Complete(...)` and `CompleteAsync(...)` each create and execute a fresh `AlgorithmRun`. Create the run explicitly when attaching analyzers or when the run object itself is needed.

Run-level `CancellationToken` parameters are for immediate execution interruption. Algorithms and operators may check that token before or during a step, so cancellation can stop the current iteration before it produces another state.

## Execution concurrency

`ExecutionConcurrency` is the general scheduling input for operations that may execute several independent items:

```csharp
ExecutionConcurrency.Sequential()
ExecutionConcurrency.Concurrent()
ExecutionConcurrency.Concurrent(4)
```

Sequential and concurrent execution are distinct categories. `Sequential()` requires one operation to finish before the next input starts and preserves input order. `Concurrent()` permits all inputs to overlap. `Concurrent(maximumConcurrency)` limits the number of active operations but does not promise sequential ordering. `Concurrent(1)` therefore remains categorically concurrent.

Maximum concurrency describes active operations, not worker objects or dedicated threads. A scheduler may implement that bound with workers, tasks, asynchronous operations or synchronous parallel execution as appropriate. The current experiment scheduler uses it to bound active algorithm runs.

`BatchExecution.Execute(...)` uses the same input for synchronous batches. `BatchExecution.Sequential(...)` and `BatchExecution.Parallel(...)` are convenience forms for fixed scheduling choices. All forms preserve output order and fork the supplied random number generator by input index. Concurrent execution follows normal TPL exception behavior, including aggregating callback failures.

`SingleCandidateEvaluator` and `SingleSolutionProblem` default to sequential execution and expose `Concurrency` for explicitly enabling concurrent batch processing. When concurrent execution is selected, the single-candidate `EvaluateCandidate(...)` method may be called concurrently on the same execution instance. Implementations and their dependencies must support that use. `RepeatingEvaluator` likewise defaults to sequential execution because each repetition uses the same resolved child evaluator instance. Its `Repetitions` setting is the total evaluation count per candidate, and its objective-vector aggregation strategy defaults to component-wise arithmetic mean.

## The main authoring model

Ordinary iterative algorithm configurations derive from `IterativeAlgorithm<TSelf, ...>`. `TSelf` is the concrete configuration type and supports type inference in fluent composition. Their execution instances derive from `IterativeAlgorithmInstance<...>`.

The configuration creates the instance and eagerly resolves its children:

```csharp
protected override IterativeAlgorithmInstance<TCandidate, TSearchSpace, TProblem, TSearchState>
    CreateIterativeAlgorithmInstance(ExecutionInstanceRegistry registry, IInterceptorInstance<TCandidate, TSearchSpace, TProblem, TSearchState>? resolvedInterceptor);
```

`resolvedInterceptor` is supplied by the base because the base declares and owns interceptor participation. The concrete instance creation method resolves its own dependencies through `registry`.

The execution instance owns step behavior:

```csharp
protected override TSearchState ExecuteStep(TSearchState? previousState, TProblem problem, IRandomNumberGenerator random)
```

The intended pattern is:

- keep settings and child operator configurations on the reusable algorithm configuration
- resolve child execution instances once in `CreateIterativeAlgorithmInstance(...)`
- store resolved children and mutable execution data on the algorithm execution instance
- keep configuration objects unchanged during execution

Algorithms that can exhaust their own structure while trying to produce the next state can override `TryExecuteStep(...)` instead. Returning `false` means the algorithm has structurally completed and the stream ends without yielding another state.

## Iterative loop semantics

For iterative algorithms, the default loop is:

1. start from `previousState = initialState`
2. enumerate yielded-state counts `0, 1, 2, ...`
3. for each yielded-state count:
   - stop if the algorithm has internally completed
   - check cancellation
   - fork the RNG using the yielded-state count
   - compute the next state with `TryExecuteStep(...)` / `ExecuteStep(...)`
   - stop if no next state can be produced
   - optionally transform it with the configured interceptor
   - determine whether this produced public state should stop future production
   - yield the produced state
   - stop before producing another state if the yielded state was terminal
   - continue from that state

Those yielded-state counts are internal to that specific iterative loop. They are useful for execution concerns such as deterministic RNG forking and internal budgets, but they are not part of the public search-state contract and do not define a cross-algorithm notion of iteration, generation, or step for nested or meta-algorithm execution.

So the model supports both:

- fresh runs, where `initialState == null`
- resumed runs, where an existing state is passed back in

A supplied `initialState` is resume input from outside the current execution; it is not a newly produced state. The default execution model therefore does not yield that state or treat it as something state-based external terminators have observed. External terminators such as `StateTerminatedAlgorithm` check only produced public states, after the state has been yielded, and a matching state stops future consumption rather than removing the state that triggered the stop.

State-based checks operate on the public produced state. If an interceptor transforms a state before it is yielded, state-based termination checks use the intercepted state, not the raw pre-interceptor state.

`CancellationTokenTerminator(...)` is different from passing a token to `Run...(...)`. It models graceful external early stopping: the wrapped algorithm finishes producing the current state, that state is yielded, and the terminator then stops future consumption if its token has been canceled. Use the run parameter token when the current iteration should be interrupted immediately; use the terminator form when a UI, service, or caller wants to stop after the current produced state.

`AfterElapsedTimeTerminator(...)` follows the same graceful external early-stopping model for elapsed wall-clock budgets. It measures elapsed time from terminator execution-instance creation with a `TimeProvider`, yields the state that crosses the elapsed-time budget, and then stops future consumption. `StateTerminatedAlgorithm` resolves its terminator before resolving the wrapped algorithm so elapsed-time measurement starts at the earliest point that wrapper controls, including wrapped algorithm instancing. If a caller needs an exact call-site start time instead, it can create a timeout `CancellationTokenSource` immediately before calling `Run...(...)` and pass that token through `CancellationTokenTerminator(...)`.

Algorithm duration is active state-production time. `WithMaxAlgorithmDuration(...)` measures only the time spent asking the wrapped algorithm to produce the next state. It excludes caller idle time after a state has been yielded and before the caller asks for another one, but it includes the algorithm work, operator work, wrapper work, and asynchronous waits involved in producing that next state. Like other external duration budgets, the state that crosses the duration budget is yielded before future consumption stops.

Operator duration is a different time budget unit. Helpers such as `WithMaxEvaluatorDuration(evaluator, ...)`, `WithMaxMutatorDuration(...)` and `WithMaxCrossoverDuration(...)` measure only active work inside observed operator calls, using a before call, inner call and after call measurement. If a caller pauses between pulling states from a stream, whole run elapsed time continues to pass, but operator duration does not increase while the observed operator is not running. Duration is recorded even if the observed operator call throws because the failed call still consumed observed work time. Like other external operator budgets, the operator call that crosses the duration budget finishes, its produced state is yielded and only future stream consumption stops.

Some algorithms also expose internal budget properties and state-based internal terminators. For example, `MaximumGenerations` on evolutionary algorithms such as `GeneticAlgorithm`, `EvolutionStrategy`, `NSGA2`, `AlpsGeneticAlgorithm`, and `OpenEndedRelevantAllelesPreservingGeneticAlgorithm` is part of the algorithm's own execution budget and counts produced generation states from the current execution. A resumed run does not count the supplied `initialState` toward that budget. If an algorithm has a custom internal `Terminator`, it is checked only against states yielded by the current execution, not against a supplied `initialState`. This differs from `WithMaxIterations(...)`, which wraps an algorithm with external early stopping over the yielded stream.

In this documentation, **completed** means the algorithm has internally finished producing states, either because an algorithm-owned budget or completion rule fired or because the algorithm has structurally no next state to produce. External early stopping is different: it stops consumption of a stream from the outside, but does not redefine whether the wrapped algorithm itself completed.

Algorithm execution continues only while both the algorithm has not internally completed and the external consumer still wants another state. Internal completion is authoritative: external early stopping can shorten a run, but it cannot extend a run past internal completion. If a caller wants more states after completion, it must configure the algorithm differently or model that explicitly as a new run, restart, cycle, or other meta-algorithm behavior.

Continuation is only meaningful for an execution instance that was early-stopped and whose API explicitly preserves that execution instance. A completed execution instance is exhausted. Calling a `Run...` method again should not be assumed to continue a previous execution instance unless the API says so directly.

For many simple runs, the observable state sequence can be the same either way. This is similar to LINQ: a source that naturally contains `n` items and a longer source consumed through `Take(n)` may produce the same items to the caller. The distinction matters when ownership and composition matter: whether the algorithm configuration carries its own budget, whether the same configured algorithm can be reused without that limit, whether a wrapper is only adapting stream consumption, and whether future completion metadata should describe the inner algorithm as completed or merely externally stopped.

HeuristicLib aims for deterministic execution when the algorithm configuration, problem, initial state, and random seed are the same. That expectation only holds while the configuration does not depend on outside mutable state. Terminators or operators that observe external state, such as a live `CancellationToken` or a future wall-clock budget, can make otherwise identical executions produce different streams. For example, reusing a `CancellationTokenTerminator(...)` after its token has already been canceled will stop the next execution immediately after its first produced state.

For example, a local search that evaluates its configured neighborhood and finds no improving move has structurally completed. It should exhaust the stream instead of yielding another copy of the previous state.

## Budget units

Budget names should say what they count.

- `MaximumGenerations` counts produced generation states owned by a generation-producing evolutionary algorithm.
- `MaximumCycles` counts attempted cycles owned by a cycle algorithm, including cycles that yield no state.
- `WithMaxIterations(...)` is an external early-stopping wrapper. It counts yielded stream states from the wrapped algorithm or composition, regardless of whether those states are generations, local-search moves, pipeline outputs, or cycle outputs.
- `WithMaxEvaluatorCalls(evaluator, ...)` is an external early stopping wrapper over observed `Evaluate(...)` calls. It installs a counted evaluator replacement for the run and stops future stream consumption after the configured call count has been observed. It does not make the wrapped algorithm internally complete.
- `WithMaxEvaluatedCandidates(evaluator, ...)` is an external early stopping wrapper over candidates processed inside observed evaluator batches. If one batch crosses the configured candidate count, the produced state for that batch is still yielded and future stream consumption stops afterward.
- `WithMaxAlgorithmDuration(...)` is an external early-stopping wrapper over active state-production duration. It measures time spent pulling produced states from the wrapped algorithm and excludes caller idle time between pulls.
- `WithMaxEvaluatorDuration(evaluator, ...)` is an external early stopping wrapper over measured evaluator work duration. It installs a measured evaluator replacement for the run and stops future stream consumption after the configured cumulative evaluator duration has been observed.
- Typed operator-budget helpers such as `WithMaxMutatorCalls(...)`, `WithMaxMutatedCandidates(...)`, `WithMaxSelectedCandidates(...)`, `WithMaxReplacementCandidates(...)`, and `WithMaxSelectorDuration(...)` observe an explicitly supplied operator and install the matching counted or measured replacement for that run.
- `WithMaxOperatorDuration(...)` is the general external operator-duration wrapper. It observes an explicitly supplied operator and a measured replacement factory, so users can apply duration budgets to custom wrappers or unusual operator boundaries.
- `WithMaxCount(...)` is the general external operator-budget wrapper. It observes an explicitly supplied operator and a counted replacement factory, so users can count custom units or operator boundaries that do not fit a typed helper.
- Evaluation-count budgets must identify the observed boundary and unit. Counting `Evaluate(...)` calls, evaluated candidates inside those calls, requests to a caching evaluator, and calls that reach the wrapped direct evaluator are different budgets.
- Terminator call and duration instrumentation exists for consistency because terminators are operators too. Treat `WithMaxTerminatorCalls(...)` and `WithMaxTerminatorDuration(...)` as advanced or niche tools for observing expensive or shared terminator checks, not as the normal way to cap a run.

Time budgets must identify whether they mean elapsed wall-clock time for a broader execution or measured duration of active work at an observed boundary.

Avoid treating "iteration" as a universal synonym for generation, step, cycle, evaluation, or yielded state. It is only the right public word when an algorithm explicitly defines iteration as its own progress unit.

## Runs and analyzers

`CreateRun(problem, random)` creates one logical algorithm execution.

An `AlgorithmRun` is a single execution object. It can start only one execution through its streaming and completion entry points. Create a new run when executing the same algorithm configuration again. The convenience methods on an algorithm configuration create a new run for every call.

This restriction applies to the public `AlgorithmRun` lifecycle, not to every direct algorithm instance invocation. A meta algorithm may deliberately invoke the same child algorithm instance again when its documented lifecycle policy calls for retained instance data. Concurrent execution through one algorithm instance remains unsupported unless that instance explicitly documents otherwise.

An algorithm run owns:

- the algorithm configuration
- the problem
- the root execution-instance registry
- analyzer state for that run

Analyzers are attached to the run, not to the algorithm configuration.

## Internal infrastructure

Internally, configurations are resolved into execution instances through `ExecutionInstanceRegistry`.

Explicit operator and algorithm instance creation methods receive the registry. Ordinary creation methods normally use it only to resolve their declared children eagerly. Execution graph compositions may also create child registries, register replacements or control execution instance reuse.

This still matters for:

- run-local operator state
- shared execution graph resolution
- meta-algorithms that need control over instance reuse vs reset

It is intentionally an execution concern. Normal algorithm execution resolves child instances eagerly and keeps them on the authored algorithm execution instance.

## Meta-algorithms

Meta algorithms such as `PipelineAlgorithm`, `CycleAlgorithm` and `StateTerminatedAlgorithm` use the same configuration and execution instance model. Their instance creation methods use the registry directly because they orchestrate child algorithms, control child registry lifetime or install replacements. Only execution instances that create child algorithms at runtime retain their originating registry. The algorithm instance base classes do not retain a run or registry.

Pipeline and cycle configurations require at least one child algorithm. An empty pipeline cannot produce a meaningful forwarded state, while an unlimited empty cycle cannot make execution progress.

Pipeline execution checks cancellation before starting each stage. Cycle execution checks cancellation at cycle and child boundaries. `MaximumCycles` must be positive when supplied. A cycle without a yielded state still counts toward that limit and yields execution control before the next attempt. An unlimited cycle keeps trying until cancellation when its children continue to produce no states.

## Related pages

- [Algorithm](algorithm.md)
- [Operator authoring](operator-authoring.md)
- [Configuration vs execution instances](execution-instances.md)
- [Observability & analysis](observability-and-analysis.md)
