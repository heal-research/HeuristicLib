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

`IOperator<TOperation, TCandidate>` prepares one `OperatorExecutorFactory` per selected configuration reference. Each resolution calls its `CreateExecutor` with a typed construction scope for the requested operation and run types; several operations and state views share the same prepared owner. Nothing below the owner is cached, so every resolution constructs its executor and children again. Resolution checks that the result implements the operation's executor interface. `OperatorExecutorFactory.FromFactory` also rejects a request its authored executor type cannot serve before invoking the construction function: compatible contravariant views share the authored executor, while an invariant mismatch constructs nothing.

The generic and legacy paths use one owner table, ancestry, aspect chain and configuration-reference cycle guard. A nominal role resolver adapts a configuration declaring the corresponding generic contract through `ResolveOperator`; it does not call a second preparation method. Low-level factory callbacks cannot switch an already selected owner's preparation path. Existing role contracts and authoring bases remain until their consumer migrations.

For the generic path, resolution calls the `Check` method of every matching advice before constructing the raw graph, so an unsupported metric or observation signature fails before any child is built. After construction it calls each advice's `Wrap` in application order. `OperatorAdvice<TOperation, TCandidate>` applies only to its operation of a configuration implementing several operations. Unsupported advice produces an explicit diagnostic. Advice is not cached; a failed check, construction or wrap preserves successful owner preparation and can be retried.

Each descriptor supplies an `OperationExecutorAdapter` for its executor interface and run types. Every operation's named method ends with the call's random number generator, search space and problem. The adapter's `WrapInvocation(executor, wrapper)` returns an executor whose named method passes each call to a struct `IInvocationWrapper` as a struct `IInvocation<TResult>` holding the wrapped executor and the call's arguments. The wrapper reads the call's `Random`, calls `Proceed()` to run the wrapped method, possibly several times, or `ProceedWith(executor, random)` to run it on another executor of the same operation or with another random number generator, and calls `CountCandidateWork(result)` when it measures candidate work. A generic wrapper cannot replace the search space or problem: it does not know the run's types, and typed role advice already receives every argument. Each layer is one executor compiled for its wrapper type, without delegates or argument tuples. `Named` returns its child's executor through `OperatorExecutorFactory.Forward`; `Retry` and external configured wrappers use `OperatorExecutorFactory.WrapInvocation(child, wrapper)`. `ChoosePerCall` resolves all its children on each construction and wraps the first with a wrapper that samples a child per call and proceeds on it. External wrappers and advice use the same contract without adding adapter members.

Candidate work means returned counts for candidate-producing operations and input count for evaluation. An operation with candidate work overrides `SupportsCandidateWork` and implements `CountCandidateWork`. Termination, interception and scalar external operations implement neither: the adapter reports false and the invocation's default `CountCandidateWork` throws `NotSupportedException`. Candidate-counting advice calls `RequireCandidateWork` in its check and counts only after success; call counting and timing never read candidate work. Wrappers use ordinary `try`/`finally`: duration measurement records failed calls, and an exception thrown while recording replaces the child's. Algorithms retain their separate streaming advice.

## Pointcuts, advice and join points

A `Pointcut<TConfiguration>` examines a configuration as it is resolved. `Pointcut.Reference(source)` uses reference identity; `Pointcut.OfType<TConfiguration>()` includes implementations and derived types. `Pointcut.Predicate<TConfiguration>(predicate)` supplies runtime-only property or attribute matching; custom selection can also derive a small class overriding `Matches`. Predicates examine configuration data, run on each query and propagate exceptions. Pointcuts do not traverse the graph.

`And` and `Or`, also spelled `&` and `|`, combine pointcuts and short-circuit from left to right. `Not()` and `!` complement a selection within its declared configuration type: negating a mutator pointcut still excludes every other role.

`Pointcut.Named<TConfiguration>(name)` matches `INamedConfiguration.Name` ordinally and case-sensitively. Concrete configurations can implement that contract, or users can configure a role-preserving wrapper through `operator.Named(name)` or `algorithm.Named(name)`. Named wrappers forward the child's executor and use the ordinary child resolution rules. They and their children remain separate selectable configurations. Repeated names select every matching configuration; they do not identify shared state or caller paths.

