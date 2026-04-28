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

## Iterative loop semantics

For iterative algorithms, the default loop is:

1. start from `previousState = initialState`
2. enumerate iteration indices `0, 1, 2, ...`
3. for each iteration:
   - check cancellation
   - fork the RNG using the iteration index
   - compute the next state with `ExecuteStep(...)`
   - optionally transform it with the configured interceptor
   - yield the produced state
   - continue from that state

Those iteration indices are internal to that specific iterative loop. They are useful for execution concerns such as deterministic RNG forking, but they are not part of the public search-state contract and do not define a cross-algorithm notion of iteration for nested or meta-algorithm execution.

So the model supports both:

- fresh runs, where `initialState == null`
- resumed runs, where an existing state is passed back in

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
