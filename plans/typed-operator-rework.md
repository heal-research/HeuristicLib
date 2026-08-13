# Typed Operator Rework

## Status

Rejected at the mutator-spike decision gate. The typed `Invoke` authoring model did not reduce the overall role-specific implementation surface enough to justify its additional contracts, generic parameters, context types, forwarding methods, and dispatch cost. It must not be migrated to further operator roles.

The production experiment has been rolled back from the mutator slice. Its measured result and the independent architectural decisions recorded below are retained.

Compile-time generation of ordinary role-specific implementations was evaluated separately and is not being adopted. Operator roles and cross-cutting concern adapters remain ordinary source; coding agents may assist with scaffolding. See [Operator scaffolding](operator-scaffolding-generation.md).

## Subsequent migration state

The rejected typed-invocation spike led to a role-specific authoring rework rather than a return to the pre-spike design.

- Mutator has been reworked and accepted as the reference role, including its arity ladders, stateless and stateful paths, single-candidate batching path, wrapping and multi topology, applicable concerns, construction paths, equality, and tests.
- Selector has been reworked and accepted as the second common-shape role, including its arity ladders, stateless and stateful paths, wrapping and multi topology, applicable concerns, construction paths, equality, and tests. Selector deliberately has no single-item base.
- Crossover and Creator have been reworked to the same shape as the third slice and await acceptance, including their arity ladders, stateless and stateful paths, single-candidate batching paths, wrapping and multi topology, applicable concerns, construction paths, equality, and tests.
- Replacer and Evaluator have been reworked together and await acceptance. Replacer follows the Selector whole-population shape; Evaluator follows the Creator batch shape and uses `SingleCandidateEvaluator` for scalar authoring with deterministic batching.
- Interceptor and Terminator have been reworked together and await acceptance. Both include search-state-aware arity ladders, stateless and stateful paths, wrapping and multi topology, applicable concerns, construction paths, equality, and tests. Interceptor operations receive the iteration RNG; Terminator additionally retains a state-agnostic candidate-only arity.
- Source generation and deterministic scaffolding were considered and rejected for the current authoring model. New role families and cross-cutting concern adapters remain ordinary checked-in source; coding agents are the recommended optional scaffolding aid.

The retained outcomes below describe architectural rules established by the spike and subsequent accepted slices. They must not be read as claiming that every operator role already implements those rules.

## Original Goal

HeuristicLib has role-specific implementations of cross-cutting concerns such as duration measurement, observation, choosing one child, and pipelining. The proposed rework attempted to centralize each concern behind a typed execution signature:

```text
(TInput, TContext) -> TOutput
```

Role-specific configurations and execution instances would remain the ordinary API, while generic meta-operator bases would invoke compatible children through `Invoke`.

The intended result was one canonical behavioral implementation plus minimal role-specific declarations.

## Decision

Do not retain the typed invocation model.

The mutator spike demonstrated that C#'s nominal interface model still requires a role-specific configuration and execution instance. A generic pipeline containing mutators cannot itself become an `IMutator` merely because its type arguments are mutators. CRTP can preserve a self type but cannot add interface membership, so it does not remove those role leaves.

The duration-measuring prototype moved only the small exception-safe timing block into `DurationMeasuringOperator`. Constructors, configuration properties, static factories, fluent extensions, the nested execution instance, role declarations, and role forwarding remained. The generic base declarations and context bridge replaced at least as much code as they removed.

The pipeline prototype likewise retained `PipelineMutator` and its role-specific execution instance. Its shared behavior was only a straightforward sequencing loop, while the generic path increased complexity and isolated dispatch cost.

The isolated pass-through benchmark measured approximately:

| Path | Mean | Allocated |
| --- | ---: | ---: |
| Previous one-stage role pipeline | 2.39 ns | 0 B |
| One-stage typed pipeline | 12.84 ns | 0 B |

Zero allocation was preserved, but that alone did not make the additional dispatch and type-system complexity acceptable. The abstraction did not pay for itself in either code reduction or performance.

