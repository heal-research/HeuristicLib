# Glossary

Use this reference when an optimization term is unfamiliar or has a specific meaning in HeuristicLib. The main guide introduces the same concepts through working examples.

## Status labels

Glossary entries may identify related terms by status:

- `Canonical`: the preferred project term.
- `Alias`: an accepted alternate term that may be useful in prose.
- `Legacy`: a term released on `main` that should be replaced with the canonical term when encountered in docs, examples, tests, plans, or new code.
- `Avoid`: a term that should not be used for this concept, usually because it has a different meaning or creates ambiguity.
- `Provisional`: a current term or candidate term that may change after further design discussion.

## Optimization Domain

### Problem

Status: `Canonical`

A problem defines the optimization task that an algorithm operates on.

A problem defines the search space, the evaluation semantics that produce an objective vector, the objective directions and the concrete data or parameters needed for evaluation.

The search space and objective stay fixed for a problem object's lifetime. Mutable instance data may change, as in a dynamic problem. Algorithms and operators bind their executors to that problem object during construction.

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

An algorithm defines a reusable search process through an algorithm configuration and its executors, constructed within a run.

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

`IOperation<TSelf>` identifies an operation in an operator contract. For example, `Mutation` in `IOperator<Mutation, TCandidate>` identifies mutation, whose executor interface is `IMutationExecutor<TCandidate>`. Its static members recognize that executor interface for the run types, wrap an executor so that each invocation passes through an invocation wrapper and state whether invocations report candidate work. Resolution and wrappers reach them through `OperationInfo`. `IItemOperation<TSelf>` marks an operation whose invocations split into items and adds routing them.

Related terms:

- `IOperation` and `IItemOperation`: `Provisional`. The type names may change because they read close to operator.

See also: Executor, Item, Operation invocation, Operator roles.

### Operation invocation

Status: `Canonical`

An operation invocation is one call to an operation method, such as `Mutate`. Names use invocation, as in `IInvocation<TResult>`, an operator's `Invocations` source and `ChoosePerInvocation`. One invocation is one method call regardless of batch size; its candidate work counts what it produced or evaluated.

Every invocation carries its primary inputs followed by a random number generator. Its executor was bound to a problem during construction. `IInvocation<TResult>` exposes that random number generator to invocation wrappers and can repeat the call with the same arguments. A wrapper that needs the problem reads it from the typed scope during construction.

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

An evaluator is the operator role that obtains one objective vector for each input candidate, in input order. Its executor returns objective vectors; pairing them with the input candidates produces evaluated candidates.

Use a refiner to transform candidates. Refinement evaluation may evaluate temporary refined candidates and return their objective vectors for the original candidates.

Use evaluator for the operator role. Use evaluation for the act of obtaining an objective vector.

See also: Candidate, Evaluated candidate, Evaluation, Objective vector, Operator, Refiner.

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

Algorithms normally refine newly created or varied candidates before evaluation and then continue with the refined candidates. Refiners can be chained in pipelines, repeated, selected, wrapped, observed, and analyzed.

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

A configuration can contain settings, child configurations, analyzer definitions and validation or construction logic. It need not be a passive data object. Mutable data belongs to executor fields or run-bound results; the configuration contains no live executor or result.

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

`IOperator<TOperation, TCandidate>` builds its executor through one `CreateExecutor<TSearchSpace, TProblem, TSearchState>(scope)` hook, which it inherits from `IOperator<TCandidate>`. The hook resolves children and looks up run-bound results through the typed scope. Multi-operation configurations implement the hook once and use one executor per problem in a scope. Wrapping and multi bases expose `ChildOperator` and `ChildOperators`.

Algorithms and consumer-defined roles build the same way. An algorithm's `CreateExecutor<TSearchSpace, TProblem>(scope)` receives a scope typed to the run and its search state; an algorithm bound to a search space and problem overrides a `CreateExecutor` typed to them instead. A consumer-defined role implements `IConfigurationNode<TExecutionNode>.CreateExecutionNode(ResolutionScope scope)`. The hook is runtime machinery, not a configuration value strategy.

