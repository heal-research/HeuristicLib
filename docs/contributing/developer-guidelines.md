# Developer guidelines

This is the canonical implementation and architecture guide for HeuristicLib contributors. It turns the [design goals](/contributing/design-goals) into rules for code, public APIs and repository changes.

Use the [Glossary](/guide/glossary) for terminology and `AGENTS.md` for repository workflow. Topic documentation owns public contracts and examples.

Cite a section as `§ 4` and a rule as `§ 4.2`. Rule numbers are stable. Add rules at the end of their section and do not reuse removed numbers.

## Documentation roles

Each steering document has one responsibility:

| Document                                                   | Responsibility                                                                                           |
| ---------------------------------------------------------- | -------------------------------------------------------------------------------------------------------- |
| [Requirements](/contributing/requirements)                 | What the library must support and which capability boundaries matter                                     |
| [Design goals and principles](/contributing/design-goals)  | Which durable qualities the resulting library should have                                                |
| [Developer guidelines](/contributing/developer-guidelines) | How contributors implement those requirements and goals                                                  |
| [Glossary](/guide/glossary)                                | Which terminology is canonical                                                                           |
| `plans/developer-backlog.md`                               | Which follow up work and design decisions remain unresolved and which approaches were tried and rejected |
| `AGENTS.md`                                                | How contributors navigate and validate the repository                                                    |

Topic pages must not become alternative policy documents.

## § 1 Project posture

### § 1.1 Treat alpha APIs as provisional

- Backward compatibility is not currently a design goal.
- Existing code and APIs are evidence, not permanent commitments.
- Replace a weak API when a materially stronger design is available instead of adding compatibility around the weak design.
- Every change must solve a concrete problem. Alpha status does not justify arbitrary churn.
- Current folders, assemblies, namespaces and packages are provisional. Do not treat them as the final architecture.

## § 2 Making design changes

### § 2.1 Start with the user problem

1. State the concrete problem.
2. Compare meaningful alternatives.
3. Show the chosen tradeoffs through representative usage.
4. Prefer designs that make intended usage obvious and misuse difficult.
5. Update implementation, tests, API usage specs and documentation together.

### § 2.2 Establish important user flows with executable specs

Use executable API usage specs for important user flows. Keep them small and representative.

### § 2.3 Require every pattern to have a concrete responsibility

Every pattern needs a concrete responsibility. This includes streaming, explicit runs, configuration and execution separation, builders and helper base classes.

## § 3 Architecture and responsibility boundaries

### § 3.1 Keep responsibilities explicit and concepts coherent

- Keep responsibility boundaries clear.
- Prefer a small number of coherent concepts over parallel abstractions with overlapping semantics.
- Keep abstractions small and explicit.
- Prefer strong static typing, explicit invariants and compile time safety to conventions or late runtime checks.
- Make invalid states unrepresentable where practical.
- Keep dependencies such as randomness, cancellation, time, scheduling, caches and execution context explicit.
- Avoid ambient state, unnecessary runtime machinery, global registries and opaque indirection.
- Describe architecture through responsibilities, not source layout.
- Optimize public APIs for human users first with clear naming, predictable contracts and visible costs.

### § 3.2 Keep the conceptual core independent of authoring conveniences

Builders, factories, extensions and helper bases may support the conceptual core. They must not define it.

## § 4 Configuration and execution ownership

### § 4.1 Separate reusable configurations from execution instances

Configurations own immutable settings and child configurations. Execution instances own resolved children, mutable run data and behavior. Never store run data on a configuration.

### § 4.2 Keep execution data out of search states

Search states are public progress values. Do not use them to carry private counters, caches or other execution data.

### § 4.3 Resolve declared children through the execution instance registry

Creation methods receive the full `ExecutionInstanceRegistry`. Resolve declared children during instance creation. Retain the registry only for child registries, replacements or delayed child algorithm creation. Do not add another resolver abstraction.

### § 4.4 Expose one execution instance factory

Expose one public `CreateExecutionInstance(...)` method with the exact execution instance role as its return type. Do not add role named alternatives such as `CreateSelectorInstance` or hide the factory behind explicit interface implementation.

A topology base may seal the public method and expose one protected overload after child resolution. Return the most concrete accessible instance type useful to derived authors. This rule applies to algorithms and operators.

### § 4.5 Keep construction entry points semantically equivalent

