# Execution-resolution redesign

Status: consolidated design plan, 2026-10-13. This is the authoritative plan for the new execution-resolution redesign; older factory/shared-state plans describe the production baseline and its history. Redesign 2 is the selected candidate, with the accepted simplifications below. The disposable proof projects have been removed after preserving their useful scenarios here. Validate the rework in the existing library and normal test projects. The complete replacement remains unapproved for production; package 5 stays on hold. The initial plan was committed as `f0769164` and the temporary handover removed.

## Objective

Make the resolution system itself substantially simpler, as well as reducing the burden on operator, algorithm and extension authors. A small public API over complicated internal ownership machinery does not meet this objective. Neither does moving the machinery into fewer classes.

HeuristicLib is alpha. Backward compatibility, preservation of existing implementation and existing type boundaries are not selection criteria. Current behavior and tests supply evidence; distinguish important user capabilities from incidental policies before retaining either.

Keep ordinary typed operations, explicit dependencies, private state initialized once and immutable constructed child references. Checked-in authoring templates may handle mechanical wiring. Do not introduce role registries, universal invocation, ambient operation contexts or compatibility shims.

Alternative ownership and indexing arrangements are outside the current direction. The limited investigation does not establish that they are generally unsuitable. The decisions below select the approach for this rework.

## Target responsibilities

| Part                         | Responsibility                                                                                                                     |
| ---------------------------- | ---------------------------------------------------------------------------------------------------------------------------------- |
| Resolution scope             | Look up selected factories locally and through parents; construct nodes with the current wrapper declarations.                     |
| Selected factory             | Initialize private state once and remember the selected factories for its dependencies.                                            |
| Wrapper registration         | Select configured sources and supply a constructor around explicit typed `inner`, preserving its own initialized state per source. |
| Caller creating child scopes | Decide when to create, reuse and release children, and store them if necessary.                                                    |

The first three are resolver responsibilities. The fourth belongs to algorithms or other callers, with no generic child-management subsystem. Keeping the selected factory for a child operator is distinct from storing a child scope: the former preserves dependency state, while the latter is the caller's reuse policy.

Factories retain factory-table lookup paths without observation declarations or constructed nodes. Wrappers and explicit-input helpers construct directly; source resolution is not intercepted to recover `inner`. Internal helpers retain initialized state without requiring cached execution-object identity.

The target has three internal storage classes: scope, selected factory entry and registration. Scope data is the factory lookup path, ordered registrations, recursion guard and, provisionally, constructed nodes/faults. A selected entry stores the execution role, preparation callback, initialized constructor/state, original lookup path, selected child factories and helper factories. A registration stores its selector/recipe/order, declaring lookup path and weak source-to-wrapper-factory association. No retained-child record or child-view dictionary remains in the target.

The candidate uses arrays of dictionary references for factory lookup paths, tuples for ordered registrations and raw/wrapped node pairs, dictionaries for tables and `Lazy<T>` for initialization attempts. These references, callbacks and allocations count as machinery even without additional named classes. Include the selected execution `Type` and erased preparation callback in cost comparisons.

## Accepted decisions

### Child scopes belong to their callers

Accepted on 2026-10-13: remove `Retained`. A caller that creates child scopes manages their storage and lifetime, including any dictionary needed to reuse them. For example, Cycle keeps the child scopes it needs to reuse and creates new ones when fresh execution is intended. Resolution scopes do not remember their children.

State reuse follows the child's parent lookup path. If S already selected Cycle's factory, child T can reuse that factory and its initialized state while constructing a new Cycle execution with T's observations. Who holds T does not change that lookup. Each scope already stores its selected factories; child retention adds no new state-sharing capability or need for another factory store.

For this rework, remove resolver-owned child retention, including the production `GetOrCreateChildScope` mechanism. Child storage and lifetime belong to the caller rather than retained-child slots or per-context child views. Caller integrations must establish their required reuse, fresh activation and observation behavior; the historical experimental checks did not validate this responsibility change.

### One factory construction callback

Accepted and tested in the removed experiment: replace `ordinary`, `supplied` and `acceptsSource` constructor arguments with one construction callback plus declared child configuration references. Common helpers resolve a child when needed or forward an already supplied `inner`, then call the same constructor. Source membership follows from the declared references, without a separate predicate. `FromChild` and `FromChildren` provide this wiring; production implementation is pending.

