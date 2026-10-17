# Execution resolution

Resolution selects persistent state by configuration reference and binds typed executors with the requesting scope's aspects. Configurations prepare factories without a scope; the returned factories receive a construction scope and resolve their declared children.

See [configuration vs executors](/contributing/architecture/execution-nodes) for authoring responsibilities.

## State and construction

`ResolutionScope.Create(aspects)` creates an independent root. `CreateChildScope(aspects)` creates child-local state and inherits the parent's aspects. `With(aspects)` shares state and adds another aspect layer. All three accept zero or several aspects in supplied order.

A scope stores its own aspects and its parent scope. Its internal `State` stores prepared factories by configuration reference, a parent state and advice factories. Aspect inheritance and state ownership are independent: adding an observation changes the bound graph without resetting prepared state.

For each resolution:

1. Find the configuration's prepared factory in the current state or its ancestors. Prepare it locally if none exists.
2. Invoke that factory with the owner's state and the requesting aspects. Resolve children in that construction scope.
3. Apply matching advice in scope order to the raw executor. Return the advised executor.

A configuration selected first in a child stays local; a later selection in the parent does not replace it. Sibling states cannot see each other's local factories. A parent-first selection is reused by descendants. Dependencies resolve in the factory owner's state, so a direct child-local selection does not redirect an ancestor composite's dependencies.

Resolution constructs nodes on every request. A prepared stateless or stateful leaf may return its existing raw node. Composite factories can construct fresh bindings around the same prepared state. Do not rely on advised node reference identity to identify a logical execution.

### Operation-parameterized operators

`IOperator<TOperation, TCandidate>` prepares one `OperatorExecutorFactory` per selected configuration reference. Its run-generic `GetExecutorFactory` returns checked construction factories for candidate, operation, search space, problem, run state and requested executor type. Several operations share the same prepared owner. Only factories are cached; contextual nodes and children are constructed for each resolution. `ExecutorFactory.FromFactory` preserves the authored executor type so compatible contravariant execution views remain usable through covariant `IExecutorWithControls<TExecutor>`.

The generic and legacy paths use one owner table, ancestry, aspect chain and configuration-reference cycle guard. A nominal role resolver adapts a configuration declaring the corresponding generic contract through `ResolveOperator`; it does not call a second preparation method. Low-level factory callbacks cannot switch an already selected owner's preparation path. Existing role contracts and authoring bases remain until their consumer migrations.

For the generic path, resolution checks the raw factory and all selected advice constructors before constructing the raw graph. `OperatorAdvice` prepares state once per advice/configuration/owner and binds each view separately; `OperatorAdvice<TOperation, TCandidate>` applies only to its selected operation. Unsupported advice produces an explicit binding diagnostic. Advice keys are weak. Failed view binding preserves successful owner and advice preparation.

Descriptors supply named-method adapters for success and lifecycle hooks. Success receives declared candidate work: returned counts for candidate-producing operations, input count for evaluation; termination and interception do not support candidate metrics. Metric advice calls `RequireCandidateWork` during binding. Lifecycle tokens belong to individual invocations; cleanup runs after successful entry, including faults, and multiple failures preserve child/callback/cleanup order. Algorithms retain their separate streaming advice.

## Pointcuts, advice and join points

A `Pointcut<TConfiguration>` examines a configuration as it is resolved. `Pointcut.Reference(source)` uses reference identity; `Pointcut.OfType<TConfiguration>()` includes implementations and derived types. `Pointcut.Predicate<TConfiguration>(predicate)` supplies runtime-only property or attribute matching; custom selection can also derive a small class overriding `Matches`. Predicates examine configuration data, run on each query and propagate exceptions. Pointcuts do not traverse the graph.

`And` and `Or`, also spelled `&` and `|`, combine pointcuts and short-circuit from left to right. `Not()` and `!` complement a selection within its declared configuration type: negating a mutator pointcut still excludes every other role.

`Pointcut.Named<TConfiguration>(name)` matches `INamedConfiguration.Name` ordinally and case-sensitively. Concrete configurations can implement that contract, or users can configure a role-preserving wrapper through `operator.Named(name)` or `algorithm.Named(name)`. Named wrappers forward the child's executor and use the ordinary child resolution rules. They and their children remain separate selectable configurations. Repeated names select every matching configuration; they do not identify shared state or caller paths.

