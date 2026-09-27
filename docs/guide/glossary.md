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

An algorithm defines a reusable search process through an algorithm configuration and its run scoped execution nodes.

In a run, an algorithm execution advances search state until the run stops. It owns resolved child operator executions, private execution data and execution behavior. One shot algorithms are still algorithms. They produce a search process with a single state.

Use algorithm for the reusable configuration unless the text explicitly says algorithm execution.

See also: Configuration, Execution node, Operator, Run, Search state.

### Meta-algorithm

Status: `Canonical`

A meta-algorithm is an algorithm whose search process is defined by coordinating one or more child algorithms.

A meta-algorithm is still an algorithm: it produces search states as part of a run and may be used wherever an algorithm is expected. The distinction is that a meta-algorithm coordinates algorithms, not just operators.

Do not confuse a meta-algorithm with an experiment. A meta-algorithm composes child algorithms inside one run; an experiment coordinates multiple independent runs.

Related terms:

- `Composed algorithm`: descriptive related term. Prefer meta-algorithm for the canonical concept.

See also: Algorithm, Experiment, Run.

### Operator

Status: `Canonical`

An operator is a reusable building block that an algorithm or another operator calls to perform one named operation in the search process.

Examples include creators, evaluators, selectors, crossovers, mutators, replacers, terminators, and interceptors. An operator does not own the overall search process or define the run's stream of search states.

Use operator for the reusable configuration unless the text explicitly says operator execution.

See also: Algorithm, Configuration, Evaluator, Execution node.

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

An interceptor is the operator role that transforms a produced search state before it is yielded or observed by state based stopping logic.

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

A configuration can contain parameters, child configurations, validation or helper logic and the mechanism that creates execution nodes. It is not required to be a passive data object. Mutable execution data belongs to an execution node, either directly or in framework managed operator state.

Use more specific terms when the context benefits from them:

- `algorithm configuration`
- `operator configuration`

Related terms:

- `Definition`: `Legacy`. Older docs may use definition for the reusable configured object graph. Prefer configuration in new text.

See also: Algorithm, Execution node, Operator, Run.

### Configuration node

Status: `Canonical`

A configuration node is a configuration considered as a node in the configuration graph. Its identity is the configuration object's reference, not its value equality or one occurrence along a caller path. Several configurations may refer to the same child node.

Configuration is accepted shorthand for configuration node when the context is clear. Use node when graph membership needs emphasis, or a role-specific term such as mutator when the role is known. The common interface is `IConfigurationNode`, including its generic form. Role-specific configuration names such as `IOperator` and `IMutator` do not acquire a `Node` suffix.

The term does not imply a common child-enumeration API or a universal operation on all nodes.

See also: Configuration, Configuration graph, Execution node, Node selector.

### Configuration graph

Status: `Canonical`

A configuration graph is the graph of reusable configuration nodes connected by references.

Use graph rather than tree because the same configuration object may be shared from more than one place. Do not use DAG as the glossary term; ordinary configuration graphs are expected to be acyclic, but the glossary term should not encode that stricter shape.

See also: Configuration, Execution graph.

### Node selector

Status: `Provisional`

A node selector is a rule for selecting algorithm or operator configurations in a configuration graph. The first `NodeSelector<TConfiguration>` API supports matching individual supplied configurations; integration with attached behavior is planned.

The current forms select by configuration reference or assignable type, including role interfaces. Selectors with the same declared configuration type or role compose by union (`Or`, `|`) or intersection (`And`, `&`). `And(predicate)` intersects with a selector constructed from the typed configuration predicate. Composition creates a new definition without changing its operands or evaluating predicates; matching evaluates predicates from left to right only as needed. A node matching several union branches still matches once. The selector does not traverse a graph or resolve execution nodes. Combinations across different roles remain deferred.

The agreed integration design selects source configuration nodes. Wrappers introduced by decoration are not additional selection targets, even when the decoration machinery represents them as configurations. Explicit wrappers in the source configuration graph remain selectable nodes. Multiple modules select the same source independently and compose their behavior using the decoration order; they do not select each other's generated wrappers. Resolver integration enforcing this boundary is planned.

A selected configuration can be referenced by several callers and resolved into more than one execution node. Selecting that configuration does not select one caller path or one invocation. Nesting selection is separate deferred work.

