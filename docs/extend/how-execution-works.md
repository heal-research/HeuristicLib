# How execution works

Every component you write has two parts, a configuration and an executor. This page explains how a run turns one into the other. The other Extend pages build on it.

## Configuration and executor

A **configuration** is an immutable record. It holds settings and child configurations and never changes during a run. The same configuration can serve many runs, also concurrently.

An **executor** does the work for one run. It holds the resolved child executors and every mutable field, such as a counter, a cursor or a cache. A configuration creates its executor in a construction hook. Operators and algorithms override `CreateExecutor`. Other nodes, such as clocks and adaptations, implement `CreateExecutionNode`.

<ConfigurationExecutors>

One configuration and the executors of two runs. Each run builds an executor graph that mirrors the configuration. The current generation and the cache entries live in the executors, so the runs do not share them.

</ConfigurationExecutors>

| Belongs in the configuration        | Belongs in the executor            |
| ----------------------------------- | ---------------------------------- |
| Settings, such as a mutation rate   | Counters, cursors, caches          |
| Child configurations, as parameters | Resolved child executors           |
| Nothing that changes during a run   | The bound problem and search space |

Values local to one call stay in local variables of that call.

## A run builds the executors

`CreateRun`, `RunStreaming` and `RunToCompletionAsync` all create an **algorithm run**. The run:

1. applies the substitutions it was given, such as analyzers, to a copy of the configuration,
2. validates the resulting configuration graph,
3. creates a root **resolution scope** and asks it for the algorithm's executor on the first advance,
4. advances the executor and yields the search states.

The scope calls the algorithm's `CreateExecutor`. That hook resolves each child through the scope, and each child's hook does the same. So the executor graph mirrors the configuration graph, and nothing is built for a configuration the run never asks for.

```csharp
protected override IterativeAlgorithmExecutor<SingleSolutionState<RealVector>> CreateExecutor<TSearchSpace, TProblem>(
    ResolutionScope<RealVector, TSearchSpace, TProblem, SingleSolutionState<RealVector>> scope)
{
    var creator = scope.Resolve(Creator);
    var evaluator = scope.Resolve(Evaluator);
    return new Executor(scope.ResolveOptional(Interceptor), creator, evaluator);
}
```

Never call a child's hook directly. The scope decides whether to build or reuse, and the `HLib0001` analyzer reports a direct call.

## The scope decides what is shared

A scope keeps one executor per configuration and problem. Resolving the same configuration twice in one scope returns the same executor, with the same fields.

A meta-algorithm can resolve a child in a **child scope**, as `CycleAlgorithm` does for each turn. A child scope builds fresh executors, so each turn starts with fresh fields. A configuration listed in `Sharing` keeps one executor across child scopes. [Sharing executors](/guide/fundamentals/configurations#sharing-executors) shows it from the user's side.

Every run has its own root scope. No executor and no field carries over from one run to the next.

## Run results belong to the run

Data a run collects for its caller, such as a trace or a count, is a **run result**. Analyzers, counters and durations create one result per run. An executor reads it through the scope, for example `scope.Results.Result(analyzer)`. The caller reads it after the run with `run.Result(analyzer)`.

An analyzer's result spans the whole run, including every turn of a cycle.

## Wrappers are placed before resolution

Counting, timing, observation and your own wrappers are ordinary configurations that hold the configuration they wrap. `Substitute` and the substitutions passed to `CreateRun` place them in a new configuration before the run resolves anything. Resolution then builds the wrapper's executor around the child's executor like any other.

## Types are checked before anything is built

A slot names only the operation and the candidate, such as `IOperator<Mutation, RealVector>`. The run supplies the search space, the problem and the search state. Validation checks that every configuration accepts these types before the run starts. Resolution checks them again before it builds, so a mismatch fails without constructing any child.

## Next steps

- [Write an operator](/extend/writing-operators) applies this model to a single operator.
- [Write an algorithm](/extend/writing-algorithms) shows the algorithm side.
- [Execution resolution](/contributing/architecture/execution-resolution) states every rule of scopes, sharing and ordering.
