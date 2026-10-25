# Execution resolution

Resolution selects scoped state by configuration reference and builds typed executors with the requesting scope's aspects. Every configuration builds its executor in a `CreateExecutor` hook that receives a scope, and resolves its declared children through that scope.

See [configuration vs executors](/contributing/architecture/execution-nodes) for authoring responsibilities.

## State and construction

`ResolutionScope.Create(aspects)` creates an independent root. `CreateChildScope(aspects)` creates child-local state and inherits the parent's aspects. `With(aspects)` shares state and adds another aspect layer. All three accept zero or several aspects in supplied order.

A scope stores its own aspects and its parent scope. Its internal `State` stores, by configuration reference, the configurations it owns with their scoped state, plus a parent state. Aspect inheritance and state ownership are independent: adding an observation changes the built executors without resetting scoped state.

For each resolution:

1. Find the configuration's owner in the current state or its ancestors. Make the current state its owner if none exists.
2. Build the executor through the configuration's `CreateExecutor`, with the owner's state and the requesting aspects. The hook resolves its children and requests its scoped state in that scope.
3. Apply matching advice in scope order to the built executor. Return the advised executor.

A configuration selected first in a child stays local; a later selection in the parent does not replace it. Sibling states cannot see each other's local scoped state. A parent-first selection is reused by descendants. Dependencies resolve in the owner's state, so a direct child-local selection does not redirect an ancestor composite's dependencies.

Resolution constructs nodes on every request. A stateless leaf may return itself; a stateful leaf and a composite build a fresh executor around the same scoped state. Do not rely on advised node reference identity to identify a logical execution.

### Operation-parameterized operators

Each resolution of an `IOperator<TOperation, TCandidate>` calls its `CreateExecutor` with a typed construction scope for the requested operation and run types. Several operations and state views share the configuration's owner and its scoped state. The configuration requests that state by passing itself, as in `scope.GetScopedState(this, create)`; the scope creates it on the first request and rejects a configuration that is not being built. Nothing else is cached, so every resolution constructs its executor and children again. Resolution checks through the operation's `IsExecutor` that each built and each wrapped executor implements the operation's executor interface for the run types; a fixed-signature leaf whose authored executor type cannot serve a request fails that check after its hook.

Operators, algorithms and consumer-defined roles use one owner table, ancestry, aspect chain and configuration-reference cycle guard. A configuration implementing several roles has one owner and one scoped state. Every operation resolves through the generic path.

For the generic path, resolution calls the `Check` method of every matching advice before constructing the raw graph, so an unsupported metric fails before any child or scoped state is built. After construction it calls each advice's `Wrap` in application order. `OperatorAdvice<TOperation, TCandidate>` applies only to its operation of a configuration implementing several operations. Unsupported advice produces an explicit diagnostic. Advice is not cached; a failed check, construction or wrap preserves scoped state already created and can be retried.

Each operation implements `IOperation<TSelf>`, whose static members recognize its executor interface for the run types, wrap an executor and state whether its calls report candidate work; resolution and advice reach them through `OperationInfo`. Every operation's named method ends with the call's random number generator, search space and problem. `Wrap(executor, wrapper)` returns a `WrappingExecutor<TExecutor, TWrapper>` whose named method passes each call to a struct `IInvocationWrapper` as the operation's private `Invocation` struct, an `IInvocation<TResult>` holding the wrapped executor and the call's arguments. The wrapper reads the call's `Random`, `SearchSpace` and `Problem`, calls `Proceed()` to run the wrapped method, possibly several times, or `Proceed(executor, random)` to run it on another executor of the same operation or with another random number generator, and calls `CountCandidateWork(result)` when it measures candidate work. A generic wrapper reads the search space and problem untyped and cannot replace them: it does not know the run's types, and typed role advice already receives every argument. Each layer is one executor compiled for its wrapper type, without delegates or argument tuples. `Named` returns its child's executor through `scope.ResolveOperator(ChildOperator)`; `Retry` and external configured wrappers use `TOperation.Wrap(scope.ResolveOperator(ChildOperator), wrapper)`. `ChoosePerCall` resolves all its children on each construction and wraps the first with a wrapper that samples a child per call and proceeds on it. `ChoosePerItem` resolves its children the same way and returns the executor of its operation's `Route(children)`, passing a `WeightedRouter<IExecutor>` over them. It accepts only operations marked with `IItemOperation<TSelf>`, for which routing is a capability of the operation: its `RoutingExecutor<TExecutor>` lets the router assign a call's items, runs each child's items as one call and restores their order. External wrappers and advice use the same contract without adding operation members.

