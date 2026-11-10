# Execution resolution

Resolution constructs typed executors with the requesting scope's advice and run-bound results. Each configuration resolves its declared children through the scope received by its `CreateExecutor` hook.

See [configuration vs executors](/contributing/architecture/execution-nodes) for authoring responsibilities.

## Results and construction

`ResolutionScope.Create()` creates an independent root with one run result table. Add aspects with `AddAspect` before resolving their targets, and share configurations with `Share` before child scopes use them. `CreateChildScope()` keeps the same run results and inherited advice while creating independent executors for configurations that no ancestor shares.

For each resolution:

1. Check the requested operation and run signature.
2. Build an executor through the configuration's `CreateExecutor`, resolving contextual children through the supplied scope, unless the owning scope already holds that configuration's executor.
3. Apply matching advice in context order and return the advised executor.

A scope keeps one bare executor per configuration reference. Each request applies that scope's advice around it. A child scope builds its own executor unless an ancestor shares the configuration. Stateless leaves may return an immutable executor repeatedly.

### Shared configurations

A scope holds shared executors by configuration reference. The nearest ancestor sharing a configuration owns its entry. A shared configuration is first built in a dedicated child scope of that owner; the children and advice added by its construction stay there. Each request wraps the shared bare executor with its requesting scope's advice.

`algorithm.Sharing(configurations)` declares the configurations whose bare executors child scopes reuse. Configuration reference identity selects them; equal but distinct configurations keep separate entries. A new sharing owner or root starts independently. A request for incompatible run types is rejected rather than constructing a second executor. Configurations never requested construct nothing.

`AlgorithmRun` creates an independent root scope and starts in `Ready`. Executor construction remains lazy until first execution. Experiment trials each own an algorithm run and its result table. Analysis is attached to the configuration through `AnalyzedBy` before creating the run.

`Instrument<TResult>` defines one result per run and the substitutions that place what collects into it. Its immutable definition is identified by reference. `RunResults.Result` lazily creates and publishes the result, without supply bindings or child resolution. Callers read results through `AlgorithmRun.Result`.

### Operation-parameterized operators

An `IOperator<TOperation, TCandidate>` inherits one construction hook from `IOperator<TCandidate>`, for the run types. The hook names no operation, so a multi-operation configuration can only implement it once, and it shares its executor and private fields across those operation views within the scope. Resolution checks the built and wrapped executors through the requested operation's `IsExecutor`.

Operators, algorithms and consumer defined roles use the same run results, aspect chain and configuration reference construction guard. Every operation resolves through the generic path.

For the generic path, resolution calls the `Check` method of every matching advice before constructing the raw graph, so an unsupported metric fails before any child is constructed. After construction it calls each advice's `Wrap` in application order. `OperatorAdvice<TOperation, TCandidate>` applies only to its operation of a configuration implementing several operations. Unsupported advice produces an explicit diagnostic. Advice is not cached; a construction or wrapping failure ends the scope tree while published results remain readable.

Each operation implements `IOperation<TSelf>`, whose static members recognize its executor interface for the run types, wrap an executor and state whether its calls report candidate work; resolution and advice reach them through `OperationInfo`. Every operation's named method ends with the call's random number generator, search space and problem. `Wrap(executor, wrapper)` returns a `WrappingExecutor<TExecutor, TWrapper>` whose named method passes each call to a struct `IInvocationWrapper` as the operation's private `Invocation` struct, an `IInvocation<TResult>` holding the wrapped executor and the call's arguments. The wrapper reads the call's `Random`, `SearchSpace` and `Problem`, calls `Proceed()` to run the wrapped method, possibly several times, or `Proceed(executor, random)` to run it on another executor of the same operation or with another random number generator, and calls `CountCandidateWork(result)` when it measures candidate work. A generic wrapper reads the search space and problem untyped and cannot replace them: it does not know the run's types, and typed role advice already receives every argument. Each layer is one executor compiled for its wrapper type, without delegates or argument tuples. `Named` returns its child's executor through `scope.ResolveOperator(ChildOperator)`; `Retry` and external configured wrappers use `TOperation.Wrap(scope.ResolveOperator(ChildOperator), wrapper)`. `ChoosePerInvocation` resolves all its children on each construction and wraps the first with a wrapper that samples a child per call and proceeds on it. `ChoosePerItem` resolves its children the same way and returns the executor of its operation's `Route(children)`, passing a `WeightedRouter<IExecutor>` over them. It accepts only operations marked with `IItemOperation<TSelf>`, for which routing is a capability of the operation: its `RoutingExecutor<TExecutor>` lets the router assign a call's items, runs each child's items as one call and restores their order. External wrappers and advice use the same contract without adding operation members.

