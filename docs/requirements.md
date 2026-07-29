# Requirements

This page records the high level capability baseline for HeuristicLib. It states what the library must support without defining the architecture or repeating individual system contracts.

Use the [Glossary](glossary.md) for terminology, [Core concepts](core-concepts.md) for the public model and the focused topic pages for detailed behavior. Incomplete work and unresolved decisions belong in the [Developer backlog](developer-backlog.md).

## Purpose and scope

- HeuristicLib must be a customizable optimization library rather than a fixed collection of solvers.
- Users must be able to define problems, candidate representations, algorithms, operators and analyses.
- Common algorithms should offer a short setup path while advanced users retain deep customization.
- The library must support heuristic optimization beyond evolutionary algorithms.
- Domain workflows may validate and extend the general model but must not define it.
- Native C# is the primary API while Python and other integrations use the same library model.

## Functional requirements

### Problems and representations

- Support built in and user defined candidate representations.
- Support single objective and multi objective optimization.
- Support constrained, deterministic, stochastic, noisy and dynamic problems.
- Support mixed and composite representations.
- Allow domain specific workflows and large data backed problems without making them core concepts.

### Algorithms and composition

- Support population based and single solution algorithms.
- Support evolutionary, local search, construction, hybrid and meta optimization workflows.
- Support reusable typed operators and problem specific extensions.
- Support algorithm and operator composition where the combined semantics are clear.
- Support multi population and layered workflows.
- Preserve static compatibility between candidates, search spaces, problems, algorithms and operators where practical.

### Execution

- Support streaming progress and convenient completion.
- Keep randomness, cancellation and other behavior affecting inputs explicit.
- Support deterministic repeated execution.
- Support sequential and concurrent execution where appropriate.
- Preserve logical identity and reproducibility independently of concurrent scheduling.
- Allow reusable configurations to produce independent executions.

### Analysis and experimentation

- Support reusable analysis with typed results.
- Allow analysis across operator activity and algorithm progress without changing optimization outcomes.
- Keep analysis valid for nested and composed execution.
- Support experiments over algorithm configurations and parameters.
- Support independent repeated trials for statistical analysis.
- Support sequential and concurrent trial execution.
- Preserve successful trial results when other independent trials fail.

### Integration

- Support embedding in applications, services and notebooks.
- Provide clean integration boundaries for Python and external evaluation systems.
- Keep the design open to specialized execution backends where they provide concrete value.

## Quality requirements

- Favor clear, statically typed APIs that are easy to use correctly.
- Keep the conceptual model small and explainable.
- Avoid requiring ordinary users to understand internal execution infrastructure.
- Keep important costs, defaults and side effects visible.
- Keep configurations reusable and execution behavior testable.
- Prefer deterministic behavior and explicit policy over ambient state.
- Keep observability lightweight.
- Maintain documentation and executable API examples for intended public usage.

## Non-goals

- GUI integration is not part of the core model.
- Backward compatibility does not drive the current alpha API.
- Current package, assembly, namespace and folder boundaries do not define the intended architecture.
- One domain workflow must not dictate the general object model.
- Existing abstractions are not retained solely because they already exist.
- Heavy runtime machinery, hidden orchestration and opaque indirection require concrete justification.
- Convenience APIs and authoring helpers do not define the conceptual core.

## Validation expectations

The requirements should be validated through representative workflows rather than isolated type inventories. Those workflows should cover:

- custom typed problems and operators
- standard single objective and multi objective algorithms
- local search and population based execution
- dynamic, noisy and expensive evaluation
- composition and concurrent execution
- run scoped analysis
- grids and repeated experiments
- domain workflows such as symbolic regression
- external language and evaluation integration

Detailed implementation gaps and open design decisions are tracked in the [Developer backlog](developer-backlog.md).