### Explicit inner and persistent initialization

Accepted: a wrapper constructor receives the execution it encloses as explicit typed `inner`. Resolving the source configuration is not temporarily redefined to recover that execution. Internal helpers preserve initialized state without requiring cached helper-node identity; repeated helper construction can produce different execution objects sharing the same private data. Ordinary-node identity and failure caching remain separate, provisional policies.

## Authoring contract

The candidate keeps one public `CreateExecutionFactory(...)` method per role, returning covariant `IExecutionFactory<TExecution>`. Fixed configuration contracts remain covariant too. The factory interface exposes ordinary `Construct(ResolutionScope)`; the common implementation handles supplied input. `ExecutionFactory` is a static helper with one internal generic implementation. It replaces the production factory delegate rather than adding a permanent parallel family. The familiar configuration/execution marker interfaces remain.

| Helper                                                | Constructor wiring                                                                                                            |
| ----------------------------------------------------- | ----------------------------------------------------------------------------------------------------------------------------- |
| `Create(construct)`                                   | Ordinary leaf or explicitly wired algorithm construction.                                                                     |
| `FromChild(child, prepare, construct)`                | Resolve one child, or use the supplied `inner`, then call the same constructor.                                               |
| `FromChildren(children, prepare, construct)`          | Snapshot ordered children; replace every reference-identical source position with `inner`, resolving other children normally. |
| `FromChild(child, prepare, selectControl, construct)` | The same child routes, with an optional typed control projected from the raw source.                                          |

`prepare` is a scope-free adapter to the child's factory method. For run-typed roles it closes the existing run type arguments. Common wiring resolves through the supplied scope; there is no role registry, reflection-based invocation or second generic operation path. Private state and derived configuration helpers are initialized before the construction callback. Stateless configurations need no artificial state object or separate node.

Wrapping and multi bases should centralize the helper calls and expose one protected typed constructor hook each: `inner` for a simple wrapper, ordered children for a composite. Advanced callbacks may also receive the scope for additional dependencies. Final hook signatures remain open; prefer standard `Func` callbacks to additional named delegate families. Leaf operation overrides retain their ordinary role methods.

For example, an external wrapper initializes one counter, then returns `FromChild` with a callback that constructs its execution from `inner`, an explicitly resolved extra child and that counter. Ordinary configured use resolves the child; generated use receives its already-built execution. A composite uses the same mechanism for all declared positions. A GA initializes its rate helper once and constructs its execution with resolved children; no scope is available during preparation.

The common factory implementation stores one callback and declared child references. Internally, existing source/inner/raw references can travel as an optional value tuple, without a new carrier class or public raw-node API. Covariance supplies erased factory storage without adapter objects. The stored typed preparation callback guarantees the output contract, so duplicate factory-output metadata is unnecessary.

One authoring gap remains: the removed separate-consumer experiment established a handwritten ordinary factory, while its generated wrapper used `FromChild`. Its input metadata and supplied-input construction were internal, preventing another assembly from implementing that generated path directly on the interface. Establish a simple public construction path before claiming the specific helpers are optional. Do not solve this with another role-specific factory family.

### Type compatibility and registration

Selector and generated configuration types are independent: a selector for a concrete configuration may return a wrapper of another concrete type. The selected source retains the closed execution role and preparation adapter from its first selection. Standard role adapters accept the role interface, not just the selected concrete configuration; a concrete-only custom adapter cannot prepare an unrelated wrapper and must fail clearly.

A generated factory must satisfy that canonical adapter, declare the source reference among its children and accept every execution admitted by the canonical source role. Validate before its constructor resolves additional children, then check the actual supplied node at construction. The raw source may already have been constructed. These are checks at the erased registration boundary, not compile-time guarantees for arbitrary selectors.

Compatible output views share the same selected state without retargeting child factories or observer callbacks. Output covariance does not prove input compatibility. Reject a narrower-only observer on a broad canonical source, and reject incompatible algorithm search-state types before wrong child construction. No operation-time casts or reflective fallback repair incompatible contracts.

