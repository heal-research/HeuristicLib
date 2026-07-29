# Developer Guidelines

This document is the canonical implementation and architecture guide for HeuristicLib contributors. It translates the durable principles in [Design goals and principles](design-goals.md) into concrete rules for code, public APIs and repository changes.

Use the [Glossary](glossary.md) for canonical terminology. Use the root [AGENTS.md](../AGENTS.md) for repository workflow and validation commands. Public authoring contracts and examples remain in the relevant topic documentation.

## Documentation roles

Each steering document has one primary responsibility:

| Document | Responsibility |
| --- | --- |
| [Requirements](requirements.md) | What the library must support and which capability boundaries matter |
| [Design goals and principles](design-goals.md) | Which durable qualities the resulting library should have |
| [Developer guidelines](developer-guidelines.md) | How contributors implement those requirements and goals |
| [Glossary](glossary.md) | Which terminology is canonical |
| [Developer backlog](developer-backlog.md) | Which follow-up work and design decisions remain unresolved |
| [AGENTS.md](../AGENTS.md) | How contributors navigate and validate the repository |

Topic pages document public behavior, usage and extension contracts. They should not become alternative repository policy documents.

## Project posture

HeuristicLib is in early alpha.

- Backward compatibility is not currently a design goal.
- Existing code and APIs are evidence rather than permanent commitments.
- Replace a weak API when a materially stronger design is available instead of adding compatibility around the weak design.
- Alpha status does not justify arbitrary churn. Every change must solve a concrete problem and leave the library more coherent.
- Current folders, assemblies, namespaces and packages are provisional. Do not treat them as the intended final architecture.

## Making design changes

Start with the user problem rather than a preferred pattern.

1. State the concrete problem.
2. Compare meaningful alternatives.
3. Explain the chosen tradeoffs with representative usage.
4. Prefer designs that make intended usage obvious and misuse difficult.
5. Update implementation, tests, API usage specs and documentation together.

Use executable API usage specs to establish important user flows before or alongside implementation. Keep examples small and representative of the intended style.

Do not introduce or preserve a pattern without a concrete responsibility. Streaming, explicit runs, configuration and execution instance separation, builders and helper base classes must remain justified by the problems they solve.

## Architecture and responsibility boundaries

- Favor SOLID design and clear responsibility boundaries.
- Prefer a small number of coherent concepts over parallel abstractions with overlapping semantics.
- Keep abstractions small, composable and explicit.
- Prefer strong static typing, explicit invariants and compile time safety over conventions or late runtime checks.
- Make invalid states unrepresentable where practical.
- Keep behavior affecting dependencies such as randomness, cancellation, time, scheduling, caches and execution context explicit.
- Avoid ambient state, unnecessary runtime machinery, global registries and opaque indirection.
- Describe architecture through concepts and responsibilities rather than current source layout.
- Optimize public APIs for human users first with clear naming, predictable contracts and visible costs.

The conceptual core and authoring conveniences are separate concerns. Builders, static factories, extension methods and helper base classes should serve the core model without defining it.

## Configuration and execution ownership

Configurations are reusable descriptions. They own immutable settings and references to child configurations. They must not be mutated during execution.

Execution instances own resolved child instances, mutable execution data and execution behavior. Data tied to one run must not be stored on reusable configurations.

Search states are public progress values. Do not place private counters, caches or other execution data in a search state merely to carry it between steps.

Explicit operator and algorithm creation methods receive the full `ExecutionInstanceRegistry`. Do not introduce a second restricted resolver abstraction. Resolve ordinary declared children eagerly when creating the execution instance. Retain the registry only when runtime composition genuinely needs child registries, replacements or delayed child algorithm creation.

Base configurations should implement `CreateExecutionInstance(...)` explicitly and expose a protected role specific creation method to derived types. Return the most concrete accessible execution instance type from that protected method when it is useful to callers.

Operator authoring must follow the ownership paths described in [Operator authoring](operator-authoring.md):

- Use a stateless role base when execution needs configuration data but no mutable execution data.
- Use a stateful role base when execution needs ordinary run scoped state but no execution graph dependencies.
- Use an explicitly authored execution instance when the operator owns child execution instances, disposable resources or custom execution structure.
- Do not store operator or algorithm configurations, execution instances, registries or delegates bound to child instances in framework managed operator state.

Algorithms coordinate operators and execution flow, so they use a configuration paired with an explicitly authored execution instance. See [Algorithm](algorithm.md).

Roslyn analyzers are guardrails for recognizable mistakes. They do not prove that configurations, state and execution instances obey every ownership invariant.

## Immutability and collection ownership

Any type used as `TCandidate` must behave as an immutable value.

- Operators must not mutate parent candidates in place.
- Mutation and crossover may create new candidates but must not change existing candidates.
- Search spaces, problems and algorithms should be designed around immutable candidate flow.

Core configurations and durable value objects use snapshot semantics for retained collections.

- Accept the narrowest read only abstraction that describes the required input shape.
- Use `IReadOnlyList<T>` for finite ordered collection inputs.
- Immediately snapshot retained collection inputs into `ImmutableArray<T>`.
- Expose owned immutable collections as `ImmutableArray<T>`.
- Later changes to a caller owned input collection must not affect an existing configuration or durable value.
- Keep transient operation batches on `IReadOnlyList<T>` when the implementation need not retain them.
- Return mutable collections or arrays only when caller mutation or ownership transfer is intentional.
- Reserve `IEnumerable<T>` for genuinely lazy or sequence oriented APIs.

Snapshotting is shallow. An immutable collection retains its element references, so the elements must satisfy their own mutability contracts.

## Nullability and defensive validation

