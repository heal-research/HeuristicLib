# Glossary

Use this reference when an optimization term is unfamiliar or has a specific meaning in HeuristicLib. The main guide introduces the same concepts through working examples.

## Status labels

Glossary entries may identify related terms by status:

- `Canonical`: the preferred project term.
- `Alias`: an accepted alternate term that may be useful in prose.
- `Legacy`: an older or transitional term that should be replaced with the canonical term when encountered in docs, examples, tests, plans, or new code.
- `Avoid`: a term that should not be used for this concept, usually because it has a different meaning or creates ambiguity.
- `Provisional`: a current term or candidate term that may change after further design discussion.

## Optimization Domain

### Problem

Status: `Canonical`

A problem defines the optimization task that an algorithm operates on.

A problem defines the search space, the evaluation semantics that produce an objective vector, the objective directions and the concrete data or parameters needed for evaluation.

See also: Candidate, Evaluation, Objective, Objective direction, Objective directions, Objective value, Objective vector, Problem instance, Search space.

### Problem instance

Status: `Canonical`

A problem instance is the concrete domain data or parameterization for a problem when that part is useful to discuss separately.

Do not use problem instance to mean an ordinary programming-language object instance of a problem class.

See also: Problem.

### Candidate

Status: `Canonical`

A candidate is the algorithm facing object that algorithms search for and operators create, transform, evaluate, select or replace.

Candidates are the values that algorithms and operators manipulate directly. Use candidate when precision matters around unevaluated or intermediate values. Candidate does not imply that the value is good, final, feasible in the broader domain sense, or already evaluated.

Related terms:

- `Genotype`: `Alias`. Use when an evolutionary-algorithm-flavored distinction between encoded representation and phenotype/domain expression is useful.
- `Solution`: `Alias`. Use in user-facing or domain-facing prose when the candidate is discussed as a possible answer to the problem. Avoid solution when the distinction between unevaluated and evaluated candidates matters.
- `Evaluated candidate`: a candidate paired with an objective vector.

See also: Evaluated candidate, Search space, Search state.

### Encoding

Status: `Canonical`

An encoding is the representation scheme used to express domain-facing solutions as candidates.

In HeuristicLib, encoding is usually reflected by the candidate type and the way a problem interprets candidates. It is not normally a separate object. Do not use encoding as a synonym for search space: the search space defines which candidate values are valid for a problem, while the encoding describes how candidate values represent possible solutions.

Related terms:

- `Representation`: `Alias`. Common in optimization and evolutionary-algorithm literature.
- `Genotype`: related evolutionary-algorithm term for the encoded candidate.

See also: Candidate, Problem, Search space.

### Evaluated candidate

Status: `Canonical`

An evaluated candidate is a candidate paired with the objective vector produced by evaluating it.

Use evaluated candidate when the distinction between the searched object and its evaluation matters. An evaluated candidate is not necessarily final, optimal, or selected for future search steps.

See also: Candidate, Evaluation, Evaluator, Search state.

### Search space

Status: `Canonical`

A search space is the algorithm-facing domain of valid candidates.

The search space defines which candidates algorithms and operators may create, transform, and evaluate. It is not necessarily the same as the broader domain-facing solution space; their relationship depends on the representation used for search.

Related terms:

- `Solution space`: the domain-facing space of possible answers to a problem. It is related to, but not a synonym for, search space.

See also: Candidate.

### Evaluation

Status: `Canonical`

Evaluation is the act of obtaining an objective vector for a candidate.

Use evaluation for the problem-level meaning: applying the problem's evaluation semantics to a candidate to produce an objective vector.

See also: Candidate, Evaluated candidate, Objective vector, Evaluator.

### Objective

Status: `Canonical`

Objective is the loose term for the direction model that says what better means.

When text refers to the objective of a problem without saying objective value or objective vector, it usually means whether the objective is minimized or maximized.

See also: Objective direction, Objective directions, Objective value, Objective vector.

### Objective value

Status: `Canonical`

An objective value is a numeric value produced by evaluating a candidate for one objective.

Use objective value for a single scalar dimension of an objective vector.

See also: Objective, Objective direction, Objective vector.

### Objective vector

Status: `Canonical`

An objective vector is the ordered vector of objective values produced by evaluating one candidate.

An objective vector may contain one objective value for single-objective problems or multiple objective values for multi-objective problems. Objective values is an accepted prose alias for objective vector when the vector shape is not important.

Comparing two objective vectors does not by itself determine which one is better. The comparison must use objective directions or another explicit comparison rule.

Related terms:

- `Loss`: related term for an objective value that is normally minimized.
- `Score`: related term for an objective value that is normally maximized.
- `Fitness`: evolutionary-algorithm-flavored related term for objective value or objective vector. Fitness usually implies higher is better, but its direction should still be explicit.
- `Quality`: broad related term for objective value or objective vector. Prefer objective value or objective vector when precision matters.
- `Objective values`: `Alias`. Prose alias for objective vector.

See also: Candidate, Evaluated candidate, Objective, Objective direction, Objective directions, Objective value.

### Objective direction

Status: `Canonical`

An objective direction says whether lower or higher objective values are better for one objective dimension.

Use objective direction for a single objective dimension.

See also: Objective directions, Objective value.

### Objective directions

Status: `Canonical`

Objective directions are the ordered objective directions aligned with an objective vector by position.

Use objective directions for the problem-level direction model. Objective directions do not, by themselves, define a full ordering of evaluated candidates with multiple objective values. Multi-objective cases may need an additional comparison rule.