An `Aspect` stores ordered `(Pointcut, Advice)` pairs in `Advices`. `Aspect.Create(pointcut, advice)` creates one pair; derived constructors assign several pairs directly. Advice is a class with reference identity. Operators using `IOperator<TOperation, TCandidate>` accept `OperatorAdvice`, which wraps every operation of its selected operators through the operation adapter, and `OperatorAdvice<TOperation, TCandidate>`, which wraps one operation. Role advice bases such as `MutatorAdvice<TCandidate>` and `EvaluatorAdvice<TCandidate>` derive from the latter and receive typed executors. Other roles retain their nominal advice bases.

For those nominal roles, the role's resolution extension supplies a callback that casts to that role's advice base and calls its wrapper factory. The returned `WrapperExecutorFactory<TExecutor>` wraps an already resolved executor. Advice resolves no configured children and introduces no configuration nodes into selection. An explicitly configured wrapper remains a configuration and can be selected independently of its child.

A join point is the executor built for one selected configuration. A shared configuration can have several contextual bindings; it does not identify one caller path or invocation. Algorithm advice and nominal role advice are prepared once per advice instance, selected configuration and owner state; allocate persistent join-point data in their `CreateWrapperFactory`, before returning the wrapper constructor. Algorithm observation uses this boundary for its iteration counter. Operator advice keeps no data per join point: it wraps each executor built for a selected operator, and data that must outlive an executor belongs to the advice object, shared by every operator and resolution it applies to. Invocation-local data stays in the operation or iterator.

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

Retained selection uses the internal `CreateChildState` and `WithState` operations. The retained state stores no aspects; each activation uses its current scope's aspect chain. Prepared operators and algorithm advice factories remain in that retained state, so their counters and other prepared data survive. Cycle exercises these policies without another public scope API.

## Controls, failures and lifetime

Control projection reads the raw executor after the entire advised chain succeeds. Invoke operations through the returned advised executor. Advice need not implement the source's control interfaces. A configured wrapper exposes its own controls; projection does not search its children.

On the generic path an executor is its own control unless it implements `IExecutorControlProvider`. `ExecutorControls.Find` follows that forwarding from the advised executor once the whole chain has been built. A wrapping executor built by an operation adapter forwards the wrapped executor's controls unless its invocation wrapper implements `IExecutorControlProvider`, which then supplies them; `ChoosePerCall`'s wrapper supplies none. `Named`, `Retry` and inspection therefore forward. A replacement implements the controls it offers, or none.

Operation-parameterized naming, retry and inspection therefore expose the child's controls. A composite supplies its own controls; the resolver does not infer them from arbitrary children. Remaining nominal roles retain their existing projection behavior.

Failed preparation or construction is not stored as a successful result and can be retried. Completed factories remain prepared when a later contextual binding fails. Recursive construction is detected across a root state and all its children. `TryResolve` converts `InvalidOperationException` to a reason; other exceptions propagate.

Algorithm and nominal role advice factories are weakly keyed by advice in the owning state; operator advice is not retained by resolution at all. A long-lived aspect does not retain discarded fresh states, and a long-lived state does not retain discarded advice. A live deferred execution can retain its construction scope and the state and aspects it still needs. Resolution itself promises neither disposal nor concurrent access.

## Inspection and budgets

Predefined observation, counting and timing advice lives in `HEAL.HeuristicLib.Inspection`. Inspection preserves the observed computation while updating its own collected data. General advice may change behavior. Counting updates after a successful call. Timing records duration in `finally`, including failed calls. The two concerns use standalone advice rather than allocating observations for every count.

Operator budgets remain configurations. During preparation they allocate an accumulator and use their counting or timing value strategy to create advice. Their child binding adds that aspect with `scope.With(aspect)` before resolving the inner algorithm. The budget therefore measures calls made through that child context. Resolving the same algorithm directly shares its prepared state but does not add the budget's advice. An observed operator absent from the algorithm matches nothing, so that budget does not stop it.

A budget's local layer wraps closer to the operator than inherited analysis. This keeps observer work outside the measured operation. Among aspects in the same layer, order is explicit; add timing before an observation when its callback should remain outside the measurement.
