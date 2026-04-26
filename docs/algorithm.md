# Algorithm

An algorithm drives the optimization process by producing a stream of search states.

For normal authoring, HeuristicLib now has one main iterative base:

- derive from `IterativeAlgorithm<TGenotype, TSearchSpace, TProblem, TSearchState, TExecutionState>`
- implement the step logic on the algorithm type itself
- resolve operator dependencies once in `CreateInitialExecutionState(IExecutionInstanceResolver resolver)`
- store the resolved execution instances and any mutable per-run data in `TExecutionState`

## Main authoring shape

```csharp
protected override TExecutionState CreateInitialExecutionState(IExecutionInstanceResolver resolver);

protected override TSearchState ExecuteStep(
  TSearchState? previousState,
  TExecutionState executionState,
  TProblem problem,
  IRandomNumberGenerator random)
```

`TExecutionState` is hidden per-run state. It is not the streamed public search state.

Typical execution-state contents:

- resolved `ICreatorInstance`, `IMutatorInstance`, `IEvaluatorInstance`, ...
- counters, caches, and other mutable per-run data

## Evaluator and interceptor

`Algorithm<...>` still provides the explicit `Evaluator`.

`IterativeAlgorithm<...>` additionally supports an optional `Interceptor` that transforms the produced state after each step.

## Why this model exists

This keeps algorithm logic on the algorithm type, while still preserving:

- run-local mutable state
- shared execution-instance identity within one run
- explicit, eager dependency resolution

Nested operator use is no longer hidden behind an executor object. If an algorithm depends on operators, its execution state makes that dependency explicit.

## Advanced path

The low-level execution-instance model still exists.

If a very advanced algorithm needs full manual control, it can still implement `IAlgorithm<...>` directly and work with `ExecutionInstanceRegistry`.

## Related pages

- [Search state](algorithm-state.md)
- [Execution model](execution-model.md)
- [Definition vs execution instances](execution-instances.md)
- [Operators](operators.md)
