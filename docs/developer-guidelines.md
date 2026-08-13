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

Base configurations should expose one public `CreateExecutionInstance(...)` method with the exact execution instance role as its return type. Do not replace it with a parallel role-named factory such as `CreateSelectorInstance`. A topology base may seal the public factory and expose one protected post-resolution overload to derived configurations. Return the most concrete accessible execution instance type from that overload when it is useful to derived authors.

Keep public constructors as the direct construction path. Add a static `Create` helper when its arguments can infer otherwise repetitive generic type parameters. Add a fluent extension when the receiver naturally becomes a child configuration. These entry points should construct the same configuration rather than introduce different semantics.

Configuration objects expose the information that describes their configured behavior through public read-only properties, including retained child operators. A wrapping base owns the canonical singular child property and a multi-operator base owns the canonical child collection. Name these properties `ChildOperator` and `ChildOperators`, or use role-specific forms such as `ChildMutator` and `ChildMutators`. Reserve nested operator for an operator at any descendant depth. Execution instances keep resolved child instances and other execution machinery private or protected by default.

Role authoring hierarchies are deliberately symmetric between configurations and execution instances. A full role base has a matching full role instance base, and wrapping and multi topology bases come in matching configuration/instance pairs. A derived configuration and its nested execution instance use the corresponding pair even when the instance base currently contributes only canonical protected child storage. This predictable hierarchy and the topology it expresses at the type level are part of the authoring model. Pass required resolved children through constructors so an execution instance cannot be created incomplete.

A child slot has exactly one public name. Which name it is follows from how the operator identifies its children:

- When a child is identified by being a child and nothing more, use the wrapping or multi base. It owns the canonical public property, and derived types add no second name for the same object. `ChooseOneMutator`, `PipelineMutator` and the observing, counting and duration-measuring concerns work this way.
- When a child plays a specific part in the operator's own logic, the topology base is the wrong base. Derive from the role base, declare exactly the children the operator needs with names that describe their part, and resolve them in `CreateExecutionInstance`. `EliteSelector` asks `SelectorForRemaining` only for the places left after the elites, and `GenderSpecificSelector` needs one `FemaleSelector` and one `MaleSelector`.

Do not reach for a forwarding alias to reconcile the two. Two public properties returning the same child leave a reader with no way to tell which is the right one. Declaring the children directly also lets the constructor fix how many there are, which a multi base cannot express.

Operator authoring must follow the ownership paths described in [Operator authoring](operator-authoring.md):

- Use a stateless role base when execution needs configuration data but no mutable execution data.
- Use a stateful role base when execution needs ordinary run scoped state but no execution graph dependencies.
- Use an explicitly authored execution instance when the operator owns child execution instances, disposable resources or custom execution structure.
- Use a single-item role base when the role offers one and the operation applies independently to each item of the batch.
- Do not store operator or algorithm configurations, execution instances, registries or delegates bound to child instances in framework managed operator state.

A single-item base seals the batch role operation it implements, so the single-item method stays the only authoring surface, and exposes an `ExecutionConcurrency Concurrency` property so batching remains a visible configuration decision rather than an override. Per-item random number generators are forked from the batch position, which keeps the result independent of the chosen concurrency. `SingleCandidateMutator` is the reference; roles whose single-item base predates it are aligned as they are migrated.

Algorithms coordinate operators and execution flow, so they use a configuration paired with an explicitly authored execution instance. See [Algorithm](algorithm.md).

Roslyn analyzers are guardrails for recognizable mistakes. They do not prove that configurations, state and execution instances obey every ownership invariant.

## Immutability and collection ownership

Any type used as `TCandidate` must behave as an immutable value.

- Operators must not mutate parent candidates in place.
- Mutation and crossover may create new candidates but must not change existing candidates.
- Search spaces, problems and algorithms should be designed around immutable candidate flow.

Core configurations and durable value objects use snapshot semantics for retained collections.

