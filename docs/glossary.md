# Glossary

This glossary defines the canonical terminology used in HeuristicLib.

It is a user-facing reference for library users, advanced users, contributors, and AI agents. It should keep common domain and API terms consistent across documentation, examples, tests, plans, and public API discussions.

The glossary is not a replacement for the user guide or API reference. Entries should define terms concisely and leave workflows, examples, rationale, and implementation details to the focused documentation pages.

## Writing Entries

Each entry should answer what the term means in HeuristicLib and what it must not be confused with.

Prefer adding or keeping an entry when the term is project-specific, overloaded in common use, easy to confuse with another HeuristicLib term, or important for consistent API and documentation language. Prefer omitting or trimming an entry when the term is already obvious to the target audience or when the text mainly repeats another documentation page.

Entries should stay short. Use examples only when they disambiguate the term. Mention code names only when they are part of the user-facing vocabulary. Use `See also` only for relationships that clarify the meaning of the current term.

## Terminology Status

Glossary entries may identify related terms by status:

- `Canonical`: the preferred project term.
- `Alias`: an accepted alternate term that may be useful in prose.
- `Legacy`: an older or transitional term that should be replaced with the canonical term when encountered in docs, examples, tests, plans, or new code.
- `Avoid`: a term that should not be used for this concept, usually because it has a different meaning or creates ambiguity.
- `Provisional`: a current term or candidate term that may change after further design discussion.

When a legacy or avoid-listed term appears in repository text, prefer changing it to the canonical term as part of nearby edits. Public type names do not need to carry these qualifiers when the unqualified type name is the primary user-facing API, such as `GeneticAlgorithm` rather than `GeneticAlgorithmConfiguration`.

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

## Algorithms and Operators

### Algorithm

Status: `Canonical`

An algorithm defines a reusable search process.

In a run, an algorithm advances search state until the run stops. This may depend on previous search state, private execution state, the problem, randomness and child operators. One-shot algorithms are still algorithms. They produce a search process with a single state.

Use algorithm for the reusable configuration unless the text explicitly says algorithm execution instance.

See also: Configuration, Execution instance, Operator, Run, Search state.

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

Use operator for the reusable configuration unless the text explicitly says operator execution instance.

See also: Algorithm, Configuration, Evaluator, Execution instance.

### Operator roles

Status: `Canonical`

Operator roles are the named categories of work that operators perform inside algorithms and other operators.

Use operator role names when discussing the responsibility of an operator. Use concrete operator type names only when discussing a specific implementation.

See also: Creator, Crossover, Evaluator, Interceptor, Mutator, Operator, Replacer, Selector, Terminator.

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

### Termination

Status: `Canonical`

Termination is the stopping of a run or algorithm stream after a produced search state or because the algorithm cannot produce another search state.

Distinguish algorithm-owned termination from external early stopping. Algorithm-owned termination is part of the algorithm's own search process. External early stopping is applied around an algorithm or meta-algorithm stream, such as a wrapper that stops after a maximum number of yielded states, elapsed time, or observed operator work.

A terminator is the operator role that decides whether execution should stop after observing a produced search state.

See also: Algorithm, Meta-algorithm, Operator, Run, Search state.

## Execution Model

### Configuration

Status: `Canonical`

A configuration is a reusable algorithm or operator object that users set up before execution.

A configuration can contain parameters, child configurations, validation or helper logic, and the mechanism that creates execution instances. It is not required to be a passive data object. Mutable execution data belongs in execution state, not in the reusable configuration.

Use more specific terms when the context benefits from them:

- `algorithm configuration`
- `operator configuration`

Related terms:

- `Definition`: `Legacy`. Older docs may use definition for the reusable configured object graph. Prefer configuration in new text.

See also: Algorithm, Execution instance, Operator, Run.

### Configuration graph

Status: `Canonical`

A configuration graph is the graph of reusable configuration objects connected by references.

Use graph rather than tree because the same configuration object may be shared from more than one place. Do not use DAG as the glossary term; ordinary configuration graphs are expected to be acyclic, but the glossary term should not encode that stricter shape.

See also: Configuration, Execution graph.

### Run

Status: `Canonical`

A run is one logical execution of an algorithm configuration on a problem.

When a meta-algorithm coordinates child algorithms, the run is created by the algorithm started by the user. Child algorithms and operators participate in that same run unless they are explicitly started as separate runs.

Analyzers are attached to a run, so their observations and results can span nested algorithms and multiple short-lived execution instances.

See also: Analyzer, Configuration, Execution instance, Search state.

### Search state

Status: `Canonical`

A search state is the public state value produced by an algorithm while it searches a search space.

Search states describe the visible progress of a search process, such as the current evaluated candidate or current population. They may be streamed, inspected, analyzed, or returned as the final state of a run.