### Source controls

Both ordinary and generated construction project controls from the selected raw source, while operation calls use the completed `inner` chain. Authors receive only the typed optional control. Another wrapper implementing that capability must not become the control source; an absent source capability stays absent.

A generated constructor may project and retain an operation-free control before the full chain is published, but may not invoke operations or controls during construction. Caller-facing projection runs only after full resolution succeeds. A throwing projection fails construction without returning a partial node/control pair. No public control-discovery traversal through internal compositions is added.

### Internal helpers and rate composition

An owning factory retains each helper's initialized factory. Construction forwards an already selected source and explicit `inner`; it neither resolves the source again to recover the supplied execution nor changes ordinary lookup. The helper cannot switch its selected source. Nested/deferred helpers forward the same input and canonical preparation adapter. The removed experiment called this `Compose`; its final forwarding name/signature remains open.

Internal plumbing and its private children are excluded from every registration and use isolated factory tables. Implicit lookup of the designated source fails at any depth. Registration-generated wrappers follow a different exclusion rule: only their own registration skips their extra children, so other registrations can observe independent configured work.

`AppliedAtRate` keeps one configuration helper, its weights and typed dispatcher construction. Ordinary use resolves its children; internal use supplies the observed source and resolves a distinct fallback privately. Algorithm authors supply no child indexes or duplicate dispatch code. Validate real weighted dispatch, seeded behavior and the zero-rate singleton case; wiring checks alone do not establish them.

Helper initialization faults are persistent; immediate construction faults belong to the caller's construction attempt. Deferred nodes may use node-local `Lazy<T>` to activate once, retaining their construction scope and explicit input. Helpers have no independent input-node cache. Actual consumer failure behavior remains to be validated.

## Resolution and lifetime rules

The current candidate performs these steps:

1. Look for a factory locally and through parents. Inside a constructor, reuse its remembered child selection; on first use select from the owning factory's original lookup path. Selection stays lazy.
2. Initialize the selected factory once and check structural recursion before reading node caches.
3. Construct the typed raw execution with the requesting observations and the selected dependencies.
4. Apply matching registrations in configuration/module, depth and sequence order. Each registration initializes its wrapper factory once per selected source and constructs directly around the current `inner`.
5. Publish the complete raw/wrapped pair. If ordinary-node caching is retained, cache the complete result or its construction fault in the requesting scope; no partial chain is returned.

Child-local selections are never published upward. A child with no added declarations can reuse completed ancestor nodes; changed declarations construct new nodes with the same selected factories. Generated wrapper roots construct directly rather than becoming independently resolved source nodes. Node caching and its fault boundary remain provisional.

Three representative checks make these rules concrete:

- **Shared state with new observations:** P owns G -> M; C adds a counter around M. C constructs G with its observed M while preserving M's state and P's existing child references. A stateful outer wrapper keeps one initialization; three calls from P and two from C produce counts 5/2.
- **Earlier child-local selection:** C selects its own M before P selects G -> M. C's later G must still use G's selected M factory, while C's direct M remains separate. Controls follow the same selected operational dependency.
- **Deferred selection:** G first asks for M later from a constructed callback. Its original factory lookup path selects M; the callback retains its original requesting observations. A later direct selection elsewhere must not redirect it.

For an extra wrapper child, the tested candidate chose the source's lookup path when it included the declaring table, otherwise the declaring path, with a private local table for added children. Compare that rule with a simpler uniform rule; neither existing tests nor reduced class count settle the policy.

Only live scopes/nodes should retain active observation paths. Persistent framework lookup tables contain factories, not scopes, observation declarations or node caches. A registration weakly associates each source factory with its initialized wrapper factory. The value may refer to the source key, but framework references must not lead back to the registration owning that weak table. Initialization-only recipe captures disappear after the cached attempt completes.

Validate both collection directions: a live source releases discarded observing contexts; a live registration releases discarded fresh source/wrapper state. Include failures and intentionally live deferred nodes using the repository's finalizer-aware GC checks. Caller-owned child lifetimes need their own validation after removing resolver-owned retention; collection does not imply deterministic resolver disposal.

## Implementation implications

