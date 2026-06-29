# Glossary

This glossary defines the canonical terminology used in HeuristicLib.

It is a user-facing reference for library users, advanced users, contributors, and AI agents. It should keep common domain and API terms consistent across documentation, examples, tests, plans, and public API discussions.

The glossary is not a replacement for the user guide or API reference. Entries should define terms concisely and link to deeper documentation when a longer explanation belongs elsewhere.

## Terminology Status

Glossary entries may identify related terms by status:

- `Canonical`: the preferred project term.
- `Alias`: an accepted alternate term that may be useful in prose.
- `Legacy`: an older or transitional term that should be replaced with the canonical term when encountered in docs, examples, tests, plans, or new code.
- `Avoid`: a term that should not be used for this concept, usually because it has a different meaning or creates ambiguity.
- `Provisional`: a current term or candidate term that may change after further design discussion.

When a legacy or avoid-listed term appears in repository text, prefer changing it to the canonical term as part of nearby edits. Public type names do not need to carry these qualifiers when the unqualified type name is the primary user-facing API, such as `GeneticAlgorithm` rather than `GeneticAlgorithmConfiguration`.

## Algorithm

Status: `Canonical`

An algorithm defines the search process for a problem.

In a run, an algorithm advances from one search state to the next until the run stops. This transition may depend on the previous search state, private runtime state, the problem, randomness, and child operators. One-shot algorithms are still algorithms; they produce a search process with a single state.

Use algorithm for the reusable configuration unless the text explicitly says algorithm runtime.

See also: Configuration, Operator, Run, Runtime, Search state.

## Candidate

Status: `Canonical`

A candidate is the algorithm-facing object being searched, created, transformed, evaluated, selected, or replaced.

Candidates are the values that algorithms and operators manipulate directly. In many problems, a candidate is also the domain object a user would casually call a solution. In other cases, the algorithm-facing candidate and the domain-facing solution may differ.

Use candidate when precision matters around unevaluated or intermediate values. Candidate does not imply that the value is good, final, feasible in the broader domain sense, or already evaluated.

Related terms:

- `Genotype`: `Alias`. Use when an evolutionary-algorithm-flavored distinction between encoded representation and phenotype/domain expression is useful.
- `Solution`: `Alias`. Use in user-facing or domain-facing prose when the candidate is being discussed as a possible answer to the problem. Avoid solution when the distinction between unevaluated candidates and evaluated candidates matters.
- `Evaluated candidate`: a candidate paired with objective values.

See also: Evaluated candidate, Search space, Search state.

## Evaluated candidate

Status: `Canonical`

An evaluated candidate is a candidate paired with objective values produced by evaluating it.

Use evaluated candidate when the distinction between the searched object and its evaluation matters. An evaluated candidate is not necessarily final, optimal, feasible in every domain sense, or selected for future search steps.

See also: Candidate, Evaluation, Evaluator, Search state.

## Evaluation

Status: `Canonical`

Evaluation is the act of obtaining objective value(s) for a candidate.

Use evaluation for the problem-level meaning: applying the problem's evaluation semantics to a candidate to produce objective value(s).

See also: Candidate, Evaluated candidate, Objective value(s), Evaluator.

## Evaluator

Status: `Canonical`

An evaluator is the operator that turns candidates into evaluated candidates.

The evaluator may evaluate the input candidate as-is, or it may first produce a transformed candidate and then evaluate that transformed candidate. In either case, the objective value(s) in the evaluated candidate must describe the candidate that is returned as evaluated, not merely the original input candidate.

Use evaluator for the operator role. Use evaluation for the act of obtaining objective value(s).

See also: Candidate, Evaluated candidate, Evaluation, Objective value(s).

## Objective value(s)

Status: `Canonical`

An objective value is a numeric value produced by evaluating a candidate for one objective.

Use objective value for one value and objective values for one or more values. A single-objective setting has exactly one objective value per evaluated candidate. A multi-objective setting has more than one objective value per evaluated candidate.

Objective values are not enough to decide what is better on their own. They must be interpreted together with objective directions or another explicit comparison rule. Some algorithms and operators require exactly one objective value or require a comparison rule that turns multiple objective values into a usable order.

Related terms:

- `Loss`: related term for an objective value that is normally minimized.
- `Score`: related term for an objective value that is normally maximized.
- `Fitness`: evolutionary-algorithm-flavored related term for objective value or objective values. Fitness usually implies higher is better, but its direction should still be explicit.
- `Quality`: broad related term for objective value or objective values. Prefer objective value or objective values when precision matters.

See also: Candidate, Evaluated candidate, Objective direction(s).

## Operator

Status: `Canonical`

An operator is a reusable building block that an algorithm or another operator calls to perform one named operation in the search process.

Examples of operator roles include creators, evaluators, selectors, crossovers, mutators, replacers, terminators, and interceptors. Operators may create candidates, evaluate candidates, select evaluated candidates, recombine candidates, mutate candidates, replace populations, decide termination, or transform produced search states.

