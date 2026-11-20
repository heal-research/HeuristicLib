# Execution resolution

Resolution constructs execution nodes, including typed executors with the scope's run-bound results. Each configuration resolves its declared children through the scope received by its construction hook. Operators and algorithms use `CreateExecutor`; other execution nodes use `IConfigurationNode<TExecutionNode>.CreateExecutionNode`.

See [Execution nodes](/contributing/architecture/execution-nodes) for what configurations, executors and run results own, and [Operator implementation](/contributing/architecture/operator-implementation) for authoring operators and operations.

## Results and construction

`ResolutionScope.Create()` creates an independent root with one run result table. Share configurations with `Share` before child scopes use them. `CreateChildScope()` keeps the same run results while creating independent executors for configurations that no ancestor shares.

A typed view from `scope.For<TCandidate, TSearchSpace, TProblem>(problem)` retains the same scope and exposes `Problem` and `SearchSpace`. It creates no child activation. The problem's search space and objective stay fixed for the problem object's lifetime; mutable instance data may change. Fixed-signature operator and algorithm bases pass their declared search-space, problem and, where fixed, search-state types to children through `IConfigurationNode.ChildExecution`, matching their typed construction scopes. Preflight applies this mapping before each parameter's own `ChildExecution` mapping. A parent may create a view for another problem to bind a child; the parameter declares that child's signature and its mapping can ignore the parent's execution.

For each operator or algorithm resolution:

1. Check the requested operation and run signature.
2. Build an executor through the configuration's `CreateExecutor`, resolving contextual children through the supplied scope, unless the owning scope already holds an executor for the configuration and problem references.
3. Check that the executor implements the requested executor interface and return it.

A scope keeps one execution node per configuration/problem reference pair for algorithms and operators, and per configuration reference for configurations whose execution node needs no problem, such as counters and adaptations, so every request in it returns the same execution node. A child scope builds its own execution node unless an ancestor shares the configuration. A context-free stateless configuration, such as a creator, crossover, mutator or refiner derived from its candidate-only stateless base, can serve as its own immutable executor, because the executor reads nothing from the scope. A stateless selector or replacer reads the objective of the bound problem, so it builds an executor that holds the problem.

### Shared configurations

A scope selects sharing by configuration reference and holds each execution node by configuration and optional problem references. The nearest ancestor sharing a configuration owns its entry. A shared configuration is first built in a dedicated child scope of that owner; the children its construction resolves stay there. Every request for that problem in the owner's subtree returns that executor.

`algorithm.Sharing(configurations)` declares the configurations whose executors child scopes reuse. Configuration reference identity selects them; equal but distinct configurations keep separate entries. A new sharing owner or root starts independently. A request for incompatible run types is rejected rather than constructing a second executor. Configurations never requested construct nothing.