- Accept the narrowest read only abstraction that describes the required input shape.
- Use HLib vector types for mathematical coordinates; use value arrays for immutable configuration topology.
- Use `RealVector`, `IntegerVector` or `BoolVector` when positions represent mathematical dimensions and vector operations or broadcasting are meaningful.
- Use `ValueArray<T>` when positions correspond to child operators, pipeline stages or another configuration collection. Accept such inputs as `IReadOnlyList<T>` and snapshot them with `ToValueArray()`.
- Use `IReadOnlyList<T>` for finite ordered collection inputs.
- Immediately snapshot retained collection inputs into `ValueArray<T>`.
- Expose owned immutable collections as `ValueArray<T>`.
- Later changes to a caller owned input collection must not affect an existing configuration or durable value.
- Keep transient operation batches on `IReadOnlyList<T>` when the implementation need not retain them.
- Return mutable collections or arrays only when caller mutation or ownership transfer is intentional.
- Reserve `IEnumerable<T>` for genuinely lazy or sequence oriented APIs.

Snapshotting is shallow. An immutable collection retains its element references, so the elements must satisfy their own mutability contracts.

### Choosing a collection type

`ValueArray<T>` exists because structural equality is a public contract of every configuration, and `ImmutableArray<T>` does not provide it. Two `ImmutableArray<T>` values with equal contents compare **unequal**, because the comparison is by underlying array reference. A record holding one therefore silently loses structural equality, with no diagnostic. Carrying that equality in the member type rather than in an attribute or a generated `Equals` makes the correct behavior the default and removes the possibility of forgetting it.

| Situation | Type |
| --- | --- |
| Collection retained by a configuration, record or other value object | `ValueArray<T>` |
| Parameter accepting a finite ordered collection | `IReadOnlyList<T>`, snapshotted with `ToValueArray()` |
| Mathematical coordinates with broadcasting | `RealVector`, `IntegerVector`, `BoolVector` |
| Transient batch inside one operation, never retained | `IReadOnlyList<T>` |
| Collection held by an **execution instance** | `ImmutableArray<T>` |
| Lazy or sequence oriented API | `IEnumerable<T>` |

Execution instances keep `ImmutableArray<T>`. They are resolved by reference identity through `ExecutionInstanceRegistry` and are never compared structurally, so value equality would cost without buying anything.

`ValueArray<T>` is a `readonly struct` over an `ImmutableArray<T>`, so it adds no allocation, and a record holding one compares and hashes it without boxing.

Construction:

- `ValueArray.Create(a, b, c)` or a collection expression `ValueArray<T> x = [a, b, c]` build from elements. Note that a single argument that is not an array or span becomes **one element** — `ValueArray.Create(someList)` produces a one element array whose element is the list.
- `items.ToValueArray()` snapshots any sequence, and does not copy an input that is already immutable. This is the form configuration constructors use.
- `ValueArray.FromOwnedArray(array)` wraps an array without copying; the caller transfers ownership and must not mutate it afterwards.
- An `ImmutableArray<T>` converts implicitly.

The default value is an empty array, not an invalid one. Do not write `IsDefault` guards or normalize a collection member before assigning it; `ValueArray<T>` has no observable default state. Use `Count` rather than `Length`.

Do not redeclare an inherited child collection with `new`. The compiler will not warn once `new` is present, and the derived record's synthesized equality then compares the shadowing member instead, reintroducing reference equality. A child that plays a specific part gets its own name rather than hiding the inherited one.

## Nullability and defensive validation

Nullable reference annotations are repository contracts.

- Do not add runtime null checks for nonnullable parameters, properties or collection elements.
- Handle null when a type is explicitly nullable or code operates at an untyped external boundary.
- Do not add checks solely to defend against `null!`, disabled nullable analysis, reflection or another deliberate contract bypass.
- Do not systematically validate `ImmutableArray<T>.IsDefault`. Assume immutable array parameters are initialized unless a specific API gives the default value meaning or a concrete domain invariant requires validation. `ValueArray<T>` needs no such check at all: its default value is an empty array.

