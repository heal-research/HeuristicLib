# Execution resolution

Resolution selects persistent state by configuration reference and binds typed executors with the requesting scope's aspects. Operators build their executors in a `CreateExecutor` hook that receives a construction scope. Algorithms prepare factories without a scope, and the returned factories receive one. Both resolve their declared children through that scope.

See [configuration vs executors](/contributing/architecture/execution-nodes) for authoring responsibilities.

## State and construction

`ResolutionScope.Create(aspects)` creates an independent root. `CreateChildScope(aspects)` creates child-local state and inherits the parent's aspects. `With(aspects)` shares state and adds another aspect layer. All three accept zero or several aspects in supplied order.

A scope stores its own aspects and its parent scope. Its internal `State` stores, by configuration reference, the configurations it owns with their execution state or an algorithm's prepared factory, plus a parent state and advice factories. Aspect inheritance and state ownership are independent: adding an observation changes the bound graph without resetting prepared state.

For each resolution:

1. Find the configuration's owner in the current state or its ancestors. Make the current state its owner if none exists; an algorithm prepares its factory there.
2. Build the executor with the owner's state and the requesting aspects, through an operator's `CreateExecutor` or an algorithm's factory. Resolve children in that construction scope.
3. Apply matching advice in scope order to the raw executor. Return the advised executor.

A configuration selected first in a child stays local; a later selection in the parent does not replace it. Sibling states cannot see each other's local execution state or factories. A parent-first selection is reused by descendants. Dependencies resolve in the owner's state, so a direct child-local selection does not redirect an ancestor composite's dependencies.

Resolution constructs nodes on every request. A stateless leaf may return itself; a stateful leaf and a composite build a fresh executor around the same execution state. Do not rely on advised node reference identity to identify a logical execution.

### Operation-parameterized operators

Each resolution of an `IOperator<TOperation, TCandidate>` calls its `CreateExecutor` with a typed construction scope for the requested operation and run types. Several operations and state views share the configuration's owner and its execution state. The configuration requests that state by passing itself, as in `scope.GetExecutionState(this, create)`; the scope creates it on the first request and rejects a configuration that is not being built. Nothing else is cached, so every resolution constructs its executor and children again. Resolution checks through the operation's `IsExecutor` that each built and each wrapped executor implements the operation's executor interface for the run types; a fixed-signature leaf whose authored executor type cannot serve a request fails that check after its hook.

The generic path and the algorithm path use one owner table, ancestry, aspect chain and configuration-reference cycle guard. Low-level factory callbacks cannot switch an already selected owner's preparation path. Every operation resolves through the generic path.

For the generic path, resolution calls the `Check` method of every matching advice before constructing the raw graph, so an unsupported metric fails before any child or execution state is built. After construction it calls each advice's `Wrap` in application order. `OperatorAdvice<TOperation, TCandidate>` applies only to its operation of a configuration implementing several operations. Unsupported advice produces an explicit diagnostic. Advice is not cached; a failed check, construction or wrap preserves execution state already created and can be retried.

Each operation implements `IOperation<TSelf>`, whose static members recognize its executor interface for the run types, wrap an executor and state whether its calls report candidate work; resolution and advice reach them through `OperationInfo`. Every operation's named method ends with the call's random number generator, search space and problem. `Wrap(executor, wrapper)` returns a `WrappingExecutor<TExecutor, TWrapper>` whose named method passes each call to a struct `IInvocationWrapper` as the operation's private `Invocation` struct, an `IInvocation<TResult>` holding the wrapped executor and the call's arguments. The wrapper reads the call's `Random`, `SearchSpace` and `Problem`, calls `Proceed()` to run the wrapped method, possibly several times, or `Proceed(executor, random)` to run it on another executor of the same operation or with another random number generator, and calls `CountCandidateWork(result)` when it measures candidate work. A generic wrapper reads the search space and problem untyped and cannot replace them: it does not know the run's types, and typed role advice already receives every argument. Each layer is one executor compiled for its wrapper type, without delegates or argument tuples. `Named` returns its child's executor through `scope.ResolveOperator(ChildOperator)`; `Retry` and external configured wrappers use `TOperation.Wrap(scope.ResolveOperator(ChildOperator), wrapper)`. `ChoosePerCall` resolves all its children on each construction and wraps the first with a wrapper that samples a child per call and proceeds on it. `ChoosePerItem` resolves its children the same way and returns the executor of its operation's `Route(children)`, passing a `WeightedRouter<IExecutor>` over them. It accepts only operations marked with `IItemOperation<TSelf>`, for which routing is a capability of the operation: its `RoutingExecutor<TExecutor>` lets the router assign a call's items, runs each child's items as one call and restores their order. External wrappers and advice use the same contract without adding operation members.

