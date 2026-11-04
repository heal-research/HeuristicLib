# Execution resolution

Resolution constructs typed executors with the requesting context's aspects and resources. Each configuration resolves its declared children through the scope received by its `CreateExecutor` hook.

See [configuration vs executors](/contributing/architecture/execution-nodes) for authoring responsibilities.

## Resources and construction

`ResolutionScope.Create(aspects)` creates an independent root with one flat resource table. `With(aspects)` adds an aspect layer and uses the same table. Both accept zero or several aspects in supplied order. Aspect ancestry controls advice order only. `Sharing(configurations)` adds a sharing layer, described under [shared configurations](#shared-configurations).

For each resolution:

1. Check the requested operation and run signature.
2. Build an executor through the configuration's `CreateExecutor`, resolving contextual children through the supplied scope, unless a sharing layer already holds one for the configuration.
3. Apply matching advice in context order and return the advised executor.

Private fields are fresh on each construction, even for two resolutions of the same configuration in one context. The resolver keeps no executors except those of shared configurations. Stateless leaves may return an immutable executor repeatedly.

### Shared configurations

A sharing layer holds one executor slot for each configuration it shares, keyed by configuration reference. Resolution walks the same layer chain that it walks for advice. The nearest layer sharing the configuration supplies its executor, which that layer constructs on the first request with its own children and the advice visible from that layer. Every request then applies its own matching advice around the shared executor, exactly as it would around a fresh one, so sharing changes how many executors exist but not which advice wraps them or in which order. Advice installed below the sharing point therefore wraps the shared configuration itself but does not reach the children inside it.

`SharingAlgorithm` and `SharingOperator` add such a layer for their child, through `algorithm.Sharing(configurations)` and `source.Sharing(configurations)`. Every construction of the sharing node creates a new layer, so shared executors live as long as the sharing node's executor: a Cycle turn or another run that constructs it again starts with new shared executors. A layer shares one executor per configuration. A later request for other run types, such as another search state, is rejected rather than given a second executor. Equal but distinct configurations are separate keys. Configurations that are listed but never requested construct nothing.

`scope.Resource(handle)` obtains one resource per handle reference in the run. Creation settings belong to the handle and its factory receives no requesting context. Different handles of one type stay distinct. Record copies preserve handles and handle reference identity participates in configuration equality.

`ResolutionScopeBuilder` collects supplied resources and aspects. `Build()` snapshots that setup into a scope, and later builder changes cannot alter existing scopes. Repeated builds have independent default resources but retain supplied instances. Duplicate handle bindings are rejected. Scopes expose only resolution and resource lookup, with no supply phase or sealing transition.

`CreateRun(problem, random, setup => ...)` collects resources and aspects through `AlgorithmRunSetup`. The callback runs once and its data is snapshotted into a scope before the run is returned. Capturing and later changing the setup object cannot affect the run. Duplicate resource bindings are rejected during setup. `AlgorithmRun` exposes execution operations and starts in `Ready`. Graph resolution and default resource creation remain lazy. Experiment setup uses `ExperimentRunSetup.AddResourcePerTrial` in materialized case order, validates every binding and then installs them. A callback or validation failure installs no bindings from that call. No trial runs are created until the experiment setup callback completes. Callback side effects remain the caller's responsibility.

Default creation is lazy and serialized for publication. A failed factory publishes nothing and a direct lookup can retry. A resource successfully created before a later executor or advice failure remains bound. A separate construction guard detects recursive configuration construction and is independent of resource storage.

`ResourceHandle<TResource>` is abstract and exposes one creation hook. `SuppliedResourceHandle<TResource>` requires an explicit binding and reports missing supply on lookup. Resource cleanup requirements are an authoring contract rather than a runtime check for disposal interfaces.

### Operation-parameterized operators

Each resolution of an `IOperator<TOperation, TCandidate>` calls its `CreateExecutor` with a typed construction scope for the operation and run types. Separate operation views construct independent private fields. Explicit handles can share domain resources between those views. Resolution checks through the operation's `IsExecutor` that each built and wrapped executor implements the requested operation interface.

Operators, algorithms and consumer defined roles use the same run resources, aspect chain and configuration reference construction guard. Every operation resolves through the generic path.

For the generic path, resolution calls the `Check` method of every matching advice before constructing the raw graph, so an unsupported metric fails before any child or resource is constructed. After construction it calls each advice's `Wrap` in application order. `OperatorAdvice<TOperation, TCandidate>` applies only to its operation of a configuration implementing several operations. Unsupported advice produces an explicit diagnostic. Advice is not cached; a failed construction or wrap retains published resources and can be retried.

Each operation implements `IOperation<TSelf>`, whose static members recognize its executor interface for the run types, wrap an executor and state whether its calls report candidate work; resolution and advice reach them through `OperationInfo`. Every operation's named method ends with the call's random number generator, search space and problem. `Wrap(executor, wrapper)` returns a `WrappingExecutor<TExecutor, TWrapper>` whose named method passes each call to a struct `IInvocationWrapper` as the operation's private `Invocation` struct, an `IInvocation<TResult>` holding the wrapped executor and the call's arguments. The wrapper reads the call's `Random`, `SearchSpace` and `Problem`, calls `Proceed()` to run the wrapped method, possibly several times, or `Proceed(executor, random)` to run it on another executor of the same operation or with another random number generator, and calls `CountCandidateWork(result)` when it measures candidate work. A generic wrapper reads the search space and problem untyped and cannot replace them: it does not know the run's types, and typed role advice already receives every argument. Each layer is one executor compiled for its wrapper type, without delegates or argument tuples. `Named` returns its child's executor through `scope.ResolveOperator(ChildOperator)`; `Retry` and external configured wrappers use `TOperation.Wrap(scope.ResolveOperator(ChildOperator), wrapper)`. `ChoosePerInvocation` resolves all its children on each construction and wraps the first with a wrapper that samples a child per call and proceeds on it. `ChoosePerItem` resolves its children the same way and returns the executor of its operation's `Route(children)`, passing a `WeightedRouter<IExecutor>` over them. It accepts only operations marked with `IItemOperation<TSelf>`, for which routing is a capability of the operation: its `RoutingExecutor<TExecutor>` lets the router assign a call's items, runs each child's items as one call and restores their order. External wrappers and advice use the same contract without adding operation members.

Candidate work means returned counts for candidate-producing operations and input count for evaluation. An operation with candidate work overrides `SupportsCandidateWork` and implements `CountCandidateWork`. Termination, interception and scalar external operations implement neither: `SupportsCandidateWork` defaults to false and the invocation's default `CountCandidateWork` throws `NotSupportedException`. Candidate-counting advice calls `RequireCandidateWork` in its check and counts only after success; call counting and timing never read candidate work. Wrappers use ordinary `try`/`finally`: duration measurement records failed calls, and an exception thrown while recording replaces the child's. Algorithms retain their separate streaming advice.

### Algorithms and consumer-defined roles

Each resolution of an algorithm calls its `CreateExecutor` with a scope typed to the run types and the requested search state. A configuration that does not produce that state is rejected before it is built, and an algorithm bound to a search space and problem rejects other run types in its base before its hook runs. Every matching `AlgorithmAdvice<TCandidate, TSearchState>` then wraps the built executor; advice for another search state of a configuration producing several is skipped.

A consumer-defined role is an `IConfigurationNode<TExecutor>` resolved through `ResolutionScope.Resolve`. Its advice derives from `Advice<TConfiguration, TExecutor>` and wraps every executor built for a selected configuration. Resolution knows the role only by its executor type, so it reaches the advice through an internal entry on the root `Advice` that takes the executor and configuration untyped; `Advice<TConfiguration, TExecutor>` implements it by passing both to the role's `Wrap`. `Aspect.Create` rejects advice for another role at compile time. Matching advice that cannot wrap the requested executor, such as advice for another executor type of the same configuration, is reported rather than cast.

### Observation points

A configuration declares a named observation point for progress that no invocation or iteration shows. `ObservationPoint.Of<TObservation>(this, nameof(Point))` returns one point per configuration reference and name, held in an internal weak table. The point implements `IConfigurationNode<IObservationPointExecutor<TObservation>>` and is also an observation source. During its build, the configuration resolves its own point through `scope.Resolve(Point)` and passes the executor to its parent executor, which calls `Report(observation)` where it reaches the point. Ordinary role advice wraps `Report`; the resolver needs no observation-specific path. Advice order and reference deduplication preserve clock-before-observation ordering and shared clocks. `CycleAlgorithm` reports `CycleStart` and `CycleTurn` this way. Unobserved points resolve to an immutable executor that ignores reports.

## Pointcuts, advice and join points

A `Pointcut<TConfiguration>` examines a configuration as it is resolved. `Pointcut.Reference(source)` uses reference identity; `Pointcut.OfType<TConfiguration>()` includes implementations and derived types. `Pointcut.Predicate<TConfiguration>(predicate)` supplies runtime-only property or attribute matching; custom selection can also derive a small class overriding `Matches`. Predicates examine configuration data, run on each query and propagate exceptions. Pointcuts do not traverse the graph.

`And` and `Or`, also spelled `&` and `|`, combine pointcuts and short-circuit from left to right. `Not()` and `!` complement a selection within its declared configuration type: negating a mutator pointcut still excludes every other role.

`Pointcut.Named<TConfiguration>(name)` matches `INamedConfiguration.Name` ordinally and case-sensitively. Concrete configurations can implement that contract, or users can configure a role-preserving wrapper through `operator.Named(name)` or `algorithm.Named(name)`. Named wrappers forward the child's executor and use the ordinary child resolution rules. They and their children remain separate selectable configurations. Repeated names select every matching configuration; they do not identify shared state or caller paths.

An `Aspect` stores ordered `(Pointcut, Advice)` pairs in `Advices`. `Aspect.Create(pointcut, advice)` creates one pair; derived constructors assign several pairs directly. Advice is a class with reference identity. Operators using `IOperator<TOperation, TCandidate>` accept `OperatorAdvice`, which wraps every operation of its selected operators through the operation adapter, and `OperatorAdvice<TOperation, TCandidate>`, which wraps one operation. Role advice bases such as `MutatorAdvice<TCandidate>` and `EvaluatorAdvice<TCandidate>` derive from the latter and receive typed executors. Every other operation has a corresponding typed `Check`/`Wrap` base; the one for termination also receives the run's search state type, and `InterceptorAdvice<TCandidate, TSearchState>` names the state its interceptors return.

Advice receives concrete runtime dependencies through its constructor and passes them to wrapping executors. Assemble resource dependent advice for each run. Supply the same instance under a handle when configurations must also resolve it or callers need `run.Resource(handle)` inspection. Advice has no resource lookup or supply capability. Advice resolves no configured children and introduces no configuration nodes into selection. An explicitly configured wrapper remains a configuration and can be selected independently of its child.

A join point is the executor built for one selected configuration. A shared configuration can have several contextual bindings and does not identify one caller path or invocation. Resolution applies advice separately for every request, so a shared configuration receives one advice wrapper per request around its single executor. Advice wrappers therefore keep no private data; data that must accumulate belongs to the advice or to an object it receives, such as a `CountAccumulator`. Runtime data held by the advice object is intentionally shared wherever that advice instance is reused. Handle based resources instead follow the current run lifetime, so reusing one counting aspect across trials preserves independent default counters. Invocation local values stay in the operation or iterator.

## Ordering and shared advice

The current scope's aspect layer precedes its parent's layers. Within each layer, aspect order and then pair order are application order. Earlier advice wraps closer to the raw executor; later advice wraps outside it. Entry order is reversed, while successful callbacks run in application order. Configured wrappers are already part of the raw graph.

Each advice instance applies once, at its first position in this chain. Two traces can include the same clock's advice without counting twice. A trace lists clock advice before its own observation, so the clock has advanced when it records an entry. Readding the clock later must not move that first position. If the same advice appears with two pointcuts, the first pairing wins without validation. Use separate advice instances to apply the same behavior at two distinct pointcuts; they may share a counter.

## Deferred construction

Cycle resolves fresh children for every turn while forwarding search state. Sharing a child configuration on the Cycle, `cycle.Sharing(child)`, keeps one executor for that child across turns, including its private fields and children; each turn still applies the Cycle's current advice around it. A separately resolved Cycle has its own shared executors.

Pipeline stages and dynamic racing contenders resolve fresh children through their current context. Every deferred construction in the run sees the same resource table. Nested cycles do not introduce nested resource lifetimes. Pause/resume continues the existing iterator and executors with their resource bindings.

## Controls, failures and lifetime

An executor is its own control unless it implements `IExecutorControlProvider`. `ExecutorControls.Find` follows that forwarding from the advised executor, which callers receive once the whole chain has been built; invoke operations through that executor. A transparent advice wrapper forwards the controls of the executor it wraps. A wrapping executor built by an operation's `Wrap` forwards the wrapped executor's controls through its `WrappingExecutor<TExecutor, TWrapper>` base unless its invocation wrapper implements `IExecutorControlProvider`, which then supplies them; `ChoosePerInvocation`'s wrapper supplies none, and `ChoosePerItem`'s routed executor exposes none of its children's. `Named`, `Retry` and inspection therefore forward. A replacement implements the controls it offers, or none.

Operation-parameterized naming, retry and inspection therefore expose the child's controls. A composite supplies its own controls; the resolver does not infer them from arbitrary children.

A failed construction or wrap can be retried through a direct scope. The next attempt constructs new private data while retaining resources already published in that run. `TryResolve` converts `InvalidOperationException` to a reason and lets other exceptions propagate.

Resources live with their run or direct root scope. A live deferred executor may retain its context and resources. Discarded executors are not retained by the resolver. Automatic resource creation is limited to resources needing no deterministic cleanup. Supplied resources remain caller owned and are never disposed by resolution or run completion. Resource table safety does not synchronize execution or resource use.

## Inspection and budgets

Predefined observation, counting and timing advice lives in `HEAL.HeuristicLib.Inspection`. Inspection preserves the observed computation while updating its own collected data. General advice may change behavior. Counting updates after a successful call. Timing records duration in `finally`, including failed calls. The two concerns use standalone advice rather than allocating observations for every count.

Operator budgets remain configurations. Each construction creates a private accumulator and the counting or timing advice that feeds it, adds that advice with `scope.With(aspect)` and resolves the inner algorithm. Calls through that child context contribute to the budget. The single operator variants select one inferred operation. The operator list variants select all operations of every listed configuration reference with one union pointcut, so duplicate references do not duplicate accounting. Their constructors and extensions use the same configuration. Directly resolving the child algorithm creates independent private data and does not add budget advice. An absent observed operator matches nothing.

`LimitEvaluator` accounting is private to its executor. Sharing the limited evaluator lets several uses draw from one allowance. Atomic accumulation does not turn separate checking, evaluation and accounting into a strict concurrent budget. Such reservation semantics remain outside this contract.

A budget's local layer wraps closer to the operator than inherited analysis. This keeps observer work outside the measured operation. Among aspects in the same layer, order is explicit; add timing before an observation when its callback should remain outside the timed span.
