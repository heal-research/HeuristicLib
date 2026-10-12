# Execution-resolution redesign: initial design plan

Status: Redesign 2 is the selected design candidate. This document is an initial proposal for review, not approval of its contracts or implementation. Package 5 remains on hold. No production changes, executable prototypes, staging, or commits are authorized by this plan.

## Direction and evaluation

The primary objective is to reduce what operator, algorithm, and extension authors must understand and write. Prefer ordinary typed constructors and explicit children. Copyable source templates and small authoring bases may handle mechanical wiring and encode correct ownership.

HeuristicLib is alpha. Backward compatibility and preservation of existing implementation are not selection criteria. Evaluate the resulting authoring burden, conceptual burden, cohesion, support machinery, debuggability, and measured costs. Existing code supplies behavioral evidence. Reusing one helper implementation is a cohesion goal, not a compatibility goal.

Redesign 2 combines shared construction machinery with an explicit typed inner execution. Redesign 1, which retains implicit predecessor resolution, remains the comparison alternative; it is not the selected direction. Historical factory options A/C are unrelated to these labels.

Do not introduce public subsystem names yet. `ExecutionBranch` and `PreparedExecution` were withdrawn: one suggests control flow, the other introduces an unexplained lifecycle concept. Establish responsibilities first, then check proposed names against the glossary, the repository, and common programming/.NET meanings. Existing terms such as configuration, execution node, state, child execution, wrapper, and resolution scope remain useful.

## Working design

There are two construction shapes:

- Ordinary construction receives a resolution scope and explicitly obtains its children.
- Generated construction receives a resolution scope and the typed execution it encloses. Additional children resolve through that scope.

Both allocate persistent data once per selected execution or generated occurrence. They may construct several nodes containing that same data and different child references. Operation calls use the typed nodes directly.

The working factory shape reuses existing names; these signatures are proposals, not compiler-verified contracts:

```csharp
ExecutionFactory<TExecution>(ResolutionScope scope)
WrapperExecutionFactory<TExecution>(ResolutionScope scope, TExecution inner)
```

Keep the ordered-child construction shape of `CompositeExecutionFactory<TExecution>` where useful. Reusing a delegate name does not require preserving its current signature. Do not add parallel decorator, advice, aspect, and composition factory families.

Persistent initialization receives no live resolution scope. Authoring bases can expose state initialization and typed construction hooks; explicitly authored factories remain available for algorithms and unusual structure. Do not require stateless leaves to allocate state or a separate node.

### Authoring examples to establish first

The following sketches describe author responsibilities, not settled method signatures:

| Author | Expected work |
| --- | --- |
| Stateful leaf | Initialize private state once; implement its typed operation. |
| Composite or algorithm | Initialize persistent data once; construct a node with explicitly resolved children. |
| Generated wrapper | Initialize occurrence data once; construct a node from supplied `inner` and any additional children. |
| Internal composition | Use the same construction mechanism; supply the observed source explicitly and resolve private plumbing. |
| Consumer-defined role | Use the same generic machinery and its own typed operation, without a built-in role registry. |

```csharp
// Leaf authoring hooks; boilerplate manages the factory.
CreateState() => new CounterState();
Execute(input, state) => /* typed operation */;

// Explicit composite factory: state survives reconstructed nodes.
var state = new AlgorithmState();
return scope => new AlgorithmExecution(
    state, scope.For<TCandidate, TSearchSpace, TProblem>().Resolve(Mutator));

// Generated wrapper factory: there is no source-resolution override.
var state = new ObservationState();
return (scope, inner) => new ObservingMutator(inner, state);
```

The fixed-role example will use a consumer-defined cooling schedule with `Temperature(int step)`. Its observer receives an `ICoolingExecution` directly. A second consumer-defined role will close its execution type over run types, to expose the generic dispatch problem rather than hide it behind a fixed-role example.

Source templates are ordinary checked-in source, following developer guidelines § 4.16. Convenience bases must not dictate the conceptual core or create independently configurable lifetime policies.

### One helper implementation, two ways to supply children

