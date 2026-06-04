# Execution model

This page explains how algorithm execution works in HeuristicLib.

## The user-facing model

At the user level, the important idea is simple:

- an algorithm definition is a reusable configured object
- a run executes that definition on a problem
- execution produces a stream of search states

The core streaming shape is:

```csharp
IAsyncEnumerable<TSearchState> RunStreamingAsync(
  TProblem problem,
  IRandomNumberGenerator random,
  TSearchState? initialState = null,
  CancellationToken ct = default);
```

Convenience methods such as `RunToCompletion(...)` are just ways of consuming that stream.

Run-level `CancellationToken` parameters are for immediate execution interruption. Algorithms and operators may check that token before or during a step, so cancellation can stop the current iteration before it produces another state.

## The main authoring model

Ordinary iterative algorithms should be authored through `IterativeAlgorithm<...>`.

That base provides a step-based model with explicit execution-state creation:

```csharp
protected override TExecutionState CreateInitialExecutionState(IExecutionInstanceResolver resolver);

protected override TSearchState ExecuteStep(
  TSearchState? previousState,
  TExecutionState executionState,
  TProblem problem,
  IRandomNumberGenerator random)
```

The intended pattern is:

- resolve operator dependencies once in `CreateInitialExecutionState(...)`
- store those resolved execution instances in `TExecutionState`
- reuse them in every `ExecuteStep(...)` call

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
   - decide whether the produced public state is terminal
   - yield the produced state
   - stop before producing another state if the yielded state was terminal
   - continue from that state

Those yielded-state counts are internal to that specific iterative loop. They are useful for execution concerns such as deterministic RNG forking and internal budgets, but they are not part of the public search-state contract and do not define a cross-algorithm notion of iteration, generation, or step for nested or meta-algorithm execution.

So the model supports both:

- fresh runs, where `initialState == null`
- resumed runs, where an existing state is passed back in

A supplied `initialState` is resume input from outside the current execution; it is not a newly produced state. The default execution model therefore does not yield that state or treat it as something state-based external terminators have observed. External terminators such as `StateTerminatedAlgorithm` check only produced public states, after the state has been yielded, and a matching state stops future consumption rather than removing the state that triggered the stop.

`CancellationTokenTerminator(...)` is different from passing a token to `Run...(...)`. It models graceful external early stopping: the wrapped algorithm finishes producing the current state, that state is yielded, and the terminator then stops future consumption if its token has been canceled. Use the run parameter token when the current iteration should be interrupted immediately; use the terminator form when a UI, service, or caller wants to stop after the current produced state.

`AfterElapsedTimeTerminator(...)` follows the same graceful external early-stopping model for elapsed runtime budgets. It measures elapsed time from terminator execution-instance creation with a `TimeProvider`, yields the state that crosses the elapsed-time budget, and then stops future consumption. `StateTerminatedAlgorithm` resolves its terminator before resolving the wrapped algorithm so elapsed-time measurement starts at the earliest point that wrapper controls, including wrapped algorithm instancing. If a caller needs an exact call-site start time instead, it can create a timeout `CancellationTokenSource` immediately before calling `Run...(...)` and pass that token through `CancellationTokenTerminator(...)`.

Operator duration is a different time budget unit. `WithMaxEvaluatorDuration(...)` measures only active work inside observed evaluator calls, using a `before call -> inner call -> after call` measurement. If a caller pauses between pulling states from a stream, whole-run elapsed time continues to pass, but evaluator duration does not increase while the evaluator is not running. Duration is recorded even if the observed evaluator call throws, because the failed call still consumed evaluator work time. Like other external operator budgets, the evaluator call that crosses the duration budget finishes, its produced state is yielded, and only future stream consumption stops.

Some algorithms also expose internal budget properties and state-based internal terminators. For example, `MaximumGenerations` on evolutionary algorithms such as `GeneticAlgorithm`, `EvolutionStrategy`, `NSGA2`, `AlpsGeneticAlgorithm`, and `OpenEndedRelevantAllelesPreservingGeneticAlgorithm` is part of the algorithm's own execution budget and counts produced generation states from the current execution. A resumed run does not count the supplied `initialState` toward that budget. If an algorithm has a custom internal `Terminator`, it is checked only against states yielded by the current execution, not against a supplied `initialState`. This differs from `WithMaxIterations(...)`, which wraps an algorithm with external early stopping over the yielded stream.

