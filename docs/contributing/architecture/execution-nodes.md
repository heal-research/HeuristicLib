# Execution nodes

A configuration is immutable data. A run builds execution nodes from configurations, and the nodes hold everything that changes. This page defines the kinds of node and what each owns. [Execution resolution](/contributing/architecture/execution-resolution) describes how a scope builds, keeps and shares them, and [Configurations](/guide/fundamentals/configurations) describes the configuration side for users.

| Concept          | Responsibility                                                                                                |
| ---------------- | ------------------------------------------------------------------------------------------------------------- |
| Configuration    | Settings, declared children, analyzers and a construction hook.                                               |
| Execution node   | A runtime object created by a configuration and held by a scope.                                              |
| Executor         | Typed operations, private fields and resolved child references.                                               |
| Analyzer         | Immutable configuration that creates one result per run and names the substitutions that place its collector. |
| Run result       | An `IRunResult` execution node, such as an analyzer result or counter accumulator.                            |
| Resolution scope | Constructing execution nodes with contextual children and run results.                                        |

## Ownership

Configurations describe reusable settings, child configurations and analyzers. Executors own private mutable fields and the child executors they call. `IExecutionNode` covers executors, adaptations and run results. Invocation-local values stay in the operation or iterator, and a configuration never stores mutable execution data.

Run results belong to the run, not to an executor. Analyzers, counters and durations create them, and configurations read them through `scope.Results.Result(configuration)`. Reusing an analyzer across runs creates independent results; reusing a wrapper of your own may intentionally retain its caller-owned runtime dependencies.

## Construction hooks

Every configuration exposes one construction hook, which resolution calls for every build. Operators and algorithms build their executors in a typed `CreateExecutor`; other nodes, such as adaptations, clocks and consumer-defined roles, implement `IConfigurationNode<TExecutionNode>.CreateExecutionNode(ResolutionScope)`. The hook resolves each child through its scope, passes it to the node it builds and initializes private counters, cursors and adaptation values on that node, so repeated construction of one configuration creates independent private data. Construction may be deferred: a cycle retains the scope its hook received and, during each turn, resolves its inner algorithms in a new child scope of it, so construction always goes through a scope and never through a child's hook. The rules are developer guidelines § 4.3 and § 4.4, and [Operator implementation](/contributing/architecture/operator-implementation) covers the authoring paths for operators. Algorithms always author an executor (§ 4.17).

A configured wrapper is an ordinary configuration that declares the configuration it wraps as its child. It checks the run in `Supports`, resolves the child through its scope and receives its other runtime dependencies through its properties. `Substitute` and analyzers place wrappers in a new configuration before resolution: [Substitution and ordering](/contributing/architecture/execution-resolution#substitution-and-ordering) describes the rewrite and [Observation boundaries](/contributing/architecture/analyzers#observation-boundaries) the wrappers that observe.

## Next

[Execution resolution](/contributing/architecture/execution-resolution) describes scopes, sharing, ordering, adaptation, failure and lifetime rules. [Operator implementation](/contributing/architecture/operator-implementation) shows authoring examples.
