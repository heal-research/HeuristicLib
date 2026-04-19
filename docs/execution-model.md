# Execution model

This page explains how algorithm execution works in HeuristicLib.

## The user-facing model

At the user level, the important idea is simple:

- an algorithm definition is a reusable configured object
- a run executes that definition on a problem
- execution produces a stream of algorithm states

The core streaming shape is:

```csharp
IAsyncEnumerable<TAlgorithmState> RunStreamingAsync(
  TProblem problem,
  IRandomNumberGenerator random,
  TAlgorithmState? initialState = null,
  CancellationToken ct = default);
```

Convenience methods such as `RunToCompletion(...)` are just ways of consuming that stream.

## The main authoring model

Most ordinary algorithms should now be authored through:

- `IterativeAlgorithm<...>`
- `StatefulIterativeAlgorithm<...>`

Those bases provide a step-based model:

```csharp
protected override TSearchState ExecuteStep(
  TSearchState? previousState,
  IOperatorExecutor executor,
  TProblem problem,
  IRandomNumberGenerator random)
```

or, with explicit hidden runtime state:

```csharp
protected override TSearchState ExecuteStep(
  TSearchState? previousState,
  TRuntimeState runtimeState,
  IOperatorExecutor executor,
  TProblem problem,
  IRandomNumberGenerator random)
```

`IOperatorExecutor` is the intended way to invoke creators, evaluators, selectors, crossovers, mutators, replacers, interceptors, and terminators from algorithm logic.

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

This still matters for:

- run-local operator state
- shared runtime graph resolution
- meta-algorithms that need control over instance reuse vs reset

But it is now intentionally an internal execution concern.
Normal algorithm authoring should work through the algorithm base classes and `IOperatorExecutor`, not through manual instance classes.

## Meta algorithms

Meta algorithms such as `PipelineAlgorithm`, `CycleAlgorithm`, and `TerminatableAlgorithm` still work closer to the low-level execution model because they orchestrate other algorithms directly.

That is a separate concern from the simplified authoring path for ordinary iterative algorithms.

## Related pages

- [Algorithm](algorithm.md)
- [Definition vs execution instances](execution-instances.md)
- [Observability & analysis](observability-and-analysis.md)