Small, explicit role-specific implementations are preferable to a shared abstraction whose supporting machinery is larger than the behavior it centralizes.

## Typed Machinery Not Retained

The rollback removes the following experimental concepts:

- `IOperatorInstance<TInput, TContext, TOutput>`;
- generic `Invoke`;
- default bridges between `Invoke` and role methods;
- `IProblemContext` and `ProblemContext`;
- generic `WrappingOperator` and its typed execution-instance base;
- generic `MultiOperator` and its typed execution-instance base;
- `DurationMeasuringOperator`;
- `PipelineOperator`;
- `ChooseOneOperator`;
- typed-operator-specific architecture tests, API usage specs, and documentation.

`IOperator<TExecutionInstance>` is retained. It expresses which execution-instance role a reusable operator configuration creates and supports typed registry resolution. It is independent of the rejected input/context/output invocation model.

## Historical alternative: Source-Generated Role Implementations

Status: rejected for the current authoring model. The analysis below is retained as evidence, not as an open implementation plan. The accepted approach keeps operator families as ordinary checked-in source and uses coding agents as optional development-time scaffolding aids. There is no generator attribute, IDE action, or build command. See [Operator scaffolding](operator-scaffolding-generation.md).

### Concept

A source generator could centralize repetitive implementation templates at compile time while leaving the runtime operator model role-specific. Instead of making one generic runtime operator implement arbitrary roles, the generator would emit ordinary nominal configurations and execution instances for each requested role.

A desired role-level authoring shape is:

```csharp
[GenerateDurationMeasuringOperator]
[GeneratePipelineOperator]
[GeneratePerItemChooseOneOperator]
public abstract record Mutator<TCandidate, TSearchSpace, TProblem>
    : IMutator<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    public abstract IMutatorInstance<TCandidate, TSearchSpace, TProblem>
        CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry);
}
```

The generator would emit normal public types such as:

- `DurationMeasuringMutator<TCandidate, TSearchSpace, TProblem>`;
- `PipelineMutator<TCandidate, TSearchSpace, TProblem>`;
- `ChooseOneMutator<TCandidate, TSearchSpace, TProblem>`;
- their role-specific execution instances;
- their constructors, configuration properties, factories, and appropriate fluent extensions.

The generated execution instances call `Mutate` directly. Equivalent generated types for other roles call their ordinary role method directly. No generic `Invoke`, problem context, runtime adapter, reflection, dynamic dispatch, or additional interface boundary is introduced.

The annotated role base does not need to be partial when the generator only inspects it and emits separate types. A lower-level alternative is a partial wrapper declaration:

```csharp
[GeneratePipelineOperator]
public sealed partial record PipelineMutator<
    TCandidate,
    TSearchSpace,
    TProblem>
    : IMutator<TCandidate, TSearchSpace, TProblem>;
```

The generator could augment that partial type with its implementation. This is easier and less implicit but retains one handwritten declaration for every role and concern. Prefer role-level generation if it can be made semantically reliable without extensive string configuration.

Use `Generate...` attribute names so the source clearly communicates that public API and behavior will be added at compile time. Names such as `PipelineOperator` would be too easy to mistake for an ordinary runtime type.

### Why this differs from typed invocation

Source generation moves the common implementation into a compile-time template rather than a runtime hierarchy. The final compilation still contains the role-specific types C# requires. Their code can be equivalent to the former handwritten implementations and can therefore preserve nominal assignability, role terminology, registry behavior, and optimized direct dispatch.

The generator does not eliminate the compiled public type matrix. It eliminates its handwritten implementation and keeps the matrix out of the runtime abstraction model.

### Advantages