Iterative algorithms return an `IterativeAlgorithmExecutor<TCandidate, TSearchSpace, TProblem, TSearchState>` from that hook. The algorithm author resolves the optional interceptor and other declared children through the scope and passes them to the iterative executor constructor.

The term does not imply a common child-enumeration API or a universal operation on all nodes.

See also: Configuration, Configuration graph, Executor.

### Configuration graph

Status: `Canonical`

A configuration graph is the graph of reusable configuration nodes connected by references.

Use graph rather than tree because the same configuration object may be shared from more than one place. Do not use DAG as the glossary term; ordinary configuration graphs are expected to be acyclic, but the glossary term should not encode that stricter shape.

See also: Configuration, Execution graph.

### Pointcut

Status: `Legacy`

A former selector of the configurations whose executors received advice. A substitution selects instead: by reference, by the type of a lambda's parameter or by a predicate, such as `node is INamedConfiguration { Name: "exploration" }` for a name.

See also: Named configuration, Substitution group.

### Named configuration

Status: `Canonical`

A named configuration carries a descriptive `Name` through `INamedConfiguration`. Configurations may implement the contract directly; the predefined `Named*` wrappers add it while retaining an operator or algorithm role and forwarding the child's executor.

Names are case-sensitive labels and need not be unique. A named wrapper and its child are separate configuration nodes. A selection by name selects the wrapper; it does not assign that name to the child or introduce a caller path. Configuration identity and execution sharing still use configuration references.

See also: Configuration node, Substitution group.

### Advice

Status: `Legacy`

A former mechanism that wrapped the executors built for selected configurations from outside the configuration graph. Use a wrapper: a configuration that holds the configuration it wraps as its declared child, placed with `Substitute`. See [Writing wrappers](/guide/extending/writing-wrappers).

See also: Invocation wrapper, Substitution group, Wrapper chain.

### Join point

Status: `Legacy`

The former name for the executor that advice wrapped. A wrapper holds its configuration instead, and wraps its child's executor.

See also: Advice, Executor.

### Aspect

Status: `Legacy`

A former ordered list of pointcut and advice pairs, attached through `AddAspect`. Use a substitution group.

See also: Advice, Pointcut, Substitution group.

### Run

Status: `Canonical`

A run is an object that owns one logical execution setup.

An `AlgorithmRun` executes one algorithm configuration on a problem. An `ExperimentRun` coordinates the independent algorithm runs materialized by one experiment configuration. Prefer the concrete term when the distinction matters.

When a meta-algorithm coordinates child algorithms, the run is created by the algorithm started by the user. Child algorithms and operators participate in that same run unless they are explicitly started as separate runs.

An algorithm's `CreateRun` accepts an optional list of `ISubstitution` values as `substitutions`, including analyzers, counter and duration placements, clocks that observe and plain substitutions. It places the whole list in one call before validation and checks that result-producing definitions with non-empty expansions are held by the graph. Place them with `Substitute` when keeping the resulting configuration. The returned run starts in `Ready` and accepts no later attachments.

See also: Analyzer, Configuration, Executor, Search state.

### Run result

Status: `Canonical`

A run result is an execution node implementing `IRunResult`, such as an analyzer's collected data, a counter's or duration's accumulator, or a clock reading. `RunResults` looks it up by configuration reference; callers use `run.Result(configuration)`. It is created at the first lookup and remains available after stopping or failure. Each result defines its supported concurrent reads.

All scopes of one root use the same result lookup, which reads the root scope. An analyzer's result spans the run, including Cycle turns. Resolving counters and durations honors a nearer sharing scope, whose accumulator is separate from the root result.

See also: Analyzer, Counter, Duration, Run, Resolution scope.

### Search state

Status: `Canonical`

A search state is the public state value produced by an algorithm while it searches a search space.

Search states describe the visible progress of a search process, such as the current evaluated candidate or current population. They may be streamed, inspected, analyzed, or returned as the final state of a run.

Search state is the public value representing search progress. Executor fields and resources are machinery used to produce that progress.