Keep public constructors as the direct path. Add `Create` for generic inference and a fluent extension when its receiver becomes a child configuration. All paths construct the same configuration.

### § 4.6 Give each configuration setting one input path

A setting has one input path.

- Required values are constructor parameters without defaults.
- Optional values are `init` properties with defaults. Do not also put them in the constructor.
- Every configuration property is `{ get; init; }` so `with` can vary it.
- Execution instance children and run data are `{ get; }` because instances are not reconfigured.

### § 4.7 Validate complete configurations during instance creation

Retain configuration values unchanged during construction and `with`. Validate the complete configuration in `CreateExecutionInstance` and throw `InvalidOperationException` before execution begins. Do not add a separate validation API.

A derived topology configuration validates in the protected overload called after child resolution.

### § 4.8 Represent configured behavior with value strategies

Represent configured behavior with a strategy interface whose implementations are immutable values. Do not use delegates because their equality depends on method and target identity and captured state is not persistable. Delegate adapters are limited to documented runtime only APIs.

### § 4.9 Use records only for value semantics

Use a record only when structural equality is the contract. Use a class for a carrier that holds identity bearing dependencies such as an RNG, execution instance or registry.

### § 4.10 Validate operation inputs before side effects

Validate problem, search space and operation inputs in the role method before random draws, state changes or substantial work. Configuration checks belong in instance creation.

### § 4.11 Expose configuration and hide execution machinery

Configurations expose their settings and child configurations. Wrapping and multi bases own `ChildOperator` and `ChildOperators` or role specific equivalents.

Execution instances keep resolved children and machinery private or protected. Use nested operator only for descendants at any depth.

### § 4.12 Keep role authoring hierarchies symmetric

Keep configuration and instance authoring hierarchies symmetric. Full role, wrapping and multi bases have matching instance bases. Derived configurations and instances use the matching pair. Pass required resolved children through constructors.

### § 4.13 Give each child slot one public name

A child slot has one public name.

- Use the wrapping or multi base when a child has no role beyond being a child. Do not add an alias for its canonical property.
- Derive from the role base when children have distinct responsibilities. Declare and resolve each child by its role, as `SelectorForRemaining`, `FemaleSelector` and `MaleSelector` do.

### § 4.14 Choose an operator base by execution ownership

Choose an authoring base by ownership. See [Operator implementation](/contributing/architecture/operator-implementation).

- Use a stateless base for configuration data without mutable run data.
- Use a stateful base for ordinary run data without execution graph dependencies.
- Author an instance explicitly for child instances, disposable resources or custom execution structure.
- Use a single item base for independent per item work.
- Never put configurations, instances, registries or child bound delegates in framework managed operator state.

### § 4.15 Keep single item batching explicit and deterministic

A single item base seals the batch operation and exposes `ExecutionConcurrency Concurrency`. Fork each item RNG from its batch position so concurrency does not change results. `SingleCandidateMutator` is the reference.

### § 4.16 Keep operator scaffolding out of the build

Operator scaffolding remains checked in source. Do not add source generators, IDE actions or scaffolding commands for operator families. Coding agents may prepare ordinary source. Review, test and maintain it like handwritten code.

### § 4.17 Give algorithms explicit execution instances

Algorithms use a configuration paired with an explicitly authored execution instance. See [Algorithms](/guide/fundamentals/algorithms).

### § 4.18 Treat Roslyn analyzers as guardrails

Roslyn analyzers catch recognizable mistakes. They do not prove ownership invariants.

### § 4.19 Keep role execution contracts role specific

Role instances expose their named operation, such as `Mutate`, `Cross`, `Select` or `Evaluate`. Do not add a generic invocation path, shared problem context carrier or generic bridge between role methods.

Matching signatures do not satisfy another nominal role. `IOperator<TExecutionInstance>` only connects a configuration to its instance role.

## § 5 Immutability and collection ownership

### § 5.1 Treat candidates as immutable values

Any type used as `TCandidate` must behave as an immutable value.

- Operators must not mutate parent candidates in place.
- Mutation and crossover may create new candidates but must not change existing candidates.
- Search spaces, problems and algorithms should be designed around immutable candidate flow.

### § 5.2 Snapshot retained collections

Accept finite ordered inputs as `IReadOnlyList<T>`. Snapshot retained inputs with `ToValueArray()` and expose them as `ValueArray<T>`. Caller changes must not affect the owner.