- Preserve pure role contracts and named methods such as `Mutate`, `Cross`, and `Select`.
- Preserve direct assignability to `IMutator`, `ICrossover`, and other role interfaces.
- Generate direct role-method calls with no additional runtime dispatch or context construction.
- Generate nested execution instances, configuration properties, constructors, validation, static factories, fluent extensions, and useful XML documentation that runtime base classes could not remove.
- Keep generated configuration types publicly inspectable and ordinary execution machinery private or protected.
- Centralize stable implementation templates without forcing ordinary operator authors or consumers to understand a generic invocation system.
- Let a future custom role opt into supported standard concerns through declarative annotations when its shape satisfies the generator contract.
- Produce normal compiled C# that remains compatible with reflection, documentation tools, AOT compilation, and ordinary debugging.

### Costs and limitations

- The generator becomes build-time infrastructure that must be versioned, tested, documented, packaged, and debugged.
- Public APIs become partly implicit in source and must remain easy to inspect through generated documents and API usage specs.
- Changing a generator template can change many public types at once and therefore requires explicit API review.
- Generated type explosion still exists in metadata and documentation even though handwritten implementation duplication is removed.
- Duration measurement, homogeneous pipelines, per-item choice, whole-invocation choice, and transformations do not share one universal semantic shape. The generator must preserve those distinctions instead of hiding them behind one attribute model.
- Invalid or ambiguous role shapes require precise compile-time diagnostics; generator exceptions or obscure downstream compiler errors are unacceptable.
- Source generators are additive and cannot rewrite handwritten code.
- Generators run without access to the output of other ordinary source generators, so a generated member cannot rely on a second generator to give it equality. Typing equality-sensitive collections as `ValueArray<T>` removes that dependency.
- IDE behavior, generated-source navigation, incremental rebuild behavior, and NuGet analyzer delivery add maintenance responsibilities beyond ordinary library code.

### Required design decisions

#### Generation granularity

Decide whether annotations live on:

1. a role base and generate all requested public concern types; or
2. one partial role-specific wrapper declaration per concern.

Role-level annotations provide the desired authoring experience and remove more boilerplate. Partial wrapper declarations provide clearer local API ownership and are easier to implement. A feasibility spike should start with the least magical form necessary to prove the generated runtime code, then test whether role-level generation remains understandable.

#### Role discovery

The generator should derive role information semantically rather than require a repeated closed role type in an attribute. Starting from the annotated configuration base, it can follow `IOperator<TExecutionInstance>` to the role execution-instance type and inspect its ordinary operation method.

The supported role shape must state:

- how the role configuration and execution-instance interfaces are identified;
- whether exactly one ordinary operation method is required;
- how overloads, inherited methods, generic methods, nullable parameters, and by-reference parameters are handled;
- how generic parameters and constraints are reproduced;
- which shapes are rejected with a diagnostic.

Do not rely on generic parameter names or positional conventions when the relationship can be obtained from symbols. Avoid string-based options unless a genuinely ambiguous semantic choice requires one.

#### Concern-specific semantics

Duration measurement can normally wrap the complete role method and is the most generally inferable concern.

Pipeline generation must identify the value threaded from one invocation to the next. For a mutator, the input batch and return type match naturally. Ambiguous roles need an explicit threaded-parameter declaration or must be rejected.

Choose-one generation must distinguish at least:

- one child selected for the complete invocation;
- one child selected independently per item;
- count-distribution semantics such as creator output;
- output cardinality and reassembly rules.

Per-item generation must know the batch parameter, RNG dependency, one-to-one cardinality rule, grouping behavior, and output-order restoration. Do not infer these semantics merely from similar generic collection types when more than one interpretation is possible.

Factories and fluent extensions are part of the public role API. Their names, overloads, inference behavior, and optional concern-specific conveniences must be specified rather than guessed. Role-specific conveniences such as `WithRate` may remain handwritten partial companions when they are not part of the general concern.

#### Configuration equality and snapshots

Generated configurations must preserve immutable collection snapshots and the library's intended structural equality. This is especially important because the execution registry uses configurations to control instance identity and sharing.

This is settled: equality-sensitive collections are `ValueArray<T>`, which compares its elements, so a generated property participates in the compiler-synthesized record equality with no cooperating equality generator. `Generator.Equals` has been removed from the repository.