See also: Objective direction, Objective vector.

## Data

### Series

Status: `Canonical`

A series is one immutable named column of values. Its name is the semantic column name and its generic element type defines the data type.

### Data frame

Status: `Canonical`

A data frame is an immutable, insertion-ordered collection of equally sized series that may have different element types. Columns are addressed by their series names.

### Supervised data

Status: `Canonical`

Supervised data pairs an input data frame with one named target series of the same row count. It does not imply whether the data is used for training, validation, or testing.

## Machine learning

### Supervised learning

Status: `Canonical`

Supervised learning fits a predictor from inputs paired with known target values. Regression and classification are supervised learning tasks distinguished by the meaning and type of their targets.

### Regression

Status: `Canonical`

Regression is a supervised learning task whose targets and predictions are numeric values.

### Predictor

Status: `Canonical`

A predictor is a fitted object that produces a named prediction series from an input data frame. It may also write predictions into caller-provided storage.

Do not use predictor for an estimator configuration that still needs fitting.

### Estimator

Status: `Canonical`

An estimator fits a predictor from training data. Fitting receives explicit randomness when the procedure may be stochastic.

See also: Predictor.

### Regressor

Status: `Canonical`

A regressor is a predictor whose predictions are `double` values.

### Regression metric

Status: `Canonical`

A regression metric compares predicted and target numeric values and declares whether lower or higher values are better. A metric is independent of the problem that may use it as an optimization objective.

### Prediction metric

Status: `Canonical`

A prediction metric compares predictions and targets of the same element type
and declares whether lower or higher values are better. A regression metric is
a prediction metric specialized for `double` values.

### Perturbation feature importance

Status: `Canonical`

Perturbation feature importance measures how much a predictor's metric worsens
when one input feature is replaced according to a perturbation policy. Positive
importance always means degradation. Permutation feature importance is the
common case where the policy shuffles the feature values.

### Expression metric

Status: `Canonical`

An expression metric evaluates a symbolic expression candidate without requiring prediction data. Examples include expression length, variable occurrence count and structural complexity. A symbolic regression problem may combine expression metrics with regression metrics in one objective vector.

### Numeric parameter fitting

Status: `Canonical`

Numeric parameter fitting fits the optimizable numeric values of a symbolic expression to supervised regression data by nonlinear least squares, and returns an immutable expression containing the fitted values.

The fitted values are parameters of the model and constants of the expression: constant within one evaluation, which is why the genotype calls them constants, and free variables of the fit, which is why the numerics call them parameters. They are not restricted to constant leaves, since a fitted value may also appear as an exponent, an offset or a divisor.

Each path from an expression root identifies a distinct constant occurrence. Two paths remain distinct occurrences even when they reference the same immutable node object, so numeric parameter fitting treats them as separate parameters.

Related terms:

- `Constant optimization`: `Alias`. The term used by PySR, HeuristicLab, and part of the symbolic-regression literature. Use it in prose that orients readers arriving from those tools. Avoid it in API names, and avoid it where fitted values are not constant leaves.
- `Parameter identification`, `parameter optimization`: `Alias`. The terms used in the research literature for the same capability.
- `Coefficient optimization`: `Avoid`. A coefficient is a multiplicative factor, which is narrower than the values this fits, and linear scaling is the operation in HeuristicLib that genuinely fits coefficients.

See also: Expression metric, Refiner, Regression metric.

## Algorithms and Operators

### Algorithm

Status: `Canonical`

An algorithm defines a reusable search process through an algorithm configuration and its run scoped executors.

In a run, an algorithm executor advances search state until the run stops. It holds its resolved child executors, and its own fields hold private execution data. One shot algorithms are still algorithms. They produce a search process with a single state.

Use algorithm for the reusable configuration unless the text explicitly says algorithm executor.

See also: Configuration, Executor, Iteration, Operator, Run, Search state.

### Meta-algorithm

Status: `Canonical`

A meta-algorithm is an algorithm whose search process is defined by coordinating one or more child algorithms.

A meta-algorithm is still an algorithm: it produces search states as part of a run and may be used wherever an algorithm is expected. The distinction is that a meta-algorithm coordinates algorithms, not just operators.

Do not confuse a meta-algorithm with an experiment. A meta-algorithm composes child algorithms inside one run; an experiment coordinates multiple independent runs.

Related terms:

- `Composed algorithm`: descriptive related term. Prefer meta-algorithm for the canonical concept.

See also: Algorithm, Cycle, Experiment, Run, Turn.

### Iteration

Status: `Canonical`

An iteration is one step of an algorithm's search process that ends with a yielded search state.

`AlgorithmObservation.Iteration` counts the iterations of one call of the algorithm from 1, as the algorithm sees them, so it restarts when a meta-algorithm calls the algorithm again, whether its executor is fresh or reused. A clock over the iterations keeps counting across calls. Every algorithm streams search states, so every algorithm has iterations. A one shot algorithm has one.

See also: Algorithm, Clock, Search state, Turn.

### Cycle

Status: `Canonical`

A cycle is one pass of `CycleAlgorithm` over its inner algorithms, running each once in order. `MaximumCycles` limits the cycles of one call.

The inner algorithms' `Calls` sources observe their starts and outcomes. There is no separate cycle-start source.

See also: Meta-algorithm, Turn.

### Turn

Status: `Canonical`

A turn is one activation of an inner algorithm by `CycleAlgorithm`. A cycle has one turn per inner algorithm.

A turn is observed through the inner algorithm's `Calls`, including a call yielding no state. The call reports its start and outcome, without a cycle number or slot index. The inner algorithm's iteration number restarts in each call.