Candidate work means returned counts for candidate-producing operations and input count for evaluation. An operation with candidate work overrides `SupportsCandidateWork` and implements `CountCandidateWork`. Termination, interception and scalar external operations implement neither: `SupportsCandidateWork` defaults to false and the invocation's default `CountCandidateWork` throws `NotSupportedException`. Candidate-counting advice calls `RequireCandidateWork` in its check and counts only after success; call counting and timing never read candidate work. Wrappers use ordinary `try`/`finally`: duration measurement records failed calls, and an exception thrown while recording replaces the child's. Algorithms retain their separate streaming advice.

### Algorithms and consumer-defined roles

Each resolution of an algorithm calls its `CreateExecutor` with a scope typed to the run types and the requested search state. A configuration that does not produce that state is rejected before it is built, and an algorithm bound to a search space and problem rejects other run types in its base before its hook runs. Every matching `AlgorithmAdvice<TCandidate, TSearchState>` then wraps the built executor; advice for another search state of a configuration producing several is skipped.

A consumer-defined role is an `IConfigurationNode<TExecutor>` resolved through `ResolutionScope.Resolve`. Its advice derives from `Advice<TConfiguration, TExecutor>` and wraps every executor built for a selected configuration. Resolution knows the role only by its executor type, so it reaches the advice through an internal entry on the root `Advice` that takes the executor and configuration untyped; `Advice<TConfiguration, TExecutor>` implements it by passing both to the role's `Wrap`. `Aspect.Create` rejects advice for another role at compile time. Matching advice that cannot wrap the requested executor, such as advice for another executor type of the same configuration, is reported rather than cast.

## Pointcuts, advice and join points

A `Pointcut<TConfiguration>` examines a configuration as it is resolved. `Pointcut.Reference(source)` uses reference identity; `Pointcut.OfType<TConfiguration>()` includes implementations and derived types. `Pointcut.Predicate<TConfiguration>(predicate)` supplies runtime-only property or attribute matching; custom selection can also derive a small class overriding `Matches`. Predicates examine configuration data, run on each query and propagate exceptions. Pointcuts do not traverse the graph.

`And` and `Or`, also spelled `&` and `|`, combine pointcuts and short-circuit from left to right. `Not()` and `!` complement a selection within its declared configuration type: negating a mutator pointcut still excludes every other role.

`Pointcut.Named<TConfiguration>(name)` matches `INamedConfiguration.Name` ordinally and case-sensitively. Concrete configurations can implement that contract, or users can configure a role-preserving wrapper through `operator.Named(name)` or `algorithm.Named(name)`. Named wrappers forward the child's executor and use the ordinary child resolution rules. They and their children remain separate selectable configurations. Repeated names select every matching configuration; they do not identify shared state or caller paths.

An `Aspect` stores ordered `(Pointcut, Advice)` pairs in `Advices`. `Aspect.Create(pointcut, advice)` creates one pair; derived constructors assign several pairs directly. Advice is a class with reference identity. Operators using `IOperator<TOperation, TCandidate>` accept `OperatorAdvice`, which wraps every operation of its selected operators through the operation adapter, and `OperatorAdvice<TOperation, TCandidate>`, which wraps one operation. Role advice bases such as `MutatorAdvice<TCandidate>` and `EvaluatorAdvice<TCandidate>` derive from the latter and receive typed executors. Every other operation has a corresponding typed `Check`/`Wrap` base; the one for termination also receives the run's search state type, and `InterceptorAdvice<TCandidate, TSearchState>` names the state its interceptors return.

Advice resolves no configured children and introduces no configuration nodes into selection. An explicitly configured wrapper remains a configuration and can be selected independently of its child.

