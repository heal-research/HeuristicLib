# Configuration vs executors

Configurations describe reusable settings, child configurations and instrument definitions. Resolution keeps one executor per configuration/problem reference pair for algorithms and operators, and per configuration reference for configurations whose executor needs no problem, such as counters within a scope. Executors own private mutable fields; `Sharing` retains selected executors across child scopes.

| Concept          | Responsibility                                                              |
| ---------------- | --------------------------------------------------------------------------- |
| Configuration    | Settings, declared children, instruments and a typed `CreateExecutor` hook. |
| Executor         | Typed operations, private fields and resolved child references.             |
| Instrument       | Immutable definition of one result per run and how it is placed.            |
| Run result       | Runtime data collected for an instrument within one run.                    |
| Resolution scope | Constructing executors with contextual children and run results.            |

## Authoring execution behavior

Stateless role bases preserve the typed author override and construct a small executor retaining its problem or search space. Context-free stateless leaves can return themselves as immutable executors. Mutable operators and compositions author an executor with fields. Algorithms author executors because they coordinate operators and execution flow.

An operator builds its executor in `IOperator<TCandidate>.CreateExecutor`. Resolution calls this hook with a typed scope containing the problem object. Resolve every child through that scope and pass it to the executor. Initialize private counters, cursors and adaptation values on that executor. Repeated construction of one configuration creates independent private data.

Algorithms follow the same rule in their typed `CreateExecutor` hook. Consumer defined roles implement `IConfigurationNode<TExecutor>.CreateExecutor(ResolutionScope)`. Invocation local values stay in the operation or iterator. Never store mutable execution data on a configuration.

Keep private execution data in its executor. Use `Sharing` on an enclosing configuration to retain that executor across child scopes. Instrument definitions describe run-bound analysis or accounting; configurations read their results through `scope.Results.Result(instrument)`. Neither configuration contains a live result.

## Configured wrappers

Configured wrappers are ordinary configurations with declared children. Operator wrappers return `scope.ResolveOperator(ChildOperator)` or wrap the resolved child with `TOperation.Wrap(childOperator, wrapper)`; wrapping algorithms resolve their `ChildAlgorithm` through the scope their `CreateExecutor` receives and build their executor around it.

`Substitute` places wrappers in a configuration before resolution and returns the new configuration. It selects by reference, by the type of a lambda's parameter or by a predicate, and reaches declared children only. A wrapper checks the run in `Supports` and receives its runtime dependencies through its properties. Observation sources wrap successful operator calls, algorithm iterations or algorithm calls. Instruments place observation wrappers through substitutions; explicit placement uses the same wrappers.

Use `Observing` for runtime callbacks and source-defined counters or durations for accounting, and attach them to an algorithm with `AnalyzedBy`. Wrappers of your own are placed with `Substitute`.

## Resources and child activations

A root scope owns one run result table. Child scopes share its results while keeping independent executors except for explicitly shared configurations. A separate root has independent results.

Create each run from its configured algorithm. There is no separate setup or supply phase. Reusing an instrument definition across runs creates independent results; reusing a wrapper of your own may intentionally retain its caller-owned runtime dependencies.

Cycle constructs fresh executors for every turn and passes search state to the next turn. `cycle.Sharing(A)` keeps one A executor, with its children, for every turn of one Cycle executor, so `Cycle(A, A)` then uses one A executor. Pipeline stages and racing contenders construct fresh executors through their current context and use the run's result table.

Operator authors own concurrent access correctness. Synchronization must protect every participant using a shared executor. Executors and result tables add no automatic disposal lifecycle.

See [execution resolution](/contributing/architecture/execution-resolution) for ordering, controls, failure retry and lifetime rules, and [operator implementation](/contributing/architecture/operator-implementation) for authoring examples.