Candidate work means returned counts for candidate-producing operations and input count for evaluation. An operation with candidate work overrides `SupportsCandidateWork` and implements `CountCandidateWork`. Termination, interception and scalar external operations implement neither: `SupportsCandidateWork` defaults to false and the invocation's default `CountCandidateWork` throws `NotSupportedException`. Candidate-counting advice calls `RequireCandidateWork` in its check and counts only after success; call counting and timing never read candidate work. Wrappers use ordinary `try`/`finally`: duration measurement records failed calls, and an exception thrown while recording replaces the child's. Algorithms retain their separate streaming advice.

### Algorithms and consumer-defined roles

Each resolution of an algorithm calls its `CreateExecutor` with a scope typed to the run types and the requested search state. A configuration that does not produce that state is rejected before it is built, and an algorithm bound to a search space and problem rejects other run types in its base before its hook runs. Every matching `AlgorithmAdvice<TCandidate, TSearchState>` then wraps the built executor; advice for another search state of a configuration producing several is skipped.

A consumer-defined role is an `IConfigurationNode<TExecutor>` resolved through `ResolutionScope.Resolve`. Its advice derives from `Advice<TConfiguration, TExecutor>` and wraps every executor built for a selected configuration. Resolution knows the role only by its executor type, so it reaches the advice through an internal entry on the root `Advice` that takes the executor and configuration untyped; `Advice<TConfiguration, TExecutor>` implements it by passing both to the role's `Wrap`. `Aspect.Create` rejects advice for another role at compile time. Matching advice that cannot wrap the requested executor, such as advice for another executor type of the same configuration, is reported rather than cast.

### Observation sources

Operator invocations and algorithm iterations or calls are observed by wrapping their executors. Sources create the same observation wrapper for explicit `Observed` placement and automatic instrument attachment. A Cycle turn is a call of an inner algorithm; observe that algorithm's `Calls` for its start and outcome.

## Pointcuts, advice and join points

A `Pointcut<TConfiguration>` examines a configuration as it is resolved. `Pointcut.Reference(source)` uses reference identity; `Pointcut.OfType<TConfiguration>()` includes implementations and derived types. `Pointcut.Predicate<TConfiguration>(predicate)` supplies runtime-only property or attribute matching; custom selection can also derive a small class overriding `Matches`. Predicates examine configuration data, run on each query and propagate exceptions. Pointcuts do not traverse the graph.

`And` and `Or`, also spelled `&` and `|`, combine pointcuts and short-circuit from left to right. `Not()` and `!` complement a selection within its declared configuration type: negating a mutator pointcut still excludes every other role.

`Pointcut.Named<TConfiguration>(name)` matches `INamedConfiguration.Name` ordinally and case-sensitively. Concrete configurations can implement that contract, or users can configure a role-preserving wrapper through `operator.Named(name)` or `algorithm.Named(name)`. Named wrappers forward the child's executor and use the ordinary child resolution rules. They and their children remain separate selectable configurations. Repeated names select every matching configuration; they do not identify shared state or caller paths.

An `Aspect` stores ordered `(Pointcut, Advice)` pairs in `Advices`. `Aspect.Create(pointcut, advice)` creates one pair; derived constructors assign several pairs directly. Advice is a class with reference identity. Operators using `IOperator<TOperation, TCandidate>` accept `OperatorAdvice`, which wraps every operation of its selected operators through the operation adapter, and `OperatorAdvice<TOperation, TCandidate>`, which wraps one operation. Role advice bases such as `MutatorAdvice<TCandidate>` and `EvaluatorAdvice<TCandidate>` derive from the latter and receive typed executors. Every other operation has a corresponding typed `Check`/`Wrap` base; the one for termination also receives the run's search state type, and `InterceptorAdvice<TCandidate, TSearchState>` names the state its interceptors return.