Do not accept reference-based `ImmutableArray` equality or changed registry behavior as an accidental consequence of generation. A generated collection member typed `ImmutableArray<T>`, `T[]` or `IReadOnlyList<T>` would produce exactly that.

#### Generator and attribute placement

Generation annotations are authoring infrastructure, not fundamental operator contracts. Keep them out of `HeuristicLib.Contracts` unless a later use case demonstrates that contract-only consumers need them.

Prefer a separate `HeuristicLib.Generators` project or a clearly isolated generator component targeting `netstandard2.0` and implementing `IIncrementalGenerator`. The repository's existing Roslyn analyzer infrastructure provides a useful starting point but is not currently packaged as a public generator.

HLib can reference the generator as an analyzer when compiling its own generated role types. If external users should generate implementations for custom roles, provide a deliberate NuGet delivery model, preferably an optional generator package, rather than imposing generator execution on every ordinary runtime consumer.

The generator assembly must not become a runtime dependency.

#### Generated API and source quality

Specify stable rules for:

- namespaces and accessibility;
- generated type, companion type, and extension type names;
- generic parameter order and constraints;
- constructor and property order;
- XML documentation;
- generated hint-file names;
- collision handling when a user declares the same type or member;
- nullable annotations and formatting;
- deterministic member and source ordering.

Generated sources should read like acceptable handwritten HLib code. Do not treat generated code as permission for unclear names, unchecked casts, broad suppressions, or lower implementation quality.

#### Diagnostics and extensibility

Every unsupported or ambiguous shape needs a diagnostic attached to the relevant role, method, parameter, or attribute argument. Diagnostics should explain both the violated requirement and the available correction.

The generator should support a small, explicit set of semantic templates. Do not evolve it into a string-driven general-purpose code templating language. A role or concern that does not fit a supported template should remain handwritten.

#### Validation and packaging

A production generator requires:

- incremental-generator unit tests over Roslyn symbols;
- generated-source snapshot or structural tests;
- compilation tests for valid and invalid role declarations;
- diagnostics tests for ambiguity, collisions, and unsupported shapes;
- role-level runtime behavioral tests;
- API usage specs for generated factories and fluent syntax;
- registry identity, equality, and immutable-snapshot tests;
- generated-versus-handwritten performance benchmarks;
- deterministic-output and incremental-update tests;
- an external-consumer test using the packaged generator if custom-role generation is supported;
- documentation showing both the small handwritten declaration and the generated public API.

### Effort estimate

Approximate focused engineering effort after restoring the direct role-based baseline:

| Scope | Estimate |
| --- | ---: |
| Proof of concept generating one pipeline mutator | 3-5 engineering days |
| Production-quality pipeline generation for known HLib roles | 1-2 engineering weeks |
| Duration and pipeline generation with factories, extensions, diagnostics, equality, and tests | 2-3 engineering weeks |
| General custom-role authoring, per-item choose-one, packaging, and full diagnostics | 4-7 engineering weeks |

The main cost is not emitting C# text. It is reliable semantic discovery, generic substitution, equality, diagnostics, public API stability, IDE behavior, testing, and delivery to external role authors.

### Feasibility spike

Do not implement all concerns together. After the typed invocation rollback is accepted, use one independently reviewed spike:

1. Generate one pipeline mutator side by side with the handwritten `PipelineMutator` without replacing the production type.
2. Emit an ordinary configuration and `IMutatorInstance` that call `Mutate` directly and use no typed invocation, context, adapter, reflection, or delegate-bound child dispatch.
3. Prove public configuration visibility, immutable snapshots, structural equality, registry identity, validation, exceptions, and output order.
4. Generate or expose a static inference helper and the fluent `Then` API.
5. Make the generated source directly inspectable and verify its formatting and nullable annotations.
6. Add diagnostics for at least one invalid or ambiguous annotated role shape.
7. Exercise the same pipeline template with one small synthetic custom role so the spike is not merely a hard-coded mutator emitter.
8. Benchmark the generated and handwritten pipelines under the same workload and require equivalent allocation and dispatch behavior within measurement noise.
9. Run generator tests, focused runtime tests, API usage specs, the Release solution build, packaging validation appropriate to the spike, and formatting verification.
10. Pause for explicit review before replacing any handwritten implementation or adding duration and choose-one generation.