- Move child management to callers and remove production retained-child records, slot/view dictionaries, keyed lookup and retained-declaration reconstruction.
- Implement the one-callback factory wiring. Wrapping and multi bases use `FromChild` and `FromChildren`; ordinary leaf behavior does not acquire another invocation path. No per-role normal/supplied factory families are needed.
- Migrate all three production users of keyed child retention: Cycle, `OperatorBudgetAlgorithm` and `OperatorDurationBudgetAlgorithm`. Pipeline already creates children directly. Budget accumulators remain initialized once; their observation children become caller-managed.
- Keep child storage out of reusable configurations. Decide its required lifetime in each caller. A dictionary on one Cycle execution handles reuse by that execution, but does not by itself establish reuse after Cycle is reconstructed. Test that case explicitly before selecting storage placement.
- Change the tests alongside the responsibility change. Assertions about shared state, observations and algorithm results remain useful; assertions requiring `Retained`, stable keys or retained-view identity do not define the replacement API.
- Recheck the necessity of remaining caches and metadata against actual consumers. The ordinary-node cache remains a provisional choice, not an accepted execution-identity requirement. Do not silently change its construction/failure behavior or add caching for explicit-input helpers.

## Behavior boundaries

Retain child-local observations through reused composites, shared persistent state without mutating existing nodes, typed controls addressing the operational source, and independent invocation/iterator progress unless an explicit alternative is reviewed. Previously rejected fixed-composite and ancestor-only-observation approaches are not silently reinstated.