Advice receives concrete runtime dependencies through its constructor and passes them to wrapping executors. Instrument hooks create the aspect using that run's result. Advice has no result lookup, supply or child-resolution capability. Configured wrappers remain independently selectable configurations.

A join point is the executor built for one selected configuration. A shared configuration can have several contextual bindings and does not identify one caller path or invocation. Resolution applies advice separately for every request, so a shared configuration receives one advice wrapper per request around its single executor. Advice wrappers therefore keep no private data; data that must accumulate belongs to the advice or to an object it receives, such as a `CountAccumulator`. Runtime data held by the advice object is intentionally shared wherever that advice instance is reused. Instrument results follow the run lifetime, so reusing a definition across trials creates independent counters. Invocation local values stay in the operation or iterator.

## Ordering and shared advice

Child-scope advice precedes inherited advice, closest to the raw executor first. Within one scope, later added aspects wrap closer; within an aspect, pairs keep their listed order. `algorithm.Advised(a).Advised(b)` therefore places `a` closer than `b`.

Pointcuts match before advice is deduplicated by reference. The closest matching occurrence of each advice instance applies once.

## Deferred construction

Cycle resolves fresh children for every turn while forwarding search state. Sharing a child configuration on the Cycle, `cycle.Sharing(child)`, keeps one executor for that child across turns, including its private fields and children; each turn still applies the Cycle's current advice around it. A separately resolved Cycle has its own shared executors.

Pipeline stages and dynamic racing contenders resolve fresh children through child scopes of their current scope. Every scope in the tree sees the same run results. Pause/resume continues the retained iterator and executors.

## Controls, failures and lifetime

An executor is its own control unless it implements `IExecutorControlProvider`. `ExecutorControls.Find` follows that forwarding from the advised executor, which callers receive once the whole chain has been built; invoke operations through that executor. A transparent advice wrapper forwards the controls of the executor it wraps. A wrapping executor built by an operation's `Wrap` forwards the wrapped executor's controls through its `WrappingExecutor<TExecutor, TWrapper>` base unless its invocation wrapper implements `IExecutorControlProvider`, which then supplies them; `ChoosePerInvocation`'s wrapper supplies none, and `ChoosePerItem`'s routed executor exposes none of its children's. `Named`, `Retry` and inspection therefore forward. A replacement implements the controls it offers, or none.

Operation-parameterized naming, retry and inspection therefore expose the child's controls. A composite supplies its own controls; the resolver does not infer them from arbitrary children.

A construction or wrapping failure ends the entire scope tree. Published results remain readable, but further resolution requires a new root. Rejections before construction, including signature and advice checks, leave the scope usable. `TryResolve` reports those rejections as reasons; a construction failure propagates.

Executors live with their owning scopes and results with the root scope tree. Deferred executors may retain a scope. Neither resolution nor run completion disposes caller-owned dependencies. Table synchronization does not synchronize execution.

## Inspection and budgets

Predefined observation, counting and timing advice lives in `HEAL.HeuristicLib.Inspection`. Inspection preserves the observed computation while updating its own collected data. General advice may change behavior. Counting updates after a successful call. Timing records duration in `finally`, including failed calls. The two concerns use standalone advice rather than allocating observations for every count.

Operator budgets remain configurations. Each construction creates a private accumulator and the counting or timing advice that feeds it, adds that advice with `scope.AddAspect(aspect)` and resolves the inner algorithm. Calls through that child context contribute to the budget. The single operator variants select one inferred operation. The operator list variants select all operations of every listed configuration reference with one union pointcut, so duplicate references do not duplicate accounting. Their constructors and extensions use the same configuration. Directly resolving the child algorithm creates independent private data and does not add budget advice. An absent observed operator matches nothing.

`LimitEvaluator` accounting is private to its executor. Sharing the limited evaluator lets several uses draw from one allowance. Atomic accumulation does not turn separate checking, evaluation and accounting into a strict concurrent budget. Such reservation semantics remain outside this contract.

A budget's local layer wraps closer to the operator than inherited analysis. This keeps observer work outside the measured operation. Among aspects in the same layer, order is explicit; add timing before an observation when its callback should remain outside the timed span.
