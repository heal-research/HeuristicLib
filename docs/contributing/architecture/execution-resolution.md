# Execution resolution

Resolution selects persistent state by configuration reference and binds typed execution nodes with the requesting scope's aspects. Configurations prepare factories without a scope; the returned factories receive a construction scope and resolve their declared children.

See [configuration vs execution nodes](/contributing/architecture/execution-nodes) for authoring responsibilities.

## State and construction

`ResolutionScope.Create(aspects)` creates an independent root. `CreateChildScope(aspects)` creates child-local state and inherits the parent's aspects. `With(aspects)` shares state and adds another aspect layer. All three accept zero or several aspects in supplied order.

A scope stores its own aspects and its parent scope. Its internal `State` stores prepared factories by configuration reference, a parent state and advice factories. Aspect inheritance and state ownership are independent: adding an observation changes the bound graph without resetting prepared state.

For each resolution:

1. Find the configuration's prepared factory in the current state or its ancestors. Prepare it locally if none exists.
2. Invoke that factory with the owner's state and the requesting aspects. Resolve children in that construction scope.
3. Apply matching advice in scope order to the raw execution. Return the advised execution.

A configuration selected first in a child stays local; a later selection in the parent does not replace it. Sibling states cannot see each other's local factories. A parent-first selection is reused by descendants. Dependencies resolve in the factory owner's state, so a direct child-local selection does not redirect an ancestor composite's dependencies.

Resolution constructs nodes on every request. A prepared stateless or stateful leaf may return its existing raw node. Composite factories can construct fresh bindings around the same prepared state. Do not rely on advised node reference identity to identify a logical execution.

## Pointcuts, advice and join points

A `Pointcut<TConfiguration>` examines a configuration as it is resolved. `Pointcut.Reference(source)` uses reference identity; `Pointcut.OfType<TConfiguration>()` includes implementations and derived types. `Pointcut.Predicate<TConfiguration>(predicate)` supplies runtime-only property or attribute matching; custom selection can also derive a small class overriding `Matches`. Predicates examine configuration data, run on each query and propagate exceptions. Pointcuts do not traverse the graph.

`And` and `Or`, also spelled `&` and `|`, combine pointcuts and short-circuit from left to right. `Not()` and `!` complement a selection within its declared configuration type: negating a mutator pointcut still excludes every other role.

`Pointcut.Named<TConfiguration>(name)` matches `INamedConfiguration.Name` ordinally and case-sensitively. Concrete configurations can implement that contract, or users can configure a role-preserving wrapper through `operator.Named(name)` or `algorithm.Named(name)`. Named wrappers forward the child's execution and use the ordinary child resolution rules. They and their children remain separate selectable configurations. Repeated names select every matching configuration; they do not identify shared state or caller paths.

An `Aspect` stores ordered `(Pointcut, Advice)` pairs in `Advices`. `Aspect.Create(pointcut, advice)` creates one pair; derived constructors assign several pairs directly. Advice is a class with reference identity. Each role has a base beside its interface, such as `MutatorAdvice<TCandidate>`, whose `CreateWrapperFactory` accepts the configuration selected by the pointcut and the run's type parameters.

The role's resolution extension supplies a callback that casts to that role's advice base and calls its wrapper factory. The returned `WrapperExecutionFactory<TExecution>` wraps an already resolved execution. Advice resolves no configured children and introduces no configuration nodes into selection. An explicitly configured wrapper remains a configuration and can be selected independently of its child.

A join point is the execution built for one selected configuration. A shared configuration can have several contextual bindings; it does not identify one caller path or invocation. Resolution prepares advice once per advice instance, selected configuration and owner state. Allocate persistent join-point data in `CreateWrapperFactory`, before returning the wrapper constructor. Algorithm observation uses this boundary for its iteration counter. Invocation-local data stays in the operation or iterator.

## Ordering and shared advice

The current scope's aspect layer precedes its parent's layers. Within each layer, aspect order and then pair order are application order. Earlier advice wraps closer to the raw execution; later advice wraps outside it. Entry order is reversed, while successful callbacks run in application order. Configured wrappers are already part of the raw graph.

Each advice instance applies once, at its first position in this chain. Two traces can include the same clock's advice without counting twice. A trace lists clock advice before its own observation, so the clock has advanced when it records an entry. Readding the clock later must not move that first position. If the same advice appears with two pointcuts, the first pairing wins without validation. Use separate advice instances to apply the same behavior at two distinct pointcuts; they may share a counter.