See also: Configuration, Configuration graph, Execution node, Execution module, Resolution scope.

### Run

Status: `Canonical`

A run is an object that owns one logical execution setup.

An `AlgorithmRun` executes one algorithm configuration on a problem. An `ExperimentRun` coordinates the independent algorithm runs materialized by one experiment configuration. Prefer the concrete term when the distinction matters.

When a meta-algorithm coordinates child algorithms, the run is created by the algorithm started by the user. Child algorithms and operators participate in that same run unless they are explicitly started as separate runs.

Run analyzers are attached while a run is preparing and installed when execution starts, so their observations can span nested algorithms and multiple short-lived execution nodes. A run does not accept more analyzers after it leaves `Preparing`.

See also: Analyzer, Configuration, Execution node, Search state.

### Search state

Status: `Canonical`

A search state is the public state value produced by an algorithm while it searches a search space.

Search states describe the visible progress of a search process, such as the current evaluated candidate or current population. They may be streamed, inspected, analyzed, or returned as the final state of a run.

Search state is distinct from execution state. Execution state is private machinery used to produce progress; search state is the public value that represents that progress.

Use search terminology for the state and the search space, but do not use search algorithm as the default term for every HeuristicLib algorithm. Prefer algorithm, optimization algorithm, metaheuristic, or a more specific algorithm family where appropriate.

See also: Candidate, Evaluated candidate, Execution state, Run, Search space.

### Execution node

Status: `Canonical`

An execution node is the resolved runtime object that implements the operations of an algorithm or operator. Its resolved child references connect it to other execution nodes. It may hold private execution data or references to that data.

An execution node is distinct from a run, a single operation invocation and the planned persistent execution record in the resolver. The name alone does not promise a particular lifetime, a fresh allocation or a separate state object. A node can be shorter-lived than the run; a stateless configuration can also serve as its own execution node.

The common interface is `IExecutionNode`. Role-specific contracts use names such as `IOperatorExecution`, `IMutatorExecution` and `IAlgorithmExecution`, without a `Node` suffix on those roles. These names do not themselves change execution-state ownership or resolution.

Execution is accepted shorthand for execution node when the context clearly refers to the callable object. Use node when graph membership needs emphasis, or role-specific terms such as algorithm execution, operator execution or mutator execution when the role is known. Use invocation or run when referring to execution over time.

See also: Configuration node, Execution graph, Execution state, Run.

### Execution instance

Status: `Legacy`

The former term for an execution node. Prefer execution node or a role-specific form such as mutator execution. Object-returning creation methods temporarily retain `CreateExecutionInstance`, `WrapExecutionInstance` and `CombineExecutionInstances` until the separately reviewed typed-factory migration; references to those methods must use their actual names.

See also: Execution node.

### Execution state

Status: `Canonical`

Execution state is private mutable data used by an execution node. The current implementation owns it on that object or in framework-managed operator state; separating persistent ownership from replaceable execution nodes is planned, not yet implemented.

Execution state can contain resolved child execution nodes, counters, caches, buffers, or other data that must not be shared through the reusable configuration.

Execution state is distinct from search state. Search states describe visible progress through the search and may be streamed, inspected, analyzed, or returned as the final state of a run.

See also: Configuration, Execution node, Run, Search state.

### Execution graph

Status: `Canonical`

An execution graph is a graph of execution nodes created from a configuration graph during a run.

A run may contain more than one execution graph over time, for example when meta-algorithms create fresh execution nodes for nested algorithms.

See also: Configuration graph, Execution node, Run.

### Resolution scope

Status: `Canonical`

A resolution scope resolves configurations to execution nodes during a run.

The scope controls execution-node identity and sharing. Explicit operator and algorithm execution creation methods receive the scope and normally resolve their declared children eagerly. Execution graph compositions may additionally create child scopes, declare decorations for them or control execution reuse. Decorations are declared on a `ResolutionScopeBuilder` before the scope resolves anything.

See also: Configuration, Decoration chain, Execution graph, Execution node, Execution module, Run.

### Decoration chain

Status: `Canonical`

A decoration chain is the ordered set of decorations that apply to one configuration at one resolution scope: every decoration declared by that scope or any of its ancestors, and no others.

