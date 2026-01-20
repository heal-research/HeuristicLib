# Randomness (RNG) design

This page documents how randomness works in HeuristicLib and how to use it for **reproducible**, **parallel** experiments.

## The two operations: `Next...` and `Fork(...)`

HeuristicLib models randomness via `IRandomNumberGenerator`.

There are two categories of operations:

- **Drawing random numbers** via `Next...` methods (for example, `NextInt()` and `NextDouble()`).
  - These methods **advance the RNG state**.
  - If you call `Next...` in a different order, or a different number of times, you will get a different sequence.
- **Creating independent child generators** via `Fork(ulong forkKey)`.
  - `Fork(...)` does **not** draw random numbers.
  - Forking is deterministic: the child generator is derived from the parent generator and the provided `forkKey`.

This gives you a simple mental model:

> **Only `Next...` consumes randomness.** `Fork(...)` only derives new streams.

## Why `Fork(...)` exists

Parallel and batched workflows need multiple RNG streams.

If the streams come from repeatedly calling `Next...`, changing the degree of parallelism (or implementation details) can change the order of draws and therefore change the results.

`Fork(...)` avoids this by letting you build a deterministic RNG hierarchy:

- A generator has an (internal) **key**.
- `Fork(forkKey)` derives a new key by combining the parent key with `forkKey`.
- The forked generator behaves like a new, independent RNG stream.

Because `Fork(...)` does **not** advance state, the hierarchy itself is stable.

## Deterministic RNG hierarchies (the recommended pattern)

Only the outermost component (typically: **your app / experiment runner**) creates a root RNG from a user-provided seed.

Everything inside the library (algorithms, operators, parallel helpers) receives an RNG and derives child RNGs via fork keys.

### Example: multiple runs of an algorithm

Algorithms in HeuristicLib do not own a seed. They receive an `IRandomNumberGenerator` from the outside.

If you run the same algorithm multiple times, the runner should fork a per-run RNG (for example using the run index):

- Root RNG: created once from a user seed
- Run RNG: `root.Fork(runIndex)`
- Algorithm uses the passed-in run RNG and may fork further

This makes reproducible experiment setups straightforward:

- change the user seed -> you get a different, but still deterministic, experiment
- keep the user seed -> you get the same results across executions

### Example: parallel mutation over individuals

When you mutate a batch of individuals in parallel, each individual should get its own forked RNG derived from an index (or stable id):

- For individual `i`: `rngForIndividual = parentRng.Fork(i)`

Now your mutation results are stable even if:

- scheduling changes
- the number of tasks/threads changes
- an implementation changes from sequential to parallel

## Choosing fork keys

A fork key should be:

- **stable** (does not depend on scheduling)
- **meaningful** within the scope where it’s used

Common fork keys:

- run index
- iteration counter
- operator index / “operator number”
- individual index inside a population

## Relationship to the execution loop

The execution APIs explicitly accept an RNG:

- `Execute(problem, random, initialState?)`
- `ExecuteStreaming(problem, random, initialState?)`

This is intentional: algorithm behavior-affecting dependencies (especially randomness) stay visible and controllable.

See also: [Execution model](execution-model.md).