Use minimal safety validation. Preserve caller supplied values exactly and accept unusual values when the operation has stable computational semantics for them. In particular, do not reject or clamp floating point values merely because they lie outside a conventional range or are `NaN` or infinite. IEEE comparison and propagation behavior is part of the computation unless a specific API defines a stricter contract.

### Unusual numeric values

Apply the following semantics consistently when an operation can give unusual floating point values a stable meaning:

- A rate or probability used as a threshold against a random value in `[0, 1)` treats a finite value below zero, negative infinity and `NaN` as never; a finite value greater than one and positive infinity mean always. Do not clamp or reject these values. An explicit guarantee such as `AtLeastOnce` may still perform its documented fallback after no item passes the threshold.
- Weighted selection retains the originally configured weights. An empty weight collection means uniform selection across all available entries. Positive finite weights participate proportionally. Zero, negative finite weights, negative infinity and `NaN` mean never. Positive infinity overrides all finite weights; multiple positive-infinity entries are selected uniformly. If no entry is selectable, fall back to uniform selection across all entries.
- Do not shift finite weights by their minimum. Weights express ratios, and shifting `[1, 3]` to `[0, 2]` would incorrectly make the first entry impossible.
- A negative normal-distribution sigma mirrors samples around the mean. A `NaN` sigma propagates `NaN`; infinite sigmas follow IEEE arithmetic.
- When converting a floating point value to a bounded integer, map `NaN` to zero before clamping. Positive infinity maps to the maximum and negative infinity to the minimum.
- Uniform random bounds use collapsed-range semantics: when `high <= low`, return `low` without consuming a random draw. Apply this per coordinate for vector bounds. Continue to validate structural requirements such as compatible dimensions.

Configuration properties expose the caller supplied values rather than normalized substitutes. An implementation may compile those values into an internal effective distribution or another optimized execution representation as long as the documented behavior is preserved.

Validate when a violation could:

- Consume random draws or otherwise change state before failing.
- Produce a structurally invalid candidate or configuration.
- Cause unsafe indexing, invalid allocation, nontermination or corruption.
- Fail only after substantial avoidable computation.
- Break an essential relationship such as compatible dimensions or matching operator and weight counts.

Do not add validation solely to replace an immediate underlying exception with a different exception type. Do not normalize or clamp configuration values unless normalization is the documented operation itself. Hard configuration invariants should be validated once when the configuration is authored. Public direct-operation methods may validate hard invariants at their boundary, but execution paths should use private trusted implementations when necessary to avoid repeating validation for every candidate.

Debug and release builds must behave the same for the same input.

- Do not use debug only validation.
- Do not use `Debug.Assert` in library code.
- Enforce correctness invariants through types or normal runtime checks.

## Public API design

Design APIs for the pit of success.

- Use descriptive names instead of documentation to compensate for unclear APIs.
- Public authoring base classes must expose the constructors, state and extension hooks required by external subclasses as `protected`, not `private protected`. If external inheritance is not supported, do not present the type as an authoring base.
- Keep related abstractions and operator roles structurally consistent.
- Make defaults, side effects and potentially expensive behavior visible.
- Return the most concrete accessible type that is useful to callers.
- Avoid capability interfaces until concrete use cases justify the additional abstraction.
- Do not add marker interfaces without behavior or a concrete static typing requirement.
- Do not preserve overloads or base classes only for symmetry when their semantics are unclear.

### Generic variance

The framework `TCandidate` type parameter must be invariant throughout the public API, even when the compiler permits covariance or contravariance. The candidate type identifies the exact representation shared by an algorithm, its problem, its search space and its operators. Do not declare `TCandidate` with `in` or `out`.