`AlgorithmRun` creates an independent root scope, and executor construction remains lazy until first execution. Experiment trials each own an algorithm run and its result table. See [Run ownership](/contributing/architecture/layering#run-ownership) for the run lifecycle.

`Analyzer<TResult>` is a configuration node that creates one `IRunResult` per run and names the substitutions that place what collects into it. It is immutable and identified by reference. `RunResults.Result` reads the root scope's one configuration result table, lazily creating the result through `CreateExecutionNode`. The analyzer's result spans every Cycle turn; a `Share` does not narrow the result that recording and `AlgorithmRun.Result` use, while resolving a shared analyzer directly through a scope creates a separate execution node that nothing records into. Counters and durations also create run results, but resolving them honors the nearest sharing scope, which owns a separate accumulator. Callers read results through `AlgorithmRun.Result`.

### Operation-parameterized operators

An `IOperator<TOperation, TCandidate>` inherits one construction hook from `IOperator<TCandidate>`, for the run types. The hook names no operation, so a multi-operation configuration can only implement it once, and it shares its executor and private fields across those operation views within the scope. Resolution checks the built executor through the requested operation's `IsExecutor`.

Operators, algorithms and consumer defined roles use the same run results and configuration reference construction guard. Every operation resolves through the generic path.

For the generic path, resolution checks the run signature before building, so a configuration that cannot run with the requested types fails before any child is constructed and leaves the scope usable. A wrapper that cannot serve its child, such as a candidate counter on an operation without candidate work, checks that in its `CreateExecutor` before it resolves the child. A construction failure ends the scope tree while published results remain readable.

Each operation implements `IOperation<TSelf>`, whose static members recognize its executor interface for the run types, wrap an executor and state whether its calls report candidate work; resolution and wrappers reach them through `OperationInfo`, which a generic wrapper creates with `OperationInfo.For<TOperation, TCandidate, TSearchState>()`. [Define a new operation](/contributing/architecture/operator-implementation#define-a-new-operation) describes those members and the invocation each wrapper receives, and developer guidelines § 4.19 states the contract. Each layer is one executor compiled for its wrapper type, without delegates or argument tuples.

`Named` returns its child's executor through `scope.ResolveOperator(ChildOperator)`; `Retry` and external configured wrappers use `TOperation.Wrap(scope.ResolveOperator(ChildOperator), wrapper)`. `ChoosePerInvocation` resolves all its children on each construction and wraps the first with a wrapper that samples a child per call and proceeds on it. `ChoosePerItem` resolves its children the same way and returns the executor of its operation's `Route(children)`, passing a `WeightedRouter<IExecutor>` over them. It accepts only operations marked with `IItemOperation<TSelf>`, for which routing is a capability of the operation: its `RoutingExecutor<TExecutor>` lets the router assign a call's items, runs each child's items as one call and restores their order ([Wrapping and multi operators](/contributing/architecture/operator-implementation#wrapping-and-multi-operators) describes the router). External wrappers use the same contract without adding operation members.

Candidate work means returned counts for candidate-producing operations and input count for evaluation. Termination, interception and scalar external operations report none; developer guidelines § 4.19 states which members an operation with candidate work implements. A candidate counter calls `RequireCandidateWork` before it resolves its child and counts only after success; call counting and timing never read candidate work. Wrappers use ordinary `try`/`finally`: duration measurement records failed calls, and an exception thrown while recording replaces the child's. Algorithms have no invocation wrapper; an algorithm wrapper builds its own executor around its child's.

### Algorithms and consumer-defined roles

Each resolution of an algorithm calls its `CreateExecutor` with a scope typed to the run types and the requested search state. A configuration that does not produce that state is rejected before it is built, and an algorithm bound to a search space and problem rejects other run types in its base before its hook runs.

A consumer-defined role is an `IConfigurationNode<TExecutionNode>` resolved through `ResolutionScope.Resolve`. A wrapper of the role implements the role, declares the child it holds and wraps the child's execution node in its `CreateExecutionNode`.

### Observation sources

Operator invocations and algorithm iterations or calls are observed by wrapping their executors. [Observation boundaries](/contributing/architecture/analyzers#observation-boundaries) describes the sources, the observation wrapper and how a Cycle turn is observed.

## Substitution and ordering

`Substitute` places wrappers before resolution and returns a new configuration; resolution builds only the configuration it is given. [Configurations](/guide/fundamentals/configurations#rewriting-a-graph-with-substitute) states for users how a substitution selects, what it reaches and how the substitutions of one call and of several calls combine.

A `Substitution` is one rewrite, created through its `Of` factories. [Select by reference](/guide/fundamentals/configurations#select-by-reference) and [Select by type or predicate](/guide/fundamentals/configurations#select-by-type-or-predicate) describe the selectors for users and what a substitution reaches. A predicate examines configuration data during substitution and propagates its exceptions.

`ISubstitution.Expand()` returns its single substitutions in application order, with the same instances on every call. `SubstitutionGroup` caches the depth-first expansion of its parts; parts may themselves be groups, and recursive containment is an error. Applying belongs to the call: `Substitute` expands all arguments together, applies a repeated single substitution once at its last position and matches every target against the graph it received.

Named wrappers, created through `operator.Named(name)` or `algorithm.Named(name)`, implement `INamedConfiguration` and forward the child's executor under the ordinary child resolution rules. They and their children remain separate selectable configurations, and repeated names select every matching configuration without identifying shared state or caller paths. [Select by name](/guide/execution/observability-and-analysis#select-by-name) shows how to select them.

The configuration states the order of wrappers: a wrapper written inside another is closer to the executor, and [Configurations](/guide/fundamentals/configurations#what-substitute-guarantees) states how substitutions order theirs. A wrapper is an ordinary configuration, so building it changes nothing for the other configurations of its scope, and a shared wrapper has one executor whose private data every request in its sharing scope uses. Data that must accumulate across scopes or runs belongs to an object the wrapper receives, such as a `CountAccumulator`, or to a run result. Run results follow the run lifetime, so reusing a counter across trials creates independent accumulators.

## Deferred construction

Cycle resolves fresh children for every turn while forwarding search state. Sharing a child configuration on the Cycle, `cycle.Sharing(child)`, keeps one executor for that child across turns, including its private fields and children. A separately resolved Cycle has its own shared executors.

Pipeline stages and dynamic racing contenders resolve fresh children through child scopes of their current scope. Every scope in the tree sees the same run results. Pause/resume continues the retained iterator and executors. `AlgorithmRun` measures each call advancing that iterator, as [Clocks](/contributing/architecture/analyzers#clocks) describes. The resolver returns the executor itself and measures no elapsed time.

## Adaptation, failures and lifetime

An adaptation is an `IConfigurationNode<IAdaptation>` that an executor resolves and applies to its configured base value. A scope resolves one runtime adaptation per configuration reference, and independent runs remain independent. The resolver does not detect unused links. [Adaptable parameters](/guide/extending/operator-composition#adaptable-parameters) shows the wiring and what to share when an adaptation is retained across Cycle turns.

A construction failure ends the entire scope tree. Published results remain readable, but further resolution requires a new root. Rejections before construction, such as signature checks, leave the scope usable. `TryResolve` reports those rejections as reasons; a construction failure propagates.

Executors live with their owning scopes and results with the root scope tree. Deferred executors may retain a scope. Neither resolution nor run completion disposes caller-owned dependencies. Table synchronization does not synchronize execution.

## Inspection and budgets

Predefined observation, counting and timing live in `HEAL.HeuristicLib.Inspection`. Counting updates after a successful call. Timing records duration in `finally`, including failed calls. `Counted` and `Timed` wrap the invocation directly rather than allocating an observation for every call. [Observation boundaries](/contributing/architecture/analyzers#observation-boundaries) states what inspection must preserve.

Operator budgets remain configurations. A budget holds its algorithm with the `Counted` or `Timed` wrappers already in place, and the `Counter` or `Duration` node they add to. [Budgets on operator work](/guide/extending/operator-composition#budgets-on-operator-work) shows the extensions that build one, including the placement form, whose delegate runs during the call and is not stored. Each construction shares the node in its own scope, so it restarts with the budget, and only calls through the wrappers inside the budget's algorithm contribute. Directly resolving the original algorithm builds its own executors, which the budget does not count.

`LimitEvaluator` accounting is private to its executor. Sharing the limited evaluator lets several uses draw from one allowance. Atomic accumulation does not turn separate checking, evaluation and accounting into a strict concurrent budget. Such reservation semantics remain outside this contract.

The order in which a budget and an analysis wrap an operator decides whether a duration budget includes observer time. [Budgets on operator work](/guide/extending/operator-composition#budgets-on-operator-work) states which placement excludes it.
