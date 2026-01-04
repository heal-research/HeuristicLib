# Algorithm state

Algorithms produce **states**: values that represent the algorithm at a specific iteration boundary.

States are the primary unit of observation and control:

- Streaming execution yields states (`IEnumerable<TState>`).
- Termination policies inspect states.
- Interceptors post-process states.

## The contract

At the abstraction level, a state only needs to provide an iteration counter:

- `int CurrentIteration { get; }`

In this repository, the common concrete base is a record:

- `AlgorithmState` is a record with an init-only `CurrentIteration`.

Concrete algorithms extend that record with their own fields (for example, evolutionary algorithms use population-based states).

## Iteration semantics

In the built-in algorithms in this repository, iteration counting follows a simple rule:

- The first produced state uses `CurrentIteration = 0`.
- Each subsequent state increments by exactly 1.

The “fresh start” signal is not an iteration number; it is the absence of a previous state:

- `previousState == null` means the algorithm should initialize.

## Checkpointing and continuation

The default streaming loop accepts an optional `initialState` parameter. Conceptually:

- If you pass `initialState`, it becomes the first `previousState`.
- The next call to `ExecuteStep(...)` will compute the next iteration from that state.

This supports a simple checkpointing pattern:

```csharp
var last = algorithm.Execute(problem, rng);

// Continue from the checkpoint.
foreach (var state in algorithm.ExecuteStreaming(problem, rng, initialState: last)) {
   // ...
}
```

## Related pages

- [Algorithm](algorithm.md)
- [Execution model](execution-model.md)