Use [R1-R16](execution-bindings-and-shared-state.md#acceptance-matrix-for-the-implementation) as a scenario inventory, not an immutable API specification. R9/R10 must be expressed through caller-managed budget/Cycle/Pipeline behavior. R1/R13 node identity and fault timing, and R8 private-child ancestry, need explicit justification. Any proposed behavior change must have a before/after example and remain visibly provisional.

Ordinary users and operator authors should not need to understand factory caches or collection machinery. Maintainers must also be able to understand resolution without several independently managed ownership systems.

## Revised sequence

1. **Resolve the remaining design questions against actual callers.** Use the existing code and test inventory below to settle caller-owned storage lifetimes, external handwritten wrapper construction and ordinary-node cache/fault policy. Show concrete before/after behavior for any policy change. No parallel proof implementation is required.
2. **Implement the core and caller responsibility change after design approval.** Replace the resolver/factory machinery, remove production keyed retention and migrate callers needed to keep the repository buildable. Validate Cycle reuse/fresh mode, repeated references, separate owners/invocations, Pipeline freshness and both budgets in the normal tests. Reconstruct Cycle under a child with a new observer: selected state must survive, inner calls must reach the new observer, and the original execution must keep its observations. Include inner-algorithm state created only in Cycle's own child scope; inheriting Cycle's factory alone does not establish that case.
3. **Complete authoring and consumer integration.** Exercise mutators, composites, wrappers and the GA rate helper through the same wiring. Validate external custom roles, optional common helpers, controls, deferred input and failures. Migrate every repository caller without shims, and update analyzers/code fixes, API usage specs, samples, glossary and architecture docs. Review coherent packages rather than maintaining a second resolver under plans.
4. **Review simplicity, costs and final behavior.** Compare actual caller code and the complete resolver storage, including callbacks, tables and tuples. Measure construction, reuse, changed observations, fresh stages and cheap operation calls. Justify or remove remaining node caches and metadata. Complete collection, seeded streaming and affected integration validation. Package 5 remains separate.

Follow `AGENTS.md` and `test/README.md`: focused core checks during development, then core/API usage and affected Experimental consumers; representative scenarios and whole-solution validation near completion. Validate collection, seeded behavior and costs. Build/test work is sequential.

## Validation scenarios

These preserve the useful checks from the removed experiments. Reuse existing coverage first and add missing cases in the normal core/API usage test projects during implementation. Validate algorithm results and streaming in the appropriate algorithm/scenario tests. Translate provisional node-identity, fault-timing and extra-child ancestry cases to the final reviewed contract.

| Area                              | Cases to retain or establish                                                                                                                                                                                                                                                                                                                                     |
| --------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Identity and shared state         | Reference identity distinguishes value-equal configurations; initialize selected state once. Parent/child/sibling ordering does not hoist local state. Outer/inner observations report 5/2 for three parent calls and two child calls, and old executions keep their observations.                                                                               |
| Pinned and deferred children      | An earlier child-local M does not replace G's selected M or its control. Late child selection uses G's original lookup path. Paused/deferred executions retain their original input, state and observation context.                                                                                                                                              |
| External authoring                | A consumer-defined role with candidate/search-space/problem type arguments composes with a fixed-role extra child and an invariant-state algorithm. Concrete and broad selectors may return another configuration type through the canonical role adapter. Cover handwritten ordinary and generated factories.                                                   |
| Compatible views and rejection    | Compatible factory/configuration/execution views share initialization without adapter objects or retargeted child roles. Wrong source references, run types, invariant search-state types and narrower-only generated inputs fail before additional child initialization. An actual node's extra capability does not widen its canonical source contract.        |
| Composite input and rate plumbing | Every repeated reference-identical source position receives the supplied inner. A value-equal distinct fallback resolves separately and stays private. Real `AppliedAtRate`, including zero rate, preserves configured-source observations and seeded weighted behavior.                                                                                         |
| Raw controls                      | Control projection addresses the same raw state used by observed operations. A control on an inner wrapper does not replace an absent raw-source capability. Constructor projection performs no operations; failure returns no partial node/control pair.                                                                                                        |
| Inline and nested helpers         | Repeated construction can create distinct nodes sharing one initialized helper state. Ordinary/supplied routes use the same constructor. Deferred activation initializes only when used; nested forwarding keeps the current explicit inner. Reject implicit source lookup at any depth and switching a helper's selected source.                                |
| Registrations and extra children  | Extra children skip their originating registration while other registrations can observe them. Validate private selection, source/declaring ancestry, module/configuration ordering, depth/sequence, unmatched selectors and overlapping registrations.                                                                                                          |
| Caller-managed scopes             | Cycle fresh/reuse modes, repeated algorithm references, separate owners/invocations, Pipeline stages and ancestor reuse. Reconstructed Cycle/budgets preserve intended private child state and apply new observations; budget accumulators initialize once. Preserve result sequences, exact RNG forks, cancellation and disposal.                               |
| Failures and collection           | Initialization/construction/projection failures publish no partial graph. Recursion cannot escape detection through new child scopes. A live source releases discarded observing contexts; a live registration releases discarded fresh sources/wrappers, including initialization failures. A deliberately live deferred execution retains what it still needs. |

## Historical experiment evidence

Both disposable proof projects were removed at the user's request on 2026-10-13. Their recorded results are historical, with no runnable proof sources retained:

- The smaller integrated resolver recorded **80 checks (43 original + 37 typed) and 6 separate-consumer checks** under .NET SDK 10.0.401, Release, warnings as errors. Its version still used resolver-owned retention; it did not validate the caller-management change.
- The earlier authoring fixture recorded **27 wiring assertions and 9 invalid-contract checks**, plus a separate consumer. It supplied canned nodes/factories and used the superseded ordinary/supplied callbacks and source predicate; it did not establish selection, once-only ownership, ordering or collection.
- Still unproved for the replacement: caller-managed Cycle/Pipeline/budgets, production bound/agnostic bridges, actual selector/module integration, all control consumers, helper failure parity, weighted rates, real streaming/RNG/cancellation/disposal and performance. The toy algorithm established typed wiring. Lookup-path copies, closures, frames and `Lazy<T>` allocations remain unmeasured costs.

This plan is the only active redesign document. The real implementation and normal tests must establish current evidence. Older production plans remain historical sources. Removing the experiments changes no production code or normal test project.

## Sources

- Production baseline: `c6957cbc` on `container-and-aspect-framing`.
- [Developer guidelines](../docs/contributing/developer-guidelines.md), [design goals](../docs/contributing/design-goals.md), [requirements](../docs/contributing/requirements.md), and [glossary](../docs/guide/glossary.md).
- [Current resolution](../docs/contributing/architecture/execution-resolution.md), [factory design](execution-factory-design.md), and [current acceptance evidence](execution-bindings-and-shared-state.md).