Nullable reference annotations are repository contracts.

- Do not add runtime null checks for nonnullable parameters, properties or collection elements.
- Handle null when a type is explicitly nullable or code operates at an untyped external boundary.
- Do not add checks solely to defend against `null!`, disabled nullable analysis, reflection or another deliberate contract bypass.
- Do not systematically validate `ImmutableArray<T>.IsDefault`. Assume immutable array parameters are initialized unless a specific API gives the default value meaning or a concrete domain invariant requires validation.

Validate domain invariants that the type system cannot express. Validation must be part of normal runtime behavior.

Debug and release builds must behave the same for the same input.

- Do not use debug only validation.
- Do not use `Debug.Assert` in library code.
- Enforce correctness invariants through types or normal runtime checks.

## Public API design

Design APIs for the pit of success.

- Use descriptive names instead of documentation to compensate for unclear APIs.
- Keep related abstractions and operator roles structurally consistent.
- Make defaults, side effects and potentially expensive behavior visible.
- Return the most concrete accessible type that is useful to callers.
- Avoid capability interfaces until concrete use cases justify the additional abstraction.
- Do not add marker interfaces without behavior or a concrete static typing requirement.
- Do not preserve overloads or base classes only for symmetry when their semantics are unclear.

### Type inference helpers

Callers should not have to spell generic arguments that available values can determine.

- Provide a static `Create(...)` helper when its arguments contain the information required to infer the constructed generic type.
- Provide a static `For(problem, ...)` helper when the problem supplies otherwise missing candidate, search space or problem types.
- Provide a static `For(algorithm, ...)` helper when the algorithm supplies additional required information such as the search state type.
- A `For(...)` argument is a type witness. The created object does not retain it unless the API explicitly says otherwise.
- Do not add a fluent creation extension when its receiver only supplies generic type information.
- Add a fluent extension when the receiver participates in the resulting configuration, for example `mutator.Then(otherMutator)`.
- Keep direct construction available when neither an existing problem nor an algorithm can provide the required type information.

Use API usage specs to prove that the intended calls compile without explicit generic arguments. Mechanically stable helper conventions may be enforced by Roslyn analyzers and code fixes. Assembly wide relationships belong in architecture tests rather than a duplicate analyzer rule.

### Stateless operator implementation methods

Stateless operators should expose direct static methods that mirror their instance entry point.

- Match the static method name to the operation, for example `Create`, `Mutate`, `Cross`, `Select` or `Evaluate`.
- Let the instance method delegate to the static implementation.
- Provide one core overload that accepts the direct inputs needed by the operation.
- Keep search space overloads as thin adapters when they only extract bounds, lengths, probabilities or other direct values.
- Retain the search space in the core overload when it is the honest semantic dependency, such as grammar or topology driven behavior.
- Order overloads from adapters to the final core implementation so the delegation path reads downward.
- Put validation protecting the operation contract in the core overload.
- Let convenience overloads delegate inward rather than duplicate logic.

### Extension APIs

Group extension methods by user facing concern rather than target type alone.

Observation, counting, duration measurement, budget composition, factory and conversion helpers should use separate extension classes when they represent different intents. Avoid generic extension classes that collect unrelated methods.

Source folders may group related extension concerns under folders such as `Instrumentation` without forcing a matching public namespace. Namespaces follow user facing concepts while folders may help maintainers separate implementations and cross cutting concerns.

Concrete wrappers used only to implement an extension concern should remain private or internal unless callers have a clear reason to depend on their concrete type.

### Random API layering

Random APIs build upward from one source of primitive randomness rather than reimplementing sampling behavior at each convenience layer.

1. Primitive RNG methods provide the base random values.
2. Scalar helpers build on those primitives.
3. Typed output helpers build domain values from scalar helpers.
4. Search space helpers forward bounds and other configuration to typed output helpers.
5. Creator operators add only operator level guarantees.
6. Type level factory aliases forward to the RNG helper layers.

Scalar helpers use concept first names such as `NextBool`, `NextDouble` and `NextNormal` because their result shape is already implicit. Typed output and search space helpers use target first names such as `NextRealVectorUniform` and `NextIntegerVectorNormal`.

Do not introduce a competing distribution object layer when an operation is another convenience route to the same sampling behavior.

## XML API documentation

Add XML documentation only when it communicates a nonobvious public contract, invariant, lifecycle rule, failure behavior, algorithm detail or usage constraint.

- Do not add summaries that only repeat a type, member or parameter name.
- Prefer clear names and signatures while the API is evolving.
- Put broader design rationale and examples in `docs`.

## Source and documentation organization

- Use glossary terms instead of legacy or ad hoc alternatives.
- Keep user facing namespaces focused on concepts. Source folders may be more detailed.
- Do not assume that one folder must map to one namespace.
- Keep public behavioral explanations in topic documentation.
- Keep contributor implementation rules in this document.
- Keep unresolved decisions in [Developer backlog](developer-backlog.md). Move settled decisions here or into the relevant public documentation.

## Enforcing guidelines

Choose the enforcement mechanism that matches the rule.

- Use the type system and API shape for invariants that can be made unrepresentable.
- Use unit tests for local behavior and domain invariants.
- Use API usage specs for intended public syntax and type inference.
- Use Roslyn analyzers and code fixes for recognizable source level mistakes or conventions where immediate contributor feedback is valuable.
- Use architecture tests for assembly wide relationships and cross cutting public type rules.
- Use documentation for judgment based guidance that cannot be reduced to a reliable mechanical check.

Follow the validation order in [AGENTS.md](../AGENTS.md) and the placement guidance in [the test suite guide](../test/README.md).
