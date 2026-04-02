# Execution model

This page explains how algorithms are executed in HeuristicLib.

## The core abstraction: algorithms produce a stream of states

At the abstraction level, an algorithm instance exposes:

```csharp
IAsyncEnumerable<TAlgorithmState> RunStreamingAsync(
  TProblem problem,
  IRandomNumberGenerator random,
  TAlgorithmState? initialState = null,
  CancellationToken ct = default);
```

That means execution is **streaming-first**:

- algorithms produce a sequence of states
- enumeration drives execution
- stopping enumeration stops execution

The convenience methods in `HEAL.HeuristicLib.Core` build on top of that stream:

- `algorithm.CreateRun(problem, analyzers...)`
- `algorithm.RunStreamingAsync(problem, random, initialState?)`
- `algorithm.RunToCompletion(problem, random, initialState?)`
- `run.RunStreaming(random, initialState?)`
- `run.RunToCompletion(random, initialState?)`

## Definitions, runs, and instances

The execution model has three levels:

### 1) Definition

A definition is the reusable object graph you configure:

- algorithms
- operators
- problems
- analyzers

Definitions should stay declarative and reusable.

### 2) Run

A `Run<TGenotype, TSearchSpace, TProblem, TState>` represents one logical execution of an algorithm on a problem.

A run owns:

- the algorithm definition
- the problem
- the root `ExecutionInstanceRegistry`
- analyzer states for that logical execution

### 3) Execution instances

When execution starts, definitions are resolved into execution instances through `ExecutionInstanceRegistry`.

Those instances perform the actual work and may carry run-specific state.

## Streaming semantics

`RunStreamingAsync(...)` is **pull-based**:

- the consumer enumerates the returned `IAsyncEnumerable<TState>`
- each `MoveNextAsync()` step advances execution
- if the consumer stops iterating, the algorithm stops progressing

The convenience method `RunToCompletion(...)` simply enumerates the stream to the last produced state.

## Iterative algorithms

Many algorithms in `HEAL.HeuristicLib.Core` are modeled as iterative algorithms.

In that style, the core step is:

```csharp
TAlgorithmState ExecuteStep(TAlgorithmState? previousState, TProblem problem, IRandomNumberGenerator random)
```

The current default iterative loop in `IterativeAlgorithmInstance` behaves like this:

1. start from `previousState = initialState`
2. generate an iteration index sequence `0, 1, 2, ...`
3. for each iteration:
   - throw if cancellation was requested
   - fork the iteration RNG via `random.Fork(currentIteration)`
   - compute `newState = ExecuteStep(previousState, problem, iterationRandom)`
   - if an interceptor exists, apply `Interceptor.Transform(newState, previousState, problem.SearchSpace, problem)`
   - `yield return newState`
   - set `previousState = newState`

So a fresh start and a resumed run are both supported:

- fresh start: `initialState == null`
- resumed execution: pass a previous algorithm state as `initialState`

## Interception

Interceptors are part of the state pipeline:

```csharp
TAlgorithmState Transform(
  TAlgorithmState currentState,
  TAlgorithmState? previousState,
  TSearchSpace searchSpace,
  TProblem problem);
```

Use interceptors when you want to **transform or enrich** produced states without changing the rest of the execution model.

Typical uses:

- attach derived metrics
- normalize state shape
- remove duplicates from populations
- collect iteration-boundary hooks for analyzers

## Termination

Termination is **not** built into the base algorithm abstraction.

An algorithm can stop in several ways depending on its implementation:

- a finite algorithm simply completes its stream
- a meta-algorithm such as `CycleAlgorithm` can stop because of its own configuration (for example `MaximumCycles`)
- an algorithm can be wrapped by `TerminatableAlgorithm<...>` to stop based on an `ITerminator<...>`

The current terminator contract is:

```csharp
bool ShouldTerminate(TAlgorithmState state, TSearchSpace searchSpace, TProblem problem);
```

So termination is a pluggable concern, but not a universal mandatory step in every algorithm implementation.

## Observation and analyzers

HeuristicLib has two related but distinct observation mechanisms:

### Observable wrappers

`ObservableEvaluator`, `ObservableMutator`, `ObservableInterceptor`, and similar wrappers define **where callbacks happen**.

They are operator-level instrumentation hooks.

### Analyzers

Analyzers are the **run-scoped** analysis system built on top of observable wrappers.

Current flow:

1. pass analyzers to `CreateRun(problem, analyzers...)`
2. `Run` creates one analyzer state per analyzer
3. analyzer states call `RegisterObservations(IObservationRegistry)`
4. `Run` installs merged observable replacements into each relevant registry
5. users retrieve analyzer state through `run.GetAnalyzerResult(analyzer)`

So observation is no longer modeled as a special `IIterationObserver<...>` abstraction on algorithms.
Instead, it is composed from observable operators and run-scoped analyzers.

## Error handling

HeuristicLib does not wrap exceptions thrown by algorithms, operators, problems, or analyzers.

If evaluation, termination, interception, or algorithm logic throws, the exception bubbles out of the streaming enumeration.

The same is true for observer and analyzer callbacks triggered through observable wrappers.

## Related pages

- [Definition vs execution instances](execution-instances.md)
- [Observability & analysis](observability-and-analysis.md)
- [Analyzer architecture](analyzer-architecture.md)
- [Randomness](randomness.md)