In this documentation, **completed** means the algorithm has internally finished producing states, either because an algorithm-owned budget or completion rule fired or because the algorithm has structurally no next state to produce. External early stopping is different: it stops consumption of a stream from the outside, but does not redefine whether the wrapped algorithm itself completed.

For many simple runs, the observable state sequence can be the same either way. This is similar to LINQ: a source that naturally contains `n` items and a longer source consumed through `Take(n)` may produce the same items to the caller. The distinction matters when ownership and composition matter: whether the algorithm definition carries its own budget, whether the same configured algorithm can be reused without that limit, whether a wrapper is only adapting stream consumption, and whether future completion metadata should describe the inner algorithm as completed or merely externally stopped.

HeuristicLib aims for deterministic execution when the algorithm configuration, problem, initial state, and random seed are the same. That expectation only holds while the configuration does not depend on outside mutable state. Terminators or operators that observe external state, such as a live `CancellationToken` or a future wall-clock budget, can make otherwise identical executions produce different streams. For example, reusing a `CancellationTokenTerminator(...)` after its token has already been canceled will stop the next execution immediately after its first produced state.

For example, a local search that evaluates its configured neighborhood and finds no improving move has structurally completed. It should exhaust the stream instead of yielding another copy of the previous state.

## Budget units

Budget names should say what they count.

- `MaximumGenerations` counts produced generation states owned by a generation-producing evolutionary algorithm.
- `MaximumCycles` counts completed cycles owned by a cycle algorithm.
- `WithMaxIterations(...)` is an external early-stopping wrapper. It counts yielded stream states from the wrapped algorithm or composition, regardless of whether those states are generations, local-search moves, pipeline outputs, or cycle outputs.
- `WithMaxEvaluatorCalls(...)` is an external early-stopping wrapper over observed `Evaluate(...)` calls. It installs a counted evaluator replacement for the run and stops future stream consumption after the configured call count has been observed; it does not make the wrapped algorithm internally complete.
- `WithMaxEvaluatedGenotypes(...)` is an external early-stopping wrapper over genotypes processed inside observed evaluator batches. If one batch crosses the configured genotype count, the produced state for that batch is still yielded and future stream consumption stops afterward.
- `WithMaxEvaluatorDuration(...)` is an external early-stopping wrapper over measured evaluator work duration. It installs a measured evaluator replacement for the run and stops future stream consumption after the configured cumulative evaluator duration has been observed.
- `WithMaxCount(...)` is the general external operator-budget wrapper. It observes an explicitly supplied operator and a counted replacement factory, so users can count units such as mutator calls or mutated genotypes without a dedicated helper method for every operator family.
- Evaluation-count budgets must identify the observed boundary and unit. Counting `Evaluate(...)` calls, evaluated genotypes inside those calls, requests to a caching evaluator, and calls that reach the wrapped direct evaluator are different budgets.
Time budgets must identify whether they mean elapsed wall-clock time for a broader execution or measured duration of active work at an observed boundary.

Avoid treating "iteration" as a universal synonym for generation, step, cycle, evaluation, or yielded state. It is only the right public word when an algorithm explicitly defines iteration as its own progress unit.

## Runs and analyzers

`CreateRun(problem, analyzers...)` creates one logical execution.

A run owns:

- the algorithm definition
- the problem
- the root execution-instance registry
- analyzer state for that run

Analyzers are attached to the run, not to the algorithm definition.

## Internal infrastructure

Internally, definitions are resolved into execution instances through `ExecutionInstanceRegistry`.

`ExecutionInstanceRegistry` also implements `IExecutionInstanceResolver`, which is the narrow surface passed into high-level authoring APIs.

This still matters for:

- run-local operator state
- shared execution graph resolution
- meta-algorithms that need control over instance reuse vs reset

But it is now intentionally an execution concern.
Normal algorithm authoring should work through execution state and resolved execution instances, not through a separate executor object.

## Meta algorithms

Meta algorithms such as `PipelineAlgorithm`, `CycleAlgorithm`, and `StateTerminatedAlgorithm` still work closer to the low-level execution model because they orchestrate other algorithms directly.

That is a separate concern from the simplified authoring path for ordinary iterative algorithms.

## Related pages

- [Algorithm](algorithm.md)
- [Definition vs execution instances](execution-instances.md)
- [Observability & analysis](observability-and-analysis.md)