Related terms:

- `Activation`: `Avoid` as a name for this concept. Activation describes any meta-algorithm starting an inner algorithm.

See also: Cycle, Iteration, Meta-algorithm.

### Operator

Status: `Canonical`

An operator is a reusable building block that an algorithm or another operator calls to perform one named operation in the search process.

Examples include creators, evaluators, selectors, crossovers, mutators, replacers, terminators, and interceptors. An operator does not own the overall search process or define the run's stream of search states.

Use operator for the reusable configuration unless the text explicitly says operator executor.

See also: Algorithm, Configuration, Evaluator, Executor.

### Operation

Status: `Canonical`

An operation is a named action an operator performs, such as mutation or evaluation. Its executor interface defines the inputs and result of that action.

An operation is distinct from the executor that performs it. One executor can support several operations. Use operation for the action, executor for the callable runtime object, and invocation for one actual use of an operation.

`IOperation<TSelf>` identifies an operation in an operator contract. For example, `Mutation` in `IOperator<Mutation, TCandidate>` identifies mutation, whose executor interface is `IMutationExecutor<...>`. Its static members recognize that executor interface for the run types, wrap an executor so that each invocation passes through an invocation wrapper and state whether invocations report candidate work. Resolution and advice reach them through `OperationInfo`. `IItemOperation<TSelf>` marks an operation whose invocations split into items and adds routing them.

Related terms:

- `IOperation` and `IItemOperation`: `Provisional`. The type names may change because they read close to operator.
- `Operation descriptor`: `Legacy`. The former name of `IOperation<TSelf>`. Use operation.

See also: Executor, Item, Operation invocation, Operator roles.

### Operation invocation

Status: `Canonical`

An operation invocation is one call to an operation method, such as `Mutate`. Names use invocation, as in `IInvocation<TResult>`, an operator's `Invocations` source and `ChoosePerInvocation`. One invocation is one method call regardless of batch size; its candidate work counts what it produced or evaluated.

Every invocation carries its primary inputs followed by a random number generator, search space and problem. `IInvocation<TResult>` exposes that random number generator, search space and problem to invocation wrappers, the latter two without their run types, and can repeat the call with the same arguments.

An executor can receive many invocations. A duration measurement around an operation call measures that invocation rather than the lifetime of its executor.

Related terms:

- `Call`, `operation call`: `Alias`. Plain prose may say call; names say invocation.

See also: Executor, Item, Operation.

### Item

Status: `Canonical`

An item is one unit an operation call splits into, such as one parent group of a crossover call or one candidate of an evaluation call. Each item yields one result, which is not always a candidate: evaluation yields one objective vector per candidate.

An item operation is marked with `IItemOperation<TSelf>`. Routing is its capability: `Route` returns an executor that runs each item of a call on the child a router assigns it, so `ChoosePerItem` can choose a different child per item. Creation, crossover, mutation, evaluation and refinement are item operations. Selection, replacement, termination and interception calls have no independent items.

Use item in names that belong to splitting a call, which any item operation can use. Use candidate when the name is about candidates themselves: candidate work, one operation's inputs and outputs, and rate helpers.

See also: Candidate, Operation, Operation invocation.

### Child operator

Status: `Canonical`

A child operator is an operator configuration directly referenced by another configuration.

Child describes one immediate edge in the configuration graph. The same child operator may be referenced by multiple configurations, so the term does not imply exclusive ownership or a tree structure. Use a role-specific form such as child mutator when the operator role matters. Use nested operator only when referring more generally to an operator at any depth below another configuration.

See also: Configuration graph, Operator.

### Operator roles

Status: `Canonical`

Operator roles are the named categories of work that operators perform inside algorithms and other operators.

Use operator role names when discussing the responsibility of an operator. Use concrete operator type names only when discussing a specific implementation.

See also: Creator, Crossover, Evaluator, Interceptor, Mutator, Operator, Refiner, Replacer, Selector, Terminator.

#### Creator

Status: `Canonical`

A creator is the operator role that creates candidates.

Creators are responsible for producing candidates that are valid for the provided search space.

See also: Candidate, Operator, Search space.

#### Evaluator

Status: `Canonical`

An evaluator is the operator role that turns candidates into evaluated candidates.

The evaluator may return the input candidate as evaluated, or return a transformed candidate as evaluated. In either case, the objective vector must describe the candidate that is returned as evaluated.

Use evaluator for the operator role. Use evaluation for the act of obtaining an objective vector.

See also: Candidate, Evaluated candidate, Evaluation, Objective vector, Operator.

#### Selector

Status: `Canonical`

A selector is the operator role that selects evaluated candidates, usually as parents for later variation.

Selection decides which evaluated candidates participate in the next operation. It does not create offspring by itself and does not decide the next population unless the algorithm explicitly uses it that way.

See also: Crossover, Evaluated candidate, Mutator, Operator, Replacer.

#### Crossover

Status: `Canonical`

A crossover is the operator role that combines parent candidates into offspring candidates.

Use crossover for variation that depends on two or more parents. Use mutator for variation that perturbs existing candidates without combining multiple parents.

See also: Candidate, Mutator, Operator, Selector.

#### Mutator

Status: `Canonical`

A mutator is the operator role that perturbs candidates to create variation.

Use mutator for changes derived from existing candidates. Use creator when candidates are generated without depending on parent candidates.

See also: Candidate, Creator, Crossover, Operator.

#### Refiner

Status: `Canonical`

