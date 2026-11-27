# Architecture overview

HeuristicLib separates what to run from running it. Configurations are immutable descriptions, and a run builds executors from them that hold all mutable state. Everything else follows from that split.

## The parts

| Part            | Responsibility                                                                       | Read                                                                          |
| --------------- | ------------------------------------------------------------------------------------ | ----------------------------------------------------------------------------- |
| Layers          | Which type may depend on which, and who owns a run                                   | [Layering](/contributing/architecture/layering)                               |
| Execution nodes | What configurations, executors and run results own, and how they are constructed     | [Execution nodes](/contributing/architecture/execution-nodes)                 |
| Resolution      | How a scope builds, keeps and shares executors, and how substitutions order wrappers | [Execution resolution](/contributing/architecture/execution-resolution)       |
| Operators       | The rules behind the operator bases, routing and new roles                           | [Operator implementation](/contributing/architecture/operator-implementation) |
| Analysis        | Observation boundaries, clocks, traces and the ownership of analyzer results         | [Analyzer architecture](/contributing/architecture/analyzers)                 |

## From configuration to result

1. A user builds a configuration graph from algorithm and operator records. Each record declares its settings and children as parameters.
2. `Substitute` and the substitutions passed to `CreateRun` rewrite the graph, for example to place an analyzer's observation wrapper.
3. Validation walks the declared parameters and checks the run types and the invariants of every configuration.
4. `AlgorithmRun` creates a root resolution scope. On the first advance the scope calls the algorithm's construction hook, which resolves its children through the scope.
5. Executors run the search. Observation wrappers report to analyzers, which write into run results owned by the root scope.
6. The caller reads the results through `run.Result`.

[How execution works](/extend/how-execution-works) describes the same flow for component authors.

## Principles that shape the code

- **No privileges for built-in operations.** A user-defined operation is resolved, wrapped, observed and traced exactly like a built-in one. Nothing lists the built-in operations.
- **Configurations hold no state.** State lives in executors and run results, so one configuration serves many runs at once.
- **Declared parameters are the graph.** Validation, substitution and sharing reach a child only through its declaration.
- **Run results belong to the run.** Analyzers are configurations and keep no data themselves.

[Design goals](/contributing/design-goals) and the [developer guidelines](/contributing/developer-guidelines) state these principles in full.