An operator does not own the overall search process and does not define the run's stream of search states.

Use operator for the reusable configuration unless the text explicitly says operator runtime.

See also: Algorithm, Configuration, Evaluator, Runtime.

## Problem

Status: `Canonical`

A problem is the complete runnable optimization task that an algorithm operates on.

A problem defines the search space of candidates, the evaluation semantics that produce objective value(s), the objective direction(s) used to interpret those values, and the concrete data or parameters needed for evaluation.

A problem may contain its concrete data directly or refer to separately modeled problem instance data.

See also: Candidate, Evaluation, Objective direction(s), Objective value(s), Problem instance, Search space.

## Population

Status: `Canonical`

A population is a collection of evaluated candidates maintained together by a population-based algorithm.

In HeuristicLib terminology, population implies evaluated candidates. Collections of candidates that have not yet been evaluated should be described more specifically, such as created candidates, offspring candidates, selected candidates, or candidate batch.

See also: Candidate, Evaluated candidate, Population structure, Search state.

## Population structure

Status: `Canonical`

Population structure is the organization of a population beyond a flat collection.

Population structures may contain subpopulations, islands, layers, or other groupings used by an algorithm. Use population topology only for graph-like or neighborhood-like relationships within a population structure.

See also: Population.

## Problem instance

Status: `Canonical`

A problem instance is the concrete domain data or parameterization for a problem when that part is useful to discuss separately.

Examples include a TSP distance matrix, TSP coordinates, a symbolic-regression dataset and target variable, or benchmark-function parameters such as dimension, bounds, shift, rotation, noise, or instance id.

Do not use problem instance to mean an ordinary programming-language object instance of a problem class.

See also: Problem.

## Objective direction(s)

Status: `Canonical`

An objective direction says whether lower or higher objective values are better for one objective.

Use objective direction for one objective. Use objective directions for the directions of multiple objective values. Objective directions are aligned with objective values by position.

Objective directions do not, by themselves, define a full ordering of evaluated candidates with multiple objective values. Multi-objective cases may need an additional comparison rule.

See also: Objective value.

## Search space

Status: `Canonical`

A search space is the algorithm-facing domain of valid candidates.

The search space defines which candidates algorithms and operators may create, transform, and evaluate. It is not necessarily the same as the broader domain-facing solution space. Their relationship depends on the representation used for search: they may be identical, the search space may intentionally cover only part of the solution space, multiple candidates may correspond to the same domain solution, or the two spaces may be structurally different.

Related terms:

- `Solution space`: the domain-facing space of possible answers to a problem. It is related to, but not a synonym for, search space.

See also: Candidate.

## Configuration

Status: `Canonical`

A configuration is a reusable algorithm or operator object that users set up before execution.

A configuration can contain user-selected parameters, references to child algorithm or operator configurations, validation or helper logic, and the mechanism that creates run-scoped runtime objects. It is not required to be a passive data object.

When mutable run-local state is needed, execution creates a separate run-scoped runtime object or runtime state. Stateless operators may let the same object serve both the configuration role and the runtime role because there is no run-local state to isolate.

Records are often used for configuration objects because value-style construction and copying are convenient, not because configuration objects must be dumb data containers.

Use more specific terms when the context benefits from them:

- `algorithm configuration`
- `operator configuration`

Related terms:

- `Definition`: `Legacy`. Older docs may use definition for the reusable configured object graph. Prefer configuration in new text.

See also: Algorithm, Operator, Run, Runtime.

## Configuration graph

Status: `Canonical`

A configuration graph is the graph of reusable configuration objects connected by references.

Configuration graph edges usually represent composition or use relationships, such as an algorithm configuration referencing creator, evaluator, crossover, mutator, selector, replacer, terminator, interceptor, or child algorithm configurations.

Use graph rather than tree because the same configuration object may be shared from more than one place. Do not use DAG as the glossary term; ordinary configuration graphs are expected to be acyclic, but the glossary term should not encode that stricter shape.

See also: Configuration, Runtime graph.

## Run

Status: `Canonical`

A run is one concrete invocation context for an algorithm configuration.

A run combines the algorithm configuration with a problem, optional analyzers, and the run-bound runtime machinery needed to produce search states. A run can exist before execution starts, while execution is in progress, and after execution has finished.

Users usually interact with runs through methods such as `CreateRun(...)`, `RunStreaming(...)`, and `RunToCompletion(...)`. Beginner-facing documentation should prefer run terminology over runtime terminology unless the runtime machinery itself is the topic.

See also: Configuration, Runtime.

## Search state

Status: `Canonical`

A search state is the public state value produced by an algorithm while it searches a search space.

Search states describe the current visible progress of a search process, such as the current evaluated candidate or current set of evaluated candidates. They are part of the algorithm output surface and may be streamed, inspected, analyzed, or returned as the final state of a run.