Use `IReadOnlyList<T>` for transient batches and `IEnumerable<T>` for lazy sequences. Return mutable collections only for intentional mutation or ownership transfer.

### § 5.3 Apply immutability contracts to retained elements

Snapshots retain element references. Each element must satisfy its own mutability contract.

### § 5.4 Choose collection types by ownership and meaning

Choose collection types by meaning and ownership.

| Situation                                              | Type                                                  |
| ------------------------------------------------------ | ----------------------------------------------------- |
| Collection retained by a configuration or value object | `ValueArray<T>`                                       |
| Parameter accepting a finite ordered collection        | `IReadOnlyList<T>`, snapshotted with `ToValueArray()` |
| Mathematical coordinates with broadcasting             | `RealVector`, `IntegerVector`, `BoolVector`           |
| Transient batch inside one operation, never retained   | `IReadOnlyList<T>`                                    |
| Collection held by an execution instance               | `ImmutableArray<T>`                                   |
| Lazy or sequence oriented API                          | `IEnumerable<T>`                                      |

`ValueArray<T>` gives configuration records structural equality. `ImmutableArray<T>` compares backing array identity and belongs on execution instances, where structural equality is unnecessary.

### § 5.5 Construct value arrays without accidental nesting

Use `ValueArray.Create(a, b, c)` or a collection expression for elements. A single list argument becomes one element, so use `items.ToValueArray()` to snapshot a sequence. Use `ValueArray.FromOwnedArray(array)` only when the caller transfers ownership and will not mutate the array.

- An `ImmutableArray<T>` converts implicitly.

### § 5.6 Treat the default value array as empty

The default `ValueArray<T>` is empty. Do not check `IsDefault` or normalize it. Use `Count`, not `Length`.

### § 5.7 Do not shadow inherited child collections

Do not redeclare an inherited child collection with `new`. It changes generated record equality. Give a distinct child role its own name instead.

## § 6 Validation and numeric behavior

### § 6.1 Treat nullable annotations as contracts

- Do not add runtime null checks for nonnullable parameters, properties or collection elements.
- Handle null when a type is explicitly nullable or code operates at an untyped external boundary.
- Do not defend against deliberate contract bypasses such as `null!`, disabled nullable analysis or reflection.
- Validate `ImmutableArray<T>.IsDefault` only when the API gives it meaning. `ValueArray<T>` needs no such check.

### § 6.2 Preserve unusual values with stable semantics

Validate only what safety or the documented contract requires. Preserve unusual floating point values when IEEE behavior gives them stable meaning. Do not reject or clamp them by convention.

### § 6.3 Apply the documented unusual numeric semantics

Apply these semantics consistently:

- Threshold rates below zero, negative infinity and `NaN` mean never. Rates above one and positive infinity mean always. Documented fallbacks such as `AtLeastOnce` still apply.
- Empty weights mean uniform selection. Positive finite weights participate proportionally. Zero, negative values, negative infinity and `NaN` mean never.
- Positive infinity overrides finite weights. Choose uniformly among multiple positive infinities. If nothing is selectable, choose uniformly among all entries.
- Do not shift finite weights by their minimum. Weights express ratios, and shifting `[1, 3]` to `[0, 2]` would incorrectly make the first entry impossible.
- Negative normal sigma mirrors samples around the mean. `NaN` propagates and infinities follow IEEE arithmetic.
- Bounded integer conversion maps `NaN` to zero before clamping, positive infinity to the maximum and negative infinity to the minimum.
- Uniform bounds with `high <= low` return `low` without a random draw. Apply this per coordinate and still validate dimensions.

### § 6.4 Keep configured values visible

Configuration properties expose supplied values, not normalized substitutes. Internal execution data may use an equivalent optimized form.

### § 6.5 Validate before dangerous or wasteful work

Validate a violation that could:

- Consume random draws or change state before failing.
- Produce a structurally invalid candidate or configuration.
- Cause unsafe indexing, invalid allocation, nontermination or corruption.
- Waste substantial computation before failing.
- Break an essential relationship such as compatible dimensions or matching operator and weight counts.

### § 6.6 Trust the algorithm's candidate validity contract

Operators may assume that algorithm supplied candidates are valid for the algorithm's search space. Do not repeat candidate dimension, permutation content or matching dimension checks.

Direct calls with invalid candidates may fail later or produce undefined results. Continue to validate independent inputs such as broadcastable parameter dimensions.

### § 6.7 Avoid redundant validation and normalization