A refiner is an operator that works on existing candidates to improve a chosen aspect of them. Common refinements include fitting candidate parameters, repairing invalid candidates, simplifying or normalizing representations, and applying local-improvement procedures.

Algorithms normally refine newly created or varied candidates before evaluation and then continue with the refined candidates. Refiners can be chained in pipelines, repeated, selected, wrapped, observed, and instrumented.

A refiner may use an evaluator when objective values guide the refinement or decide whether to retain its result. Continuing the search with the refined candidate is Lamarckian refinement; refinement evaluation instead evaluates a temporary refined candidate and associates its objective vector with the original candidate, providing Baldwinian refinement.

See also: Candidate, Creator, Crossover, Evaluated candidate, Evaluator, Mutator, Operator.

#### Replacer

Status: `Canonical`

A replacer is the operator role that chooses the evaluated candidates that form the next population or survivor set.

Do not use replacer as a synonym for selector. A selector usually chooses parents or inputs for another operation, while a replacer chooses survivors after previous candidates and offspring are available.

See also: Evaluated candidate, Operator, Selector.

#### Terminator

Status: `Canonical`

A terminator is the operator role that decides whether execution should stop after observing a produced search state.

The owner of the terminator determines whether this means algorithm owned termination or external early stopping.

See also: Operator, Search state, Termination.

#### Interceptor

Status: `Canonical`

An interceptor is the operator role that transforms a produced search state before it is yielded or observed by state based stopping logic. Its operation, `Interception<TSearchState>`, names the state it returns, so an interceptor slot is an `IOperator<Interception<TSearchState>, TCandidate>`.

Use interceptor for state post processing. Do not use it for read only analysis. Use observation or analyzers for that.

See also: Analyzer, Observation, Operator, Search state.

### Single-candidate operator

Status: `Canonical`

A single-candidate operator is a regular operator whose public role operation remains batch-wise. It can be used anywhere an ordinary operator of that role is expected and does not introduce a separate single-candidate execution contract.

The term describes an authoring pattern: the operator author implements the role logic for one candidate, and the single-candidate operator applies that logic independently across a batch of candidates and handles the batching. Use a role-specific form such as single-candidate mutator when the operator role matters.

See also: Candidate, Operator, Operator roles.

### Termination

Status: `Canonical`

Termination is the stopping of a run or algorithm stream after a produced search state or because the algorithm cannot produce another search state.

Distinguish algorithm-owned termination from external early stopping. Algorithm-owned termination is part of the algorithm's own search process. External early stopping is applied around an algorithm or meta-algorithm stream, such as a wrapper that stops after a maximum number of yielded states, elapsed time, or observed operator work.

A terminator is the operator role that decides whether execution should stop after observing a produced search state.

See also: Algorithm, Meta-algorithm, Operator, Run, Search state.

## Execution Model

### Configuration

Status: `Canonical`

A configuration is a reusable object that users set up before execution, such as an algorithm or operator.

A configuration can contain settings, child configurations, instrument definitions and validation or construction logic. It need not be a passive data object. Mutable data belongs to executor fields or run-bound results; the configuration contains no live executor or result.

Use more specific terms when the context benefits from them:

- `algorithm configuration`
- `operator configuration`

Related terms:

- `Definition`: `Legacy`. Use configuration for the reusable configured object graph.

See also: Algorithm, Executor, Operator, Run.

### Configuration node

Status: `Canonical`

A configuration node is a configuration considered as a node in the configuration graph. Its identity is the configuration object's reference, not its value equality or one occurrence along a caller path. Several configurations may refer to the same child node.

Configuration is accepted shorthand for configuration node when the context is clear. Use node when graph membership needs emphasis, or a role-specific term such as mutator when the role is known. The common interface is `IConfigurationNode`, including its generic form. Role-specific configuration names such as `IOperator` and `IOperator<Mutation, TCandidate>` do not acquire a `Node` suffix.

`IOperator<TOperation, TCandidate>` builds its executor through one `CreateExecutor<TSearchSpace, TProblem, TSearchState>(scope)` hook, which it inherits from `IOperator<TCandidate>`. The hook resolves children and looks up run-bound results through the typed scope. Multi-operation configurations implement the hook once and use one executor per scope. Wrapping and multi bases expose `ChildOperator` and `ChildOperators`.

Algorithms and consumer-defined roles build the same way. An algorithm's `CreateExecutor<TSearchSpace, TProblem>(scope)` receives a scope typed to the run and its search state; an algorithm bound to a search space and problem overrides a `CreateExecutor` typed to them instead. A consumer-defined role implements `IConfigurationNode<TExecutor>.CreateExecutor(ResolutionScope scope)`. The hook is runtime machinery, not a configuration value strategy.

Iterative algorithms return an `IterativeAlgorithmExecutor<TCandidate, TSearchSpace, TProblem, TSearchState>` from that hook. The algorithm author resolves the optional interceptor and other declared children through the scope and passes them to the iterative executor constructor.

The term does not imply a common child-enumeration API or a universal operation on all nodes.

See also: Configuration, Configuration graph, Executor, Pointcut.

### Configuration graph

Status: `Canonical`

A configuration graph is the graph of reusable configuration nodes connected by references.

Use graph rather than tree because the same configuration object may be shared from more than one place. Do not use DAG as the glossary term; ordinary configuration graphs are expected to be acyclic, but the glossary term should not encode that stricter shape.

See also: Configuration, Execution graph.

### Pointcut

Status: `Canonical`

A pointcut selects configurations whose executors receive advice. `Pointcut<TConfiguration>` examines one supplied configuration through `Matches`.

