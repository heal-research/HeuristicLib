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

A supplied `initialState` is resume input from outside the current execution; it is not a newly produced state. The default execution model therefore does not yield that state or treat it as something state-based external terminators have observed. External terminators such as `TerminatableAlgorithm` check only produced public states, after the state has been yielded, and a matching state stops future consumption rather than removing the state that triggered the stop.

Some algorithms also expose internal budget properties and state-based internal terminators. For example, `MaximumGenerations` on evolutionary algorithms such as `GeneticAlgorithm`, `EvolutionStrategy`, `NSGA2`, `AlpsGeneticAlgorithm`, and `OpenEndedRelevantAllelesPreservingGeneticAlgorithm` is part of the algorithm's own execution budget and counts produced generation states from the current execution. A resumed run does not count the supplied `initialState` toward that budget. If an algorithm has a custom internal `Terminator`, it is checked only against states yielded by the current execution, not against a supplied `initialState`. This differs from `WithMaxIterations(...)`, which wraps an algorithm with external early stopping over the yielded stream.

In this documentation, **completed** means the algorithm has internally finished producing states, either because an algorithm-owned budget or completion rule fired or because the algorithm has structurally no next state to produce. External early stopping is different: it stops consumption of a stream from the outside, but does not redefine whether the wrapped algorithm itself completed.

For example, a local search that evaluates its configured neighborhood and finds no improving move has structurally completed. It should exhaust the stream instead of yielding another copy of the previous state.

## Budget units

Budget names should say what they count.

- `MaximumGenerations` counts produced generation states owned by a generation-producing evolutionary algorithm.
- `MaximumCycles` counts completed cycles owned by a cycle algorithm.
- `WithMaxIterations(...)` is an external early-stopping wrapper. It counts yielded stream states from the wrapped algorithm or composition, regardless of whether those states are generations, local-search moves, pipeline outputs, or cycle outputs.
- Evaluation-count budgets must identify the observed boundary. Counting requests to a caching evaluator and counting calls that reach the wrapped direct evaluator are different budgets.

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

Meta algorithms such as `PipelineAlgorithm`, `CycleAlgorithm`, and `TerminatableAlgorithm` still work closer to the low-level execution model because they orchestrate other algorithms directly.

That is a separate concern from the simplified authoring path for ordinary iterative algorithms.

## Related pages

- [Algorithm](algorithm.md)
- [Definition vs execution instances](execution-instances.md)
- [Observability & analysis](observability-and-analysis.md)