Candidate work means returned counts for candidate-producing operations and input count for evaluation. An operation with candidate work overrides `SupportsCandidateWork` and implements `CountCandidateWork`. Termination, interception and scalar external operations implement neither: `SupportsCandidateWork` defaults to false and the invocation's default `CountCandidateWork` throws `NotSupportedException`. Candidate-counting advice calls `RequireCandidateWork` in its check and counts only after success; call counting and timing never read candidate work. Wrappers use ordinary `try`/`finally`: duration measurement records failed calls, and an exception thrown while recording replaces the child's. Algorithms retain their separate streaming advice.

## Pointcuts, advice and join points

A `Pointcut<TConfiguration>` examines a configuration as it is resolved. `Pointcut.Reference(source)` uses reference identity; `Pointcut.OfType<TConfiguration>()` includes implementations and derived types. `Pointcut.Predicate<TConfiguration>(predicate)` supplies runtime-only property or attribute matching; custom selection can also derive a small class overriding `Matches`. Predicates examine configuration data, run on each query and propagate exceptions. Pointcuts do not traverse the graph.

`And` and `Or`, also spelled `&` and `|`, combine pointcuts and short-circuit from left to right. `Not()` and `!` complement a selection within its declared configuration type: negating a mutator pointcut still excludes every other role.

`Pointcut.Named<TConfiguration>(name)` matches `INamedConfiguration.Name` ordinally and case-sensitively. Concrete configurations can implement that contract, or users can configure a role-preserving wrapper through `operator.Named(name)` or `algorithm.Named(name)`. Named wrappers forward the child's executor and use the ordinary child resolution rules. They and their children remain separate selectable configurations. Repeated names select every matching configuration; they do not identify shared state or caller paths.

An `Aspect` stores ordered `(Pointcut, Advice)` pairs in `Advices`. `Aspect.Create(pointcut, advice)` creates one pair; derived constructors assign several pairs directly. Advice is a class with reference identity. Operators using `IOperator<TOperation, TCandidate>` accept `OperatorAdvice`, which wraps every operation of its selected operators through the operation adapter, and `OperatorAdvice<TOperation, TCandidate>`, which wraps one operation. Role advice bases such as `MutatorAdvice<TCandidate>` and `EvaluatorAdvice<TCandidate>` derive from the latter and receive typed executors. Every other operation has a corresponding typed `Check`/`Wrap` base; the one for termination also receives the run's search state type, and `InterceptorAdvice<TCandidate, TSearchState>` names the state its interceptors return.

Advice resolves no configured children and introduces no configuration nodes into selection. An explicitly configured wrapper remains a configuration and can be selected independently of its child.

A join point is the executor built for one selected configuration. A shared configuration can have several contextual bindings; it does not identify one caller path or invocation. Algorithm advice is prepared once per advice instance, selected configuration and owner state; allocate persistent join-point data in its `CreateWrapperFactory`, before returning the wrapper constructor. Algorithm observation uses this boundary for its iteration counter. Operator advice keeps no data per join point: it wraps each executor built for a selected operator, and data that must outlive an executor belongs to the advice object, shared by every operator and resolution it applies to. Invocation-local data stays in the operation or iterator.