Variance remains appropriate for search space and problem type parameters when it enables an operator or observer defined for a general contract to work with a more specific search space or problem. Declare variance on `TSearchSpace` and `TProblem` only where the type positions are valid and the substitution supports that specialization model.

Domain specific type parameters may use variance when there is a concrete substitution requirement. Name such parameters after their domain role rather than `TCandidate`.

Sonar rule S3246 is disabled because it recommends variance based only on permitted type positions and cannot express these semantic policies. Treat variance as an intentional API decision rather than an automatic style improvement.

For operator role contracts this is not a free choice. Every role interface and role instance interface declares `in TSearchSpace` and `in TProblem`, and a new role must do the same. That contravariance is what allows an operator authored against a general contract to be used where a more specific one is expected — `NoChangeMutator<TCandidate>` is declared over `ISearchSpace<TCandidate>` and `IProblem<...>`, yet composes into a `ChooseOneMutator<TCandidate, TSearchSpace, TProblem>` over any concrete search space and problem. Without `in`, every arity-reduced operator stops being usable in a higher-arity composition, and the failure surfaces as unrelated-looking conversion errors at the composition site rather than at the interface.

A type parameter that appears in a return position cannot be contravariant, so exceptions are structural rather than stylistic. `TSearchState` is contravariant on `ITerminator`, whose operation only consumes the state, and invariant on `IInterceptor`, whose operation returns it. Record such an exception where it occurs; do not generalize it to other roles.

### Role arity reduction

A role provides its operation at several arities: the full `<TCandidate, TSearchSpace, TProblem>` form, a search-space-only form, and a candidate-only form. Reduced arities exist so an author who needs neither the problem nor the search space does not have to name them.

Reduce with an explicit interface implementation that forwards to a narrower abstract method, as `MutatorInstance<TCandidate, TSearchSpace>` and `StatelessMutator<TCandidate>` do. The narrower method is the authoring surface; the explicit implementation satisfies the full contract by supplying the arguments the author declined to take. This is the sanctioned use of explicit interface implementation and is unrelated to how the configuration factory is exposed.

The ladder is not one shape repeated. Configuration bases reduce by narrowing generic arguments with no member changes, instance bases add a narrower abstract operation plus a forwarder, stateless bases combine both, and stateful bases pair a reduced configuration with a nested instance on the reduced instance base. Keep each family internally consistent across roles rather than forcing one mechanism onto all of them.

The two sides of the ladder differ in how a reduced base attaches to the fuller one. This is deliberate, not an accident of the mutator implementation:

- A reduced **configuration** base derives from the next fuller configuration base. `Mutator<TCandidate, TSearchSpace>` derives from `Mutator<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>>`. Reduction substitutes type arguments and adds no member, so the inherited `CreateExecutionInstance` is already the correct one and inheritance costs nothing.
- A reduced **execution instance** base does not derive from the fuller instance base. It implements the role instance interface directly and forwards explicitly, as `MutatorInstance<TCandidate, TSearchSpace>` does. Deriving would inherit the full-arity abstract operation, which the reduced base would then have to override and seal, leaving two public operations on the authoring surface where the author must implement one and ignore the other. Implementing the interface directly leaves exactly one visible `Mutate`.
- A reduced **stateless** base follows both rules at once, because it is a configuration and its own execution instance: it derives from the reduced configuration base and implements the role instance interface directly.
- A reduced **stateful** base derives from the reduced configuration base, and its nested instance derives from the reduced instance base.

Apply the same split when reducing a new role. A reduced instance base that inherits from a fuller instance base is a review finding rather than a style variation.

#### Which bases get reduced arities

Reduce an arity only when the type arguments describe what the operator's own code consumes. Do not reduce when they describe a child slot the base owns.

