# Requirements

This page records what HeuristicLib must support, what it should optimize for, what it must avoid, and which questions remain open. It is a capability baseline, not a design blueprint.

## Primary users and priorities

- The library must optimize for human researchers and engineers first.
- It should remain friendly to AI agents and external-language workflows, but native C# use has priority.
- The primary use case must be a custom optimization library: users define problems, choose or build algorithms, and extend the system with new representations and operators.
- Broad metaheuristics must be in scope from the start. The redesign must not optimize only for evolutionary algorithms.
- Symbolic regression and other domain workflows must validate the design, not dictate the core model.
- Common algorithms should have a short turnkey path, while deep customization remains possible.
- The redesign should optimize for a clear mental model over maximum exposed framework machinery.

## Functional requirements

### Problem and representation modeling

- The library must support built-in and user-defined candidate representations.
- It must support single-objective and multi-objective optimization.
- It must support constrained optimization as a first-class capability.
- It must support mixed and composite representations.
- It must support deterministic, stochastic, dynamic, and noisy problems.
- Dynamic problems should fit within the normal HeuristicLib model without awkward hacks.
- Domain-specific workflows must build on the generic model without forcing the generic model to become domain-specific.

### Algorithms and operators

- The library must support both population-based and single-solution metaheuristics.
- It must support composition such as sequencing, wrapping, cycling, or restarting.
- It must support reusable operator composition patterns such as choose-one and pipeline-style composition.
- It must support weighted probabilistic operator choice.
- It must support stateful operators with short-lived run-local state.
- It must preserve strong compile-time compatibility for representation-specific, domain-specific, and problem-specific operators.
- It must support evolutionary mating schemes with explicit parent roles and different selection pressure per role when needed.
- It must allow an explicit evaluator layer between algorithms and `Problem.Evaluate(...)`.
- It must support strong defaults for standard algorithm families while still allowing deep customization.
- It must support island and other multi-population workflows, including age-layered variants such as ALPS.
- It must distinguish between population topology and per-member side metadata.
- It should make hybrid or memetic workflows possible through normal extension mechanisms.
- It should make meta-optimization possible by allowing one problem evaluation to run a complete inner optimization run, without making this a core concept.

### Execution, analysis, and experimentation

- The conceptual center of the redesign must be an iterative execution model.
- A simpler `solve` style should exist as a convenience layer, not the foundational model.
- The library must support lightweight progress inspection during execution and usable completion results afterward.
- It must support reusable run-scoped analysis modules that observe selected operators or algorithm-boundary points and produce typed results.
- It must support analyzers that combine multiple observed events into one result.
- It must allow users to query the current analyzer result while a run is still executing.
- It must support terminators as regular operators with run-local state and compositions such as conjunction, disjunction, and decoration.
- It should support resumption or continuation, but not at the expense of core clarity.
- It must strongly support experimentation over algorithm configurations, operators, and parameters.
- It must support defining families of algorithm configurations from a prototype plus variation axes.
- It must support repeated independent executions of the same configuration for statistical significance.
- Repeated execution must be modeled as experiment orchestration over independent runs, not as a state-transforming metaalgorithm.

## Non-functional requirements

- The redesign must strongly favor static type safety and pit-of-success APIs.
- The conceptual core must stay small and understandable.
- Behavior-affecting dependencies must remain explicit.
- Operator execution should have a small explicit context for concerns such as randomness and cancellation.
- The redesign must be C#-first while exposing clean seams for Python and AI-agent usage.
- Users must not need to learn a heavy internal framework before solving their first problem.
- Experiment execution must remain reproducible through deterministic random forking.
- Parallelism must be first-class wherever it is meaningful: evaluation, operators, and algorithms.
- Parallel execution must remain reproducible under different schedules or degrees of parallelism.
- Shared operators should support batch-first APIs where batch context is semantically meaningful.
- The design should leave room for future GPU-capable execution.
- Observability must be useful but lightweight.
- Analysis must be read-only and must not change optimization outcomes.
- Analysis must remain valid under nested or composed algorithms.
- The redesign should be easy to explain, test, and embed.

## Non-goals and anti-requirements

- The redesign must not be driven by backward compatibility.
- It must not assume any GUI.
- It must not treat the current package, assembly, namespace, or folder layout as the intended architecture.
- It must not let one domain, such as symbolic regression, dictate the general object model.
- It must not preserve abstractions only because they already exist.
- It must not introduce heavy runtime machinery, hidden orchestration, or opaque indirection without strong justification.
- It must not freeze final vocabulary names at the requirements stage.
- It must not confuse convenience APIs with the conceptual core.
- It must not let builders, helper base classes, or other authoring conveniences define the conceptual core.

## Validation use cases

The redesign must support these scenarios cleanly:

- a constrained mixed-variable optimization problem
- a custom representation with custom operators
- a problem-specific operator workflow checked at compile time
- a short-path standard genetic algorithm setup
- a short-path standard multi-objective evolutionary setup
- a local-search workflow over one current candidate
- a dynamic or noisy problem
- a dynamic problem extension with reevaluation, cache invalidation, or environment updates
- an expensive parallel evaluation workflow
- a workflow where non-evaluation operators also benefit from parallelism
- composition between different population topologies, such as flat, island, or layered structures
- a symbolic-regression workflow over a large dataset
- symbolic regression with local numeric refinement
- an island or age-layered workflow such as ALPS
- a run with lightweight progress and result inspection
- a quality-curve analysis over evaluation activity and iteration or generation boundaries
- a best-solution analysis that does not require the algorithm itself to track best-so-far
- a genealogy analysis over creation and variation activity
- a tree-based evolutionary workflow with different selectors for different parent roles
- a grid-search-style experiment with repeated runs
- experiment results that expose per-run analyzer results and cross-configuration comparisons
- a meta-optimization workflow where algorithm configurations are evaluated by running inner optimization runs

## Open design questions

This baseline intentionally leaves the following questions open:

- the final public vocabulary and type names
- whether `search space` remains a first-class public term
- whether `solution` should remain distinct from candidate representation, and how
- how much of the current operator taxonomy should survive
- the final public run and completion-result shape
- how resumability should appear in the public API
- how `solve` should relate to the iterative core
- whether the long-term public term should be `analysis`, `analyzer`, or something similar
- whether analyzers should observe operators only, boundary points only, or both
- the final experimentation API and result shape
- how strongly experiment results should optimize for streaming and external-tool interoperability
- the final package and assembly boundaries
