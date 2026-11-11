# Execution resolution

Resolution constructs typed executors with the scope's run-bound results. Each configuration resolves its declared children through the scope received by its `CreateExecutor` hook.

See [configuration vs executors](/contributing/architecture/execution-nodes) for authoring responsibilities.

## Results and construction

`ResolutionScope.Create()` creates an independent root with one run result table. Share configurations with `Share` before child scopes use them. `CreateChildScope()` keeps the same run results while creating independent executors for configurations that no ancestor shares.

For each resolution:

1. Check the requested operation and run signature.
2. Build an executor through the configuration's `CreateExecutor`, resolving contextual children through the supplied scope, unless the owning scope already holds that configuration's executor.
3. Check that the executor implements the requested executor interface and return it.

A scope keeps one executor per configuration reference, so every request in it returns the same executor. A child scope builds its own executor unless an ancestor shares the configuration. Stateless leaves may return an immutable executor repeatedly.

### Shared configurations

A scope holds shared executors by configuration reference. The nearest ancestor sharing a configuration owns its entry. A shared configuration is first built in a dedicated child scope of that owner; the children its construction resolves stay there. Every request in the owner's subtree returns that executor.

`algorithm.Sharing(configurations)` declares the configurations whose executors child scopes reuse. Configuration reference identity selects them; equal but distinct configurations keep separate entries. A new sharing owner or root starts independently. A request for incompatible run types is rejected rather than constructing a second executor. Configurations never requested construct nothing.

`AlgorithmRun` creates an independent root scope and starts in `Ready`. Executor construction remains lazy until first execution. Experiment trials each own an algorithm run and its result table. Analysis is attached to the configuration through `AnalyzedBy` before creating the run.

`Instrument<TResult>` defines one result per run and the substitutions that place what collects into it. Its immutable definition is identified by reference. `RunResults.Result` lazily creates and publishes the result, without supply bindings or child resolution. Callers read results through `AlgorithmRun.Result`.

### Operation-parameterized operators

An `IOperator<TOperation, TCandidate>` inherits one construction hook from `IOperator<TCandidate>`, for the run types. The hook names no operation, so a multi-operation configuration can only implement it once, and it shares its executor and private fields across those operation views within the scope. Resolution checks the built executor through the requested operation's `IsExecutor`.

Operators, algorithms and consumer defined roles use the same run results and configuration reference construction guard. Every operation resolves through the generic path.

For the generic path, resolution checks the run signature before building, so a configuration that cannot run with the requested types fails before any child is constructed and leaves the scope usable. A wrapper that cannot serve its child, such as a candidate counter on an operation without candidate work, checks that in its `CreateExecutor` before it resolves the child. A construction failure ends the scope tree while published results remain readable.

Each operation implements `IOperation<TSelf>`, whose static members recognize its executor interface for the run types, wrap an executor and state whether its calls report candidate work; resolution and wrappers reach them through `OperationInfo`. Every operation's named method ends with the call's random number generator, search space and problem. `Wrap(executor, wrapper)` returns a `WrappingExecutor<TExecutor, TWrapper>` whose named method passes each call to a struct `IInvocationWrapper` as the operation's private `Invocation` struct, an `IInvocation<TResult>` holding the wrapped executor and the call's arguments. The wrapper reads the call's `Random`, `SearchSpace` and `Problem`, calls `Proceed()` to run the wrapped method, possibly several times, or `Proceed(executor, random)` to run it on another executor of the same operation or with another random number generator, and calls `CountCandidateWork(result)` when it measures candidate work. A generic wrapper reads the search space and problem untyped and cannot replace them: it does not know the run's types, and a typed wrapper of one operation already receives every argument. Each layer is one executor compiled for its wrapper type, without delegates or argument tuples. `Named` returns its child's executor through `scope.ResolveOperator(ChildOperator)`; `Retry` and external configured wrappers use `TOperation.Wrap(scope.ResolveOperator(ChildOperator), wrapper)`. `ChoosePerInvocation` resolves all its children on each construction and wraps the first with a wrapper that samples a child per call and proceeds on it. `ChoosePerItem` resolves its children the same way and returns the executor of its operation's `Route(children)`, passing a `WeightedRouter<IExecutor>` over them. It accepts only operations marked with `IItemOperation<TSelf>`, for which routing is a capability of the operation: its `RoutingExecutor<TExecutor>` lets the router assign a call's items, runs each child's items as one call and restores their order. External wrappers use the same contract without adding operation members.

Candidate work means returned counts for candidate-producing operations and input count for evaluation. An operation with candidate work overrides `SupportsCandidateWork` and implements `CountCandidateWork`. Termination, interception and scalar external operations implement neither: `SupportsCandidateWork` defaults to false and the invocation's default `CountCandidateWork` throws `NotSupportedException`. A candidate counter calls `RequireCandidateWork` before it resolves its child and counts only after success; call counting and timing never read candidate work. Wrappers use ordinary `try`/`finally`: duration measurement records failed calls, and an exception thrown while recording replaces the child's. Algorithms have no invocation wrapper; an algorithm wrapper builds its own executor around its child's.

### Algorithms and consumer-defined roles

Each resolution of an algorithm calls its `CreateExecutor` with a scope typed to the run types and the requested search state. A configuration that does not produce that state is rejected before it is built, and an algorithm bound to a search space and problem rejects other run types in its base before its hook runs.

