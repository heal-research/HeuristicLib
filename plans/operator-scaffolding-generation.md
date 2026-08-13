# Operator Scaffolding Generation

## Decision summary

Source generation remains an undecided alternative for reducing the mechanical work required to author a new operator role. It is deliberately not scheduled yet.

The current preferred hypothesis is narrow:

- generation may be valuable for role scaffolding such as role and execution-instance arity ladders and topology bases;
- generation is unlikely to be valuable for semantic composition operators such as `ChooseOne*`, `Pipeline*`, or `Transformed*`;
- handwritten roles must remain a fully supported, first-class path;
- generated code must have the same runtime shape and performance characteristics as accepted handwritten code.

Do not freeze a generator API or start a generator project from the mutator slice alone. First finish and accept the mutator reference, then migrate and measure a second role. Two matching roles are enough to evaluate the common three-parameter role shape, but not enough to claim support for every role shape.

This document supersedes the section "Alternative: Source-Generated Role Implementations" in [typed-operator-rework.md](typed-operator-rework.md). The retained decisions in that document continue to apply where they do not conflict with this plan.

## Current gate

The generator is blocked on evidence, not implementation capacity.

Only the mutator role currently has the intended authoring shape. A generator derived from that one example would risk encoding mutator-specific mechanics as universal role conventions. The next actionable work is therefore the Selector migration described in [Migration and acceptance sequence](#migration-and-acceptance-sequence).

Generator work is Phase 4. It is reached only after the Selector migration is accepted in Phase 2 and the Phase 3 decision explicitly chooses a spike, and abandoning generation is a permitted outcome of that decision. Phases 2 and 3 contain no generator code.

If asked to implement the generator before that gate is satisfied, offer the next migration task instead. Do not silently turn the current illustrative syntax into a public generator contract.

## Goal and non-goals

### Goal

Let an internal or external role author request mechanical scaffolding instead of hand-writing repeated generic substitution, factory forwarding, and topology plumbing before implementing role-specific operator behavior.

Generation is a convenience, never a requirement. A role written entirely by hand must compile and behave identically without an attribute, a generator package, or a generated member that the runtime framework requires.

### Non-goals

- Do not introduce a generic runtime invocation abstraction.
- Do not replace named role methods such as `Mutate`, `Select`, or `Cross`.
- Do not generate semantic behavior that cannot be selected or derived unambiguously.
- Do not evolve into string-driven source templating.
- Do not eliminate the compiled public type matrix. The goal is to eliminate justified handwritten repetition.
- Do not make direct contract implementations or handwritten role bases second-class.
- Do not optimize primarily for the eight built-in roles. External custom-role authoring is the principal justification for generator work.

## Illustrative authoring model

The exact attribute API is intentionally not decided. The following example illustrates the division of responsibility rather than proposing final names or parameters.

The role author writes the fundamental contracts and the ordinary role operation:

```csharp
public interface IExampleOperator<TCandidate, in TSearchSpace, in TProblem>
    : IOperator<IExampleOperatorInstance<TCandidate, TSearchSpace, TProblem>>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>;

public interface IExampleOperatorInstance<TCandidate, in TSearchSpace, in TProblem>
    : IOperatorInstance
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    IReadOnlyList<TCandidate> ApplyExample(
        IReadOnlyList<TCandidate> candidates,
        IRandomNumberGenerator random,
        TSearchSpace searchSpace,
        TProblem problem);
}
```

An illustrative opt-in declaration requests selected authoring families:

```csharp
[GenerateOperatorScaffolding(
    Families = OperatorScaffoldingFamilies.RoleLadder
        | OperatorScaffoldingFamilies.WrappingTopology)]
public partial record ExampleOperator<TCandidate, TSearchSpace, TProblem>;
```

Conceptually, the generator supplies ordinary nominal C# for:

- the full and reduced role configuration bases;
- the matching execution-instance bases and explicit arity-reduction forwarders;
- the requested full-arity wrapping topology base;
- public `CreateExecutionInstance(...)` factories with the exact role-instance return type;
- canonical `ChildExampleOperator` configuration properties and protected resolved-child fields or properties on execution instances.

The author still writes:

- the fundamental role contracts;
- the role operation and its domain semantics;
- any unsupported authoring family;
- equality-sensitive configuration state unless the selected generator family explicitly owns its equality implementation;
- semantic composition operators and role-specific conveniences.

The final API must look like acceptable handwritten HeuristicLib code. The feasibility spike should begin with the least magical opt-in form, even if a later accepted design can infer more from the annotated role.

## Generation tiers

The tiers are non-overlapping. A later tier may depend on an earlier tier, but it must not redefine it.

| Tier | Content | Current target | Main gate |
| ---: | --- | :--: | --- |
| 1 | Role configuration and execution-instance arity ladders | Yes | Two accepted roles with the common shape |
| 2A | Full-arity `Wrapping*` topology base | Yes | Tier 1 proven |
| 2B | Full-arity `Multi*` topology base | Conditional | Explicit structural-equality strategy proven |
| 3 | Optional `Stateless*`, `Stateful*`, and role-applicable single-item authoring bases | Later | Each family justified independently |
| 4 | `Observable*`, `DurationMeasuring*`, and `Counting*` concerns | Not initially | Naming and observation semantics declared |
| 5 | `ChooseOne*`, `Pipeline*`, and `Transformed*` compositions | Handwritten by default | Material role semantics remain |

### Tier 1: role ladders

Tier 1 contains only two families:

1. Role configuration bases narrow generic arguments without adding alternative factories.
2. Role execution-instance bases add narrower abstract role operations and explicit interface forwarders.

These families are related but not one template. Their accepted behavior is described in [developer-guidelines.md](../docs/developer-guidelines.md), under "Role arity reduction."

### Tier 2: topology bases

Topology bases stay at full role arity because their generic arguments type the child slots. Reduced topology arities would reject problem-specific children rather than make composition easier.

`Wrapping*` and `Multi*` are separate generation targets:

- Tier 2A can generate a wrapping topology after the role ladder is proven.
- Tier 2B may generate a multi topology only after the generator owns or otherwise deliberately preserves ordered structural equality for its child collection.

A generated `Multi*` child collection must be a `ValueArray<T>` property, which carries ordered structural equality in the member type and needs no cooperating equality generator. Do not emit a child collection typed `ImmutableArray<T>`, `T[]` or `IReadOnlyList<T>`; those compare by reference and would silently break configuration equality.

### Tier 3: optional authoring conveniences

`Stateless*`, `Stateful*`, and single-item bases are independent authoring conveniences, not part of the fundamental role ladder.

- `Stateless*` combines a configuration and execution instance in one type.
- `Stateful*` owns fresh run-scoped state and a nested execution instance.
- A single-item base is generated only when the role has an accepted independent-item interpretation. It is not universal; Selector, Terminator, and other roles may have no meaningful equivalent.

Each family needs its own evidence and opt-in decision. Similarity in the mutator role is not sufficient.

### Tiers 4 and 5: concerns and compositions

These tiers contain increasing amounts of role judgement:

- counting requires irregular public names such as `CountMutatedCandidates` and `CountReplacementCandidates`;
- observer conveniences need to know which operation argument is the primary result;
- choose-one has per-item, whole-call, and count-distribution variants;
- pipelines require an endomorphic operation;
- transformed operators coordinate two different roles;
- rate conveniences require role-specific identity operators.

They stay handwritten unless later evidence identifies a small, closed semantic choice that is worth generating.

## Semantic parameters

Where a semantic choice cannot be inferred, a future generator may accept an enum-valued parameter selecting an observed behavior:

```csharp
[GenerateChooseOneOperator(Dispatch = ChoiceDispatch.PerItem)]
```

This is only illustrative. Such a parameter is acceptable when all of these hold:

- it is a closed enum rather than a string or source fragment;
- every value corresponds to behavior already implemented and accepted in the repository;
- invalid role and option combinations produce a diagnostic on the declaration;
- the complete combination set remains practical to test exhaustively;
- the concern does not require three or more orthogonal behavior parameters.

Strings may describe genuinely irregular public names, but must never carry behavior. Role-specific types and conveniences should normally remain in handwritten partial companions.

## Migration and acceptance sequence

Every task below is a review gate. Pause after its code, tests, and documentation are complete. Do not begin the next task until the user accepts the current one.

The sequence has four phases. Only Phase 4 contains generator work, and it is reached only if the Phase 3 decision explicitly chooses it.

| Phase | Content | Generator code |
| ---: | --- | :--: |
| 1 | Mutator reference slice | No |
| 2 | Selector migration | No |
| 3 | Evidence and decision | No |
| 4 | Generator feasibility spike, if chosen | Yes |

### Phase 1: the mutator reference — accepted

The mutator slice was accepted on this basis:

- one public `CreateExecutionInstance(...)` factory per base configuration;
- role arity reduction follows the documented forwarding model;
- `WrappingMutator` and `MultiMutator` expose canonical public child configuration properties;
- execution instances keep resolved children private or protected;
- stateless, stateful, and single-candidate authoring paths have API usage specs;
- concern configurations have structural-equality coverage;
- mutation behavior, allocation-sensitive batching, and public construction paths have focused tests;
- the single-candidate base seals its batch role operation and exposes batching as an `ExecutionConcurrency` configuration property;
- the retained terminology and authoring rules are documented.

Later mutator changes are ordinary follow-up work rather than grounds for reopening this phase.

### Phase 2: migrate Selector as the common-shape probe

Selector is a useful second role because it uses the common candidate/search-space/problem shape, has full authoring and concern families, and currently exposes the legacy protected `InnerSelector` topology. Selection is inherently whole-population, so Selector has no single-item base and produces no second data point for that family.

Split the migration into separate accepted tasks:

1. Migrate the Selector role and execution-instance ladders together with the `WrappingSelector` and `MultiSelector` child slot: one public `CreateExecutionInstance(...)` per base configuration, canonical public child properties, protected resolved children on execution instances, and ordered structural equality for the child collection. Pause.
2. Migrate the Selector stateless and stateful bases and the applicable concerns. Pause.

The child slot is deliberately part of the first task rather than a separate one. Both touch the same topology types, and renaming the child property in one task while changing its visibility in another would leave an intermediate shape matching neither the old nor the target model. The cost is coarser evidence granularity in Phase 3, which is accepted.

Do not migrate unrelated roles as part of these tasks merely to make the Selector diff compile unless they are direct consumers that must be updated.

`ISelectorInstance.Select` currently takes its objective and count arguments before the random number generator, which does not match the documented operator parameter order. Raise that as an explicit decision during Phase 2; do not normalize it silently.

### Phase 3: evidence and decision

Record the evidence first: elapsed engineering time, changed lines by category, mechanical versus judgement-heavy changes, defects uncovered, and aspects that resisted substitution.

Mutator plus Selector can justify only a decision about the common three-parameter role shape. Choose explicitly among:

- abandon generation because the measured repetition is too small or too semantic;
- allow an isolated Tier 1 feasibility spike for the common shape;
- collect more migration evidence before deciding.

Abandoning generation is a legitimate outcome, not a failure. This decision does not authorize a production generator or broad role support. Pause for acceptance of the decision itself.

### Phase 4: generator feasibility spike

Reached only if Phase 3 explicitly chooses the spike. This is the first phase containing any generator code.

1. Create an isolated incremental-generator test project or fixture.
2. Generate Tier 1 for synthetic roles in test-only namespaces.
3. Compare the generated structure with separately compiled handwritten fixtures for both accepted common-shape roles.
4. Do not emit types with the same namespace and metadata name as the production handwritten types. Source generators are additive and cannot replace those declarations side by side.
5. Evaluate diagnostics, IDE navigation, incremental rebuilds, API clarity, and measured runtime equivalence.
6. Pause and decide whether to abandon, revise, or proceed to Tier 2A.

Production handwritten types are not replaced during the spike. Remaining role migrations need not be completed before an isolated spike, but no generated replacement ships until the affected role migrations and API reviews are complete.

Before claiming that scaffolding supports every operator role, migrate and accept one structurally different role, preferably Interceptor or Terminator, which exercises an additional search-state type parameter and its variance rules. If the accepted generator scope stays explicitly limited to the common three-parameter shape, that migration may be deferred, but the limitation must be part of the documented public scope.

## Migration decisions already settled

These are operator-rework decisions, not generator decisions.

### Public factory and child topology

- Base configurations expose one public `CreateExecutionInstance(...)` method returning the exact role execution-instance type.
- Topology bases may seal that method and expose a protected natural overload with already-resolved children.
- Configuration objects expose canonical `ChildOperator` or role-specific child properties publicly.
- A child slot has exactly one public name. A wrapping or multi base is used only when the children are identified by being children and nothing more, and derived concerns add no forwarding alias for a child the base already exposes. An operator whose child plays a specific part derives from the role base and declares that child itself, as `EliteSelector` and `GenderSpecificSelector` do.
- Execution instances keep resolved child machinery private or protected.

See [developer-guidelines.md](../docs/developer-guidelines.md), under "Configuration and execution ownership."

### Counting concerns

The `CountingMutator` shape is the reference. A counting concern wraps its child directly and owns a handwritten counting execution instance. It does not derive from an observable concern merely to reuse a callback.

Counting occurs after the child succeeds so failed calls are not counted. Structurally identical counting configurations must compare equal. Add equivalent failure and equality tests as each role is migrated.

### Terminology

Use `SingleCandidate*`, not `SingleSolution*`, when the operation concerns unevaluated candidates. The single-candidate form remains a regular batch-wise role operator; only its authoring method is scalar.

Use the canonical terminology in [glossary.md](../docs/glossary.md).

### Construction companions

- Static `Create` factories stay in the non-generic static `<Type>` companion.
- Fluent extensions stay in an explicit `<Type>Extensions` companion.
- Instrumentation concerns may use role-first names such as `MutatorDurationExtensions` when that is the accepted convention.
- Generated XML documentation is reserved for non-obvious behavior and contracts; do not generate comments that merely repeat member names or types.

## Analyzer role

Analyzers and generators solve different problems:

| Mechanism | Responsibility |
| --- | --- |
| Generator | Writes opted-in scaffolding |
| Analyzer | Checks generated or handwritten conventions |
| In-repository tests | Check built-in role behavior and API usage |

The repository already contains Roslyn analyzer infrastructure and operator-authoring rules. It must not yet be described as an externally delivered custom-role analyzer contract: the current analyzer projects are non-packable, and consumer delivery through the runtime package has not been verified.

After the second role is accepted, evaluate additional diagnostics for:

- canonical child property naming;
- redundant forwarding aliases;
- one public exact-role `CreateExecutionInstance(...)` factory;
- required role variance and documented structural exceptions.

External analyzer delivery needs an explicit packaging test. An analyzer can enforce conventions for handwritten roles, but it cannot remove their scaffolding work.

## Technical design constraints

### Semantic role discovery

Discover the execution-instance contract by following `IOperator<TExecutionInstance>` and inspect the ordinary role operation semantically. Do not infer relationships from generic parameter names or fixed positions when symbols provide them.

The supported declaration contract must state how it handles inherited and overloaded operations, generic methods, nullable annotations, by-reference parameters, constraints, accessibility, and ambiguous operations. Unsupported shapes require precise diagnostics.

### Equality and registry identity

The execution registry resolves configurations by object reference. Structural record equality does not control execution-instance sharing.

Structural equality still matters as a public configuration contract. This is now carried by the member type: ordered configuration collections use `ValueArray<T>`, which compares its elements, so a record holding one gets correct equality from the compiler-synthesized `Equals` and `GetHashCode`. `Generator.Equals` has been removed from the repository.

This resolves the former Tier 2B blocker. The earlier constraint was that ordinary source generators cannot consume one another's output in the same compilation, so `Generator.Equals` could not see a generated child property. A generator emitting a `ValueArray<T>` property now needs no cooperating equality generator at all.

Therefore:

- Tier 1 and Tier 2A must not introduce unreviewed equality-sensitive state;
- Tier 2B may emit a `Multi*` child collection as a `ValueArray<T>` property, and must test the resulting equality;
- reference-based child-array equality must never appear as an accidental fallback, which means a generated collection member must never be an `ImmutableArray<T>`, a `T[]` or an `IReadOnlyList<T>`.

### Performance

Generation must not introduce runtime allocations, adapters, reflection, extra context objects, or additional dispatch layers compared with the accepted handwritten implementation.

Before replacing handwritten code:

- capture reviewed baselines for representative empty, small, and larger batches;
- compare allocation counts and direct dispatch costs;
- define regression thresholds from benchmark stability rather than requiring literal equality or the vague phrase "within measurement noise";
- investigate any regression before accepting generated output.

Generated configuration code must preserve immutable snapshots without adding repeated execution-path normalization or validation.

### Packaging and inspection

Prefer an isolated `netstandard2.0` project implementing `IIncrementalGenerator`. The generator assembly must not become a runtime dependency.

The external delivery model remains open. CI currently runs solution-wide `dotnet pack` and publishes produced packages from `main`, but that does not by itself prove that a new generator package, transitive analyzer asset, or optional dependency reaches consumers correctly. Verify the final package contents and an external consuming project before claiming support.

Generated files must be inspectable through IDE navigation and test artifacts. Whether repository builds enable `EmitCompilerGeneratedFiles` remains an implementation-time decision.

## Validation required before shipping

- Incremental-generator tests over Roslyn symbols.
- Generated-source snapshot or structural tests.
- Compilation tests for valid, invalid, ambiguous, and colliding declarations.
- Diagnostic location and message tests.
- API usage specs proving generic inference and ordinary role assignability.
- Runtime behavioral tests against the handwritten reference.
- Immutable-snapshot and structural-equality tests.
- Registry reference-identity tests.
- Reviewed allocation and dispatch benchmarks.
- Deterministic-output and incremental-rebuild tests.
- IDE generated-source navigation verification.
- A package-content test and an external consumer project using the packed generator.
- A handwritten external role that works without generator attributes or a generator package.

The current hand-written analyzer test harness may be insufficient for this scope. Evaluate `Microsoft.CodeAnalysis.Testing` or an equivalent standard harness as part of the spike rather than assuming the existing raw-compilation utilities will scale.

## Open decisions

Do not settle these from the mutator slice alone:

- final attribute granularity and names;
- whether families are requested together or by separate partial declarations;
- whether generation is limited to the common three-parameter role shape;
- the Tier 2B equality strategy;
- optional versus bundled external generator delivery;
- generated namespace, accessibility, member ordering, hint names, collision policy, and nullable behavior;
- whether generated files are written to build artifacts by default;
- whether packaged analyzers accompany the generator;
- whether internal conformance tests remain valuable once generation and analyzers cover part of the model.

Record each accepted answer here before generated public API ships.

## Evidence appendix

### Current migration finding

The concern matrix represents a half-finished migration rather than eight equally valid shapes. Mutator is the current reference. The other seven roles still use some combination of protected role-named factories, `Inner*` child properties, and derived forwarding aliases that conflict with the accepted developer guidelines.

This migration cost is shared by every option that aims for a coherent API. It must not be charged solely to generator work.

### Why leaf composition is a weak first target

`ChooseOne` already has three observed semantics:

| Variant | Current roles | Behavior |
| --- | --- | --- |
| Per item | Mutator, Crossover | Select independently per input item and restore order |
| Whole call | Selector, Replacer | Select one child for the complete invocation |
| Count distribution | Creator | Distribute a requested count without an input batch |

`Pipeline` additionally requires an endomorphic operation, while `Transformed*` coordinates different roles. These concerns contain materially more judgement than the scaffolding they might save.

### Provisional effort hypothesis

Previous estimates suggested that a tier-limited generator could cost materially less than a generator covering every concern. Those figures were speculative and depended on an assumed amount of repeated scaffolding. Replace them with measured migration and spike data rather than treating engineer-day ranges as a decision criterion.

The decision should use:

- measured mechanical lines and review effort per migrated role;
- expected number of internal and external custom roles;
- generator and analyzer maintenance cost;
- public API clarity and debugging cost;
- demonstrated runtime equivalence.

### Copy-drift lesson

The evaluation found copied rate-complement behavior that differed between choose-one roles. The stable rule now lives in `WeightedBatchDispatch.GetRateWeights`, and callers consume the complete derived pair.

Two lessons carry forward:

- prefer a shared helper that returns a complete derived value over duplicated fragments;
- test behavior, not only generated structure or configuration properties, because copy drift can preserve the apparent shape while changing probabilistic semantics.
