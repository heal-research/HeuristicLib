# Execution model

This page explains how algorithms are executed in HeuristicLib.

## The core loop: iterative state transformation

At the abstraction level, an algorithm is a function that produces a sequence of states:

- `ExecuteStep(problem, previousState, random)` computes the next state.
- `previousState == null` signals that the algorithm should initialize.

Most algorithms in this repository start with `CurrentIteration = 0` and then increment by 1.

## Two modes: batch vs streaming

The `IIterable<...>` contract is streaming-first and comes with extension methods:

- `Execute(problem, random, initialState?)` returns the last produced state.
- `ExecuteStreaming(problem, random, initialState?)` yields each produced state.

Streaming is **pull-based**: enumeration drives execution. If you stop enumerating, you stop executing.

## The exact streaming semantics

The default implementation of `ExecuteStreaming(...)` behaves like this:

1. `previousState` is set to `initialState`.
2. If `previousState` is not null, the terminator is consulted once before the first step.
3. While the terminator says “continue”:
   - compute `newState = ExecuteStep(problem, previousState, random)`
   - optionally apply `Interceptor.Transform(newState, previousState, ...)`
   - `yield return newState`
   - ask the terminator whether to continue using `(newState, previousState)`
   - set `previousState = newState`

This makes two patterns explicit:

- Fresh start: call `ExecuteStreaming(problem, random)` and let the algorithm initialize.
- Continue from checkpoint: call `ExecuteStreaming(problem, random, initialState: checkpoint)`.

## Termination

Termination is a pluggable policy:

- `ITerminator.ShouldTerminate(currentState, previousState, searchSpace, problem)`

The interface also provides a default `ShouldContinue(...)` convenience method.

The repository includes common terminators such as `AfterIterationsTerminator<TGenotype>`.

## Interception

Interceptors are part of the state pipeline:

- `IInterceptor.Transform(currentState, previousState, searchSpace, problem)`

Use interceptors when you want to **modify/enrich the produced state** (for example, attach derived metrics).

## Observation

The library defines `IIterationObserver<...>` as a side-effect hook.

Algorithms may expose an observer via `IObservable.Observer`.

> [!IMPORTANT]
> The execution loop invokes the observer once per produced iteration state.

Observer invocation happens as part of the normal iteration pipeline:

1. `ExecuteStep(...)` produces the next state.
2. If an interceptor is present, `Interceptor.Transform(...)` post-processes that state.
3. The observer is invoked with `(currentState, previousState, searchSpace, problem)`.

Observers are intended for side effects like logging, progress reporting, and metrics collection.

## Error handling

HeuristicLib does not wrap exceptions thrown by algorithms/operators/problems.

If evaluation, termination, interception, or algorithm logic throws, the exception will bubble out of the streaming enumeration.