Do not validate only to replace an immediate exception or normalize values unless normalization is the operation. Validate configuration invariants once under § 4.7. Public methods may validate inputs once, then call a trusted private implementation.

### § 6.8 Keep debug and release behavior equivalent

Debug and release builds must behave the same. Do not use debug only validation or `Debug.Assert` in library code. Enforce invariants through types or normal runtime checks.

## § 7 Performance and deterministic execution

### § 7.1 Treat hot path cost as part of the design

Treat operator and evaluation cost as part of the design. See [Performance as a design constraint](/contributing/design-goals#performance-as-a-design-constraint).

- Avoid unnecessary allocation, boxing, delegate creation, context object construction and interface indirection in operator and evaluation paths.
- Shared paths must remain cheap enough for lightweight operators such as `InversionMutator`.
- Keep straightforward code until measurement shows a cost.
- Remove an abstraction whose machinery outweighs the behavior it centralizes.
- Keep results independent of workers, partitions, scheduling, concurrency limits and CPU core count. Assign random sequences by logical item or key as described in [Randomness](/guide/execution/randomness).

### § 7.2 Add benchmark infrastructure only by accepted decision

Benchmark locally when a change presents a performance risk and report the numbers. Permanent benchmark infrastructure needs a separate accepted decision.

## § 8 Public API design

### § 8.1 Design APIs for intended use

Make intended use easy and misuse difficult.

- Use descriptive names. Documentation does not excuse an unclear API.
- Public authoring bases expose required constructors, state and hooks as `protected`, not `private protected`. Otherwise do not present the type as an authoring base.
- Keep related abstractions and operator roles structurally consistent.
- Make defaults, side effects and potentially expensive behavior visible.
- Do not give a nullable operator slot a non-null default. `null` would then mean both "not supplied, use the default" and "deliberately absent", and a caller could no longer express the second. Either the slot stays nullable and absence is its default, or it becomes non-nullable with a no-op implementation as the default, such as an identity interceptor. A nullable _parameter_ on a factory is unaffected: it means "not supplied" for a slot that cannot itself be null.
- Return the most concrete accessible type useful to callers.
- Add capability interfaces only for concrete use cases.
- Do not add marker interfaces without behavior or a concrete static typing requirement.
- Do not preserve unclear overloads or bases for symmetry.
- Add a self type parameter only when a consumer needs the exact derived type, as fluent experiment composition does. Do not add one for symmetry.
- Seal completed composition policies. Keep topology and authoring bases open.

### § 8.2 Present the ordinary XML contract first

Put the ordinary contract and expected range in `<summary>`. Put unusual values and edge cases in `<remarks>`.

An invariant contract states something narrower, and the two must not be conflated. The range in a contract is the range in which a **guarantee holds**, not the range that is a good choice. A blend crossover with an alpha above one is a legitimate setting that widens the search; what it stops doing is keeping candidates inside the bounds the search space states. A mutation rate of `0.9` is unusual advice and breaks no invariant, so it stays prose and never reaches a contract. Keep judgement about good values in `<summary>`, and keep guarantee ranges in the contract member, so a contract does not accumulate opinions.

Where a parameter carries both, its documentation points at the member that defines the contract instead of restating the threshold. The condition is then written once, in the executable place, and prose cannot drift away from it.

### § 8.3 Use variance only when substitution is meaningful

- `TCandidate` is invariant throughout the public API. It identifies the exact shared representation.
- Operator role interfaces declare `in TSearchSpace` and `in TProblem` so general operators compose with specific problems. New roles must do the same.
- A returned type parameter cannot be contravariant. This makes `TSearchState` contravariant on `ITerminator` but invariant on `IInterceptor`.
- Other domain parameters use variance only for a concrete substitution requirement and use a domain specific name.
- Sonar S3246 is disabled because permitted positions alone do not establish meaningful substitution.

### § 8.4 Reduce role arity through the authoring hierarchy

Reduced arities let authors omit operation inputs they do not use. Keep each role family consistent.

- A reduced configuration base derives from the next fuller configuration base.
- A reduced instance base implements the full interface directly and forwards explicitly to one narrower abstract operation. It does not derive from the fuller instance base.
- A reduced stateless base derives from the reduced configuration base and implements the role instance interface directly.
- A reduced stateful base derives from the reduced configuration base. Its nested instance derives from the reduced instance base.

### § 8.5 Reduce only arities that describe operation inputs

Reduce type arguments consumed by the operator's own code. Do not reduce type arguments that describe an owned child slot.

| Base                                                            | Reduced arities | Why                                    |
| --------------------------------------------------------------- | :-------------: | -------------------------------------- |
| `Mutator`, `MutatorInstance`                                    |       Yes       | No owned child                         |
| `StatelessMutator`, `StatefulMutator`, `SingleCandidateMutator` |       Yes       | Type arguments describe operation data |
| `WrappingMutator`, `MultiMutator`                               |       No        | Type arguments describe child slots    |

Keep topology type arguments open so their children determine them. Role contracts remain full arity. Reduced arities are authoring conveniences, not separate contracts.

### § 8.6 Provide type inference helpers when values supply the types

Do not make callers spell generic arguments available values can determine.

- Put public constructors on the configuration type.
- Put `Create(...)`, `For(problem, ...)` and `For(algorithm, ...)` on a static companion named after the type.
- Use `Create(...)` when the caller supplies the required collaborators and they determine the type arguments. Use `For(anchor, ...)` when an anchor value supplies both the type arguments and the defaults, so every remaining parameter can be optional.
- A `Create(...)` or `For(...)` must be able to return a complete configuration. Accept every configurable member as a parameter, required in `Create` and optional in `For`, so a caller is never left finishing a partly configured result. `with` changes a configuration that already exists; it is not the way to complete one the factory could not build.
- Anchor a `For(...)` that also takes optional operators on an invariant parameter type. A covariant anchor such as `IProblem<TCandidate, out TSearchSpace>` contributes only a lower bound, so an operator declared at a reduced arity widens the inferred search space and fails against constraints the caller never named. An invariant anchor contributes an exact bound, fixing the type from the anchor alone.
- Treat a `For(...)` argument as a type witness unless the contract says it is retained.
- Add a fluent extension only when its receiver becomes part of the configuration.
- Put fluent methods in `<Type>Extensions`. A concern specific helper such as `WithRate` may use its own companion.
- Keep direct construction when no value can supply the types.
- Prove intended inference with API usage specs. Use architecture tests or analyzers only where each fits under § 10.1.

### § 8.7 Keep operator parameter groups in a common order

Order parameters as primary inputs, RNG, search space and problem. Omit groups that do not apply. Lower level inputs that replace a search space occupy its position.

### § 8.8 Expose direct static implementations when the operation needs one

Expose a direct static implementation when the candidate or genotype does not already own the operation.

- Do not add a static method that only forwards to the candidate or genotype.
- Name the static method for the operation, such as `Create`, `Mutate`, `Cross`, `Select` or `Evaluate`. Let the instance method delegate to it.
- Provide one core overload that accepts the direct inputs needed by the operation.
- Use search space overloads as thin adapters unless the search space is the true dependency.
- Order adapters before the core overload and delegate inward.
- Validate at the public boundary. Use a trusted private implementation to avoid repeated checks.

### § 8.9 Group extensions by user facing concern

Group extensions by user concern, not target type alone. Give observation, counting, duration, budget, factory and conversion helpers separate classes. Use concern names and role first names such as `MutatorDurationExtensions`.

Folders may group implementations without forcing a public namespace. Keep implementation wrappers private or internal unless callers need their concrete type.

### § 8.10 Build random APIs in layers

Build random APIs from one source of base random values.

1. RNG methods provide base random values.
2. Scalar helpers build on them.
3. Typed output helpers build domain values from scalar helpers.
4. Search space helpers forward configuration to typed output helpers.
5. Creator operators add only operator level guarantees.
6. Type factory aliases forward to the RNG helpers.

Use concept first scalar names such as `NextDouble` and target first output names such as `NextRealVectorUniform`. Do not add a competing distribution layer for the same sampling behavior.

## § 9 Documentation and source organization

### § 9.1 Document nonobvious public contracts

Add XML documentation only for a nonobvious contract, invariant, lifecycle, failure, algorithm detail or usage constraint. Prefer a clear name and signature.

- Do not add summaries that only repeat a type, member or parameter name.
- Put design rationale and examples in `docs`.
- Do not add XML documentation to tests.

### § 9.2 Keep policy and public documentation in their assigned documents

- Use glossary terms instead of legacy or ad hoc alternatives.
- Keep user facing namespaces focused on concepts. Folders may be more detailed.
- Do not assume that one folder must map to one namespace.
- Keep public behavioral explanations in topic documentation.
- Keep contributor implementation rules in this document.
- Keep unresolved decisions in `plans/developer-backlog.md`. Move settled decisions here or into the relevant public documentation.

### § 9.3 Name types for what binds them

Name a type for the narrowest domain or representation that binds it. Use genotype names for types that work with any problem over that genotype. Use domain names for types that require one problem. The placement question remains in `plans/developer-backlog.md`.

### § 9.4 Allow authoring dependencies required by construction

An authoring type may depend on a search space even when the candidate representation does not. Construction may require this dependency. Do not weaken the authoring API to remove it.

For example, `ExpressionDraft` takes an `ExpressionTreeSearchSpace` to resolve symbols while building, even though `ExpressionTree` itself does not depend on a search space.

### § 9.5 Match namespaces and folders by audience

- Put user facing types in concept namespaces.
- Use detailed namespaces only for contracts, bases and machinery that authors reach for rather than users.
- Give a concept one namespace. Do not split it into small public namespaces that a user has to import together.
- A source subfolder adds a namespace segment only when it changes the audience. Folders may organize implementation categories without adding a namespace segment.
- Use architecture tests for reviewed concept boundaries. Do not enforce a mechanical namespace to path equation across the project.
- Keep project roots free of source files. Put every type in the folder for its owning concept even when its namespace is intentionally broader than that folder.

### § 9.6 Keep package placement deliberate

A component belongs in the main package when its responsibility is part of the standard HeuristicLib toolkit, its current contract is coherent and complete, its important behavior is tested and its dependencies are suitable for ordinary users.

A component belongs in Experimental when it has a real use case but its public model or semantics remain unsettled, it is a research workflow with limited validation, it is retained legacy code or it depends on another experimental component.

Do not move empty implementations, inaccessible results or known unbounded defects to Experimental. Remove them, make them internal or complete them first.

Promote a feature from Experimental only when its responsibility belongs in the standard toolkit, its central API has no expected replacement, its ownership rules match this guide, its advertised behavior is complete, its normal setup has an API usage spec and its important failure behavior has unit tests. Promotion must not force unrelated experimental concepts or unsuitable dependencies into the main package.

The main and Contracts packages must never reference Experimental. Experimental may reference the main package.

### § 9.7 Keep XML documentation verifiable by the build

`Directory.Build.props` sets `GenerateDocumentationFile`, so the compiler parses every `///` block and resolves every
`cref`. The generated XML file ships in the NuGet package for consumer IntelliSense, but the checks are the reason the
flag is on.

A documentation comment that states something false about the code is an error. A comment that is merely missing is not.

- Errors: CS1570, CS1571, CS1572, CS1574, CS1580, CS1584, CS1587, CS1710 and CS1711. These report badly formed XML, an
  unresolvable or malformed `cref`, a `param` or `typeparam` tag naming something that does not exist or naming it
  twice, and a comment placed on an element that cannot carry one.
- Warnings: CS1573 and CS1712, which report a tag that is absent rather than one that is wrong.
- CS1591 is suppressed. It reports documentation coverage, which § 9.1 deliberately does not require.
- Add a diagnostic to the error list only when it reports a comment that contradicts the code.

An incremental build does not re-emit these diagnostics, so verification needs a clean build.

## § 10 Enforcement and implementation style

### § 10.1 Match each rule to its enforcement mechanism

- Use the type system and API shape for invariants that can be made unrepresentable.
- Use unit tests for local behavior and domain invariants.
- Use API usage specs for intended public syntax and type inference.
- Use Roslyn analyzers and code fixes for recognizable source mistakes.
- Use architecture tests for assembly wide relationships and cross cutting public type rules.
- Use documentation for rules that require judgment.

### § 10.2 Follow repository validation and test placement guidance

Follow `AGENTS.md` for validation order and `test/README.md` for test placement.

### § 10.3 Use expression bodies for value producing members

Use an expression body for a value producing or forwarding member. Use a block body for constructors, state changes and side effects. IDE0021 enforces this for constructors. Review other methods manually.

### § 10.4 Keep diagnostic exceptions narrow

Fix diagnostics instead of suppressing them. For an intentional local exception with a standard ID, use a narrow `#pragma warning disable` and `#pragma warning restore` pair.

Reserve `.editorconfig` severity changes for repository wide policy. Keep vendor specific suppressions in personal IDE settings.