Search state is distinct from runtime state. Runtime state is private run-bound machinery used to produce progress; search state is the public value that represents that progress.

Use search terminology for the state and the search space, but do not use search algorithm as the default term for every HeuristicLib algorithm. Prefer algorithm, optimization algorithm, metaheuristic, or a more specific algorithm family where appropriate.

See also: Candidate, Evaluated candidate, Run, Runtime state, Search space.

## Runtime

Status: `Provisional`

Runtime is the current preferred family term for the run-bound world created when reusable configurations are activated for a concrete run. This term is provisional because `execution` remains a plausible alternative if `runtime` proves too overloaded.

The runtime world contains the resolved executable objects, private runtime state, and object graph that belong to that run-scoped activation. Runtime objects do not need to be actively executing at every moment. They may be dormant between calls while still holding resolved dependencies and run-local state.

Use runtime as the direct noun when it is combined with a specific role. Prefer role-specific runtime terms over generic wording:

- `algorithm runtime`
- `operator runtime`
- `creator runtime`
- `evaluator runtime`
- `crossover runtime`
- `mutator runtime`

Do not use runtime as a synonym for the .NET runtime or process environment in HeuristicLib terminology.

Related terms:

- `Execution`: the act or process of running an algorithm, consuming a stream, or producing states. Execution remains useful for process-level phrases such as execution model or streaming execution, but should not be the preferred family term for run-bound objects and state.
- `Runtime object`: `Alias`. Use this only when a generic noun is necessary. Prefer role-specific terms such as algorithm runtime or operator runtime when possible.
- `Execution instance`: `Legacy`. Current docs and code use this term for the run-bound object created from a configuration. Prefer runtime terminology in new glossary language.
- `Runtime instance`: `Avoid`. Prefer runtime object when a generic noun is necessary; instance is too easily confused with ordinary .NET object instances and singleton `Instance` members.
- `Execution state`: `Legacy`. Current docs and code use this term for private run-bound state containers. Prefer runtime state in new glossary language.

See also: Configuration, Run.

## Runtime graph

Status: `Provisional`

A runtime graph is the run-bound graph of runtime objects created from a configuration graph for a run.

Runtime graph edges represent resolved runtime relationships, such as an algorithm runtime holding operator runtimes or a wrapping operator runtime holding the runtime it wraps. The runtime graph may preserve sharing from the configuration graph when the same configuration object resolves to the same runtime object within a runtime registry.

Use graph rather than tree because runtime objects may be shared. Do not use DAG as the glossary term; ordinary runtime graphs are expected to be acyclic, but advanced runtime mechanics should not make the glossary term depend on that stricter shape.

Related terms:

- `Execution graph`: `Legacy`. Older docs may use execution graph for this concept. Prefer runtime graph when describing the configuration-to-runtime model.

See also: Configuration graph, Run, Runtime.

## Runtime registry

Status: `Provisional`

A runtime registry is the run-bound mechanism that resolves eligible source objects to runtime objects.

A runtime registry controls runtime object identity and sharing: within one registry, resolving the same source object should return the same runtime object unless replacement or explicit registry behavior says otherwise. Advanced runtime plumbing can also use a runtime registry to pre-register runtime objects, pre-register replacement source objects, or create child registries.

Ordinary algorithm and operator authoring should usually receive a runtime resolver rather than the full runtime registry.

Related terms:

- `Runtime resolver`: the restricted resolving capability exposed to ordinary authoring code.
- `Execution instance registry`: `Legacy`. Current docs and code use this term for the registry that creates and caches execution instances. Prefer runtime registry when describing the configuration-to-runtime model.

See also: Runtime, Runtime graph, Runtime resolver.

## Runtime resolver

Status: `Provisional`

A runtime resolver is the restricted resolving view of a runtime registry.

Authoring APIs use a runtime resolver when they only need to resolve child runtime objects from source objects. A runtime resolver should not expose registry-management capabilities such as pre-registration, replacement registration, or child-registry creation.

Related terms:

- `Runtime registry`: the full mechanism that usually backs a runtime resolver.
- `Execution instance resolver`: `Legacy`. Current docs and code use this term for the narrow resolution API passed to authoring code. Prefer runtime resolver when describing the configuration-to-runtime model.

See also: Runtime, Runtime registry.

## Runtime state

Status: `Provisional`

Runtime state is mutable or otherwise run-local state owned by an algorithm runtime or operator runtime.

Runtime state is created for a run or for a runtime object within a run. It can contain resolved runtime dependencies, counters, caches, buffers, or other data that must not be shared through the reusable configuration.

Runtime state is distinct from the public search states produced by an algorithm. Search states describe progress through the search; runtime state is private machinery used to produce that progress.

Related terms:

- `Execution state`: `Legacy`. Current docs and code use execution state for this concept. Prefer runtime state when describing the configuration-to-runtime model.

See also: Configuration, Run, Runtime, Search state.
