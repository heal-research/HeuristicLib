# Configuration vs executors

Configurations describe reusable settings, child configurations and optional typed resource handles. Resolution builds typed executors for the requesting context on every request. Private mutable data belongs to executor fields. Sharing is explicit through handle reference identity within one run.

| Concept          | Responsibility                                                                   |
| ---------------- | -------------------------------------------------------------------------------- |
| Configuration    | Settings, declared children, resource handles and a typed `CreateExecutor` hook. |
| Executor         | Typed operations, private fields and resolved child references.                  |
| Resource handle  | Immutable creation settings and explicit sharing identity.                       |
| Resource         | Runtime data intentionally shared by its participants.                           |
| Resolution scope | Constructing executors with contextual children, advice and run resources.       |
| Pointcut         | Selecting configurations during resolution.                                      |
| Advice           | Wrapping the executor built for a selected configuration.                        |
| Aspect           | Ordered pointcut and advice pairs added to a scope or run.                       |

## Authoring execution behavior

Stateless leaves may use the stateless role bases and return themselves as immutable executors. Mutable operators and compositions author an executor with fields. Algorithms author executors because they coordinate operators and execution flow.

An operator builds its executor in `IOperator<TOperation, TCandidate>.CreateExecutor`. Resolution calls this hook with a scope typed to the requested operation and run types. Resolve every child through that scope and pass it to the executor. Initialize private counters, cursors and adaptation values on that executor. Repeated construction of one configuration creates independent private data.

Algorithms follow the same rule in their typed `CreateExecutor` hook. Consumer defined roles implement `IConfigurationNode<TExecutor>.CreateExecutor(ResolutionScope)`. Invocation local values stay in the operation or iterator. Never store mutable execution data on a configuration.

Where sharing has domain meaning, expose a `ResourceHandle<TResource>` on the configuration and call `scope.Resource(handle)` during construction. A handle contains immutable creation settings and no live resource, executor or resolution context. The resource contains shared data and no contextual child executor or advice chain. Sharing resources leaves each executor's children and advice independent.

## Configured wrappers and advice

Configured wrappers are ordinary configurations with declared children. Operator wrappers return `scope.ResolveOperator(ChildOperator)` or wrap the resolved child with `TOperation.Wrap(childOperator, wrapper)`; wrapping algorithms resolve their `ChildAlgorithm` through the scope their `CreateExecutor` receives and build their executor around it.

Advice is separate from the configuration graph. For operation-parameterized operators, `OperatorAdvice` and `OperatorAdvice<TOperation, TCandidate>` check a selected operator before its executor is built and wrap each executor built for it, usually through the operation's `Wrap`. Role advice such as `MutatorAdvice<TCandidate>` receives typed executors, and every operation has the same typed `Check`/`Wrap` pattern; termination and interception advice also receive the run's search state type. Supply concrete runtime dependencies through advice constructors and pass them to wrapping executors. Assemble advice with independent dependencies for independent runs. Advice has no resource lookup capability. Advice introduces no configured children. Observation advice reports successful operator invocations or states yielded by an algorithm; standalone counting and timing advice avoid observation allocations. A configuration declares a point inside its executor, such as the start of a cycle's turn, as an `ObservationPoint<TObservation>` child. Its `CreateExecutor` resolves the point and passes its executor to the parent executor, which calls `Report` at that point. Observation advice wraps the point's executor through ordinary role advice.

Use `Aspect.Observe`, `Aspect.CountInvocations`, `Aspect.CountCandidates` or `Aspect.MeasureDuration` from `Inspection` for predefined behavior. Add the result through the run setup callback with `setup.AddAspect`, or pass aspects to `ResolutionScope.Create` or `With`.

## Resources and child activations

A run owns one flat resource table. A trial is one run. `With` adds an aspect context using the same table. A separate root created with `ResolutionScope.Create` has an independent table. There is no configuration ownership lookup or resource ancestry. Separate constructions share an executor only when a sharing node lists the configuration.

Supply existing resources with `algorithm.CreateRun(problem, random, setup => setup.AddResource(handle, resource))` or with `new ResolutionScopeBuilder().AddResource(handle, resource).Build()` for a direct scope. A builder remains reusable. Each build snapshots bindings and aspects, creates independent default resources and retains explicitly supplied instances. A scope accepts no further bindings. `ExperimentRunSetup.AddResourcePerTrial` supplies resources to materialized cases before trial runs are created. Explicitly supplying the same instance to several runs deliberately couples them.

Cycle constructs fresh executors for every turn and passes search state to the next turn. `cycle.Sharing(A)` keeps one A executor, with its children, for every turn of one Cycle executor, so `Cycle(A, A)` then uses one A executor. Each request still applies its own advice around it. Pipeline stages and racing contenders construct fresh executors through their current context and use the run's resource table.

Operator authors own concurrent access correctness. Synchronization must protect every participant sharing a resource. Automatically created resources must need no deterministic cleanup. Resources requiring cleanup must be supplied and remain caller owned. Executors and resource tables add no automatic disposal lifecycle.

See [execution resolution](/contributing/architecture/execution-resolution) for ordering, controls, failure retry and lifetime rules, and [operator implementation](/contributing/architecture/operator-implementation) for authoring examples.