The proposed route for `AppliedAtRate` is to reuse its configuration and existing typed child-construction hook:

```text
ordinary configured use:
    existing dispatcher constructor(resolved source, resolved fallback)

internal use:
    same dispatcher constructor(supplied observed source, private fallback)
```

Derive the configuration once from the unchanged algorithm parameters. Prepare its dispatcher once per owning execution. Supply children afresh for each constructed node. The algorithm must not know child-array positions or duplicate rate dispatch.

Topology boilerplate can translate a designated configured child into the explicit execution input before invoking the typed constructor. This is explicit child wiring, not a resolver override or a complete configuration-graph traversal. The same declared source used more than once must receive the supplied execution in every designated position.

This route is straightforward for the current rate helper's direct multi-child topology. Nested and custom factory implementations need an explicit source-input path; automatic conversion of arbitrary factory code is not assumed. Design work must show how existing generated recipes become explicit factories or reuse their typed construction hooks.

Preserve the distinct fallback configuration even when the source is `NoChangeMutator.Instance`. Independent work must remain a visible configuration parameter; internal composition adds only plumbing.

### Run-type dispatch: first unresolved contract

Use the role factory boundary to close candidate/search-space/problem/search-state types before invoking a typed constructor. The source selector's type and the generated execution's construction contract must be independent: selecting a concrete configuration must not require a generated object to impersonate that concrete configuration type.

Do not adopt the previously sketched `IMutatorTransformation` family as a default. First work out whether existing role/topology contracts and a generic typed adapter can supply both construction shapes coherently. A new contract is acceptable when it reduces the total authoring burden; any per-role interfaces, adapters, factory carriers, and overloads must be included in the comparison.

The first review deliverable must contain exact C# declarations and call sites for fixed and run-typed roles, generic and type-specific observations, and a broad selector. Claims of type safety or inference remain unproven until a separately authorized compilation proof succeeds.

## Responsibilities and ownership

These are responsibility descriptions, not a commitment to three new classes:

| Responsibility | Retained information |
| --- | --- |
| Select shared execution state | Configuration-reference selections, ancestor lookup, and fresh/retained child scopes. |
| Own a selected execution | Persistent data and factory, chosen dependency identities, retained children, owned generated occurrences, and initialization faults. |
| Construct nodes for a declaration path | Immutable ordered registrations, completed raw/wrapped nodes, construction faults, and contextual retained child scopes. |

State selections, persistent execution data, and contextual node caches have different lifetimes. Use those lifetimes to evaluate consolidation; do not turn this table into one class per row. Wrapper occurrences and internal compositions should use the common initialization/construction machinery rather than have parallel lifecycle owners.

Registration occurrences are keyed by selected source execution and registration identity. Internal compositions belong to their owning execution; a reused composition reference cannot silently switch its selected source. Reserve an occurrence before executing a potentially throwing recipe. Fresh child scopes use the same representation as retained children; retention is an owner/key relationship.

### Required construction traces

- P owns G -> M; C adds an observer of M. Construct another G/M node path using the existing G/M state and G's chosen dependency. C's earlier direct selection of another M remains separate; P's nodes remain unchanged.
- A retained child keeps its state and declaration snapshot, but applies the requesting path's depth and registrations. Cycle's repeated algorithm references and Pipeline's fresh stages keep their existing activation and RNG semantics.
- Deferred ordinary children retain their owner's selection rules and original declaration path. Deferred generated helpers receive and retain `inner` explicitly, including when passed through another helper.
- A paused iterator retains its original nodes, children, RNG forks, and invocation progress. Creating other nodes does not update the suspended iterator.

Generated construction never changes what `Resolve(originalSource)` means. Registration-added children use ordinary selection and recursion rules. For internal plumbing, implicit resolution of the designated source should fail clearly to prevent a hidden private copy; the supplied execution can be passed at any depth. This is a proposed misuse rule requiring review, not preservation of the current direct-root restriction.

Internal plumbing has isolated state and is excluded from every registration. Registration-added work preserves the current ancestry rule and is excluded only from its originating registration; other registrations can observe it. Explicitly configured wrappers remain ordinary selectable configurations.

