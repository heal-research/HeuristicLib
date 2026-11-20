# Writing algorithms

Create an algorithm only when the state transition or control flow is new. Put domain evaluation in a problem and local search policies in operators whenever those existing extension points are sufficient. This page assumes [Write an operator](/guide/extending/writing-operators): an algorithm resolves operators that you or the library wrote and runs them.

## Configuration and execution

An algorithm has two parts:

1. An immutable configuration record that holds operators and settings.
2. An executor bound to resolved operators and the interceptor, with private fields and references to any run-bound results it uses.

Each call to `RunStreaming` or `RunToCompletionAsync` starts an independent root execution. A scope keeps one executor per configuration and problem; [Configurations](/guide/fundamentals/configurations#sharing-executors) explains when a child scope builds its own and how `Sharing` keeps one.

For custom streaming control flow, derive the executor from `AlgorithmExecutor<TSearchState>` and write it as a private nested `Executor` class. Keep the search space or problem in the executor constructor when it reads that contract. A wrapper can use `WrappingAlgorithmExecutor<TSearchState>` to retain its resolved `ChildAlgorithm`.

## Implement an iterative algorithm

Derive from `IterativeAlgorithm` when one previous state produces one next state. This example creates and evaluates one candidate per step:

<<< ../../../test/HeuristicLib.Tests.Docs/Extending/AlgorithmAuthoringDocumentationSpecs.cs#iterative-algorithm-imports{csharp}

<<< ../../../test/HeuristicLib.Tests.Docs/Extending/AlgorithmAuthoringDocumentationSpecs.cs#iterative-algorithm-type{csharp}

### Why the configuration declares its properties

Pre-flight validation and substitution reach a child only through its declared parameter, so an undeclared child is not checked before the run and cannot be replaced. `GetParameters` adds the inherited parameters, here the interceptor of the iterative base.

### Why the configuration names only the candidate

`SingleCreateAlgorithm` never reads a member of a particular problem. It binds its operators through the supplied scope, so it does not need to name either, and the base above takes three type arguments: the algorithm's own type, the candidate, and the state it produces.

Operator slots, including terminators, name their operation and candidate. An interceptor slot also names the search state its operation returns, as the inherited `IOperator<Interception<SingleSolutionState<RealVector>>, RealVector>? Interceptor` does. `IOperator<Creation, RealVector>` accepts any creator written for real vectors, including one written against `BoundedRealVectorSearchSpace`, because the run supplies the space when the executor is created.

The run's search space and problem arrive as method type arguments on `CreateExecutor`, together with a scope typed to them whose `Problem` property supplies the object. The nested executor only needs the resolved operators and interceptor. Configurations omit the run types from operator slots.

Resolution calls this protected hook for every build. The hook returns `IterativeAlgorithmExecutor`, so the compiler requires an executor that applies the interceptor. Resolve the optional `Interceptor` alongside the other configured children and pass it to the executor constructor. The executor base applies it in the iteration loop, before checking terminal state and yielding; its result becomes the next iteration's previous state. `ExecuteStep`, the interceptor and the protected `IsTerminalState` hook receive the same per-iteration random number generator, so an algorithm that owns a terminator passes that generator on.

### When to name the search space and problem instead

An algorithm that reads something only one problem has — a distance matrix, a dataset, a domain-specific bound — cannot work that way, and derives from the five-argument base. This declaration excerpt shows only the base type arguments; the parameters, `CreateExecutor` and executor follow as in the previous example:

```csharp
public sealed record MatrixAwareAlgorithm
    : IterativeAlgorithm<
        MatrixAwareAlgorithm,
        Permutation,
        PermutationSearchSpace,
        TravelingSalesmanProblem,
        SingleSolutionState<Permutation>>
```

Its protected `CreateExecutor` takes no method type arguments and receives a scope typed to the declared contracts. Pass the typed problem to the executor if it reads the distance matrix. The hook returns `IterativeAlgorithmExecutor<SingleSolutionState<Permutation>>`. Operator slots still omit the run types. The base rejects a run whose search space or problem does not convert to the declared contracts before it calls the hook. Choose this base when the algorithm reads those contracts itself.

Use `.TerminatedAfterIterations(count)` or another terminator when the algorithm does not stop itself.

## Bind children and keep state

Resolve the optional interceptor and every other configured child operator through the scope that `CreateExecutor` receives. Retain the resolved children on the executor. Do not call operator configurations directly from `ExecuteStep` or resolve children on every iteration.

Keep private counters and adaptation data in executor fields. An executor that should outlive a child scope, such as a cache, is kept by sharing its configuration: [Sharing executors](/guide/fundamentals/configurations#sharing-executors) states who owns it and which scopes reuse it, and [Evaluations of a refined candidate](/guide/extending/operator-composition#evaluations-of-a-refined-candidate) shows it for a cache. Resolve a counter or duration with `scope.Resolve(counter)`, and use `scope.Results.Result(analyzer)` for an analysis result. Keep iteration position, previous search state and invocation local values in the operation or iterator. A paused iterator retains its children, interceptor and run-bound results when another context constructs an executor. Never mutate the configuration record.

## An objective that changes during a run

A problem's objective is fixed for the lifetime of the problem object, and a selector or replacer reads it from the problem it is bound to. An algorithm whose ranking changes while it runs does not pass that objective to a selector or a replacer. MOEA/D, for example, compares under a scalarization whose ideal point moves after every evaluation. That ranking is the algorithm's own value strategy over objective vectors: an immutable value that holds what is configured, such as the weights of the scalarization, while what changes, such as the ideal point, is executor state. MOEA/D is not in the library, so this sketch shows only the pattern:

<<< ../../../test/HeuristicLib.Tests.Docs/Extending/VaryingObjectiveDocumentationSpecs.cs#varying-objective-imports{csharp}

<<< ../../../test/HeuristicLib.Tests.Docs/Extending/VaryingObjectiveDocumentationSpecs.cs#varying-objective-strategy{csharp}

The static selection helpers take an objective for this case. `TournamentSelector.Select(population, objective, count, random, tournamentSize)` ranks under the `ObjectiveDirections` it is given, so the executor builds one from its current state whenever it selects. The example's inputs are a scalarization with two weights, an ideal point over two objectives, a random number generator and a neighbourhood of two evaluated candidates, `a` and `b`:

<<< ../../../test/HeuristicLib.Tests.Docs/Extending/VaryingObjectiveDocumentationSpecs.cs#varying-objective-inputs{csharp}

The helper then ranks under the scalarization of whatever the ideal point is at the call:

<<< ../../../test/HeuristicLib.Tests.Docs/Extending/VaryingObjectiveDocumentationSpecs.cs#varying-objective-usage{csharp}

In the example `b` wins while the ideal point sits where the two candidates put it, and `a` wins once an evaluation has moved the ideal point. The operators the algorithm resolves through its scope keep ranking under the problem's objective. To give an operator another fixed objective instead, [resolve it under a problem with that objective](/guide/extending/writing-operators#resolve-an-operator-under-another-objective).

## Preserve run behavior

A custom algorithm should:

1. Yield a state after each meaningful step.
2. Accept the supplied random source and derive stable child streams when work is independent.
3. Check cancellation through the inherited run loop.
4. Keep simultaneous runs independent.
5. Work with interceptors when it derives from `IterativeAlgorithm`.

Read [Running algorithms](/guide/execution/running-algorithms) for the consumer model and [Execution nodes](/contributing/architecture/execution-nodes) for the internal ownership rules.

To write an algorithm that coordinates other algorithms rather than operators, continue with [Write a meta-algorithm](/guide/extending/writing-meta-algorithms).