`Pointcut.Reference(source)` selects by object reference. `Pointcut.OfType<TConfiguration>()` selects implementations and derived types. `Pointcut.Predicate<TConfiguration>(predicate)` provides runtime-only matching by configuration data; custom selection may also derive a small pointcut class. `Pointcut.Named<TConfiguration>(name)` matches the exact, case-sensitive name carried by `INamedConfiguration`.

Pointcuts of the same declared configuration type combine through `And` and `Or`, also spelled `&` and `|`, with left-to-right short-circuit matching. `Not()` and `!` complement a selection within that declared type. Matching neither traverses a graph nor resolves executions.

A selected configuration can have several callers and contextual executor bindings. Selecting it does not select one invocation or caller path.

See also: Advice, Aspect, Configuration node, Join point, Resolution scope.

### Named configuration

Status: `Canonical`

A named configuration carries a descriptive `Name` through `INamedConfiguration`. Configurations may implement the contract directly; the predefined `Named*` wrappers add it while retaining an operator or algorithm role and forwarding the child's executor.

Names are case-sensitive labels and need not be unique. A named wrapper and its child are separate configuration nodes. A name pointcut selects the wrapper; it does not assign that name to the child or introduce a caller path. Configuration identity and execution sharing still use configuration references.

See also: Configuration node, Pointcut.

### Advice

Status: `Canonical`

Advice wraps the executors built for the configurations a pointcut selects. It contains no pointcut and is separate from the configuration graph.

The single `Advice` concept provides around advice. Its wrapper can run code around an operation, change its arguments or results, or decide whether to invoke it. Advice is not restricted to observation.

`Advice<TConfiguration>` identifies the configuration role it can advise. Operator advice checks a selected operator before its executor is built and wraps each executor built for it: `OperatorAdvice` covers every operation, `OperatorAdvice<TOperation, TCandidate>` one operation, and role bases such as `MutatorAdvice<TCandidate>` receive typed executors. `AlgorithmAdvice<TCandidate, TSearchState>` wraps each executor built for a selected algorithm, and a consumer-defined role's advice derives from `Advice<TConfiguration, TExecutor>`. Advice receives concrete runtime dependencies through its constructor. Runtime data held by advice is shared wherever that advice instance is reused. Assemble advice with independent dependencies for independent runs. Advice has no resource lookup or child resolution capability. Resolution compares advice by reference. Observation, counting and duration measurement are predefined advice.

See also: Aspect, Configuration node, Join point, Pointcut.

### Join point

Status: `Canonical`

A join point is the executor built for one configuration selected by a pointcut. Advice wraps every contextual executor built for the same configuration, once per request, so a shared configuration receives one wrapper for each use. Advice wrappers keep no private data. Data that must accumulate belongs to the advice or to an object it receives, such as a counter.

A join point does not identify one invocation or one occurrence of a configuration along a caller path.

See also: Advice, Configuration node, Executor, Pointcut.

### Aspect

Status: `Canonical`

An aspect is the ordered list of its pointcut/advice pairs. `Aspect.Create(pointcut, advice)` creates one pair. A derived constructor assigns its pairs to the protected `Advices` and spreads other aspects into them, as in `Advices = [.. Aspect.Observe(evaluator.Invocations, Record)]`.

Attach aspects through `Advised` before creating the run. Direct scope users call `AddAspect`. Within a scope, later added aspects wrap closer; within an aspect, pairs retain their listed order. Match pointcuts first and keep the closest occurrence of each advice instance.

An aspect holds its pairs and any runtime dependencies in its advice. Reusing a raw aspect may couple caller-owned data. Instruments instead create independent results for each run.

See also: Advice, Join point, Pointcut, Resolution scope.

### Run

Status: `Canonical`

A run is an object that owns one logical execution setup.

An `AlgorithmRun` executes one algorithm configuration on a problem. An `ExperimentRun` coordinates the independent algorithm runs materialized by one experiment configuration. Prefer the concrete term when the distinction matters.

When a meta-algorithm coordinates child algorithms, the run is created by the algorithm started by the user. Child algorithms and operators participate in that same run unless they are explicitly started as separate runs.

Attach analysis definitions to the algorithm with `AnalyzedBy` before creating its run. Their results span nested algorithms and fresh executor activations within that run. The returned run starts in `Ready` and accepts no later attachments.

See also: Analyzer, Configuration, Executor, Search state.

### Instrument

Status: `Canonical`

An instrument is an immutable definition of one typed result per run and the substitutions that place what collects into it. `Instrument<TResult>` creates the result on the first lookup. Definitions use reference identity. Several placements within one run share its result; another run creates independent data.

Attach instruments through `AnalyzedBy` or explicit placement such as `Observed`.

See also: Analyzer, Clock, Run result.

### Run result

Status: `Canonical`

A run result is the runtime data created for one instrument in one run. `RunResults` looks it up by definition reference, and callers use `AlgorithmRun.Result(instrument)`. The result remains available after stopping or failure and defines its supported concurrent reads. All scopes of one root use the same result table.

See also: Instrument, Run, Resolution scope.

### Search state

Status: `Canonical`

A search state is the public state value produced by an algorithm while it searches a search space.

Search states describe the visible progress of a search process, such as the current evaluated candidate or current population. They may be streamed, inspected, analyzed, or returned as the final state of a run.

Search state is the public value representing search progress. Executor fields and resources are machinery used to produce that progress.