An `Aspect` stores ordered `(Pointcut, Advice)` pairs in `Advices`. `Aspect.Create(pointcut, advice)` creates one pair; derived constructors assign several pairs directly. Advice is a class with reference identity. Mutation and evaluation use `OperatorAdvice<TOperation, TCandidate>`. Its `CreateWrapperFactory` receives the selected configuration once; the returned factory binds wrappers through the operation adapter for each run signature. Other roles retain their nominal advice bases.

The role's resolution extension supplies a callback that casts to that role's advice base and calls its wrapper factory. The returned `WrapperExecutorFactory<TExecutor>` wraps an already resolved executor. Advice resolves no configured children and introduces no configuration nodes into selection. An explicitly configured wrapper remains a configuration and can be selected independently of its child.

A join point is the executor built for one selected configuration. A shared configuration can have several contextual bindings; it does not identify one caller path or invocation. Resolution prepares advice once per advice instance, selected configuration and owner state. Allocate persistent join-point data in `CreateWrapperFactory`, before returning the wrapper constructor. Algorithm observation uses this boundary for its iteration counter. Invocation-local data stays in the operation or iterator.

## Ordering and shared advice

The current scope's aspect layer precedes its parent's layers. Within each layer, aspect order and then pair order are application order. Earlier advice wraps closer to the raw executor; later advice wraps outside it. Entry order is reversed, while successful callbacks run in application order. Configured wrappers are already part of the raw graph.

Each advice instance applies once, at its first position in this chain. Two traces can include the same clock's advice without counting twice. A trace lists clock advice before its own observation, so the clock has advanced when it records an entry. Readding the clock later must not move that first position. If the same advice appears with two pointcuts, the first pairing wins without validation. Use separate advice instances to apply the same behavior at two distinct pointcuts; they may share a counter.

## Child activation policies

`ChildScopeSelector` belongs to a meta-algorithm's prepared state and selects a scope for each activation:

| Policy     | State for an activation                                                            |
| ---------- | ---------------------------------------------------------------------------------- |
| `Fresh`    | A new child state, still reusing factories already owned by ancestors.             |
| `Retained` | Child state kept by child configuration reference across activations and bindings. |
| `Shared`   | The parent's state.                                                                |

Retained selection uses the internal `CreateChildState` and `WithState` operations. The retained state stores no aspects; each activation uses its current scope's aspect chain. Advice factories remain in that retained state, so their counters and other prepared data survive. Cycle exercises these policies without another public scope API.

## Controls, failures and lifetime

Control projection reads the raw executor after the entire advised chain succeeds. Invoke operations through the returned advised executor. Advice need not implement the source's control interfaces. A configured wrapper exposes its own controls; projection does not search its children.

The generic path returns both the advised `Executor` and its explicit `ControlProvider` of type `IExecutorControlProvider`. Transparent forwarding and inspection hooks preserve that provider. A replacement chooses its own controls or `EmptyExecutorControlProvider`; controls are queried only after the whole chain succeeds.

Mutation and evaluation naming, retry and inspection forward the child's control provider. A composite supplies its own controls; the resolver does not infer them from arbitrary children. Remaining nominal roles retain their existing projection behavior.

Failed preparation or construction is not stored as a successful result and can be retried. Completed factories remain prepared when a later contextual binding fails. Recursive construction is detected across a root state and all its children. `TryResolve` converts `InvalidOperationException` to a reason; other exceptions propagate.

Advice factories are weakly keyed by advice in the owning state. A long-lived aspect does not retain discarded fresh states, and a long-lived state does not retain discarded advice. A live deferred execution can retain its construction scope and the state and aspects it still needs. Resolution itself promises neither disposal nor concurrent access.

## Inspection and budgets

Predefined observation, counting and timing advice lives in `HEAL.HeuristicLib.Inspection`. Inspection preserves the observed computation while updating its own collected data. General advice may change behavior. Counting updates after a successful call. Timing records duration in `finally`, including failed calls. The two concerns use standalone advice rather than allocating observations for every count.

Operator budgets remain configurations. During preparation they allocate an accumulator and use their counting or timing value strategy to create advice. Their child binding adds that aspect with `scope.With(aspect)` before resolving the inner algorithm. The budget therefore measures calls made through that child context. Resolving the same algorithm directly shares its prepared state but does not add the budget's advice. An observed operator absent from the algorithm matches nothing, so that budget does not stop it.

A budget's local layer wraps closer to the operator than inherited analysis. This keeps observer work outside the measured operation. Among aspects in the same layer, order is explicit; add timing before an observation when its callback should remain outside the measurement.