A join point is the executor built for one selected configuration. A shared configuration can have several contextual bindings; it does not identify one caller path or invocation. Advice keeps no data per join point: it wraps each executor built for a selected configuration, and data that must outlive an executor belongs to the advice object, shared by every configuration and resolution it applies to. Algorithm observation keeps its iteration counter there, so the count continues across a meta-algorithm's fresh activations. Invocation-local data stays in the operation or iterator.

## Ordering and shared advice

The current scope's aspect layer precedes its parent's layers. Within each layer, aspect order and then pair order are application order. Earlier advice wraps closer to the raw executor; later advice wraps outside it. Entry order is reversed, while successful callbacks run in application order. Configured wrappers are already part of the raw graph.

Each advice instance applies once, at its first position in this chain. Two traces can include the same clock's advice without counting twice. A trace lists clock advice before its own observation, so the clock has advanced when it records an entry. Readding the clock later must not move that first position. If the same advice appears with two pointcuts, the first pairing wins without validation. Use separate advice instances to apply the same behavior at two distinct pointcuts; they may share a counter.

## Child activation policies

`ChildScopeSelector` is a meta-algorithm's scoped state, so every build in its owning scope, for any run type, shares it. It selects a scope for each activation:

| Policy     | State for an activation                                                          |
| ---------- | -------------------------------------------------------------------------------- |
| `Fresh`    | A new child state, still reusing scoped state owned by ancestors.                |
| `Retained` | Child state kept by child configuration reference across activations and builds. |
| `Shared`   | The parent's state.                                                              |

Retained selection uses the internal `CreateChildState` and `WithState` operations. The retained state stores no aspects; each activation uses its current scope's aspect chain. Scoped state in that retained state, such as an operator's counter, survives across activations. Cycle exercises these policies without another public scope API.

## Controls, failures and lifetime

An executor is its own control unless it implements `IExecutorControlProvider`. `ExecutorControls.Find` follows that forwarding from the advised executor, which callers receive once the whole chain has been built; invoke operations through that executor. A transparent advice wrapper forwards the controls of the executor it wraps. A wrapping executor built by an operation's `Wrap` forwards the wrapped executor's controls through its `WrappingExecutor<TExecutor, TWrapper>` base unless its invocation wrapper implements `IExecutorControlProvider`, which then supplies them; `ChoosePerCall`'s wrapper supplies none, and `ChoosePerItem`'s routed executor exposes none of its children's. `Named`, `Retry` and inspection therefore forward. A replacement implements the controls it offers, or none.

Operation-parameterized naming, retry and inspection therefore expose the child's controls. A composite supplies its own controls; the resolver does not infer them from arbitrary children.

A failed state creation, construction or wrap is not stored and can be retried. Scoped state already created remains when a later build fails. Recursive construction is detected across a root state and all its children. `TryResolve` converts `InvalidOperationException` to a reason; other exceptions propagate.

Resolution retains no advice. A long-lived aspect does not retain discarded fresh states, and a long-lived state does not retain discarded aspects or advice. A live deferred execution can retain its scope and the state and aspects it still needs. Resolution itself promises neither disposal nor concurrent access.

## Inspection and budgets

Predefined observation, counting and timing advice lives in `HEAL.HeuristicLib.Inspection`. Inspection preserves the observed computation while updating its own collected data. General advice may change behavior. Counting updates after a successful call. Timing records duration in `finally`, including failed calls. The two concerns use standalone advice rather than allocating observations for every count.

Operator budgets remain configurations. Their scoped state holds an accumulator and the counting or timing aspect that feeds it. Each build adds that aspect with `scope.With(aspect)` before resolving the inner algorithm. The budget therefore measures calls made through that child context. Resolving the same algorithm directly shares its scoped state but does not add the budget's advice. An observed operator absent from the algorithm matches nothing, so that budget does not stop it.

A budget's local layer wraps closer to the operator than inherited analysis. This keeps observer work outside the measured operation. Among aspects in the same layer, order is explicit; add timing before an observation when its callback should remain outside the timed span.