Search state is distinct from execution state. Execution state is private machinery used to produce progress; search state is the public value that represents that progress.

Use search terminology for the state and the search space, but do not use search algorithm as the default term for every HeuristicLib algorithm. Prefer algorithm, optimization algorithm, metaheuristic, or a more specific algorithm family where appropriate.

See also: Candidate, Evaluated candidate, Execution state, Run, Search space.

### Execution instance

Status: `Canonical`

An execution instance is the concrete algorithm or operator object created from a configuration for use during a run.

Execution instances perform the work of an algorithm or operator after configuration has been resolved for execution. They may hold private execution state and resolved child execution instances.

An execution instance can be shorter-lived than the run. Meta-algorithms may create fresh execution instances for nested algorithms while all of those nested execution instances still belong to the same run.

Use role-specific terms when the context benefits from them:

- `algorithm execution instance`
- `operator execution instance`
- `creator execution instance`
- `evaluator execution instance`
- `crossover execution instance`
- `mutator execution instance`

See also: Configuration, Execution state, Run.

### Execution state

Status: `Canonical`

Execution state is private mutable state owned by an execution instance.

Execution state can contain resolved child execution instances, counters, caches, buffers, or other data that must not be shared through the reusable configuration.

Execution state is distinct from search state. Search states describe visible progress through the search and may be streamed, inspected, analyzed, or returned as the final state of a run.

See also: Configuration, Execution instance, Run, Search state.

### Execution graph

Status: `Canonical`

An execution graph is a graph of execution instances created from a configuration graph during a run.

A run may contain more than one execution graph over time, for example when meta-algorithms create fresh execution instances for nested algorithms.

See also: Configuration graph, Execution instance, Run.

### Execution instance registry

Status: `Canonical`

An execution instance registry is the advanced mechanism that resolves configurations to execution instances during a run.

The registry controls execution-instance identity and sharing. Ordinary algorithm and operator authoring should usually use an execution instance resolver rather than managing a registry directly.

See also: Configuration, Execution graph, Execution instance, Execution instance resolver, Run.

### Execution instance resolver

Status: `Canonical`

An execution instance resolver is the restricted resolving capability used by ordinary authoring code to obtain child execution instances.

It lets an algorithm or operator resolve the configured child algorithms or operators it depends on.

See also: Configuration, Execution graph, Execution instance, Execution instance registry.

### Random number generator (RNG)

Status: `Canonical`

A random number generator is the explicit source of randomness passed to algorithms, operators, problems, and execution helpers.

Drawing random values from an RNG changes its state, so draw order matters. Forking an RNG creates deterministic child RNGs without drawing random values from the parent. A user-provided seed creates the root RNG.

See also: Experiment, Run.

## Analysis and Experiments

### Analyzer

Status: `Canonical`

An analyzer is a reusable, run-scoped analysis configuration.

An analyzer declares which observations it needs and exposes analysis results through the run. It records or derives information about execution; it should not control optimization behavior.

See also: Analyzer result, Observation, Observation plan, Run.

### Analyzer result

Status: `Canonical`

An analyzer result is the run scoped object that stores the data produced by an analyzer during a run.

Analyzer results may contain counters, curves, traces, genealogy graphs or summaries. Users retrieve analyzer results from the run with the analyzer configuration that produced them.

Do not store analyzer result data on reusable analyzer configurations, observable operator wrappers or execution registries.

See also: Analyzer, Observation, Observation plan, Run.

### Observation plan

Status: `Canonical`

An observation plan is the run scoped registration plan that records which analyzer callbacks should be installed at which observable operator boundaries.

Analyzer run states add their observation requirements to the observation plan. The run then uses the plan to install the required observable replacements into execution instance registries.

Do not use observation plan to mean the collected analysis data. The plan describes what to observe. Analyzer results store what was observed.

See also: Analyzer, Analyzer result, Observation, Run.

### Observation

Status: `Canonical`

An observation is a read-only capture at a defined algorithm or operator boundary.

An observation may record data, but it must not change the observed operation's inputs, outputs, or internal computation. Explicit policies may later read observation data to make decisions, such as stopping a run.

Related terms:

- `Observer`: the callback object or function that receives an observation.
- `Observable operator`: an operator wrapper that installs observers around an operator boundary.

See also: Analyzer, Operator, Run.

### Experiment

Status: `Provisional`

An experiment is an execution setup that coordinates multiple independent runs.

An experiment is not an algorithm. It does not produce one continuous stream of search states and does not pass search states from one run to the next. A repeated experiment executes the same algorithm configuration multiple times. A comparative experiment executes different algorithm configurations, parameter settings, problems or problem instances.

Seed policy is an important part of an experiment because it defines how random seeds are assigned to independent runs, especially when stochastic algorithms are repeated or executed in parallel.

See also: Algorithm, Problem, Problem instance, Run.