Use search terminology for the state and the search space, but do not use search algorithm as the default term for every HeuristicLib algorithm. Prefer algorithm, optimization algorithm, metaheuristic, or a more specific algorithm family where appropriate.

See also: Candidate, Evaluated candidate, Executor, Resource, Run, Search space.

### Executor

Status: `Canonical`

An executor is the executing kind of execution node: a runtime object that performs one or more operations of an algorithm or operator. It exposes callable methods such as `Mutate`, `Evaluate` or `RunStreamingAsync`. It holds resolved children, private fields and any explicitly shared resources. Private data lives with its executor.

Algorithms and other executors call its operation methods. One executor can receive many invocations and can support several operations. Each construction has independent private fields. Use explicit `Sharing` to keep an executor across child scopes. A context-free stateless configuration can also serve as its own executor; other stateless helpers retain the binding in a small executor.

The common interface is `IExecutor : IExecutionNode`. Operation-specific interfaces include `IMutationExecutor` and `IEvaluationExecutor`; algorithms use `IAlgorithmExecutor`. The term does not give the object scheduling, threading, disposal or run-lifecycle responsibilities.

See also: Configuration, Execution, Execution graph, Resource, Operation, Operation invocation, Run.

### Execution

Status: `Canonical`

Execution is the activity of performing work. Use the term in execution time, execution concurrency and execution graph. Use executor for the callable runtime object, operation for the action it supports, and invocation for one actual method call.

`AlgorithmRun` owns a run's lifecycle. An executor performs the work that participates in that run.

See also: Executor, Execution concurrency, Resource, Operation, Operation invocation, Run.

### Execution node

Status: `Canonical`

An execution node is a runtime object created by a configuration and held in a resolution scope's table. `IExecutionNode` is its marker interface. An executor performs operations; an adaptation applies a runtime value to a base value; a run result holds collected data. The last two do not need to execute operations.

`IConfigurationNode<TExecutionNode>.CreateExecutionNode` constructs nodes whose type is independent of the run's types. Operators and algorithms retain their typed `CreateExecutor` hooks.

See also: Adaptation, Configuration, Executor, Execution graph, Run result.

### Adaptation

Status: `Canonical`

An adaptation is an optional configuration link applying a runtime value to an operator's configured base value. `IAdaptation.Apply(double)` reads it. Built-in adaptation nodes derive from `Adaptation<TAdapted>` and use reference equality: the same instance links readers and writers to one runtime value within a scope.

`ScaleAdaptation` produces an `AdaptedScale` with a settable `Factor` starting at 1; `OffsetAdaptation` produces an `AdaptedOffset` with a settable `Offset` starting at 0. A reader names `IConfigurationNode<IAdaptation>?`; a rule's writer names the form it updates. Share the adaptation when its value must survive child scopes, including when sharing a mutator that reads it. Independent runs create independent values.

See also: Configuration, Execution node, Parameter, Resolution scope.

### Execution instance

Status: `Legacy`

A former name for executor. Use executor or a role-specific form such as mutation executor.

See also: Executor.

### Resource

Status: `Canonical`

A resource is a runtime dependency with its own ownership and concurrency contract. Private resources such as an evaluation cache belong to an executor. Share that executor through `Sharing` when several uses need the same data. Accounting and analysis accumulators are run results.

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

An execution graph is a graph of execution nodes created from a configuration graph during a run. It contains executing nodes and non-executing nodes such as adaptations and run results.

A run may contain more than one execution graph over time or through different observation contexts. A shared configuration has one executor per problem reference for its uses within the sharing node.

See also: Configuration graph, Executor, Run.

### Resolution scope

Status: `Canonical`

A resolution scope resolves configurations to execution nodes during a run.

The scope holds one execution node per configuration/problem reference pair for algorithms and operators, and per configuration reference for configurations whose execution node needs no problem, such as counters and adaptations. All its problem views use the root's run results. `For<TCandidate, TSearchSpace, TProblem>(problem)` is a view of the same scope; only `CreateChildScope()` creates a fresh activation. `CreateChildScope()` inherits explicit sharing while constructing independent execution nodes for other configurations. `Share` selects configurations retained for child scopes. Independent roots have separate results.