The chain determines whether an existing ancestor execution is eligible for reuse. Resolution searches its own scope and then ancestors, stopping at a scope that declares decorations for that configuration if no execution was found there. An execution built in a child stays in that child. Identical chains therefore permit reuse but do not guarantee it: a child may have built its own execution before its parent resolved the configuration. Siblings cannot read each other's caches, but can both reuse an execution already held by a common ancestor.

Within a chain, decorations declared by an execution module sit outside those declared by the configuration, and a deeper scope's decorations bind more tightly than a shallower one's.

See also: Configuration, Execution node, Execution module, Resolution scope.

### Execution module

Status: `Canonical`

An execution module adds behavior at chosen configurations in a run's execution graph, before the graph is resolved, represented by `IExecutionModule`.

A module declares its decorations on a `ResolutionScopeBuilder` and resolves nothing itself, so it cannot participate in building the graph it decorates. Run-level additions wrap configuration-level ones, so a module always sees the fully configured operator.

Analyzers install most of the modules a run sees, but the contract is not analysis specific. Writing to a log, reporting progress, advancing a dynamic problem at an iteration boundary and bridging to another runtime are equally valid modules.

See also: Analyzer, Configuration, Decoration chain, Observation, Resolution scope, Run.

### Random number generator (RNG)

Status: `Canonical`

A random number generator is the explicit source of randomness passed to algorithms, operators, problems, and execution helpers.

Drawing random values from an RNG changes its state, so draw order matters. Forking an RNG creates deterministic child RNGs without drawing random values from the parent. A user-provided seed creates the root RNG.

See also: Experiment, Run.

### Value array

Status: `Canonical`

A value array is an immutable ordered collection that compares by its elements rather than by the identity of its backing storage, represented by `ValueArray<T>`.

Configurations use a value array for every retained ordered collection, such as child operators, pipeline stages or weights, so that structurally identical configurations compare equal without an equality attribute or a hand-written comparison. An `ImmutableArray<T>` compares by underlying array reference and must not be used for collection state that participates in equality. Execution nodes keep `ImmutableArray<T>`, because they are resolved by reference identity and never compared structurally.

See also: Child operator, Configuration, Execution node, Resolution scope.

### Execution concurrency

Status: `Canonical`

Execution concurrency describes whether independent operations must execute sequentially or may execute concurrently.

`Sequential()` preserves input order and requires each operation to finish before the next starts. `Concurrent()` permits all operations to overlap. `Concurrent(maximumConcurrency)` limits active operations without promising sequential ordering.

Execution concurrency describes scheduling permission and bounds. It does not imply dedicated threads.

See also: Execution node, Run.

## Analysis and Experiments

### Analyzer

Status: `Canonical`

An analyzer is a stateful execution module that records or derives information from selected execution boundaries. `IAnalyzer` inherits `IExecutionModule`, and runs attach it through `Attach` alongside other modules.

The caller creates an analyzer for selected algorithm or operator sources, clocks and analysis behavior. A trace owns its aggregation and retention objects, which are never resolved through a run. The run installs observations when execution starts, before materializing the execution graph. Reusing an analyzer across runs intentionally combines its history. A trace exposes typed reads during and after execution. An accumulating analyzer is read once its run has finished.

Properties on a trace return already published values without allocation. Its methods may allocate immutable snapshots or projections, and it does not expose its mutable entries through a read-only collection interface. An accumulating analyzer publishes the accumulator itself instead, as described under Accumulator.

Do not use analyzer for Roslyn analyzers without the Roslyn qualifier when the context could be ambiguous.

See also: Accumulator, Analysis snapshot, Observation, Run.

### Analysis snapshot

Status: `Canonical`

An analysis snapshot is an immutable value published from an analyzer at a point in time.

A trace publishes snapshots: `Snapshot()` and `By(clock)` copy its entries, so what a caller holds stays unchanged while the trace collects more. Creating one may allocate, so these are methods rather than properties.

An accumulator does not. Copying a Pareto front or a genealogy graph per read would cost the whole accumulator, so those analyzers publish the object itself and are read once their run has finished.

The analyzer remains the owner of its mutable accumulator. The run installs observations but does not own analyzer disposal or provide result lookup.