Reject the generator approach if the spike requires extensive string configuration, weakens equality or registry semantics, produces difficult diagnostics, obscures the generated API, or cannot match the handwritten runtime path. If the spike succeeds, duration measurement is the next concern. Per-item choose-one remains last because its semantic and allocation behavior is substantially more complex.

## Retained Outcomes

### Role contracts and authoring bases

- Ordinary consumers depend only on role contracts such as `IMutator` and `IMutatorInstance`.
- Role execution instances expose their named operation such as `Mutate`, `Cross`, `Select`, or `Evaluate`; they do not expose a second generic invocation path.
- Base classes are optional authoring conveniences. Implementing the role contracts directly remains fully supported.
- Do not introduce a parallel typed role hierarchy or require consumers to use authoring-only interfaces.
- Keep role-specific topology bases when they remove real ownership and registry-resolution boilerplate. For example, `WrappingMutator` and `MultiMutator` remain useful without generic invocation.
- Use `SingleCandidateMutator` for the optional authoring base that applies one-candidate mutation logic across a batch. Its public `MutateCandidate` hook supports authoring and deliberate single-candidate calls, while the ordinary `IMutator` contract remains batch-wise. Candidate is the correct term because mutation does not require a completed or evaluated solution.

### Configuration and execution-instance visibility

- A configuration publicly exposes every value and child configuration that describes its configured behavior through read-only properties.
- Retained input collections are immutable snapshots rather than caller-owned mutable collections.
- A wrapping configuration owns one canonical child property, such as `ChildMutator`.
- A multi-child configuration owns one canonical child collection, such as `ChildMutators`.
- Derived configurations do not add forwarding aliases such as `Mutator` or `Mutators` unless a distinct name communicates different domain semantics.
- Execution instances keep resolved children, mutable state, counters, caches, and other execution machinery private by default.
- Execution-instance members are protected only when derived implementations need them. Configuration inspectability does not imply public execution machinery.

### Execution-instance creation

- A base configuration exposes one public `CreateExecutionInstance(ExecutionInstanceRegistry registry)` method with the exact role-specific execution-instance return type.
- Deliberate callers do not need an interface cast to create an execution instance.
- Do not add parallel factories such as `CreateOperatorInstance` and `CreateMutatorInstance` for the same operation.
- Use `instanceRegistry` as the parameter name.
- Resolve ordinary declared children eagerly through the supplied registry so registry identity and sharing remain authoritative.
- A topology base may seal the public registry overload and expose a protected natural overload, also named `CreateExecutionInstance`, that receives the already-resolved child instance or instances.
- Retain the registry only when runtime composition genuinely needs delayed resolution, replacements, or child registries.

### Genericity and public API usability

- Introduce CRTP only when a concrete consumer needs the exact self type. `Algorithm<TSelf, ...>` retains such a consumer; the operator spike did not establish one for role operators.
- Do not propagate `TSelf` merely for symmetry or possible future use.
- Public role wrappers normally store their child as the role interface. A caller that supplied a concrete child already owns that concrete reference, while preserving it through wrapper type parameters causes disproportionate generic growth.
- Prefer type-inference helpers when arguments already determine generic parameters.
- Fluent extensions remain appropriate when the receiver participates in the resulting configuration.
- Keep constructors as the direct construction path, use static `Create` helpers for type inference, and add fluent extensions only when the receiver naturally becomes a child configuration.
- Completed composition policies may be sealed when they are not intended as authoring extension points; topology and authoring bases remain open where inheritance carries a defined responsibility.
- Use `var` when an inferred local type would otherwise repeat a large generic declaration without improving understanding.
- Avoid casts, runtime proxies, reflection, `dynamic`, and opaque adapters as substitutes for a coherent static model.
- Deterministic generation is not selected for the current authoring model. Reconsider it only through a new explicit decision if stable cross-role boilerplate later becomes materially costly; ordinary source and agent-assisted scaffolding are the accepted approach today.