See [execution resolution](/contributing/architecture/execution-resolution) for state, construction and observation rules.

See also: Configuration, Wrapper chain, Execution graph, Executor, Run.

### Shared configuration

Status: `Canonical`

A shared algorithm or operator configuration resolves to one executor per problem reference in the nearest scope that shares it. Plain configuration nodes keep configuration-only identity. Its first construction uses a dedicated child scope of that owner, and every request for that binding receives that executor. Other configurations have one executor per requesting scope. A new root or sharing owner creates independent data.

The configuration reference identifies what is shared. Sharing preserves private executor fields and resolved children for that owner's lifetime.

See [execution resolution](/contributing/architecture/execution-resolution#shared-configurations).

See also: Configuration, Executor, Resolution scope.

### Substitution

Status: `Provisional`

A substitution is a rewrite applied while building a configuration, never stored or serialized. Persist the ordinary configuration it produced, containing the nodes it placed. `ISubstitution.Expand()` returns the single substitutions it stands for in application order, with the same instances on every call; applying belongs to the call. `Substitution` is one sealed rewrite created through its `Of` factories, selecting by reference, type or predicate. A call matches targets against the graph it received and applies a repeated substitution instance once, at its last position.

See also: Configuration node, Substitution group.

### Substitution group

Status: `Provisional`

A substitution group holds parts that belong together and caches their depth-first expansion. A part may itself be a group; a group containing itself is an error. Later parts sit closer to their targets; list a shared clock after what reads it. `SubstitutionGroup` is the authoring base used by analyzers and observation callbacks, and by clocks that observe.

See also: Analyzer, Clock, Substitution.

### Wrapper chain

Status: `Canonical`

A wrapper chain is the ordered set of wrappers around one configuration, each holding the next as its declared child.

Wrappers belong to the configuration graph, so the configuration states the order: a wrapper written inside another is closer to the executor. Substitutions apply one after another, and a later one sits closer to the node it matches.

Constructing another chain creates its own executor fields and child bindings. Existing chains keep their original children and adaptation links. Run-bound results are shared within the root scope tree, and `Sharing` retains explicitly selected executors across child scopes.

See also: Configuration, Executor, Invocation wrapper, Resolution scope, Substitution group.

### Invocation wrapper

Status: `Canonical`

An invocation wrapper adds behavior such as retry, timing, counting or per-invocation choice around each call to an executor's named method. The operation builds it into one wrapping executor through `IOperation<TSelf>.Wrap`, which passes every call to the wrapper as an invocation holding the wrapped executor and the call's arguments.

`IInvocationWrapper` represents this contract and is implemented by small structs. The wrapper calls the invocation's `Proceed()` to run the wrapped method; retry, for example, calls it again after a retryable failure. It reads the call's random number generator as `Random` and can call `Proceed(executor, random)` to run the call on another executor of the same operation, or with another generator; `ChoosePerInvocation` proceeds on a child it samples with that generator. Each executor runs against the problem it was bound to. A wrapper that needs the problem reads the typed `scope.Problem` in its own `CreateExecutor` and retains it in its wrapper struct or executor. Wrappers measuring candidate work ask the invocation for `CountCandidateWork(result)`. Each operation writes its invocation as a private struct. Each wrapping executor derives from `WrappingExecutor<TExecutor, TWrapper>` and holds its own wrapped executor, so another binding gets its own children.

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

An analyzer places observation wrappers and defines how observations are handled. A result-producing analyzer creates each run's collected data and derives from `Analyzer<TResult>`, an immutable configuration node whose execution node implements `IRunResult`. Read it with `run.Result(analyzer)`. It is also a substitution group, placed through `CreateRun` or `Substitute`, or through explicit observation wrappers.

`IObservationReceiver<TObservation>` is the part that receives one observation type through `CreateRecorder(RunResults)`. One analyzer may implement it for several observation types; other configuration nodes placed by `Observed`, such as clocks that observe, implement it too. The callback behind `Observing` implements it without a result or the `Analyzer<TResult>` base.

An analyzer's result spans the run, including Cycle turns; a `Share` does not narrow the result that recording and `run.Result` use, and resolving a shared analyzer directly gives a separate result that nothing records into. Reusing a definition across runs keeps their data independent. Use Roslyn analyzer for compiler diagnostics when the context could be ambiguous.

See also: Accumulator, Analysis snapshot, Observation, Observer, Run result, Substitution group.

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

Each `TraceEntry<TValue>` contains an analyzed value and its recorded clock readings. The per-run result, `Trace<TValue>`, owns the entries, supports live reads and immutable snapshots, and enumerates a snapshot. A general trace records only the clocks it is given.

Do not use series for this concept. In HeuristicLib, a series is a named column in tabular data.

`Analyzer.Trace` reads each observation through one function, as in `crossover.Invocations.Trace(observation => observation.Offspring.Count)`. Counting, collecting and single values are plain functions. Ranking functions such as `BestMedianWorst` receive the objective directions they order by, usually the observed invocation's own, as in `observation.ObjectiveVectors.BestMedianWorst(observation.Problem.Objective)`. A best-so-far object keeps its running best across observations: `BestSoFar` the best objective vector, and `BestSoFar<T>` any other value, ranked by the objective vector its selector reads. Create that state through the trace's state factory so each run has its own. The first directions it receives fix it, and other directions or another dimensionality throw.

Related terms:

- `Aggregation`: `Legacy`. The former object a trace summarized readings with. Use a function, a ranking function or a best-so-far object.
- `Measurement`: `Legacy`. The former value strategy a trace read from observations. Use a function of each observation.

See also: Analysis snapshot, Analyzer, Best-so-far analyzer, Moment, Run.

### Accumulator

Status: `Canonical`

An accumulator is one mutable object updated in place rather than a history of immutable values. A Pareto front, genealogy graph and performance model are accumulators.

An accumulating analysis creates its accumulator separately for each run through `Analyzer<TResult>`. `Aggregate` wraps plain state for this purpose. Read live collections after execution has stopped unless the state type supports concurrent reads. Clocks and retention apply to trace entries rather than to the accumulator itself.

See also: Aggregate, Analyzer, Run result, Trace.

### Aggregate

Status: `Provisional`

An aggregate updates one run's state in place from observations. `Analyzer.Aggregate(sources, create, update)` and `source.Aggregate(create, update)` create an `AggregateAnalyzer<TObservation, TState>`. Each run calls the state factory once and serializes updates to its state. The result, `Aggregate<TState>`, exposes that state as `Value`; the state type needs no `IRunResult` marker.

The analyzer holds delegates, so a configuration containing it is not serializable. Concurrent reads are the state type's concern.

See also: Accumulator, Analyzer, Observation source, Run result.

### Best-so-far analyzer

Status: `Canonical`

A best-so-far analyzer records the running best evaluated candidate. `Analyzer.BestSoFar<TCandidate>()` creates a `BestSoFarAnalyzer<TCandidate>` selecting evaluators of that candidate type with no such evaluator among their declared descendants. `Analyzer.BestSoFar(evaluator)` selects one evaluator by reference and infers the candidate type. Name the evaluator when a composite evaluator or an inner problem needs a different boundary.

It places an observer and its public `Evaluations` counter, counting evaluated candidates before the observer records. Its result retains improvements only by default, with optional retention, objective comparer and additional clocks. Explicit placement must place both the counter and the observer.

See also: Analyzer, Best-so-far curve, Best-so-far point, Counter, Evaluator.

### Best-so-far curve

Status: `Provisional`

A best-so-far curve is one run's retained running bests, represented by `BestSoFarCurve<TCandidate>`. It implements `IRunResult`, exposes `Count` and nullable `Latest`, and enumerates a snapshot safe to read during collection. It continues across fresh executor activations in the run.

See also: Best-so-far analyzer, Best-so-far point, Run result.

### Best-so-far point

Status: `Provisional`

A best-so-far point is one retained entry of a best-so-far curve, represented by `BestSoFarPoint<TCandidate>`. It gives the cumulative `Evaluations`, the best objective vector as `Value` and its `Candidate`. Evaluation count is its only default axis. Additional clock readings, such as elapsed time, are read with `At(clock)` using a clock given to the analyzer.

See also: Best-so-far curve, Clock, Moment.

### Trace retention

Status: `Canonical`

Trace retention is an immutable policy that creates a separate `RetentionDecider<TValue>` for each trace result. Its decision appends, replaces the latest entry or skips publication. It runs after the value function and never skips that computation. Sharing a policy definition does not share retention progress.

### Clock

Status: `Canonical`

A clock implements `IClock<TTime>`, a configuration node whose execution node is an `IClockReading<TTime>`. Its reading is a run result owned by the root; `Read()` returns its typed time. Mixed clock lists use `IReadOnlyList<IClock>`. Typed reads such as `At` and `By` infer `TTime`; `results.Result(clock)` infers through `IClock`, while `IClock<TTime>` needs an explicit result type because it has two configuration-node constructions. `IClockReading.Read()` boxes values for mixed clock lists. A clock only reads. One needing placement also implements `ISubstitution`, and the caller lists it after the analyzers that read it. `counter.Clock`, `duration.Clock` and `Clock.Elapsed` place nothing. Custom clocks may read domain quantities such as dynamic-problem epochs.

A trace explicitly selects its clocks, deduplicates them by equality and projects entries through equal clocks. Two clocks of one counter or duration are equal; every `ElapsedClock` equals every other. A class clock without an equality override, such as the epoch clock, matches by reference. Column order and readings are fixed when the result is created; each retained entry stores one row of readings. Each clock instance has one reading per run, even when another instance is equal. Reads may be concurrent and must preserve the observed computation.

See also: Counter, Duration, Elapsed time, Moment, Time, Trace.

### Counter

Status: `Canonical`

A counter is a configuration link to a `CountAccumulator`, read with `run.Result(counter).CurrentCount`. Its `counter.Clock` property creates a `CounterClock` reading that root accumulator, whatever a nearer `Share` narrows. Repeated reads of the property give equal clocks. Operator sources offer `Counting(counter)` and `CountingCandidates(counter)` placements for successful invocations or candidate work; algorithm sources offer `Counting(counter)` for iterations or started calls.

A counter places nothing. Create one with `new Counter()` and place a wrapper with a source substitution or directly with `Counted` or `CountedAlgorithm`, which states what is counted. A counter placed nowhere reads zero, also through its clock in a trace. Counts span the run unless resolution shares the counter in a nearer scope, as a budget does to restart its allowance. Independent runs keep separate counts.

See also: Clock, Duration, Run result, Shared configuration.

### Duration

Status: `Canonical`

A duration is a configuration link to a `DurationAccumulator`, read with `run.Result(duration).CurrentDuration`. Its `duration.Clock` property creates a `DurationClock` reading that root accumulator, whatever a nearer `Share` narrows. Repeated reads of the property give equal clocks. `Timed` wrappers add the time spent in calls, including failed calls, using the run's time provider.

A duration places nothing. Create one with `new Duration()` and place a wrapper with a source's `Timing(duration)` substitution or directly with `Timed` or `TimedAlgorithm`. A duration placed nowhere reads zero, also through its clock in a trace. It spans the run unless resolution shares it in a nearer scope, as a budget does. Independent runs keep separate durations.

See also: Clock, Counter, Elapsed time, Run result.

### Elapsed time

Status: `Canonical`

Elapsed time is the time an `AlgorithmRun` spends advancing its retained algorithm iterator, read through an `ElapsedClock` such as `Clock.Elapsed`, or `RunResults.Elapsed`. All elapsed clocks are equal, so `new ElapsedClock()` identifies the column selected with `Clock.Elapsed`. It includes the current advance and deferred child construction during it. Initial executor construction, pauses and consumer work between states are excluded.

Direct scope execution leaves it at zero; use `algorithm.Timed(duration)` to measure that execution explicitly.

See also: Clock, Duration, Run.

### Time

Status: `Canonical`

Time is the typed value returned by one clock reading's `Read()`. Examples include an iteration count, a cumulative candidate count, elapsed
duration and epoch number.

See also: Clock, Moment.

### Moment

Status: `Provisional`

A moment is the row of clock readings recorded for one trace entry or best-so-far point. Its internal `Moment` shares a fixed column layout with the other rows in that result. Clocks are read in column order when an entry is retained; the row is not an atomic snapshot of all clocks.

`TraceEntry.At(clock)` and `BestSoFarPoint.At(clock)` find a clock by equality and reject a clock unequal to every clock the analyzer was given. Equal clocks identify one column.

See also: Best-so-far point, Clock, Time, Trace.

### Inspection

Status: `Canonical`

Inspection observes and measures execution while preserving the observed operation's inputs, outputs and computation. It may update its own counters, clocks and collected data. Explicit algorithm or operator policies may use those counts and durations to change behavior, such as stopping after a budget is consumed.

`HEAL.HeuristicLib.Inspection` contains observations, predefined observation, counting and timing wrappers, clocks and accumulators. Wrappers of your own belong to the execution model and may change behavior. Analysis derives and publishes results from observations.

See also: Analyzer, Observation, Wrapper chain.

### Observation

Status: `Canonical`

An observation is a read-only capture at a defined algorithm or operator boundary, or at the boundary of a consumer-defined role.

An observation may record data, but it must not change the observed operation's inputs, outputs, or internal computation. Explicit policies may later read observation data to make decisions, such as stopping a run.

Each operation defines the record of one successful invocation, such as `CrossoverObservation<TCandidate>` with `Parents` and `Offspring`; an algorithm's record is `AlgorithmObservation<TCandidate, TSearchState>`. A record holds the call's inputs and outputs in their original types, plus the executor's bound search space and problem. A record of a call without them holds none. An observation of a configuration of your own may be any value, such as the temperature a cooling schedule returned.

Related terms:

- `Observation callback`: the method or runtime delegate that receives an observation.
- `Observed configuration`: the algorithm or operator configuration whose boundary is observed. It is matched by reference, so a copied configuration is observed separately.
- `Operator observation`: the observation record of one operation's invocation. It keeps the operation's domain names for the invocation's inputs and result.
- `Observable operator`: an operator configuration wrapped with an observer that reports completed operations.
- `Observable algorithm`: an algorithm configuration wrapped with an observer that reports its iterations or call starts and outcomes.

See also: Algorithm, Analyzer, Observation source, Observer, Operator, Run.

### Observer

Status: `Canonical`

An observer is a configuration wrapper that captures observations during execution and passes them to its receiver, usually an analyzer. `Observed` wraps an operator; `ObservedAlgorithm` wraps an algorithm. It holds its receiver and binds the recorder for that run through `IObservationReceiver<TObservation>.CreateRecorder`.

Explicit placement and substitution use the same wrappers.

See also: Analyzer, Observation, Observation source, Wrapper chain.

### Observation source

Status: `Canonical`

An observation source names a boundary of one configuration: `Invocations` on an operator for successful invocations, or `Iterations` and `Calls` on an algorithm for yielded states and call starts and outcomes. `ObservationSource<TObservation>` represents it; the type name is `Provisional`. `ObservationSource.Create` also supports a consumer-defined role with its own observation wrapper.

Creating a source installs nothing. `RecordingInto(analyzer)` creates its placement substitution; `CreateRun` or `Substitute` applies it as part of an analyzer's group. `Observing` forwards observations to a callback without collecting a result. Sources are matched by configuration reference and observation type, so a trace over two sources of the same boundary observes it once. An operator supporting several operations offers each source through a view typed to that operation.

See also: Analyzer, Observation, Observer, Substitution, Trace.

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

See also: Analyzer, Experiment trial, Run result.
