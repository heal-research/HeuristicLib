# Design Goals And Principles

This page records the durable design principles for HeuristicLib. It should outlive the current implementation and the current redesign draft.

For contributor workflow and change rules, see the root `AGENTS.md`.

## North star

HeuristicLib should become a modern, library-first toolkit for heuristic optimization that is:

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

The library should serve researchers and practitioners first, while remaining easy to use from notebooks, services, automation, and AI-driven workflows.

## Alpha stance

HeuristicLib is still early alpha.

Therefore:

- backward compatibility is not a design goal
- current APIs are evidence, not commitments
- redesign is encouraged when it materially improves correctness, clarity, or usability

Alpha status is not a license for arbitrary churn. Changes should still be intentional, documented, and clearly better.

## Implementation path

The active path is not a broad parallel rewrite. It is example-first, incremental improvement of the existing implementation.

In practice:

- executable API specs should expose the intended user flow first
- implementation changes should follow those specs
- archived redesign notes may inform the work, but they do not define it directly

## Core principles

### Small, honest concepts

The public model should revolve around a deliberately small vocabulary, including concepts such as:

- problem
- domain
- evaluator
- objective
- candidate and evaluation
- algorithm
- run and state
- operators where they are genuinely useful

Prefer one honest unified concept over parallel hierarchies when the semantics are truly shared.

### Pit of success and strong typing

The library should make correct usage natural and incorrect usage difficult.

- Prefer static typing over conventions.
- Make invalid states unrepresentable where practical.
- Validate at construction boundaries when the type system cannot express an invariant.
- Avoid half-configured or ambiguous public objects.
- If something should not be allowed, prefer preventing it by API shape.

### Explicit dependencies and data flow

Behavior-affecting inputs must be explicit, especially:

- randomness
- cancellation
- time or scheduling concerns
- caches or evaluator wrappers

Avoid ambient state, hidden global registries, and opaque orchestration.

### Honest execution and evaluation boundaries

The problem defines canonical evaluation semantics.

The design may place an explicit evaluator layer between algorithms and `Problem.Evaluate(...)` when that improves composability, for example for:

- caching
- repeated evaluation
- observation
- dynamic-problem-aware behavior
- specialized scheduling

Shared operators should prefer batch-first APIs when batch context is the honest semantic model.

### Humans first, AI compatible

Humans are the primary users. AI compatibility is a design constraint, not the main goal.

In practice:

- names should be descriptive and low-ambiguity
- related abstractions should follow consistent patterns
- defaults, costs, and side effects should be explicit
- examples should be clear to both humans and tools

### Responsibilities before packages

Architectural guidance should be phrased in terms of concepts and responsibilities, not the current folder or assembly layout.

- current package boundaries are provisional
- no current project should be treated as uniquely central unless explicitly decided

### Core model versus authoring layers

Not every useful abstraction belongs in the conceptual core.

Convenience layers may include:

- builders
- extension helpers
- helper base classes

These should serve the core model, not define it.

### Patterns are tools, not law

Some current patterns are promising, but should remain justified by use:

- streaming execution
- explicit runs
- definition versus execution separation

Use them where they solve a real problem. Do not preserve them as doctrine.

## Change discipline

When evolving the architecture:

- start from the user problem
- state the problem clearly
- compare meaningful alternatives
- justify the chosen direction with concrete examples
- prefer designs that make misuse harder and intended use more obvious
- update docs, examples, and tests together

HeuristicLib should stay willing to redesign early, but each redesign should leave the library more coherent than before.