See also: Analyzer, Observation, Run.

### Trace

Status: `Canonical`

A trace is an ordered history of values that a trace analyzer records while a run executes.

Each trace entry contains one analyzed value and the moment when the value was observed. A trace analyzer owns its
mutable trace and exposes safe live reads and immutable trace snapshots during and after execution.

Do not use series for this concept. In HeuristicLib, a series is a named column in tabular data.

See also: Analysis snapshot, Analyzer, Run.

### Accumulator

Status: `Canonical`

An accumulator is the single object an analyzer updates in place as it observes a run, rather than a history of
values. A Pareto front, a genealogy graph and a fitted model are accumulators.

An analyzer holding one derives from `AccumulatingAnalyzer` and updates it under the analyzer's lock. What it
publishes is that object, not a copy of it, so it is read once the run has finished. Neither trace retention nor
clocks apply to an accumulator: there is nothing to store apart from what was computed, and one current state has no
moment of its own.

See also: Trace, Analyzer.

### Trace retention

Status: `Canonical`

Trace retention decides what a trace does with an aggregated observation: append it as a new entry, store it over the entry before it, or drop it. `TraceRetention` is an object that holds whatever counting or remembering its policy needs, so each trace is given its own. Retention happens after measurement and aggregation and never skips that work. A policy that appends or drops leaves stored entries untouched; `TraceRetention.LatestOnly()` replaces, so a trace using it keeps one entry rather than a history.

### Measurement

Status: `Canonical`

A measurement is what a trace reads from one observation, such as the objective vectors of a population or the offspring of a crossover. It receives the typed observation of the boundary it observes and returns the readings an aggregation then summarizes.

A named measurement is an immutable value strategy implementing `IMeasurement<TInput, TValue>`, usually reached through `Measurement`. A delegate passed to `Analyzer.Trace` is a runtime-only projection with identity semantics and no value-equality or serialization contract.

See also: Aggregation, Observation, Trace.

### Aggregation

Status: `Canonical`

An aggregation turns an observation's readings into an immutable result and may accumulate across observations. `IAggregation<TValue, TResult>` is an object with one `Aggregate` method, which owns any history it keeps, so each trace is given its own. A stateless aggregation summarizes each observation independently and holds nothing.

### Clock

Status: `Canonical`

A clock defines one typed notion of time for a trace. A clock may progress with algorithm iterations, candidate
evaluations, elapsed duration or a domain event such as a dynamic-problem epoch.

The caller explicitly selects the clocks a trace analyzer reads. The same clock object is used to project the trace by
the time it reports.

See also: Moment, Time, Trace.

### Time

Status: `Canonical`

Time is the typed value read from one clock. Examples include an iteration number, cumulative evaluation count, elapsed
duration and epoch number.

See also: Clock, Moment.

### Moment

Status: `Provisional`

A moment is the collective time of one trace entry. It contains simultaneous readings from every clock selected by
that trace analyzer, and is read back one clock at a time through `TraceEntry.At`.

See also: Clock, Time, Trace.

### Observation

Status: `Canonical`

An observation is a read-only capture at a defined algorithm or operator boundary.

An observation may record data, but it must not change the observed operation's inputs, outputs, or internal computation. Explicit policies may later read observation data to make decisions, such as stopping a run.

Related terms:

- `Observation callback`: the method or runtime delegate that receives an observation.
- `Observation source`: the algorithm or operator configuration whose boundary is observed. Sources are matched by reference, so a copied configuration is a different source.
- `Observable operator`: a runtime operator wrapper that reports completed operations to observation callbacks.
- `Observable algorithm`: a runtime algorithm wrapper that reports the search states an algorithm yields, at the end of every iteration.

See also: Algorithm, Analyzer, Operator, Run.

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

A trial analyzer is a factory that creates an analyzer from each trial algorithm configuration. `TrialAnalyzer.Create` returns a `TrialModule` that the experiment accepts through `AttachPerTrial`. It can select several observation boundaries and create clocks within that factory.

The trial analyzer factory is also the typed lookup object passed to `GetAttached` to retrieve ordered trial/analyzer pairs. Each returned `TrialAttachment` exposes the trial as `Trial` and the concrete analyzer as `Module`.

See also: Analyzer, Experiment trial, Run.
