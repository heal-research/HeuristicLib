# Configuration vs executors

Configurations describe reusable settings and child configurations. Resolution creates their scoped state once per owning scope and builds typed executors for the current context on every request. Lookup uses configuration object reference rather than service type or value equality. Executor construction and child resolution are explicit.

| Concept          | Responsibility                                                     |
| ---------------- | ------------------------------------------------------------------ |
| Configuration    | Settings, declared children and a typed `CreateExecutor` hook.     |
| Scoped state     | Data shared by every build of a configuration in its owning scope. |
| Executor         | Typed operations and resolved child references.                    |
| Resolution scope | State selection and applying the requesting aspects.               |
| Pointcut         | Selecting configurations during resolution.                        |
| Advice           | Wrapping the executor built for a selected configuration.          |
| Aspect           | Ordered pointcut/advice pairs added to a scope or run.             |

## Authoring execution behavior

Stateless operators use role-specific stateless bases. Operators with ordinary execution data use stateful bases and framework-managed state. Operators needing child executors or custom structure use an authored executor. Algorithms use authored executors because they coordinate operators and execution flow.

An operation-parameterized operator builds its executor in `IOperator<TOperation, TCandidate>.CreateExecutor`, which resolution calls for every build with a construction scope typed to the requested operation and run types. Resolve every child through that scope and pass the resolved child to the executor. Take persistent counters, caches and helper data from `scope.GetScopedState(this, create)`, which creates them on the first request of the configuration's owner and returns the same object to every later build. Construction must not create that state itself or retain an earlier executor's children.

An algorithm builds its executor the same way, in a `CreateExecutor` hook that resolution calls for every build with a scope typed to the run types and its search state. A consumer-defined role implements `IConfigurationNode<TExecutor>.CreateExecutor(ResolutionScope)`. Both resolve every child through their scope and take persistent data from `scope.GetScopedState(this, create)`. Invocation-local values stay in the operation or iterator. Never store run data on a configuration.

A stateless leaf may return itself as its executor. A stateful leaf and a composite return fresh nodes around the same scoped state. Therefore the same configuration does not promise the same node reference on every resolve. A parent caller keeps its original children and observations when another context binds the same state.

## Configured wrappers and advice

Configured wrappers are ordinary configurations with declared children. Operator wrappers return `scope.ResolveOperator(ChildOperator)` or wrap the resolved child with `TOperation.Wrap(childOperator, wrapper)`; wrapping algorithms resolve their `ChildAlgorithm` through the scope their `CreateExecutor` receives and build their executor around it.

Advice is separate from the configuration graph. For operation-parameterized operators, `OperatorAdvice` and `OperatorAdvice<TOperation, TCandidate>` check a selected operator before its executor is built and wrap each executor built for it, usually through the operation's `Wrap`. Role advice such as `MutatorAdvice<TCandidate>` receives typed executors, and every operation has the same typed `Check`/`Wrap` pattern; termination and interception advice also receive the run's search state type. Advice introduces no configured children. Observation advice reports successful operator calls or states yielded by an algorithm; standalone counting and timing advice avoid observation allocations.

Use `Aspect.Observe`, `Aspect.CountCalls`, `Aspect.CountCandidates` or `Aspect.MeasureDuration` from `Inspection` for predefined behavior. Add the result to a run with `AddAspect`, or pass aspects to `ResolutionScope.Create`, `With` or `CreateChildScope`.

## State ownership and child activations

A scope finds the owner of a configuration's scoped state in itself and its ancestors. Parent-first selection lets descendants reuse state. Child-first selection stays local, and siblings cannot see each other's local selections. Children resolve in their parent's owning scope with the requesting aspect chain.

`With` changes aspects while sharing state. `CreateChildScope` creates child-local state and inherits aspects. A meta-algorithm keeps one `ChildScopeSelector` for its activation policy as scoped state: Fresh creates a child state, Retained keeps child state by configuration reference, and Shared uses parent state. Retained state keeps scoped state but applies each activation's current aspects.

Stateful operations are not inherently thread safe. Concurrent use needs synchronization appropriate to the execution path. Executors have no general disposal contract; resources needing deterministic cleanup require an explicit lifecycle owner.

See [execution resolution](/contributing/architecture/execution-resolution) for ordering, controls, failure retry and lifetime rules, and [operator implementation](/contributing/architecture/operator-implementation) for authoring examples.