### Operator semantics

- Duration measurement remains exception-safe and records elapsed time in a `finally` block when the child throws.
- Counting instrumentation records completed calls and returned candidates. A failed child invocation is not counted.
- Keep timing logic direct and readable. A disposable measurement scope or another clever helper is not justified without a demonstrated clarity or performance benefit.
- A pipeline passes the complete output batch from each stage to the next stage in order.
- Prefer a straightforward `foreach` pipeline loop unless a benchmark demonstrates a reason to use a less readable implementation.
- Per-item choose-one and whole-invocation choice are different operations. Do not merge them merely because both select a child.
- `ChooseOneMutator` selects independently per input item, preserves original item order, and requires every selected child to return one result per assigned item. These non-obvious semantics warrant XML documentation and behavioral tests.
- Do not force duration measurement, pipelines, choice, transformation, or another concern through one generic abstraction when their actual role semantics differ.

### Variance and value semantics

- `TCandidate` remains invariant because it identifies the exact representation shared by the problem, search space, algorithm, and operators.
- Preserve useful search-space and problem variance where it supports a general operator being used with a compatible specialization.
- Do not add an interface solely to obtain variance for a context or carrier that has no independent use case.
- Records should communicate value semantics. A carrier containing a stateful RNG or other identity-based dependency should not become a record merely for concise syntax.

### Performance and deterministic randomness

- High performance remains a first-class design constraint. Avoid unnecessary allocation, boxing, delegate creation, context construction, and interface indirection on operator hot paths.
- Measure small abstractions before broad migration. Expensive operators do not excuse regressions that are material for lightweight operators.
- Allocation measurements and throughput measurements are both required; success on one does not compensate automatically for regression on the other.
- The measured spike result is historical decision evidence. Do not retain permanent benchmark infrastructure solely for a rejected design.
- Random sequences are assigned by logical item or key and remain independent of worker count, partitions, scheduling, concurrency limits, and CPU-core count.
- Lightweight batching and per-item RNG-fork optimization remain a separate backlog concern. Do not require every RNG to become counter-based solely for that optimization.

### Review process

- Significant architectural changes begin with a narrow compile-time and performance spike.
- A prototype is allowed to fail its decision gate; passing tests is not sufficient evidence that its abstraction is worthwhile.
- Each rollback or implementation task includes its code, tests, API usage specs, documentation, and proportionate validation.
- Pause after every task and wait for explicit review and acceptance before proceeding.

## Retained Tests and Documentation

Retain or restore role-oriented tests that verify:

- configuration collections use snapshot semantics;
- child configurations and behavior-defining settings are publicly inspectable;
- resolved child execution instances obey registry identity and sharing;
- duration measurement records elapsed time on success and exception;
- pipelines preserve stage order, forward the same RNG, search-space, and problem references, and stop when a stage throws;
- per-item choose-one preserves selection semantics, result cardinality, and output order;
- contract-only operator implementations remain valid without inheriting authoring bases;
- candidate invariance and supported search-space/problem variance remain intact.

Remove tests whose only purpose is proving generic `Invoke`, context forwarding, default interface bridges, or typed topology relationships.

## Rollback Review Task

The rollback is one independently reviewable task:

1. Remove the rejected typed contracts, context types, and generic meta-operator bases.
2. Restore direct role-method dispatch and role-specific mutator topology implementations.
3. Preserve the retained configuration visibility, factory naming, registry ownership, variance, semantic tests, measured decision evidence, and backlog decisions described above.
4. Update operator documentation so it does not present typed invocation as the intended architecture.
5. Run focused mutator and operator tests, the complete core test project, API usage specs, the Release solution build, and formatting verification.
6. Pause for explicit review before any further operator redesign or role migration.

The rollback must be selective. Existing user changes and unrelated accepted cleanup in the dirty worktree must not be reverted wholesale.