| Base | Reduced arities | Why |
| --- | :--: | --- |
| `Mutator`, `MutatorInstance` | Yes | The base owns no child; an author who needs children declares and types them itself |
| `StatelessMutator`, `StatefulMutator`, `SingleCandidateMutator` | Yes | The type arguments are exactly the operation's own inputs |
| `WrappingMutator`, `MultiMutator` | No | The type arguments type `ChildMutator` and `ChildMutators` |

For a leaf, reduction is generalization: a `SingleCandidateMutator<Permutation>` declares that it needs neither the search space nor the problem, and role contravariance then lets it be used wherever a narrower mutator is expected.

For a topology base the effect is reversed. Its type arguments are the child slot, and narrowing the slot widens the contract a child must satisfy: `MultiMutator<Permutation>` would type its children as `IMutator<Permutation, ISearchSpace<Permutation>, IProblem<Permutation, ISearchSpace<Permutation>>>` and therefore accept only mutators that work for every search space and every problem. A problem-specific child could not be composed in, and the composition an author needs is usually not known when the topology is written. Keep `TSearchSpace` and `TProblem` open on topology types so they are inferred from the children — which is what every shipped concern (`ObservableMutator`, `CountingMutator`, `DurationMeasuringMutator`, `PipelineMutator`, `ChooseOneMutator`) already does.

The role contracts stay at the full arity for a related reason. A reduced-arity `IMutator<TCandidate>` would only be satisfied by types that implement it explicitly, so operators that implement the role contract directly would stop being interchangeable with those that derive from an authoring base. Reduced arities are an authoring convenience and must not become a second contract.

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

### Operator parameter order

Order operator operation parameters as the role's complete primary input group, random number generator, search space and problem. A primary input group may contain several values; selection, for example, receives the population, objective directions and requested count before its random number generator. Omit parameters that do not apply while preserving the relative order of the remaining groups. Lower-level inputs that replace a search space, such as explicit minimum and maximum bounds, occupy the search-space position.

### Stateless operator implementation methods

Stateless operators should expose direct static implementation methods when the underlying candidate or genotype does not already provide the functionality executed by the operator. When the functionality belongs naturally to the candidate or genotype, use that API instead of duplicating it on the operator.

- Do not add a static operator method that only forwards to an existing candidate or genotype method.
- When a static implementation method is needed, match its name to the operation, for example `Create`, `Mutate`, `Cross`, `Select` or `Evaluate`.
- Let the instance method delegate to the static implementation.
- Provide one core overload that accepts the direct inputs needed by the operation.
- Keep search space overloads as thin adapters when they only extract bounds, lengths, probabilities or other direct values.
- Retain the search space in the core overload when it is the honest semantic dependency, such as grammar or topology driven behavior.
- Order overloads from adapters to the final core implementation so the delegation path reads downward.
- Put necessary public-boundary validation before random draws, state changes or expensive work. Use a private trusted implementation when operator execution would otherwise repeat validation for every candidate.
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
- Prefer fixing a diagnostic over suppressing it.
- Use a narrowly scoped `#pragma warning disable` and `#pragma warning restore` pair for an intentional local exception when the diagnostic has a standard compiler or analyzer ID.
- Reserve `.editorconfig` severity changes for repository wide policy. Do not use them for isolated exceptions.
- Do not commit IDE vendor specific suppression comments or suppression attributes. Configure inspections that exist only in one IDE in personal IDE settings when the code should remain unchanged.

Follow the validation order in [AGENTS.md](../AGENTS.md) and the placement guidance in [the test suite guide](../test/README.md).

### Expression-bodied members

Use an expression body for a member that produces a value: a property, an indexer, an accessor, or a method that computes a result or forwards to another member.

Do not use an expression body for a member whose purpose is to change state. A constructor always uses a block body, which `csharp_style_expression_bodied_constructors = false` enforces as IDE0021. The same reasoning applies to a method that exists to mutate state or to run a side effect rather than to return a value; no mechanical check covers that case, so apply it by review.