A consumer-defined role is an `IConfigurationNode<TExecutor>` resolved through `ResolutionScope.Resolve`. A wrapper of the role implements the role, declares the child it holds and wraps the child's executor in its `CreateExecutor`.

### Observation sources

Operator invocations and algorithm iterations or calls are observed by wrapping their executors. Sources create the same observation wrapper for explicit `Observed` placement and automatic instrument attachment. A Cycle turn is a call of an inner algorithm; observe that algorithm's `Calls` for its start and outcome.

## Substitution and ordering

`Substitute` places wrappers before resolution and returns a new configuration; resolution builds only the configuration it is given. `Substitution.Of(target, replace)` selects by reference and fails when the configuration does not hold the target. The type form, `Substitution.Of((TNode node) => ...)`, selects every configuration of the parameter's type, and the predicate form selects those of a type that a predicate accepts; both change nothing when they match nothing. Predicates examine configuration data during substitution and propagate exceptions. A substitution reaches the children a configuration declares, not configurations that a delegate creates at run time.

A predicate selects by name through `INamedConfiguration.Name`, as in `node is INamedConfiguration { Name: "exploration" }`. Concrete configurations can implement that contract, or users can configure a role-preserving wrapper through `operator.Named(name)` or `algorithm.Named(name)`. Named wrappers forward the child's executor and use the ordinary child resolution rules. They and their children remain separate selectable configurations. Repeated names select every matching configuration; they do not identify shared state or caller paths.

The configuration states the order of wrappers: a wrapper written inside another is closer to the executor. Substitutions apply one after another, and a later one, in the same call or a later one, sits closer to the node it matches. A wrapper is an ordinary configuration, so building it changes nothing for the other configurations of its scope, and a shared wrapper has one executor whose private data every request in its sharing scope uses. Data that must accumulate across scopes or runs belongs to an object the wrapper receives, such as a `CountAccumulator`, or to a run result. Instrument results follow the run lifetime, so reusing a definition across trials creates independent counters. Invocation local values stay in the operation or iterator.

## Deferred construction

Cycle resolves fresh children for every turn while forwarding search state. Sharing a child configuration on the Cycle, `cycle.Sharing(child)`, keeps one executor for that child across turns, including its private fields and children. A separately resolved Cycle has its own shared executors.

Pipeline stages and dynamic racing contenders resolve fresh children through child scopes of their current scope. Every scope in the tree sees the same run results. Pause/resume continues the retained iterator and executors. `AlgorithmRun` measures each call advancing that iterator in `try`/`finally`. Executor construction before the first advance, pauses and consumer work between states are excluded. Deferred child construction during an advance, such as a fresh Cycle turn, is included. The resolver returns the executor itself and measures no elapsed time. Direct scope execution leaves `scope.Results.Elapsed` and `Clock.Elapsed` at zero; use `algorithm.Timed(duration)` for explicit measurement.

## Controls, failures and lifetime

An executor is its own control unless it implements `IExecutorControlProvider`. `ExecutorControls.Find` follows that forwarding from the executor that resolution returns; invoke operations through that executor. A wrapping executor built by an operation's `Wrap` forwards the wrapped executor's controls through its `WrappingExecutor<TExecutor, TWrapper>` base unless its invocation wrapper implements `IExecutorControlProvider`, which then supplies them; `ChoosePerInvocation`'s wrapper supplies none, and `ChoosePerItem`'s routed executor exposes none of its children's. `Named`, `Retry` and inspection therefore forward. A replacement implements the controls it offers, or none.

Operation-parameterized naming, retry and inspection therefore expose the child's controls. A composite supplies its own controls; the resolver does not infer them from arbitrary children.

A construction failure ends the entire scope tree. Published results remain readable, but further resolution requires a new root. Rejections before construction, such as signature checks, leave the scope usable. `TryResolve` reports those rejections as reasons; a construction failure propagates.

Executors live with their owning scopes and results with the root scope tree. Deferred executors may retain a scope. Neither resolution nor run completion disposes caller-owned dependencies. Table synchronization does not synchronize execution.

## Inspection and budgets

Predefined observation, counting and timing live in `HEAL.HeuristicLib.Inspection`. Inspection preserves the observed computation while updating its own collected data. Wrappers of your own may change behavior. Counting updates after a successful call. Timing records duration in `finally`, including failed calls. `Counted` and `Timed` wrap the invocation directly rather than allocating an observation for every call.

Operator budgets remain configurations. A budget holds its algorithm with the `Counted` or `Timed` wrappers already in place, and the `Counter` or `Duration` node they add to. The single operator extensions place one wrapper on the operator by reference, with its inferred operation, and fail when the algorithm does not hold it. `LimitedToCount` and the placement form of `LimitedToDuration` create the node and pass it to a delegate that places the wrappers and states what each one counts; the delegate runs during the call and is not stored. Each construction shares the node in its own scope, so it restarts with the budget, and only calls through the wrappers inside the budget's algorithm contribute. Directly resolving the original algorithm builds its own executors, which the budget does not count.

`LimitEvaluator` accounting is private to its executor. Sharing the limited evaluator lets several uses draw from one allowance. Atomic accumulation does not turn separate checking, evaluation and accounting into a strict concurrent budget. Such reservation semantics remain outside this contract.

A budget applied after `AnalyzedBy` wraps closer to the operator than the analysis, which keeps observer work outside the measured operation. Applied before it, the analysis sits inside the budget's wrappers, and a duration budget measures the observers too.