## Ordering and shared advice

The current scope's aspect layer precedes its parent's layers. Within each layer, aspect order and then pair order are application order. Earlier advice wraps closer to the raw executor; later advice wraps outside it. Entry order is reversed, while successful callbacks run in application order. Configured wrappers are already part of the raw graph.

Each advice instance applies once, at its first position in this chain. Two traces can include the same clock's advice without counting twice. A trace lists clock advice before its own observation, so the clock has advanced when it records an entry. Readding the clock later must not move that first position. If the same advice appears with two pointcuts, the first pairing wins without validation. Use separate advice instances to apply the same behavior at two distinct pointcuts; they may share a counter.

## Child activation policies

`ChildScopeSelector` belongs to a meta-algorithm's prepared state and selects a scope for each activation:

| Policy     | State for an activation                                                            |
| ---------- | ---------------------------------------------------------------------------------- |
| `Fresh`    | A new child state, still reusing execution state and factories owned by ancestors. |
| `Retained` | Child state kept by child configuration reference across activations and bindings. |
| `Shared`   | The parent's state.                                                                |

Retained selection uses the internal `CreateChildState` and `WithState` operations. The retained state stores no aspects; each activation uses its current scope's aspect chain. Operator execution state, prepared algorithms and algorithm advice factories remain in that retained state, so their counters and other prepared data survive. Cycle exercises these policies without another public scope API.

## Controls, failures and lifetime

Control projection reads the raw executor after the entire advised chain succeeds. Invoke operations through the returned advised executor. Advice need not implement the source's control interfaces. A configured wrapper exposes its own controls; projection does not search its children.

On the generic path an executor is its own control unless it implements `IExecutorControlProvider`. `ExecutorControls.Find` follows that forwarding from the advised executor once the whole chain has been built. A wrapping executor built by an operation's `Wrap` forwards the wrapped executor's controls through its `WrappingExecutor<TExecutor, TWrapper>` base unless its invocation wrapper implements `IExecutorControlProvider`, which then supplies them; `ChoosePerCall`'s wrapper supplies none, and `ChoosePerItem`'s routed executor exposes none of its children's. `Named`, `Retry` and inspection therefore forward. A replacement implements the controls it offers, or none.

Operation-parameterized naming, retry and inspection therefore expose the child's controls. A composite supplies its own controls; the resolver does not infer them from arbitrary children.

Failed preparation or construction is not stored as a successful result and can be retried. Created execution state and completed factories remain when a later contextual binding fails. Recursive construction is detected across a root state and all its children. `TryResolve` converts `InvalidOperationException` to a reason; other exceptions propagate.

Algorithm advice factories are weakly keyed by advice in the owning state; operator advice is not retained by resolution at all. A long-lived aspect does not retain discarded fresh states, and a long-lived state does not retain discarded advice. A live deferred execution can retain its construction scope and the state and aspects it still needs. Resolution itself promises neither disposal nor concurrent access.

## Inspection and budgets

Predefined observation, counting and timing advice lives in `HEAL.HeuristicLib.Inspection`. Inspection preserves the observed computation while updating its own collected data. General advice may change behavior. Counting updates after a successful call. Timing records duration in `finally`, including failed calls. The two concerns use standalone advice rather than allocating observations for every count.

Operator budgets remain configurations. During preparation they allocate an accumulator and use their counting or timing value strategy to create advice. Their child binding adds that aspect with `scope.With(aspect)` before resolving the inner algorithm. The budget therefore measures calls made through that child context. Resolving the same algorithm directly shares its prepared state but does not add the budget's advice. An observed operator absent from the algorithm matches nothing, so that budget does not stop it.

A budget's local layer wraps closer to the operator than inherited analysis. This keeps observer work outside the measured operation. Among aspects in the same layer, order is explicit; add timing before an observation when its callback should remain outside the timed span.
