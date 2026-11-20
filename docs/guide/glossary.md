# Glossary

Use this reference when a term is unfamiliar or has a specific meaning in HeuristicLib. It holds the most important concepts in tiers, from fundamental to specialized. The guide introduces the same concepts through working examples, and the contributor architecture pages describe the machinery behind them.

## Status labels

Glossary entries may identify related terms by status:

- `Canonical`: the preferred project term.
- `Alias`: an accepted alternate term that may be useful in prose.
- `Avoid`: a term that should not be used for this concept, usually because it has a different meaning or creates ambiguity.
- `Provisional`: a current term or candidate term that may change after further design discussion.

## Fundamental concepts

### Problem

Status: `Canonical`

A problem defines the optimization task that an algorithm operates on.

A problem defines the search space, the evaluation semantics that produce an objective vector, the objective directions and the concrete data or parameters needed for evaluation.

The search space and objective stay fixed for a problem object's lifetime. Mutable instance data may change, as in a dynamic problem. Algorithms and operators bind their executors to that problem object during construction.

See also: Candidate, Evaluation, Objective, Problem instance, Search space.

### Candidate

Status: `Canonical`

A candidate is the algorithm facing object that algorithms search for and operators create, transform, evaluate, select or replace.

Candidates are the values that algorithms and operators manipulate directly. Use candidate when precision matters around unevaluated or intermediate values. Candidate does not imply that the value is good, final, feasible in the broader domain sense, or already evaluated.

Related terms:

- `Evaluated candidate`: a candidate paired with the objective vector produced by evaluating it. Use it when the distinction between the searched object and its evaluation matters. An evaluated candidate is not necessarily final, optimal, or selected for future search steps.
- `Genotype`: `Alias`. Use when an evolutionary-algorithm-flavored distinction between encoded representation and phenotype/domain expression is useful.
- `Solution`: `Alias`. Use in user-facing or domain-facing prose when the candidate is discussed as a possible answer to the problem. Avoid solution when the distinction between unevaluated and evaluated candidates matters.

See also: Encoding, Search space, Search state.

### Search space

Status: `Canonical`

A search space is the algorithm-facing domain of valid candidates.

The search space defines which candidates algorithms and operators may create, transform, and evaluate. It is not necessarily the same as the broader domain-facing solution space; their relationship depends on the representation used for search.

Related terms:

- `Solution space`: the domain-facing space of possible answers to a problem. It is related to, but not a synonym for, search space.

See also: Candidate, Problem.

### Evaluation

Status: `Canonical`

Evaluation is the act of obtaining an objective vector for a candidate: applying the problem's evaluation semantics to it.

The evaluator is the operator role that performs evaluation for an algorithm. Use evaluation for the act and evaluator for the role.

See also: Candidate, Objective, Operator roles.

### Objective

Status: `Canonical`

An objective says what better means for a problem. Evaluating a candidate produces an objective vector: an ordered vector of numeric objective values, one for each objective. A single-objective problem has one objective value and a multi-objective problem several.

The objective directions are the ordered directions, aligned with an objective vector by position, that say for each objective value whether lower or higher is better. Comparing two objective vectors does not by itself determine which one is better. The comparison must use the objective directions or another explicit comparison rule. Directions alone do not define a full ordering when there are several objective values, so a multi-objective problem may need an additional comparison rule.

Related terms:

- `Objective values`: `Alias`. Prose alias for objective vector when the vector shape is not important.
- `Loss`: related term for an objective value that is normally minimized.
- `Score`: related term for an objective value that is normally maximized.
- `Fitness`: evolutionary-algorithm-flavored related term for objective value or objective vector. Fitness usually implies higher is better, but its direction should still be explicit.
- `Quality`: broad related term for objective value or objective vector. Prefer objective value or objective vector when precision matters.

See also: Candidate, Evaluation, Problem.

### Algorithm

Status: `Canonical`

An algorithm defines a reusable search process through an algorithm configuration and its executors, constructed within a run.

In a run, an algorithm executor advances search state until the run stops. It holds its resolved child executors, and its own fields hold private execution data. One shot algorithms are still algorithms: they produce a search process with a single state.

Use algorithm for the reusable configuration unless the text explicitly says algorithm executor.

Related terms:

- `Search algorithm`: `Avoid` as the default term for every HeuristicLib algorithm. Use search terminology for the state and the search space, and prefer algorithm, optimization algorithm, metaheuristic, or a more specific algorithm family.

See also: Configuration, Executor, Iteration, Meta-algorithm, Operator, Run, Search state.

### Operator

Status: `Canonical`

An operator is a reusable building block that an algorithm or another operator calls to perform one named operation in the search process.

An operator does not own the overall search process or define the run's stream of search states. Use operator for the reusable configuration unless the text explicitly says operator executor.

See also: Algorithm, Configuration, Executor, Operation, Operator roles.

### Operator roles

Status: `Canonical`

Operator roles are the named categories of work that operators perform inside algorithms and other operators.

Use operator role names when discussing the responsibility of an operator. Use concrete operator type names only when discussing a specific implementation.

See also: Creator, Crossover, Evaluator, Interceptor, Mutator, Operator, Refiner, Replacer, Selector, Terminator.

#### Creator

Status: `Canonical`

A creator is the operator role that creates candidates that are valid for the provided search space.

See also: Candidate, Operator, Search space.

#### Evaluator

Status: `Canonical`

An evaluator is the operator role that obtains one objective vector for each input candidate, in input order. Pairing the objective vectors with the input candidates produces evaluated candidates.

See also: Candidate, Evaluation, Objective, Operator, Refiner.

#### Selector

Status: `Canonical`

A selector is the operator role that selects evaluated candidates, usually as parents for later variation.

Selection decides which evaluated candidates participate in the next operation. It does not create offspring by itself and does not decide the next population unless the algorithm explicitly uses it that way.

See also: Crossover, Mutator, Operator, Replacer.

#### Crossover

Status: `Canonical`

A crossover is the operator role that combines parent candidates into offspring candidates.

Use crossover for variation that depends on two or more parents. Use mutator for variation that perturbs existing candidates without combining multiple parents.

See also: Mutator, Operator, Selector.

#### Mutator

Status: `Canonical`

A mutator is the operator role that perturbs candidates to create variation.

Use mutator for changes derived from existing candidates. Use creator when candidates are generated without depending on parent candidates.

See also: Creator, Crossover, Operator.

#### Refiner

Status: `Canonical`

A refiner is the operator role that works on existing candidates to improve a chosen aspect of them, such as fitting candidate parameters, repairing invalid candidates, simplifying representations or applying local improvement.

A refiner may use an evaluator when objective values guide the refinement. Continuing the search with the refined candidate is Lamarckian refinement; refinement evaluation instead evaluates a temporary refined candidate and associates its objective vector with the original candidate, providing Baldwinian refinement.

See also: Creator, Evaluator, Mutator, Operator.

#### Replacer

Status: `Canonical`

A replacer is the operator role that chooses the evaluated candidates that form the next population or survivor set.

Do not use replacer as a synonym for selector. A selector usually chooses parents or inputs for another operation, while a replacer chooses survivors after previous candidates and offspring are available.

See also: Operator, Selector.

#### Terminator

Status: `Canonical`

A terminator is the operator role that decides whether execution should stop after observing a produced search state.

The owner of the terminator determines whether this means algorithm-owned termination or external early stopping.

See also: Operator, Search state, Termination.

#### Interceptor

Status: `Canonical`

An interceptor is the operator role that transforms a produced search state before it is yielded or observed by state based stopping logic.

Use interceptor for state post processing. Do not use it for read-only analysis. Use observation or analyzers for that.

See also: Analyzer, Observation, Operator, Search state.

### Configuration

Status: `Canonical`

A configuration is a reusable object that users set up before execution, such as an algorithm or operator.

A configuration is immutable data: settings, child configurations and wrappers. It holds no live executor, result or mutable data, which belong to executors and run results.

Use more specific terms when the context benefits from them:

- `algorithm configuration`
- `operator configuration`

See also: Algorithm, Configuration graph, Executor, Operator, Run.

### Executor

Status: `Canonical`

An executor is the runtime object that performs the operations of an algorithm or operator, such as mutating or evaluating candidates or advancing a search. A configuration creates it during a run, and `IExecutor` is its common interface. It holds the resolved child executors and its private mutable fields.

One executor can support several operations and receive many invocations. Each construction has independent private fields. The term gives the object no scheduling, threading, disposal or run-lifecycle responsibilities.

