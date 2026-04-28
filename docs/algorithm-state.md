# Search state

Algorithms produce **search states**: public values that represent the current search progress.

States are the primary unit of observation and control:

- Streaming execution yields states.
- Termination policies inspect states.
- Interceptors post-process states.

## The contract

At the abstraction level, a search state is just a marker for the public streamed state shape:

- `ISearchState` is the interface.
- `SearchState` is an empty convenience record.

That base type does **not** imply any particular payload. In practice, concrete search states usually hold things like:

- a current population
- a current solution
- a best-so-far summary
- any other public progress snapshot the algorithm wants to expose

Built-in examples include `PopulationState<TGenotype>` and `SingleSolutionState<TGenotype>`.

Iteration counts are not part of the search-state contract. In nested, wrapped, or cycled executions there is no single globally meaningful notion of “the current iteration”, so that kind of counting remains an execution concern rather than public search state.

## Iteration semantics

`IterativeAlgorithm<...>` internally advances a loop-local counter for its execution loop and uses it for tasks like deterministic RNG forking.

That counter is **not** automatically stored in the public search state, and it should not be treated as part of `ISearchState` or `SearchState`.

The “fresh start” signal is the absence of a previous state:

- `previousState == null` means the algorithm should initialize.

## Checkpointing and continuation

The default streaming loop accepts an optional `initialState` parameter. Conceptually:

- If you pass `initialState`, it becomes the starting `previousState` for the resumed execution.
- The next call to `ExecuteStep(...)` will compute the next public search state from that state.

This supports a simple checkpointing pattern:

```csharp
var last = algorithm.RunToCompletion(problem, rng);

// Continue from the checkpoint.
foreach (var state in algorithm.RunStreaming(problem, rng, initialState: last)) {
   // ...
}
```

## Related pages

- [Algorithm](algorithm.md)
- [Execution model](execution-model.md)