Use search terminology for the state and the search space, but do not use search algorithm as the default term for every HeuristicLib algorithm. Prefer algorithm, optimization algorithm, metaheuristic, or a more specific algorithm family where appropriate.

See also: Candidate, Evaluated candidate, Executor, Resource, Run, Search space.

### Executor

Status: `Canonical`

An executor is a runtime object that performs one or more operations of an algorithm or operator. It exposes callable methods such as `Mutate`, `Evaluate` or `RunStreamingAsync`. It holds resolved children, private fields and any explicitly shared resources. Private data lives with its executor.

Algorithms and other executors call its operation methods. One executor can receive many invocations and can support several operations. Each construction has independent private fields. Use explicit `Sharing` to keep an executor across child scopes. A stateless configuration can also serve as its own executor.

The common interface is `IExecutor`. Operation-specific interfaces include `IMutationExecutor` and `IEvaluationExecutor`; algorithms use `IAlgorithmExecutor`. The term does not give the object scheduling, threading, disposal or run-lifecycle responsibilities.

See also: Configuration, Execution, Execution graph, Resource, Operation, Operation invocation, Run.

### Execution

Status: `Canonical`

Execution is the activity of performing work. Use the term in execution time, execution concurrency and execution graph. Use executor for the callable runtime object, operation for the action it supports, and invocation for one actual method call.

`AlgorithmRun` owns a run's lifecycle. An executor performs the work that participates in that run.

See also: Executor, Execution concurrency, Resource, Operation, Operation invocation, Run.

### Execution node

Status: `Legacy`

A former name for executor. Use executor for the callable runtime object; use node only when discussing its position in the execution graph.

See also: Executor, Execution graph.

### Execution instance

Status: `Legacy`

A former name for executor. Use executor or a role-specific form such as mutation executor.

See also: Executor.

### Resource

Status: `Canonical`

A resource is a runtime dependency with its own ownership and concurrency contract. Private resources such as an evaluation cache belong to an executor. Share that executor through `Sharing` when several uses need the same data. Accounting and analysis accumulators are run-bound instrument results.

See also: Executor, Run result, Shared configuration.

### Scoped state

Status: `Legacy`

A former mechanism that shared private data by configuration ownership and ancestor lookup. Private mutable execution data now belongs to executor fields. Use `Sharing` when a configuration's executor should be shared across child scopes.

`Execution state` is also a legacy name for that former mechanism. Search state remains the public value describing search progress.

See also: Executor, Resource, Search state.

### Child scope policy

Status: `Legacy`

A former mechanism for selecting private state lifetimes during child activation. Cycle now constructs fresh executors for every turn. Sharing a child configuration on the Cycle keeps one executor for it across turns.

See also: Executor, Resource, Resolution scope, Shared configuration.

### Execution graph

Status: `Canonical`

An execution graph is a graph of executors created from a configuration graph during a run.

A run may contain more than one execution graph over time or through different observation contexts. A shared configuration has one executor for all its uses within the sharing node, each use wrapped by its own observers.

See also: Configuration graph, Executor, Run.

### Resolution scope

Status: `Canonical`

A resolution scope resolves configurations to executors during a run.

The scope holds one bare executor per configuration reference, ordered advice and the root's run results. `CreateChildScope()` inherits advice and explicit sharing while constructing independent executors for other configurations. `Share` selects configurations retained for child scopes. `AddAspect` adds advice before its targets resolve. Independent roots have separate results.

See [execution resolution](/contributing/architecture/execution-resolution) for state, construction and observation rules.

See also: Configuration, Wrapper chain, Execution graph, Executor, Aspect, Run.

### Shared configuration

Status: `Canonical`

A shared configuration resolves to one bare executor in the nearest scope that shares it. Its first construction uses a dedicated child scope of that owner, and every request applies its own scope's advice. Other configurations have one executor per requesting scope. A new root or sharing owner creates independent data.

The configuration reference identifies what is shared. Sharing preserves private executor fields and resolved children for that owner's lifetime; advice below the sharing point cannot reach those children.

