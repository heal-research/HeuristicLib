# Design goals and principles

This page records the durable product and architectural principles for HeuristicLib. It describes why the library is designed as it is and should outlive the current implementation.

HeuristicLib is in early alpha. Public APIs may change when a materially stronger design is identified and backward compatibility is not yet a design goal.

For implementation rules, public API conventions and contributor decisions, see [Developer guidelines](/contributing/developer-guidelines). Repository workflow and validation commands live in `AGENTS.md`.

## North star

HeuristicLib should become a modern library first toolkit for heuristic optimization that is:

- predictable
- composable
- testable
- easy to embed
- easy to use correctly

This implies:

- no GUI assumptions
- plain C# composition over framework magic
- explicit execution control and integration points
- a small number of strong concepts instead of many overlapping abstractions

The library should serve researchers and practitioners first while remaining easy to use from notebooks, services, automation and AI driven workflows.

## Core principles

### Small coherent conceptual model

The public model should revolve around a deliberately small vocabulary such as:

- problem
- candidate
- search space
- objective
- algorithm
- operator
- run
- search state
- executor

Prefer one honest concept over parallel hierarchies when their semantics are truly shared.

### Composition and progressive API complexity

Algorithms and operators should compose through their ordinary contracts. A composed configuration remains usable wherever its supported contract is accepted, including alongside consumer defined components. Algorithms own optimization orchestration, so replacing or composing components does not require callers to assemble a search loop.

Common tasks should have short domain entry points over the same machinery available through explicit APIs. Callers can request a final search state, stream progress or retain a run for additional control. Executor construction remains automatic. Additional control should reveal only the responsibilities the caller needs, with predictable ownership, failure and resource sharing semantics.

Conveniences should compose a small coherent core. Their brevity does not justify redundant execution paths or unnecessary public machinery. Dependencies that affect behavior remain explicit.

Documentation should introduce the simple path first and link to more control when a task requires it. Learning paths should follow the goals of practitioners, experimenters, integrators and component authors. Custom component authoring and explicit run control are independent choices.

### Pit of success and strong typing

The library should make correct usage natural and incorrect usage difficult.

Static typing, explicit invariants and clear API shapes should prevent mistakes where practical. A user should not need deep framework knowledge to configure and run a standard algorithm.

### Explicit dependencies and data flow

Behavior affecting inputs such as randomness, cancellation, time, scheduling and caches should remain explicit.

Avoid ambient state, hidden orchestration and opaque indirection. Execution should be understandable from the configuration and the data passed through it.

### Immutable candidate flow

Candidates represent values being searched. Existing candidates should remain stable while operators produce new candidates and search states describe public progress.

This gives algorithms, analysis and concurrent execution a predictable value oriented foundation.

### Clear execution and evaluation boundaries

The problem defines canonical evaluation semantics. An explicit evaluator layer may sit between algorithms and problem evaluation when it provides meaningful composition such as caching, repeated evaluation, observation or scheduling.

Problems score candidates by returning objective vectors, and evaluators return objective vectors for the candidates they are given.

Shared operators should support batch oriented APIs when batch context is part of the operation semantics.

### Performance as a design constraint

Heuristic optimization invokes the same operator and evaluation paths millions of times in one run. Performance is therefore a first-class constraint on the design itself, not a later optimization pass over a design that ignored it.

Shared machinery should avoid unnecessary allocation, boxing, delegate creation and interface indirection. An expensive operator does not excuse overhead that is material for a lightweight one, because both travel the same path.

This constrains abstractions rather than excusing unclear code. An abstraction whose supporting machinery costs more than the behavior it centralizes should not exist. A readable implementation that has not been shown to cost anything should not be replaced by a clever one.

Where a hot path genuinely needs caller-owned scratch memory, output buffers or caches, express that through overloads or a scoped-state object whose ownership and lifetime rules match the surrounding operator or execution model. Do not invent one-off scratch-memory conventions on isolated operators. Immutable candidates may still allocate when a new candidate is the real result. The goal is to avoid the accidental temporaries around that result.
