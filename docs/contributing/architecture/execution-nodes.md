# Configuration vs executors

Configurations describe reusable settings and child configurations. Resolution prepares their persistent state once and constructs typed executors for the current context. Lookup uses configuration object reference rather than service type or value equality. Executor construction and child resolution are explicit.

| Concept          | Responsibility                                                    |
| ---------------- | ----------------------------------------------------------------- |
| Configuration    | Settings, declared children and a typed preparation method.       |
| Executor factory | Persistent state prepared once; construction of contextual nodes. |
| Executor         | Typed operations and resolved child references.                   |
| Resolution scope | State selection and applying the requesting aspects.              |
| Pointcut         | Selecting configurations during resolution.                       |
| Advice           | Wrapping the executor built for a selected configuration.         |
| Aspect           | Ordered pointcut/advice pairs added to a scope or run.            |

## Authoring execution behavior

Stateless operators use role-specific stateless bases. Operators with ordinary execution data use stateful bases and framework-managed state. Operators needing child executors or custom structure use an authored executor. Algorithms use authored executors because they coordinate operators and execution flow.

A configuration prepares an `ExecutorFactory<TExecutor>` without a scope. Allocate persistent counters, caches and helper data before returning that factory. Its construction callback receives a `ResolutionScope`; resolve every child through it and pass the resolved child to the node constructor. Invocation-local values stay in the operation or iterator. Never store run data on a configuration.

For operation-parameterized operators, `IOperator<TOperation, TCandidate>.CreateExecutorFactory()` creates one `OperatorExecutorFactory` holding the persistent state. Its `CreateExecutor` constructs an executor for the requested operation and run types from a typed construction scope, resolving children through that scope. Construction must not initialize state again or retain an earlier executor's children. Existing nominal role bases and algorithm factories keep their current preparation entry points during migration.

A prepared leaf may return one retained raw executor. Composite factories can return fresh nodes around the same persistent state. Therefore the same configuration does not promise the same node reference on every resolve. A parent caller keeps its original children and observations when another context binds the same state.

## Configured wrappers and advice

Configured wrappers are ordinary configurations with declared children. Generic wrappers return `OperatorExecutorFactory.Forward(child)` or `OperatorExecutorFactory.WrapInvocation(child, wrapper)`; nominal wrapping bases prepare a `WrapperExecutorFactory<TExecutor>` through their protected hook and receive the child resolved by the construction scope.

Advice is separate from the configuration graph. For operation-parameterized operators, `OperatorAdvice` and `OperatorAdvice<TOperation, TCandidate>` check a selected operator before its executor is built and wrap each executor built for it, usually through the operation adapter's `WrapInvocation`. Role advice such as `MutatorAdvice<TCandidate>` receives typed executors. Other roles retain their role-specific advice constructors. Advice introduces no configured children. Observation advice reports successful operator calls or states yielded by an algorithm; standalone counting and timing advice avoid observation allocations.

Use `Aspect.Observe`, `Aspect.CountCalls`, `Aspect.CountCandidates` or `Aspect.MeasureDuration` from `Inspection` for predefined behavior. Add the result to a run with `AddAspect`, or pass aspects to `ResolutionScope.Create`, `With` or `CreateChildScope`.

## State ownership and child activations

A scope finds prepared factories in its state and ancestors. Parent-first selection lets descendants reuse state. Child-first selection stays local, and siblings cannot see each other's local selections. Children resolve in their factory owner's state with the requesting aspect chain.

`With` changes aspects while sharing state. `CreateChildScope` creates child-local state and inherits aspects. A meta-algorithm prepares one `ChildScopeSelector` for its activation policy: Fresh creates a child state, Retained keeps child state by configuration reference, and Shared uses parent state. Retained state keeps advice preparation but applies each activation's current aspects.

Stateful operations are not inherently thread safe. Concurrent use needs synchronization appropriate to the execution path. Executors have no general disposal contract; resources needing deterministic cleanup require an explicit lifecycle owner.

See [execution resolution](/contributing/architecture/execution-resolution) for ordering, controls, failure retry and lifetime rules, and [operator implementation](/contributing/architecture/operator-implementation) for authoring examples.