See [execution resolution](/contributing/architecture/execution-resolution#shared-configurations).

See also: Configuration, Executor, Resolution scope.

### Substitution bundle

Status: `Provisional`

A substitution bundle is an ordered, flat list of substitutions that belong together, such as the placement of a trace together with its clocks. `ISubstitutionBundle` represents it. A single `Substitution` is a bundle of one, and traces, counters, durations and custom clocks are bundles of the substitutions that place them. `Substitute` takes bundles and applies their substitutions one after another; `AnalyzedBy` is the same call for analysis. A substitution listed twice in one call applies once, at its last position.

The type name is a working name.

See also: Analyzer, Clock, Instrument.

### Wrapper chain

Status: `Canonical`

A wrapper chain is the ordered set of execution wrappers applied to one selected configuration in a resolution context.

Configured wrappers belong to the configuration graph. Advice wraps the raw executor built for a configuration. Earlier advice wraps closer to that source executor; later advice wraps outside it. A new scope's advice precedes inherited advice. Each advice instance applies once at its first position.

Constructing another chain creates its own executor fields and child bindings. Existing chains keep their controls and advice. Run-bound results are shared within the root scope tree, and `Sharing` retains explicitly selected executors across child scopes.

See also: Advice, Aspect, Configuration, Executor, Resolution scope.

### Invocation wrapper

Status: `Canonical`

An invocation wrapper adds behavior such as retry, timing, counting or per-invocation choice around each call to an executor's named method. The operation builds it into one wrapping executor through `IOperation<TSelf>.Wrap`, which passes every call to the wrapper as an invocation holding the wrapped executor and the call's arguments.

`IInvocationWrapper` represents this contract and is implemented by small structs. The wrapper calls the invocation's `Proceed()` to run the wrapped method; retry, for example, calls it again after a retryable failure. It reads the call's random number generator as `Random` and can call `Proceed(executor, random)` to run the call on another executor of the same operation or with another generator; `ChoosePerInvocation` proceeds on a child it samples with that generator. Wrappers measuring candidate work ask the invocation for `CountCandidateWork(result)`. Each operation writes its invocation as a private struct. Each wrapping executor derives from `WrappingExecutor<TExecutor, TWrapper>` and holds its own wrapped executor, so another binding gets its own children and advice. The wrapping executor exposes the wrapped executor's controls unless the wrapper implements `IExecutorControlProvider` and supplies them.

See also: Operation invocation, Executor, Wrapper chain, Retry.

### Retry

Status: `Canonical`

Retry is a configured wrapper, represented by `Retry<TOperation, TCandidate>`, that repeats a child call only when it throws `RetryableOperationException`. Its maximum number of attempts includes the first call.

Attempts reuse the child, input values and RNG. Failed attempts' state changes and random draws remain; retry provides no rollback.

See also: Child operator, Operation, Executor, Random number generator (RNG).

### Random number generator (RNG)

Status: `Canonical`

A random number generator is the explicit source of randomness passed to algorithms, operators, problems, and execution helpers.

Drawing random values from an RNG changes its state, so draw order matters. Forking an RNG creates deterministic child RNGs without drawing random values from the parent. A user-provided seed creates the root RNG.

See also: Experiment, Run.

### Value array

Status: `Canonical`

A value array is an immutable ordered collection that compares by its elements rather than by the identity of its backing storage, represented by `ValueArray<T>`.

Configurations use a value array for every retained ordered collection, such as child operators, pipeline stages or weights, so that structurally identical configurations compare equal without an equality attribute or a hand-written comparison. An `ImmutableArray<T>` compares by underlying array reference and must not be used for collection state that participates in equality. Executors keep `ImmutableArray<T>`, because they are resolved by reference identity and never compared structurally.

See also: Child operator, Configuration, Executor, Resolution scope.

### Execution concurrency

Status: `Canonical`

Execution concurrency describes whether independent operations must execute sequentially or may execute concurrently.

`Sequential()` preserves input order and requires each operation to finish before the next starts. `Concurrent()` permits all operations to overlap. `Concurrent(maximumConcurrency)` limits active operations without promising sequential ordering.

Execution concurrency describes scheduling permission and bounds. It does not imply dedicated threads.

See also: Executor, Run.

## Analysis and Experiments

### Analyzer

Status: `Canonical`

An analyzer is an immutable instrument definition that derives data from selected execution boundaries. Each run creates its own collector and typed result, obtained through `run.Result(analyzer)`. Attach the definition with `AnalyzedBy` or explicit observation placement. An analyzer is a [substitution bundle](#substitution-bundle) of its placement, which is what `AnalyzedBy` takes.

A trace result supports live reads and immutable snapshots. An accumulating result publishes its domain accumulator and is read after execution has stopped. Reusing a definition across runs keeps their data independent.

Use Roslyn analyzer for compiler diagnostics when the context could be ambiguous.

See also: Accumulator, Analysis snapshot, Instrument, Observation, Run result.

### Analysis snapshot

Status: `Canonical`

An analysis snapshot is an immutable value published from an analyzer at a point in time.

A trace publishes snapshots: `Snapshot()` and `By(clock)` copy its entries, so what a caller holds stays unchanged while the trace collects more. Creating one may allocate, so these are methods rather than properties.

An accumulator does not. Copying a Pareto front or a genealogy graph per read would cost the whole accumulator, so those analyzers publish the object itself and are read once their run has finished.

The run result owns the mutable accumulator. The run exposes it through `Result`, and neither provides automatic disposal.

See also: Analyzer, Observation, Run.

### Trace

Status: `Canonical`

A trace is an ordered history of values that a trace analyzer records while a run executes.

Each trace entry contains an analyzed value and its recorded clock readings. A per-run trace result owns the entries and supports live reads and immutable snapshots during or after execution.

Do not use series for this concept. In HeuristicLib, a series is a named column in tabular data.

`Analyzer.Trace` reads each observation through one function, as in `crossover.Invocations.Trace(observation => observation.Offspring.Count)`. Counting, collecting and single values are plain functions. Ranking functions such as `BestMedianWorst` receive the objective directions they order by, usually the observed invocation's own, as in `observation.ObjectiveVectors.BestMedianWorst(observation.Problem.Objective)`. A best-so-far object keeps its running best across observations: `BestSoFar` the best objective vector, and `BestSoFar<T>` any other value, ranked by the objective vector its selector reads. The first directions it receives fix it, and other directions or another dimensionality throw.

Related terms:

- `Readings`: `Legacy`. The former projection of a source's observations. A trace reads each observation through its function.
- `Aggregation`: `Legacy`. The former object a trace summarized readings with. Use a function, a ranking function or a best-so-far object.
- `Measurement`: `Legacy`. The former value strategy a trace read from observations. Use a function of each observation.

See also: Analysis snapshot, Analyzer, Run.

### Accumulator

Status: `Canonical`

An accumulator is one mutable object updated in place rather than a history of immutable values. A Pareto front, genealogy graph and performance model are accumulators.

An accumulating analysis creates its accumulator separately for each run through `Instrument<TResult>`. Synchronization belongs to the result or its collector. Read its live collections after execution has stopped. Clocks and retention apply to trace entries rather than to the accumulator itself.

See also: Analyzer, Run result, Trace.

### Trace retention

Status: `Canonical`

Trace retention is an immutable policy that creates a separate `RetentionDecider<TValue>` for each trace result. Its decision appends, replaces the latest entry or skips publication. It runs after the value function and never skips that computation. Sharing a policy definition does not share retention progress.

### Clock

Status: `Canonical`

A clock defines one typed notion of time for a trace, which reads it through `IClock<TTime>`. Counters and durations are clocks. A custom clock derives from `Clock<TTime, TResult>`, an instrument whose run-bound result is read as a time. Operator sources offer counters of calls or candidate work and durations; algorithm sources offer counters of iterations or started calls and a duration of iterations. `Clock.Elapsed` reads the root algorithm's active execution time. Custom clocks may read domain quantities such as dynamic-problem epochs.

A counter keeps progressing across executor activations within the run. Reusing its definition in another run creates independent state. A trace explicitly selects its clocks and is projected through those same definition references.

See also: Instrument, Moment, Time, Trace.

### Time

Status: `Canonical`

Time is the typed value read from one clock. Examples include an iteration count, a cumulative candidate count, elapsed
duration and epoch number.

See also: Clock, Moment.

### Moment

Status: `Provisional`

A moment is the collective time of one trace entry. It contains simultaneous readings from every clock selected by
that trace analyzer, and is read back one clock at a time through `TraceEntry.At`.

See also: Clock, Time, Trace.

### Inspection

Status: `Canonical`

Inspection observes and measures execution while preserving the observed operation's inputs, outputs and computation. It may update its own counters, clocks and collected data. Explicit algorithm or operator policies may use those counts and durations to change behavior, such as stopping after a budget is consumed.

`HEAL.HeuristicLib.Inspection` contains observations, predefined observation/counting/timing advice, clocks and accumulators. General aspects and advice belong to the execution model and may change behavior. Analysis derives and publishes results from observations.

See also: Advice, Analyzer, Observation.

### Observation

Status: `Canonical`

An observation is a read-only capture at a defined algorithm or operator boundary, or at a named observation point.

An observation may record data, but it must not change the observed operation's inputs, outputs, or internal computation. Explicit policies may later read observation data to make decisions, such as stopping a run.

Each operation defines the record of one successful invocation, such as `CrossoverObservation<TCandidate>` with `Parents` and `Offspring`; an algorithm's record is `AlgorithmObservation<TCandidate, TSearchState>`. A record holds the call's inputs and outputs in their original types, plus the call's search space and problem, typed as the call receives them. A record of a call without them holds none. An observation of a configuration of your own may be any value, such as the temperature a cooling schedule returned.

Related terms:

- `Observation callback`: the method or runtime delegate that receives an observation.
- `Observed configuration`: the algorithm or operator configuration whose boundary is observed. It is matched by reference, so a copied configuration is observed separately.
- `Operator observation`: the observation record of one operation's invocation. It keeps the operation's domain names for the invocation's inputs and result.
- `Observable operator`: a runtime operator wrapper that reports completed operations to observation callbacks.
- `Observable algorithm`: a runtime algorithm wrapper that reports the search states an algorithm yields, at the end of every iteration.

See also: Algorithm, Analyzer, Observation source, Operator, Run.

### Observation source

Status: `Canonical`

An observation source names the observations of one observed configuration: `Invocations` on an operator for its successful invocations, `Iterations` on an algorithm for the search states it yields, or a point the configuration publishes, such as `Cycles` and `Turns` on a cycle algorithm. `ObservationSource<TObservation>` represents it; the type name is `Provisional`. `ObservationSource.Create` creates a boundary source. An observation point is itself a source, identified by the point reference rather than its value type.

A source describes observations; creating one installs nothing. `AnalyzedBy` places a wrapper recording it into the configuration graph, and `Aspect.Observe` attaches it through an aspect, which resolution weaves into the executors a run builds. In that it resembles a cold `IObservable<T>`, but the library implements neither that interface nor Rx-style operators. A trace observes each boundary once, matched by configuration reference and observation type, so two sources of one configuration and observation type record each observation once. An operator implementing several operations offers each operation's source through a view typed to that operation.

See also: Observation, Trace.

### Experiment

Status: `Provisional`

An experiment is an execution setup that coordinates multiple independent runs.

An experiment is not an algorithm. It does not produce one continuous stream of search states and does not pass search states from one run to the next. A repeated experiment executes the same algorithm configuration multiple times. A comparative experiment executes different algorithm configurations, parameter settings, problems or problem instances.

Random assignment policy is an important part of an experiment because it defines how deterministic random forks are assigned to independent runs, especially when stochastic algorithms are repeated or executed concurrently.

See also: Algorithm, Experiment trial, Problem, Problem instance, Run.

### Experiment trial

Status: `Canonical`

An experiment trial is one materialized algorithm configuration and algorithm run within an experiment run.

Each trial has a deterministic typed key, its own algorithm run, its own resolution scope and its own random number generator fork. Trials do not pass search states or analyzer state to one another.

See also: Algorithm, Experiment, Run.

### Trial analyzer

Status: `Canonical`

A trial analyzer is an analysis definition attached to a trial's algorithm configuration. Each trial's algorithm run creates an independent result, read through `trial.Run.Result(definition)`. A definition may be shared across trials while their collectors, clocks and retention progress remain separate.

See also: Analyzer, Experiment trial, Instrument, Run result.