## Failures, observations, and lifetimes

- Initialization/recipe faults persist with the selected execution or occurrence. Construction and selector-predicate faults belong to the requesting declaration path. Retained declaration faults stay with the retained child. Publish only complete chains; already completed dependencies remain valid.
- Preserve `TryResolve` exception behavior, cross-scope recursion detection, and the distinction between resolution failures and invocation failures. No concurrent-operation or disposal guarantee is introduced.
- Preserve configuration-before-module, deeper-before-shallower, and declaration-sequence ordering. Match selectors during construction, including late sources. Modules declare contributions without resolving executions.
- Keep raw-node control projection tied to the same selected source as the observed operations, and perform it only after complete construction. Access to source controls from inside an explicit transformation is unresolved; a completed `inner` alone is insufficient. Control projection through internal compositions remains outside the initial scope.
- Retain weak source associations and avoid strong occurrence-to-registration paths. Live deferred nodes retain what they need; discarded fresh executions and discarded observing contexts must both be collectible. A single strong root registry is unsuitable.

Target elimination: `WrappedNodes` and source interception; separate `PreparedWrapper` and `InternalComposition` lifecycle owners; a separate retained-child carrier where the common scope can own its declarations. These are hypotheses. Count all replacement machinery before claiming a net reduction; fewer internal classes cannot justify a worse authoring experience.

## Design work and review gates

| Step | Reviewable result |
| --- | --- |
| 1. Authoring contract | Exact sketches for the five author stories above, including run-typed custom roles, private children, controls, deferred source inputs, and `AppliedAtRate` reuse. Resolve the dispatch contract before selecting internal classes. |
| 2. Ownership proof | Construction traces above; identity/key tables; strong/weak ownership diagram; failure boundaries; correspondence with R1-R16. |
| 3. Cohesion and naming | Complete retained/removed/introduced inventory, including role conveniences and adapters. Name components from demonstrated responsibilities and check terminology collisions. |
| 4. Design review | One consolidated proposal, remaining decisions, authoring templates, and an implementation package plan. Stop for explicit implementation authorization. |

This document completes the initial planning step. Steps 1-3 are further design work, not authorization for source changes or a prototype. Compiler experiments, executable API specs, and implementation require an explicit continuation into that work.

After implementation is authorized, separate pure moves/renames from behavioral changes and migrate every repository consumer without compatibility shims. Follow `AGENTS.md` and `test/README.md`: focused/core tests first, API usage specs and affected Experimental consumers, representative scenarios near completion, and final solution validation for this cross-project change. Build/test work must be sequential.

Evidence required before completion: R1-R16 adapted to explicit inputs; seeded GA/NSGA2/ALPS rate parity including the zero-rate singleton regression; actual Cycle/Pipeline and paused-iterator cases; both collectibility directions; scoped format/analyzer checks; docs validation. Compare setup, reconstruction, fresh/retained activation, allocation, and already-constructed operation costs against `c6957cbc`. Historical C4 measurements are workload guidance, not current-redesign results.

## Sources

- Baseline: `c6957cbc` on `container-and-aspect-framing`. Redesign 2 and the authoring-first priorities above were selected in the design discussion; the temporary handover was removed at the user's request.
- [Developer guidelines](../docs/contributing/developer-guidelines.md), especially §§ 1.1, 3.1-3.3, 4, 8.11; [design goals](../docs/contributing/design-goals.md); [glossary](../docs/guide/glossary.md).
- [Current resolution](../docs/contributing/architecture/execution-resolution.md), [factory design](execution-factory-design.md), and [R1-R16](execution-bindings-and-shared-state.md#acceptance-matrix-for-the-implementation).
- [Current topology assembly](../src/HeuristicLib/Operators/Mutators/Composition/MultiMutator.cs), [rate helper](../src/HeuristicLib/Operators/Mutators/ChooseOneMutator.cs), and [run-typed observation](../src/HeuristicLib/Analysis/Tracing/Observations/MutatorObservation.cs).