Use executor for the callable runtime object, operation for the action it supports and invocation for one actual call.

See also: Configuration, Operation, Run.

### Run

Status: `Canonical`

A run is the object that owns one logical execution.

An `AlgorithmRun` executes one algorithm configuration on a problem. Child algorithms and operators participate in the run of the algorithm the user started, unless they are explicitly started as separate runs.

See also: Algorithm, Executor, Run result, Search state.

### Search state

Status: `Canonical`

A search state is the public state value produced by an algorithm while it searches a search space.

Search states describe the visible progress of a search process, such as the current evaluated candidate or current population. They may be streamed, inspected, analyzed, or returned as the final state of a run.

Search state is the public value representing search progress. Executor fields are machinery used to produce that progress.

See also: Candidate, Executor, Run, Search space.

### Run result

Status: `Canonical`

A run result is data that a run collects and owns, such as an analyzer's collected data or a clock reading. Read it with `run.Result(configuration)`, which finds it by configuration reference. It is created at the first lookup and remains available after the run stops or fails.

A run result belongs to the run as a whole, not to an individual executor or call, and independent runs have separate results.

See also: Analyzer, Clock, Run.

### Random number generator (RNG)

Status: `Canonical`

A random number generator is the explicit source of randomness passed to algorithms, operators, problems, and execution helpers.

Drawing random values from an RNG changes its state, so draw order matters. Forking an RNG creates deterministic child RNGs without drawing random values from the parent. A user-provided seed creates the root RNG.

See also: Experiment, Run.

## Composing and running

### Configuration graph

Status: `Canonical`

A configuration graph is the graph of reusable configuration nodes connected by references.

A configuration node is a configuration considered as a node in that graph. Its identity is the configuration object's reference, not its value equality or one occurrence along a caller path, so several configurations may refer to the same child node. Configuration is accepted shorthand for configuration node when the context is clear.

Use graph rather than tree because the same configuration object may be shared from more than one place. Do not use DAG as the glossary term; ordinary configuration graphs are expected to be acyclic, but the glossary term should not encode that stricter shape.

See also: Child operator, Configuration, Resolution scope, Substitution.

### Parameter

Status: `Canonical`

A parameter is a declared property of a configuration. Its descriptor supplies the name and instructions for reading its value or creating a configuration with a replacement value. The configuration stores the value.

See also: Configuration, Parameter grid.

### Substitution

Status: `Canonical`

A substitution is a rewrite applied while building a configuration, such as placing an analyzer or a wrapper around selected nodes. It is never stored or serialized: persist the ordinary configuration it produced, which contains the nodes it placed.

A substitution selects its targets by reference, by type or by predicate and matches them against the graph it received. `Substitute` returns the rewritten configuration, and `CreateRun` applies substitutions for that run only.

See also: Configuration graph, Invocation wrapper, Substitution group.

### Substitution group

Status: `Canonical`

A substitution group holds substitutions that belong together, such as the parts of an analyzer. A part may itself be a group; a group containing itself is an error. Later parts sit closer to their targets.

See also: Analyzer, Clock, Substitution.

### Shared configuration

Status: `Canonical`

A shared configuration is a configuration selected with `Sharing`. Sharing applies to any execution node, such as an algorithm or operator executor, an adaptation, a counter or a duration. The nearest scope that shares a configuration owns its node, and every request for it from that scope or below receives it, with its private fields and resolved children.

Without sharing, an ordinary node belongs to the requesting scope and a run result, such as a counter, to the root, so sharing a counter gives the sharing scope a count of its own. The configuration reference identifies what is shared.