## Child activation policies

`ChildScopeSelector` belongs to a meta-algorithm's prepared state and selects a scope for each activation:

| Policy     | State for an activation                                                            |
| ---------- | ---------------------------------------------------------------------------------- |
| `Fresh`    | A new child state, still reusing factories already owned by ancestors.             |
| `Retained` | Child state kept by child configuration reference across activations and bindings. |
| `Shared`   | The parent's state.                                                                |

Retained selection uses the internal `CreateChildState` and `WithState` operations. The retained state stores no aspects; each activation uses its current scope's aspect chain. Advice factories remain in that retained state, so their counters and other prepared data survive. Cycle exercises these policies through this selector.

## Forking prepared state

`ExecutionFactory.Forkable(state, copyState, bind)` explicitly opts one prepared factory into state copying. Its copier returns independent mutable state; its binder receives that state and resolves children through the supplied construction scope. Neither the copier nor the binder may retain and use the original mutable state instead of their argument. Immutable settings and configuration references may remain shared. Copy ordinary mutable data only; resolve graph dependencies again in the binder. Copying does not resolve children or clone execution nodes.

`scope.CreateForkedScope()` creates a peer with snapshots of the scope's forkable local preparations. The snapshot is taken immediately, before the peer resolves anything. All copied preparations are installed before binding children, so a forkable descendant is found even when an intervening composite prepares an ordinary factory. Other local factories prepare afresh as needed. Ancestor state remains shared, and configuration references remain the lookup keys. A newly constructed, equal configuration does not inherit another configuration's snapshot.

The peer inherits the source observation context but prepares its own advice factories. External accumulators intentionally shared by aspects remain shared. Existing execution nodes keep their original state and observations. Fork between invocations: concurrent mutation is unsupported, and forking during graph construction throws `InvalidOperationException`. A failed copy leaves the source unchanged and publishes no partial peer; the copier itself must not mutate the source.

`retainedScope.WithContext(currentScope)` retains state ownership while replacing the observation context used by subsequent resolutions. It does not redirect an execution that has already been bound. A racing algorithm uses this to retain the winner's child state across races and contextual bindings; challengers receive forked peer state and newly bound children. Active iterators are disposed at race boundaries while their explicitly retained prepared state survives.

The consumer flow is exercised by `PreparedStateForkingSpecs` in the API usage suite. Resolver tests cover snapshots, fresh ordinary dependencies, ancestor sharing and contextual advice independently of the racing algorithm.

## Controls, failures and lifetime

Control projection reads the raw execution after the entire advised chain succeeds. Invoke operations through the returned advised execution. Advice need not implement the source's control interfaces. A configured wrapper exposes its own controls; projection does not search its children.

This leaves a known correctness bug: a configured wrapper can hide a child capability that its algorithm needs. Even a named forwarding wrapper can lose the child's control when child advice changes the forwarded execution type. See [Known capability loss](/guide/extending/operator-composition#known-capability-loss) for the effect on mutation-strength adaptation.

Failed preparation or construction is not stored as a successful result and can be retried. Completed factories remain prepared when a later contextual binding fails. Recursive construction is detected across a root state and all its children. `TryResolve` converts `InvalidOperationException` to a reason; other exceptions propagate.

Advice factories are weakly keyed by advice in the owning state. A long-lived aspect does not retain discarded fresh states, and a long-lived state does not retain discarded advice. A live deferred execution can retain its construction scope and the state and aspects it still needs. Resolution itself promises neither disposal nor concurrent access.

## Inspection and budgets

Predefined observation, counting and timing advice lives in `HEAL.HeuristicLib.Inspection`. Inspection preserves the observed computation while updating its own collected data. General advice may change behavior. Counting updates after a successful call. Timing records duration in `finally`, including failed calls. The two concerns use standalone advice rather than allocating observations for every count.

Operator budgets remain configurations. During preparation they allocate an accumulator and use their counting or timing value strategy to create advice. Their child binding adds that aspect with `scope.With(aspect)` before resolving the inner algorithm. The budget therefore measures calls made through that child context. Resolving the same algorithm directly shares its prepared state but does not add the budget's advice. An observed operator absent from the algorithm matches nothing, so that budget does not stop it.

A budget's local layer wraps closer to the operator than inherited analysis. This keeps observer work outside the measured operation. Among aspects in the same layer, order is explicit; add timing before an observation when its callback should remain outside the measurement.