See [execution resolution](/contributing/architecture/execution-resolution#shared-configurations).

See also: Configuration, Executor, Resolution scope.

### Resolution scope

Status: `Canonical`

A resolution scope resolves configurations to executors and other runtime objects during a run and holds them for its lifetime. It keys an executor by configuration and problem reference.

A child scope inherits explicit sharing while constructing independent executors for other configurations.

See [execution resolution](/contributing/architecture/execution-resolution) for state, construction and observation rules.

See also: Configuration, Executor, Run, Run result, Shared configuration.

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

An iteration is one search state an algorithm yields. A call that yields no search state, such as a turn in which the inner algorithm yields nothing, has no iteration.

An algorithm counts its iterations per call, from 1. The count restarts when a meta-algorithm calls the algorithm again, whether its executor is fresh or reused. A clock over the iterations keeps counting across calls.

See also: Algorithm, Clock, Search state, Turn.

### Cycle

Status: `Canonical`

A cycle is one pass of `CycleAlgorithm` over its inner algorithms, running each once in order.

See also: Meta-algorithm, Turn.

### Turn

Status: `Canonical`

A turn is one call of an inner algorithm by `CycleAlgorithm`. A cycle has one turn per inner algorithm, and a turn may yield no search state. The inner algorithm's `Calls` observe turns.

Related terms:

- `Activation`: `Avoid` as a name for this concept. Activation describes any meta-algorithm starting an inner algorithm.

See also: Cycle, Iteration, Meta-algorithm.

### Termination

Status: `Canonical`

Termination is the stopping of a run or algorithm stream after a produced search state or because the algorithm cannot produce another search state.

Distinguish algorithm-owned termination from external early stopping. Algorithm-owned termination is part of the algorithm's own search process. External early stopping is applied around an algorithm or meta-algorithm stream, such as a wrapper that stops after a maximum number of yielded states, elapsed time, or observed operator work.

See also: Algorithm, Meta-algorithm, Run, Search state, Terminator.

### Adaptation

Status: `Canonical`

An adaptation is an optional link that applies a runtime value to an operator's configured base value, such as scaling a mutation strength while a run progresses.

An adaptation configuration resolves to an `IAdaptation`, the execution node that applies the runtime value. The configuration uses reference equality: the same instance links readers and writers to one runtime value within a scope. Share the adaptation when its value must survive child scopes, including when sharing a mutator that reads it. Independent runs create independent values.

See also: Configuration, Parameter, Resolution scope.

### Analyzer

Status: `Canonical`

An analyzer defines what a run collects from observations and how the observation wrappers are placed. A result-producing analyzer is an immutable configuration that creates one run result for each run, read with `run.Result(analyzer)`. It is a substitution group: pass it to `CreateRun` to analyze one run, or place it with `Substitute` to keep it in the resulting configuration.

Reusing an analyzer across runs keeps their data independent. A trace analyzer records a history of values; other analyzers, such as [best-so-far](/guide/execution/observability-and-analysis#best-so-far-selection-and-placement) and [aggregate](/guide/execution/observability-and-analysis#accumulating-analyzers) analyzers, are described in observability and analysis.

Use Roslyn analyzer for compiler diagnostics when the context could be ambiguous.

See also: Observation, Observation source, Run result, Substitution group, Trace.

### Trace

Status: `Canonical`

A trace is an ordered history of values that a trace analyzer records while a run executes. Each entry holds an analyzed value and the clock readings recorded with it. A trace reads each observation through one function, and an immutable retention policy decides which entries it keeps.

Do not use series for this concept. In HeuristicLib, `Series<T>` is a named column in tabular data ([Data and machine learning](/guide/domains/data-and-machine-learning#represent-regression-data)).

See [observability and analysis](/guide/execution/observability-and-analysis) for retention policies and ranking.

See also: Analyzer, Clock, Observation source, Run result.

### Clock

Status: `Canonical`

A clock supplies the time axis for recorded entries. It is a configuration whose per-run reading is a run result, and it only reads. Its time is a typed value (`IClock<TTime>`), such as an iteration count, a cumulative candidate count, elapsed time or a domain quantity. A trace selects its clocks explicitly and records only those.

See [observability and analysis](/guide/execution/observability-and-analysis) for clock placement, equality, counters and durations.

See also: Iteration, Run result, Trace.

### Observation source

Status: `Canonical`

An observation source names a boundary of one configuration at which observations are captured: `Invocations` on an operator for its successful invocations, or `Iterations` and `Calls` on an algorithm for its yielded states and for call starts and outcomes. `ObservationSource<TObservation>` represents it, and `ObservationSource.Create` supports a consumer-defined role.

Creating a source installs nothing. An analyzer places the wrapper that captures its observations, and `Observing` forwards them to a callback without collecting a result.

See also: Analyzer, Observation, Substitution, Trace.

### Experiment

Status: `Canonical`

An experiment is an ordered list of parameter grids with repetitions and a random stream setting. Creating an experiment run pairs every parameter candidate with each supplied problem and repetition.

See also: Experiment trial, Parameter candidate, Parameter grid, Problem, Run.

### Parameter grid

Status: `Canonical`

A parameter grid (`ParameterGrid`) is an algorithm prototype and its ordered dimensions. Each combination of dimension values yields a parameter candidate. A grid with no dimensions yields its prototype once.

See also: Algorithm, Experiment, Parameter, Parameter candidate.

### Parameter candidate

Status: `Canonical`

A parameter candidate is one algorithm configuration generated from a parameter grid. The experiment numbers parameter candidates in grid order, and equal configurations can have different numbers.

See also: Experiment, Experiment trial, Parameter, Parameter grid.

### Experiment trial

Status: `Canonical`

An experiment trial is one run of a parameter candidate on a problem for a particular repetition. Each trial has its own algorithm run, resolution scope and random generator.

See also: Experiment, Parameter candidate, Problem, Run.

## Authoring

### Operation

Status: `Canonical`

An operation is a named action an operator performs, such as mutation or evaluation. Its executor interface defines the inputs and result of that action.

An operation is distinct from the executor that performs it: one executor can support several operations. Use operation for the action, executor for the callable runtime object, and invocation for one actual call of an operation, whatever the batch size.

Related terms:

- `Call`, `operation call`: `Alias`. Plain prose may say call; names say invocation.

See also: Executor, Invocation wrapper, Operator, Operator roles.

### Single-candidate operator

Status: `Canonical`

A single-candidate operator is a regular operator whose public role operation remains batch-wise. It can be used anywhere an ordinary operator of that role is expected and does not introduce a separate single-candidate execution contract.

The term describes an authoring pattern: the operator author implements the role logic for one candidate, and the single-candidate operator applies that logic independently across a batch of candidates and handles the batching. Use a role-specific form such as single-candidate mutator when the operator role matters.

See also: Candidate, Operator, Operator roles.

### Child operator

Status: `Canonical`

A child operator is an operator configuration directly referenced by another configuration.

Child describes one immediate edge in the configuration graph. The same child operator may be referenced by multiple configurations, so the term does not imply exclusive ownership or a tree structure. Use a role-specific form such as child mutator when the operator role matters. Use nested operator only when referring more generally to an operator at any depth below another configuration.

See also: Configuration graph, Operator.

### Invocation wrapper

Status: `Canonical`

An invocation wrapper adds behavior, such as retrying, timing, counting or choosing a child, around each call an executor receives for one operation. It decides whether, how often and on which executor or random number generator the call proceeds.

A configured operator wrapper is a configuration that declares its child. When resolved, it installs an `IInvocationWrapper` around the resolved operation's executor. The configuration states the order of a chain of wrappers: a wrapper written inside another is closer to the executor, and a substitution applied later sits closer to the node it matches. Unlike an observation, a wrapper may change the behavior of the call.

See also: Executor, Observation, Operation, Substitution.

### Observation

Status: `Canonical`

An observation is a read-only capture at a defined algorithm or operator boundary, or at the boundary of a consumer-defined role. What it carries depends on the boundary.

An observation may record data, but it must not change the observed operation's inputs, outputs, or internal computation. Explicit policies may later read observation data to make decisions, such as stopping a run after a budget is consumed.

See [observability and analysis](/guide/execution/observability-and-analysis) for what each boundary reports.

See also: Analyzer, Invocation wrapper, Observation source.

## Domain vocabulary

### Encoding

Status: `Canonical`

An encoding is the representation scheme used to express domain-facing solutions as candidates.

In HeuristicLib, encoding is usually reflected by the candidate type and the way a problem interprets candidates. It is not normally a separate object. Do not use encoding as a synonym for search space: the search space defines which candidate values are valid for a problem, while the encoding describes how candidate values represent possible solutions.

Related terms:

- `Representation`: `Alias`. Common in optimization and evolutionary-algorithm literature.
- `Genotype`: related evolutionary-algorithm term for the encoded candidate.

See also: Candidate, Problem, Search space.

### Problem instance

Status: `Canonical`

A problem instance is the concrete domain data or parameterization for a problem when that part is useful to discuss separately.

Do not use problem instance to mean an ordinary programming-language object instance of a problem class.

See also: Problem.
