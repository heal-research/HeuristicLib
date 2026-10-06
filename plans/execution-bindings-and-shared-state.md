# Design execution factories and shared state

Status: factory implementation and consumer migration complete. The selected direction is option C using typed execution factories (candidate 1 in the [design comparison](execution-bindings-design-investigation.md)), superseding option A in the [container and aspect rework](container-and-aspect-framing.md#case-5-a-shared-composite-retains-its-dependency-bindings). The migration is reviewed and consolidated as `85c1b24f`. C4 performance costs are accepted for the intended long-running optimization workloads. M3 control integration, narrow evidence gaps and racing failure cleanup are reviewed and committed as `f7a89747`. Cache statistics retention is the current repair package; shared epoch-subscription/cache ownership and final foundation review remain before selector registration. End the turn for explicit review after each remaining package. Leave index and commit operations to the user unless explicitly authorized.

C1 has a [concrete design](execution-factory-design.md), including authoring examples, resolver ownership, retained child scopes, decoration construction, capabilities and failure behavior. C2 and C3 supplied the local design proofs. The branch retains persistent-state preparation as `23d65748`, the factory/resolver implementation as `aa72a5a7` and the complete consumer migration as `85c1b24f`. Normal restore and the Release solution build succeed; all 2,885 tests across core, Experimental, API usage and scenarios pass, with no failures or reported skips. The optional activated-TSP scenario returns before execution because its external prerequisites are absent. These results close migration compilation/runtime validation. The reviewed C4 results close performance acceptance; resource-lifecycle questions and final foundation acceptance remain open. The review records below preserve the original package history; their pre-squash commit IDs are historical checkpoint identifiers rather than current branch boundaries.

Naming decision, 2026-09-27: adopt the [graph naming family](execution-factory-design.md#agreed-naming-family): `IConfigurationNode`, `IExecutionNode` and role-specific `...Execution` types, retaining ordinary algorithm/operator configuration names. The [commit boundaries](#agreed-commit-boundaries) separate a behavior-preserving rename from the factory/resolution rework. The naming migration is the first implementation review package; it retains the existing object-returning creation methods and behavior.

### Completed migration history cleanup

The user requested one coherent migration commit after completing the small review packages. M2n was reviewed and committed as `bea3e60d`, then all seventeen checkpoints after `aa72a5a7` were replaced by `85c1b24f` (`Migrate consumers to typed execution factories`). Earlier preparation, naming and design history remains separate.

A temporary backup tag preserved the unsquashed tip during the rewrite. Both commits have tree `924b9ee15cf96348510efc307584f940705d31b3`, and `git diff --exit-code` confirmed equality before and after updating the branch. The index SHA256 remained `DD65A84F3DA227FD0D265A89802D254AA48021494ED24F5D874D8BDD298AA86A`. The user subsequently requested removal of `backup/execution-factory-migration-before-squash-2026-10-03`; that tag is removed. No further migration squash is pending.

## Problem and current decision

Before the factory migration, an outer scope P constructed algorithm G, which retained its resolved mutator M. A child scope C adding an observer of M and reusing G missed calls through G's existing M binding. Recreating G through the old object-creation factory could reset execution state. This was the problem addressed by option C.

The 2026-09-26 decision chose option A: retain a reused composite's existing bindings and reject reuse that would bypass a required child observation. On 2026-09-27 the user selected option C instead: preserve logical execution state while constructing scope-specific typed bindings, including a reused composite's child bindings. Do not implement option A's composite-reuse guard as an intermediate prerequisite. Silent observer omission remains unacceptable; any unsupported case during a future migration must have an explicit reviewed policy.

The design must remove that composite limitation while preserving parent-first state reuse, independent siblings without ancestor state, child-first ownership and no hoisting. Keep ordinary configuration users and stateless/stateful leaf authors insulated from factory/state machinery. Show the necessary cooperation for non-terminal operator and algorithm authors concretely, and keep it as small as possible.

## Selected direction and open design

A configuration prepares a typed factory once per logical execution. The factory binds typed execution nodes using persistent execution data and the requesting binding's child/observer context. Resolution returns a fully constructed execution node. A child obtains another G binding that reaches outer and inner observations while retaining the same logical G and M state. Parent callers keep their original bindings. The common/role naming family, delegate shapes, protected authoring hooks and resolver structures are implemented; C4 measured their cost and the user accepted the tradeoff for long-running optimization. General selector registration and advice semantics remain separate design work.

The [design comparison](execution-bindings-design-investigation.md) supplies candidate sketches and source evidence. Its explicit-rebind and factory-replay alternatives remain comparison material, not parallel APIs to implement. The lambda return form was compiled successfully in a small standalone compiler check; that confirms C# target typing, not the proposed execution model.

The original sequence of concrete design, local proofs and bounded migration is complete, and the pre-change comparison is measured and reviewed. The next work reconciles integrated acceptance evidence and resource-lifecycle limitations before final foundation review.

Execution objects can be rebuildable glue around shared state. Two qualifications must be retained:

- Binding objects still contain meaningful immutable configuration context, typed child references and perhaps a scope for deferred construction. They are not empty pass-through objects, and they need not be rebuilt on every operation. Define when they can be cached and when they can be discarded.
- Moving mutable fields into a shared object does not by itself preserve all execution semantics. Retained delegates, child caches, RNG progression, resources and suspended iterators can capture a particular binding. Inventory these rather than assuming all state is freely interchangeable.

“Binding” and “state holder” are local descriptive terms here, not approved public types or replacements for glossary terms. State may remain a data object while typed operation logic lives on a configuration or execution object. This investigation does not decide that state objects implement operator roles.

## Evidence from the current implementation

| Existing pattern | Relevance and limit |
| --- | --- |
| [StatefulMutator](../src/HeuristicLib/Operators/Mutators/BaseClasses/StatefulMutator.cs) | Authors already implement `CreateInitialState()` and a typed operation receiving `TState`. A private instance holds configuration and state. Today every factory call creates fresh state; there is no separate operation that rebinds an existing state. |
| [StatelessMutator](../src/HeuristicLib/Operators/Mutators/BaseClasses/StatelessMutator.cs) | The configuration can return itself as its execution node. Do not force state allocation or an extra adapter onto this case merely to uniformize the model. |
| [WrappingMutator](../src/HeuristicLib/Operators/Mutators/Composition/WrappingMutator.cs) and authored composites | Execution nodes own typed child references. Changing bindings while preserving their own state requires an authoring contract beyond the existing leaf-stateful bases. |
| [CycleAlgorithm](../src/HeuristicLib/Algorithms/Composition/CycleAlgorithm.cs) and [PipelineAlgorithm](../src/HeuristicLib/Algorithms/Composition/PipelineAlgorithm.cs) | Instances retain their creating scope; the cycle may also cache child algorithm executions. A proposed shared state object must not retain an old scope-specific child binding and defeat rebinding. |
| [IterativeAlgorithm](../src/HeuristicLib/Algorithms/BaseClasses/IterativeAlgorithm.cs) | The async iterator retains progress values and uses instance-bound child operations. Sharing an ordinary state holder does not automatically share, resume or replace that active enumeration correctly. |
| [Mutator observations](../src/HeuristicLib/Analysis/Tracing/Observations/MutatorObservation.cs) | Thin wrappers retain a typed child and callback. Rebinding a wrapper can preserve a callback's accumulator, but arbitrary stateful advice still needs an ownership contract. |

The [developer guidelines](../docs/contributing/developer-guidelines.md) currently put mutable data and resolved children on authored execution nodes (§ 4.1, § 4.12, § 4.17). Framework-managed operator state must not contain configurations, instances, registries or child-bound delegates (§ 4.14). These are current rules, not evidence that a future model has already been adopted. Any necessary change must be proposed explicitly and reviewed with replacement authoring examples.

## Authoring requirements

Ordinary users should continue composing configurations, attaching modules and starting runs without managing bindings or state stores. The investigation should aim to retain the existing ease of authoring stateless and ordinary stateful operators; a new stateful leaf should not need a hand-written binding class just to increment a counter.

For compositions and advanced authors, compare current and proposed source side by side. Count added types, generic parameters, overrides, factories and repeated child wiring. Include a consumer-defined role and a stateful composite with two differently typed child roles. Convenience bases are insufficient evidence if the public contract makes custom roles second-class or forces every author to hand-write an equivalent framework.

Do not silently adopt a blanket ban on mutable instance fields. Explain which state must survive rebinding, which data belongs to a binding or one invocation, and what the framework can enforce. Distinguish compiler-enforced guarantees, possible Roslyn guardrails and conventions; nullable references do not need runtime null guards.

Preserve explicit typed calls and handwritten wrappers. No type-keyed auto-wiring, built-in-role switches, dynamic proxies, runtime code generation, universal invocation, ambient current-scope state or runtime service locator. Keep concept namespaces/folders, the attachment methods and run-specific lifecycle ownership; do not introduce a shared RunHost. Resolution and binding costs should remain outside steady-state calls wherever possible.

## Questions that must be answered

1. **State ownership and identity.** Define which configuration/run/owning-scope identity selects persistent state. Rebinding must initialize logical state once, preserve child-first ownership and never promote child state into an ancestor. Resolve the collision where C already owns M_child before P constructs G with M_parent and C then requests G: preserving G's child identity and C's direct-resolution identity may make both M states reachable from C. Define failed initialization/binding behavior and which selections remain pinned. Different wrapper/view references must not be mistaken for different logical state; equal configurations must not merge independent state.
2. **Graph construction and factories.** Specify preparation versus repeatable binding, stable derived configurations, shared subgraphs and explicit configured wrappers. Make the typed G-to-M call path and changes to algorithm/operator authoring bases concrete. Do not introduce a general child-dependent initialization helper solely for hypothetical uses: distinguish configuration-derived data, initialization on first execution and an accepted requirement to read a resolved child's live state during binding. Rebinding G while retaining its original child references does not solve the problem.
3. **Deferred children and caches.** Decide which scope a new G binding retains and where Cycle's reuse cache lives. Logical child-state reuse and caching a decorated child binding are different responsibilities. Both cycle modes must preserve their existing state reset/reuse and RNG behavior.
4. **Iterator and resource lifetime.** Define binding lifetime while a run is paused, how an active async iterator retains its dependencies, and what a new binding can safely change. Rebinding must not restart an in-flight algorithm, duplicate enumerators, dispose shared state or introduce a new cleanup contract incidentally. Shared state does not make concurrent calls safe; preserve the execution path's concurrency constraints.
5. **Observer and advice state.** Separate source execution state from per-advice state and a caller-owned analyzer accumulator. Outer observations must not be reset or doubled by additional bindings. Keep original-source attribution, wrapper exclusion, clock/trace ordering and duration-budget isolation.
6. **Specialized capabilities and identity-dependent code.** Test custom instance capabilities, such as mutable operator parameters, through rebound wrappers. Explain implications for `ReferenceEquals` and caches retaining execution-node objects; sharing state is not equivalent to preserving the same returned object.
7. **Costs.** Measure binding/state allocations, retained memory, first-resolution cost, repeated resolution and operation throughput against a fixed pre-change implementation. Record its behavior and limitations; option A's revised guard/binding model was not fully implemented and must not be built just to provide a baseline. Compare equivalent cases directly and report new capability cases separately. Neither “extra indirection must be slow” nor “binding only happens once” is evidence. Include very cheap operators and repeated short cycles.

## Acceptance examples for the design and proof

| Case | Required evidence for a candidate design |
| --- | --- |
| Siblings without ancestor state | Same source M yields independent M states; a shared outer analyzer may aggregate both. No hoisting through a state or binding cache. |
| Ancestor M, new inner G | Three outer and two inner successful calls advance one M state five times; outer observer receives five reports and inner observer two. |
| Ancestor G already holds M | A new child binding of G reaches the child observer while preserving logical G and M state. Parent/sibling callers remain unaffected. This is the additional capability beyond option A. |
| Child resolves before parent | The child's state remains independent when the parent later resolves the same configuration; subsequent child rebinding must not switch to the parent's newer state. |
| Cycle and pipeline deferred children | Outer and appropriate inner observations reach children resolved later. Cycle reset/reuse modes, algorithm-reference reuse and deterministic RNG forks remain intact. |
| Stateful configured wrapper, composite and custom role | State survives rebinding without hiding the chosen child's typed capabilities or requiring library registration of the custom role. |
| Paused enumeration and failures | Resuming continues the original logical execution with defined bindings; failed construction or advice does not duplicate initialization or silently change ownership. |
| No matching observer or repeated resolution | No phantom reports, duplicate wrappers or repeated state initialization; a cached binding does not force cross-scope state sharing. |

## Review packages

The revised workflow authorized on 2026-09-27 follows naming, C2 and C3 with direct implementation in the existing library/test files. The user reviews the actual Git diff and stages accepted changes; later edits to reviewed files must be identified. Temporary broken builds during the contract cutover are acceptable review checkpoints, but the eventual functional commit must compile and pass validation. C4 compares the integrated implementation with the previous version in a temporary worktree before final acceptance. This replaces the earlier requirement to finish C4 before library edits. No duplicate candidate implementation or compatibility path is needed to keep intermediate packages compiling.

The completed local proofs are reference evidence, not a dependency of committed code or a replacement for integrated regression tests. Do not expand them further. Remove obsolete proof references as implementation supersedes them. Do not begin the next package until the user explicitly continues after review. If a requirement proves infeasible, present the conflict and revisit the decision explicitly rather than silently falling back to option A.

| Package | Deliverable | Validation and review stop |
| --- | --- | --- |
| C0. Plan and decision alignment | Select typed-factory option C; supersede option A in the earlier plan and backlog; update package 4c dependencies and mark the old review prompt historical. | Documentation consistency and link/diff checks. No runtime changes. |
| C1. Concrete factory design | Specify the typed factory and authoring-base contracts, ownership/cache rules, failure publication, observation ordering and capability access using the agreed graph/role names. Show before/after stateless/stateful leaves, a stateful non-terminal operator, a two-role consumer composite, HillClimber/GeneticAlgorithm, and Cycle/Pipeline. Separate persistent, binding-local and invocation-local fields. Resolve or explicitly bound every question above. | Review complete typed call paths and all acceptance cases, author ceremony and required guideline changes. The naming family is settled; remaining signatures and semantics require design review. |
| C2. Small typed proof | Prove the reviewed contract with a stateful leaf, a consumer-defined two-child composite and a representative algorithm. Include richer capability preservation and the child-first dependency collision. Avoid a full role-family migration. | Focused sharing/observation/failure tests and executable API usage specs, Release build and appropriate core tests. Review before expansion; a typed syntax check alone is insufficient. |
| C3. Deferred/lifecycle proof | Prove both Cycle modes, Pipeline, retained iterators, budget/advice ownership and late child binding. Define migration handling for unsupported existing factories. | Focused lifecycle/budget tests; core/API suites, experimental consumers when affected and a selected workflow scenario. Preserve RNG behavior and paused invocation identity. |
| C4. Cost evidence and final acceptance | Compare the integrated library with the recorded pre-change baseline in a temporary worktree. Record measured costs, completed migration scope and any necessary design adjustments. | Stop for review before accepting the completed functional change. Final validation includes affected suites, formatting/analyzers, docs and one full solution run. |

Follow [AGENTS.md](../AGENTS.md) and [test/README.md](../test/README.md): focused tests first, full core after meaningful core changes, API usage specs for authoring changes, experimental tests for affected consumers and scenarios when validating broad workflows. Use repository Release build, formatting and analyzer commands. A substantial public execution-model change requires a full solution test run near completion, not repeated scenario runs during sketches.

## Detailed execution plan

### C1 deliverable and review

Review [the concrete design](execution-factory-design.md) as one coherent contract. Its D1-D7 table identifies the consequential proposals: pinned dependency continuity, preparation hooks, retained child scopes, original-source decoration recipes, explicit controls, sticky construction failures and iterator ownership. Review the complete examples before approving executable work. The factory direction itself is already selected.

In particular, changing `Wrap` to receive the original source and introducing a construction-time control projection are supporting API changes, not consequences that should be hidden inside a resolver implementation. There is no proposal to make handwritten wrappers transparently implement arbitrary interfaces. Binding-time persistent initialization that requires a child's live value is outside the initial contract; no current production need has been established for its additional helper.

### C2: typed core proof

Build the smallest isolated proof of the reviewed contract before changing every public role. Use a test-only namespace/project or internal candidate engine exercised by tests, keeping it out of the public API. Reuse the eventual resolver implementation where practical; do not mistake a disconnected hand-simulated state dictionary for a proof of the actual binding design. Do not create a permanent second resolution workflow.

1. Implement typed preparation, reference-identity logical selection, explicit construction frames, dependency pinning and view-owned binding caches. Include failed preparation and re-entrant construction states from the beginning.
2. Implement stable generated-decoration occurrences and predecessor binding with the reviewed `Wrap` semantics. Cover ordering, original-source attribution and exclusion of generated wrappers before adding selector integration.
3. Port a minimal stateful leaf, `PredefinedCandidatesCreator`-shaped composite, the two-child consumer role and one ordinary algorithm. Compile both bound and agnostic factory bridges, wrapping/multi hooks and iterative hooks. Compile all C1 authoring snippets as executable usage examples in the candidate context.
4. Prove the proposed control projection with Gaussian/EvolutionStrategy behavior and a consumer-defined control, including an explicit configured wrapper that deliberately does not forward the control.
5. Review the focused results and authoring cost before expanding to delayed construction. A passing return-lambda example does not fulfill this package.

Use focused core unit tests for invariants and API usage specs for syntax/authoring. Do not modify every existing test simply to make the prototype compile. If the prototype requires changes to the real core, run the full core suite and Release build after those changes; otherwise its own build and focused tests are the first feedback loop.

### C3: deferred, advice and lifecycle proof

1. Add fresh and retained logical child domains, immutable declaration snapshots and contextual views. Keep reference keys and declaration occurrence identities stable; calculate ordering from the current observation path.
2. Exercise both Cycle modes, repeated algorithm references, independent Cycle nodes, two invocations and Pipeline. Confirm existing ancestor reuse, fresh local state and exact RNG fork positions.
3. Migrate representative count and operator-duration budgets in the proof. Preserve each budget's accumulator across bindings and isolate different budget owners. Keep algorithm-duration measurement invocation-local.
4. Retain a running/paused iterator while obtaining another observed binding. Resume the old iterator and verify it retains its original dependencies and callbacks without restarting or duplicating initialization.
5. Cover faults at each construction phase, source/declaration cycles across child contexts, operation/advice failure, cancellation and disposal. Verify raw/completed cache publication separately.
6. Test lifetime retention with many short child contexts over one long-lived source, and many fresh Cycle children under one long-lived root observer. Test collectibility in isolated helpers with weak references and bounded GC verification; a throughput benchmark alone cannot detect these leaks.
7. Audit the remaining production constructors, especially Experimental DynamicRacing, before declaring the contract complete. Record any required additional API as a design revision, not an undocumented workaround or fallback to option A.

Reuse existing [Cycle tests](../test/HeuristicLib.Tests/Algorithms/MetaAlgorithms/CycleAlgorithmTests.cs), [Pipeline tests](../test/HeuristicLib.Tests/Algorithms/MetaAlgorithms/PipelineAlgorithmTests.cs), [budget tests](../test/HeuristicLib.Tests/Algorithms/MetaAlgorithms/OperatorBudgetAlgorithmTests.cs), [observation tests](../test/HeuristicLib.Tests/Analysis/ObservationModuleTests.cs) and [run lifecycle tests](../test/HeuristicLib.Tests/Execution/AlgorithmRunTests.cs) as behavioral evidence. Add one representative composed scenario after the focused cases pass. Run Experimental tests when real Experimental consumers are migrated.

### Acceptance matrix for the implementation

| ID | Scenario | Required result |
| --- | --- | --- |
| R1 | Same source twice; distinct but equal source records | One selected node/binding in the first case; independent nodes in the second. |
| R2 | Parent-first, child-first and independent siblings | Decoration-independent ancestor state reuse, pinned child ownership and no direct-resolution hoisting. |
| R3 | P calls M three times; C calls M twice with another observer | One M state advances five times; outer count five, inner count two. |
| R4 | P already owns G -> M; only M gets a new declaration in C | Rebound G reaches C's M observer while parent callers keep their bindings. |
| R5 | C owns M_child before P owns G -> M_parent | C's G binding retains M_parent; C's direct M remains M_child. Control projection selects the same dependency as the operational reference. |
| R6 | G's deferred direct dependency first resolves from C's binding | The documented original logical-domain rule applies; fresh per-invocation child domains still keep their newly created state local. |
| R7 | Stateful generated A; C inserts B inside it | A keeps its data, receives each call once, and calls the context-specific predecessor. B does not affect parent callers. |
| R8 | Generated wrapper resolves its predecessor later, or resolves unrelated X which resolves M | Deferred direct predecessor uses the retained frame; X follows its own ordinary dependency selection. Extra children use the occurrence's defined private domain, preserve first selection and do not leak into parent/sibling caches. Cover inherited declarations on child-owned sources and incomparable retained domains. |
| R9 | Retained budget D rebound through P -> C -> D | Stable D declaration/data; effective depth and configuration/module origin preserve clock, trace and measurement order. |
| R10 | Cycle reset/reuse, repeated entries, two Cycle nodes, two invocations; Pipeline stages | Correct algorithm-reference reuse, local resets and ancestor sharing; unchanged candidate/state results and RNG forks. |
| R11 | Adaptable source plus observers and an unrelated consumer control | Adaptation remains effective through the typed control; calls still traverse observations; absent capabilities remain absent. |
| R12 | Paused enumerator and new binding | Original enumeration resumes with its binding and RNG; a new binding neither resumes nor duplicates it. |
| R13 | Preparation/dependency/binder/decoration/declaration failures and structural recursion | No duplicate preparation or completed partial graph; failures remain at their defined node/occurrence/context boundary. Cycles cannot evade detection by making new contexts. |
| R14 | Compatible/incompatible typed resolutions, including bound algorithms and custom roles | Compatible views share the selected state; incompatible contracts fail clearly before wrong child construction. |
| R15 | Long-lived source with short-lived contexts; root declaration with many fresh sources | Discarded contexts, advice occurrences and fresh domains can be collected; intentionally retained reuse domains remain alive. |
| R16 | No matching observer, repeated resolution and overlapping future selectors | No phantom reports, repeated initialization or duplicate wrappers; valid empty selection remains valid. |

Tests must assert externally meaningful identity, state transitions, call reports and deterministic results. Do not assert private dictionary layouts or require every raw stateless binding to allocate an object. Update old scope tests that encode the decoration reuse barrier explicitly, retaining their other invariants.

### M3: integrated acceptance reconciliation

Migration checkpoint: `85c1b24f`. Migration and its normal-project validation are complete. The evidence reconciliation below includes the subsequent M3 repairs and preserves the remaining lifecycle questions; it does not introduce a cleanup API.

The 2026-10-05 independent verification inspected R1-R16 against the integrated assertions and production paths. The following index retains its relevant findings with the subsequent repair evidence. Runtime validation is recorded in the repair and convenience records below. Passing suites alone do not establish final foundation acceptance.

| Acceptance cases | Integrated evidence | Verified findings and remaining boundaries |
| --- | --- | --- |
| R1-R3 | [Resolution selection/rebinding tests](../test/HeuristicLib.Tests/Execution/ExecutionFactoryResolutionTests.cs), [role factory tests](../test/HeuristicLib.Tests/Execution/RoleExecutionFactoryTests.cs) | Assertions cover reference identity, equal-but-distinct configurations, parent/child/sibling selection orders, once-only preparation and exact outer/inner report counts of 5/2. |
| R4-R5 | [Topology factory tests](../test/HeuristicLib.Tests/Execution/TopologyExecutionFactoryTests.cs), resolution collision/control tests, [concrete algorithm factory tests](../test/HeuristicLib.Tests/Algorithms/ConcreteAlgorithmFactoryTests.cs) | Assertions cover descendant-only observations, continued composite/source state and preservation of the pinned dependency/control despite an earlier child-local selection. |
| R6-R8 | [Deferred/lifetime resolution tests](../test/HeuristicLib.Tests/Execution/ExecutionFactoryResolutionTests.Lifetimes.cs), resolution advice/predecessor tests | Assertions cover original deferred ownership, fresh child isolation, retained predecessor context, private additional-child selection and incomparable domains. `PredecessorOverride_DoesNotLeakIntoAnAdditionalChildFrame` constructs an unrelated forwarding child which resolves the source and asserts recursion; it proves ordinary resolution rather than a leaked predecessor override. A successful recursive forwarding path is not promised. |
| R9-R10 | [Deferred algorithm factory tests](../test/HeuristicLib.Tests/Algorithms/MetaAlgorithms/DeferredAlgorithmFactoryTests.cs), resolution ordering tests, [Cycle analysis scenarios](../test/HeuristicLib.Tests.Scenarios/Core/Algorithms/MetaAlgorithms/CycleAlgorithmAnalysisScenarios.cs) | Assertions cover origin/depth/sequence ordering, retained count/duration budgets, both Cycle modes, Pipeline freshness, repeated references, owner/invocation isolation and RNG behavior. The M3 repair adds the actual clock/trace/budget rebinding case; generic ordering tests alone did not establish that combination. |
| R11-R14 | Resolution projection/failure tests, role factory compatibility tests, [iterative algorithm factory tests](../test/HeuristicLib.Tests/Algorithms/IterativeAlgorithmFactoryTests.cs), [adaptation tests](../test/HeuristicLib.Tests/Operators/Mutators/AdaptableMutationStrengthTests.cs) | Assertions cover optional raw controls, paused iterator progress/RNG, sticky construction faults and cross-domain recursion. Generic control tests did not prove observed EvolutionStrategy adaptation; the M3 repair supplies that concrete consumer proof. Custom-role and compatible algorithm-state evidence is identified below. |
| R15 | Resolution weak-reference/retained-domain tests, [dynamic cache/lifetime tests](../test/HeuristicLib.Tests.Experimental/Problems/Dynamic/Operators/DynamicEvaluationCacheTest.cs), [racing failure tests](../test/HeuristicLib.Tests.Experimental/Algorithms/DynamicRacingFactoryTests.cs) | Discarded contexts/advice/fresh domains can be collected, and live deferred bindings retain required domains. The M3 repair covers disposal of started racing contenders on failures. Problem-retained cache/reevaluation state, unsubscribe ownership and deterministic cache disposal remain open; node/child collection does not prove their cleanup. |
| R16 | Resolution caching/observation tests, [selector definition tests](../test/HeuristicLib.Tests/Execution/NodeSelectorTests.cs) and [usage specs](../test/HeuristicLib.Tests.ApiUsageSpecs/Execution/NodeSelectionSpecs.cs) | Reference matching, wrapper-target exclusion and repeated-binding identity are asserted. The M3 repair adds a direct ordinary unmatched-source zero-report/once-only preparation case. Both overlapping registrations and zero-match registered selectors await package 4c.2; selector-definition tests do not establish runtime registration behavior. |

For R14, the custom-role evidence in [ResolutionScopeTests](../test/HeuristicLib.Tests/Execution/ResolutionScopeTests.cs) is `ResolvingThroughAPreparationAdapter_AppliesTheWrappers`, `AnExecutionHeldAsAnotherType_IsReportedRatherThanCast` and `AWrapperThePreparationAdapterCannotAccept_IsReported`. `BoundFactory_AcceptsASupportedConcreteProblemAndPreservesItsPreparedState` in the iterative algorithm factory tests proves state advancement across compatible algorithm bindings; the simpler bridge test only proves preparation count and non-null bindings.

The untimed pre-repair EvolutionStrategy/Gaussian reproduction produced `[0, -1.5, -2.5]` without observation and `[0, -1.5, -3]` with a generated counting observer reporting two mutation calls. This established the concrete adaptation limitation, without attributing its introduction to the factory migration. The repaired integrated test expects the same adapted sequence in both cases. The initial verification passed 247 focused cases; the broader post-repair results below supersede that run for runtime acceptance evidence.

Record each reconciled case as covered, missing evidence or an explicit accepted limitation. Add focused tests only for identified behavioral gaps. Review current architecture/authoring docs against the implemented contracts and reconcile remaining local-proof references; preserve historical review records as history rather than current instructions.

The M3 audit identified two production lifecycle issues:

- DynamicCachingEvaluator and ReevaluationInterceptor install epoch handlers once per preparation. The problem event retains their state until the problem releases the subscription; there is no unsubscribe owner, and the dynamic cache has no deterministic disposal owner. Existing weak-reference cases prove discarded nodes and child evaluators can be collected, not that subscribed state/cache resources are released. Establish the intended owner and end-of-lifetime behavior before proposing a focused fix; rebinding must not unsubscribe or dispose still-shared state.
- DynamicRacing created contender entries before its race-loop `try`, and disposed them only in the later winner/merge `finally`. Failure while creating a later entry or during `MakeMove`/termination checks could bypass disposal of entries already created. The repair package below covers these phases and preserves epoch-handler removal; burn-in retains its scoped `using`.

At M3 review, decide which lifecycle repairs are required for foundation acceptance and which, if any, can remain explicitly accepted follow-up work. A green suite does not silently close them. Do not add a resolver-wide disposal protocol or alter shared-state ownership as incidental cleanup. Review the reconciliation and any repair proposal before beginning its implementation. The user separately authorized C4 measurement while M3 remains open; final foundation acceptance still requires the M3 review.

### M3 repair package, 2026-10-05

The user authorized repairing missing or incorrect work after the independent acceptance-evidence verification summarized above. This package is unstaged and uncommitted; shared resource ownership is not changed.

- R11: `IMutationStrengthControl` replaces the role-bearing `IAdaptableMutationStrengthExecution`. Gaussian's raw execution exposes the operation-free control. EvolutionStrategy projects it from the same selected binding used for its wrapped mutator, so generated observation does not disable adaptation. Deterministic observed/unobserved executions both produce `[0, -1.5, -2.5]`. A paused parent plus rebound child proves shared strength, exactly two child reports and preservation of an independently selected child-local strength. Explicit configured wrappers still expose only their own declared capabilities. The API usage example demonstrates projecting control while invoking the observed mutation role; all active references and docs migrate together.
- R9: `OperatorBudget_RebindingKeepsClockBeforeTraceAndExcludesObservationDuration` combines a real mutator clock and trace with both retained budget types. The root stream pauses after one call; a child adds observation and consumes the remaining two calls. Trace times are `[1, 2, 3]`, the budget factory runs once, count remains three, and measured operator duration remains three seconds despite ten-second trace callbacks and twenty-second child callbacks.
- R16: `UnmatchedReferenceDeclaration_DoesNotObserveOrRepeatPreparation` invokes an ordinary unmatched source twice and checks zero reports, shared completed binding and one preparation. Selector-registration cases remain deferred to package 4c.2.
- R8: keep the existing direct-predecessor isolation/recursion proof. Preselecting an unrelated forwarding dependency in an ancestor does not turn a reentrant child observation graph into a supported successful binding; it still reaches ordinary recursion detection. No resolver policy is changed to satisfy an invented positive-path expectation.
- R15 racing repair: construction, moves, termination checks, winner selection and merging share one cleanup boundary. Every created entry is disposed, even if another entry's disposal throws. An unchanged original failure is rethrown with its stack when cleanup succeeds; cleanup failures are aggregated with any original race failure first. Entry disposal relinquishes its enumerator before calling `Dispose`, preventing repeated disposal if restart construction or cleanup fails. Five failure cases prove disposal of every started contender and exact failure preservation for step, termination, merge, disposal and combined merge/disposal faults. Partial construction is enclosed by the same cleanup boundary; unstarted async streams have not entered their resource-owning iterator body, so the tests do not claim such bodies were disposed.

Validation: 68 focused core cases and seven focused racing cases pass. The complete core suite passes 2,482 cases; the API usage suite passes 194. The final normal Release solution test run passes all 2,897 cases, with zero failures or reported skips, including Experimental and scenario suites. The activated-TSP scenario retains its external-prerequisite early return, so this run does not establish that optional workflow executed. The Release solution build, including a non-incremental compiler/XML documentation check, and documentation build pass; existing analyzer warnings remain. Whole-solution whitespace, style at warning severity, analyzers at error severity and `git diff --check` pass. Format tools report workspace-loading warnings, so the normal build and runtime suites provide independent compilation evidence. After adding the test cancellation token to the paused adaptation enumerator, all eight adaptation cases pass again. Logs are under ignored `artifacts/execution-factory/m3-*`; benchmarks are not rerun. Git staging remains untouched; index SHA256 is `071BB5E372C8B380ADCDC1A780B89FAAA366085AC2929EC1F3DA59595C3ADA6B`, matching the starting checkpoint. `.claude/settings.local.json` remains untouched.

### Typed mutator control convenience, 2026-10-05

The user approved hiding the factory callback and run type arguments at EvolutionStrategy's control-resolution call site. `MutatorResolutionExtensions` provides a typed `Resolve<TControl>(IMutator<TCandidate>, out TControl?)` overload, used as `typed.Resolve(Mutator, out IMutationStrengthControl? strength)`. It forwards to the existing generic raw-control projection; no new resolver selection, state, caching or control discovery mechanism is introduced. A capability absent from the selected raw binding returns null. The generic projection remains available for advanced projections and control bundles. Other roles are not extended without a concrete use case.

EvolutionStrategy, the executable operator-authoring example and the Gaussian rebinding/pinned-control tests use the short form. The rebinding test additionally asserts identical controls across contexts, a wrapped execution that does not itself expose the control, repeated execution/control identity and unchanged report counts. The explicit configured-wrapper case checks an absent control directly before running the strategy. User-facing docs show the compact call and describe current behavior.

Validation: all 64 focused control/factory cases, all 2,482 core cases and all 194 API usage cases pass, with zero failures or skips. The normal Release solution build and documentation build pass. Whitespace, style at warning severity and analyzers at error severity pass for the four changed C# files; `git diff --check` passes. Format tools retain their workspace-loading warnings, and the normal build/test runs provide independent compilation evidence. Earlier full-solution acceptance evidence remains the M3 repair record above; Experimental/scenario runtime suites and benchmarks are not repeated for this convenience-only addition. Logs are under ignored `artifacts/execution-factory/m3-control-convenience-*`. The user had staged the earlier repair package before this change; the index remains untouched with SHA256 `8E42462D950B4CC2BD933437044785E07A2FBF5B401C062FFA78D1930E35BAE2`. The user subsequently staged and committed this package as `f7a89747`. Shared epoch-state lifetime and final foundation acceptance remain open.

### Cache statistics retention repair, 2026-10-06

The user authorized progress using the epoch-invalidation inventory after its source verification. The next coherent package removes cache-statistics retention from both caching evaluators without changing epoch subscriptions or introducing a disposal protocol.

- The inventory's important additional finding is supported by the pinned `Microsoft.Extensions.Caching.Memory` 10.0.11 implementation: [MemoryCache](https://github.com/dotnet/runtime/blob/v10.0.11/src/libraries/Microsoft.Extensions.Caching.Memory/src/MemoryCache.cs) creates `ThreadLocal<Stats>` when statistics are enabled, and each `Stats` strongly references its cache. [ThreadLocal](https://github.com/dotnet/runtime/blob/v10.0.11/src/libraries/System.Private.CoreLib/src/System/Threading/ThreadLocal.cs) stores those values in a thread-static slot array. An abandoned cache can therefore remain alive on a live thread independently of the problem event. Core `CachingEvaluator` has this exposure too, although it never reads the statistics.
- Both caches retain `MemoryCache`, its existing size limit and admission/eviction policy, with statistics disabled. Dynamic grace accounting uses the existing deduplicated miss list: an empty batch does nothing, any miss resets the streak, and an all-hit batch adds its candidate count, including repeated keys. No separate hit counter or statistics snapshot is needed.
- Two core regression cases, bounded and unbounded, hold the reusable configuration and problem alive while discarding the selected execution and scope. They confirm a cached result is reused by identity before discarding, then require both the cached candidate and objective vector to become collectible on the still-live test thread. Before the production change, both cases fail because the candidate remains alive; the other three focused cases pass. Both collection cases pass after disabling statistics. A zero-capacity case preserves returned results and rejection of cache admission.
- Two dynamic cases pin repeated cached candidates counting toward grace, a fresh grace streak after an applied epoch, and a mixed batch with repeated misses resetting the streak while deduplicating the child evaluation. All fifteen dynamic cache cases pass before the production change; these tests preserve existing behavior rather than establish a new epoch policy.
- The inventory overstates two disagreements with the plan: the proposal explicitly says polling does not establish deterministic cache disposal, and the relative-quality evaluator's post-evaluation epoch read serves a different purpose from consuming a reevaluation request. Reading before reevaluation preserves the event-driven consume-before-call behavior. Baseline initialization and failure behavior still need explicit tests in the later reevaluation package.

Validation: all fifteen focused core cache/composition/accounting cases, all 2,485 core cases and all 199 Experimental cases pass, with zero failures or reported skips. The final Release solution build and documentation build pass. Whole-solution whitespace, style at warning severity, analyzers at error severity and `git diff --check` pass. Format tools retain workspace-loading warnings; the normal build/test runs provide independent compilation evidence. The compiler reports existing diagnostics and two `S1215` warnings for the new test's intentional collection assertions; no vendor suppression is added. API usage/scenario runtime suites and benchmarks are not repeated for this internal cache repair. Logs are under ignored `artifacts/execution-factory/m3-cache-*`. Changes remain unstaged and uncommitted; the index SHA256 is unchanged at `EC2F03BCD656AE11E65F8AD81EA6D5A8DB4E9DC1B96D440B74FFF92972CEEE29`, and unrelated `.claude/settings.local.json` is untouched.

Stop for review of this package. The next proposed package replaces the reevaluation subscription with a preparation-time epoch baseline and a consumed epoch captured before evaluation. Dynamic cache invalidation follows separately, with checks before lookup and after the child call before publication evaluated against the existing behavior. In particular, deferred updates inside a child call are an ordinary supported path, not merely a concurrency edge case. Mixed-epoch result admission and grace requests that remain pending through all-hit batches need a deliberate policy rather than incidental changes in this statistics repair. Removing the dynamic cache event while leaving statistics enabled would not solve retention; removing both external reference paths may permit managed collection without replacing `MemoryCache`, but that remains to be proved for the dynamic cache. No deterministic disposal contract is promised.

### Shared epoch-state lifetime proposal awaiting review

The problem event still strongly retains prepared cache/reevaluation state after their scopes and bindings are discarded. Rebinding must not end that lifetime: parent bindings, child bindings and paused streams can all use the same state. There is no meaningful last binding to dispose without adding an ownership protocol.

Prefer investigating `CurrentEpoch` checks at operation boundaries before adding a resolver-wide disposal API:

1. Reevaluation can retain the last consumed source epoch in shared state and compare it in `Transform`. Capture the epoch before reevaluation so an update during evaluation remains pending for the next transform. Independent preparations keep independent consumed epochs; rebinding preserves the selected preparation. This removes the long-lived event handler without introducing a disposable resource.
2. Dynamic caching can compare the epoch before cache lookup and clear stale entries and grace-count state there. This changes invalidation timing: entries remain stored between an epoch change and the next evaluation, although they cannot supply a result after that check. Check updates during child evaluation and grace-count advancement against the existing event-driven behavior explicitly. Do not claim polling alone supplies deterministic cache disposal.
3. The cache still uses `MemoryCache`. The statistics repair above removes its thread-local retention path while preserving admission and eviction. Investigate whether removing the event as well makes the state and entries collectible after all preparations and bindings are discarded. That can avoid a resolver-wide lifetime protocol if deterministic disposal is not required. If deterministic disposal is required, define an explicit logical-execution lifetime owner covering direct scope usage and paused streams, or replace the cache with managed storage and review its size-limit/admission/eviction semantics separately. Collection and deterministic disposal are distinct contracts.

This is a proposal, not an accepted contract or implemented repair. The immediate next review should choose the invalidation and cache ownership policy. Add prepared-state collection and cache-release evidence with that repair; existing node/child collection tests are insufficient. M3 and final foundation acceptance remain open, and selector registration does not start in this package.

### C4: cost evidence and final acceptance

The pre-functional-change baseline is `edcf81d1c898c5e6b2ec0586b877783ba3b8c43a`; source is unchanged from the naming commit. At the start of direct implementation, the working tree adds only plan edits and an unrelated local tool-settings file. Record SDK/runtime, build configuration, machine and benchmark source with the results. The user authorized a temporary worktree at the previous version, with an equivalent benchmark harness added there and adapted only as needed for the old API. Inspect existing worktree attachments before creating one. Keep the active checkout and Git index intact; do not stage, commit or reset to obtain a baseline. Keep benchmark source and adaptations available as reviewable Git diffs and record exact run commands. Do not hide benchmark code in an ignored duplicate project or add a permanent benchmark framework without a concrete need.

Status: the user explicitly authorized execution on 2026-10-04, and all sequential comparison/retention stages completed successfully. The [C4 results](execution-factory-cost-results-2026-10-04.md) preserve the measured environment, source hash, representative timing/allocation/retention results and local evidence locations. Already-bound operation calls show no regression or allocations; declaration, resolution and fresh-domain costs increase substantially in relative terms. After reviewing absolute costs and their estimated contribution to long-running GA/GP workloads, the user accepted the performance tradeoff. Resolver profiling/optimization is not a migration prerequisite. M3 lifecycle reconciliation and final foundation review remain open independently. The measured migrated checkpoint is `85c1b24f`; assess whether later behavior changes require affected workloads to be measured again. The removed migration-backup tag is not the old-version comparison baseline.

Measure these cases separately:

- First preparation/resolution and warm repeated resolution for stateless leaves, stateful leaves and composites.
- Additional binding with no new declarations, a leaf observer, and an observer only on a descendant.
- Cheap operation throughput with zero, one and several wrappers; no factory invocation or scope lookup may enter the steady-state operation path.
- Allocations per node, binding and declaration; retained bytes after many short Cycle/Pipeline activations, both reset and reuse modes.
- Real HillClimber/GA execution and short repeated algorithm invocations; include the one-time construction delegates and typed-scope views in total costs.

Use warmups, repeated samples and allocation measurements, report variance and distinguish equivalent baseline workloads from option C's newly supported composite case. Do not assign an arbitrary acceptable percentage without seeing the cheap-operation and memory results. If costs conflict with the design goals, present the measured tradeoff and an explicit design revision before final acceptance. C4 records the resulting implementation scope and any follow-up packages; it does not waive their review stops.

Deliver the reviewable harness and old-API adaptations, exact commands and environment, per-workload timing/allocation/retention results with variance, the reconciled acceptance/lifecycle status and an explicit acceptance recommendation. Only after reviewing this foundation resume selector registration (package 4c.2); advice semantics and public wrapper/registration vocabulary remain subsequent design work.

### C4 harness review record, 2026-10-03

- Added a focused ExecutionFactories harness, independent of the solution and ordinary tests. BenchmarkDotNet 0.15.8 handles timed runs and managed allocation diagnostics. Coverage includes root/warm resolution, declarations, additional observations, cheap operation calls with 0/1/4 wrappers, first/repeated HillClimber and GA invocations, Cycle reset/reuse and Pipeline activations, and separate retained-heap samples. No permanent benchmark CLI or production changes are included. After C4 review, the temporary harness was moved to the ignored local evidence directory rather than committed.
- Created and attached a baseline worktree at `edcf81d1c898c5e6b2ec0586b877783ba3b8c43a`: `C:\Users\phili\.codex\worktrees\execution-factory-baseline\HeuristicLib`. Both checkouts contain identical reviewable harness source. One conditional adapter uses baseline `Decorate` versus migrated `Wrap`; production bases and resolution extensions handle their respective creation APIs. Factories-only preparation/binding and descendant-observation groups are excluded from baseline measurement. Stateful direct-observation semantics also differ and are explicitly documented rather than treated as equivalent state behavior.
- Release builds passed for both harness copies. Untimed smoke checks passed all parameter combinations twice: 128 migrated and 114 baseline workload invocations, with deterministic positive workflow checksums, cached execution identity, exact wrapper counts and revision-specific descendant observation. One migrated descendant-binding `Dry` case verified BenchmarkDotNet's generated process. A two-activation/one-sample retention-output smoke check verified all three modes, JSON generation, and collection of released root scopes/executions. Dry and tiny heap samples are harness validation only; their numbers provide no C4 performance evidence.
- The README records measurement boundaries, environment/source-hash capture, full-run commands, raw retention samples and baseline-copy instructions. Timed comparisons and meaningful retention runs remain deferred until explicitly requested. M3 acceptance/lifecycle reconciliation remains open; no performance acceptance or selector-registration work is implied. New source and this plan update remain unstaged, with the user's previously staged review checkpoint preserved.

### C4 measurement review record, 2026-10-04

- Completed 57 baseline and 64 migrated full cases, six reversed-order operation repeats and 60 retained-heap samples sequentially, with identical harness sources and runtime. All stages returned exit code zero. The [results report](execution-factory-cost-results-2026-10-04.md) contains boundaries, variance, allocations, lifecycle limits and the accepted tradeoff. Detailed cost/retention datasets and raw output remain local evidence outside the commit.
- Steady-state operation calls remain allocation-free with no observed slowdown. First resolution costs 3.15–9.15× baseline, cached resolution 2.21–7.97×, fresh-child resolution without added declarations 5.87–14.17×, and declaration snapshots with 16 registrations 42.64×. Short reset/repeated-Pipeline activations also regress; Cycle reuse amortizes preparation. Stateful direct-observation rows have deliberately different state semantics and are identified separately.
- Retained memory does not scale with activation count in the measured workloads, but Cycle reuse has a larger fixed footprint. Every released root scope/execution was collected; this does not establish cleanup of dynamic subscriptions/cache resources or arbitrary observation-context workloads.
- The initial recommendation was to keep the cost gate open and investigate construction/allocation/cache paths. The subsequent workload-level review below supersedes that recommendation. No production code, Git index or commits changed during measurement.

### C4 performance acceptance decision, 2026-10-04

- Accept the measured resolution and activation overhead for the intended long-running optimization workloads. A normal GA execution resolves its operator graph during construction and uses bound operators throughout its generations; setup cost is not paid per candidate or generation. Already-bound calls remain allocation-free with no observed regression in the measured path.
- The [workload estimate](execution-factory-cost-results-2026-10-04.md#performance-acceptance-and-workload-estimate) uses 5,000 generations and 500 candidates, approximately 2.5 million evaluations. Assumed evaluation costs of 1/10/100 microseconds give 2.5/25/250 seconds of evaluation work alone. Even an illustrative 100-microsecond extra setup allowance contributes only 0.004% to the cheapest example. These are arithmetic estimates, not additional benchmark evidence.
- Small Cycle reset/Pipeline workloads indicate roughly one extra microsecond per child activation. If a comparable fresh child were activated once per generation, approximately five milliseconds of additional work would contribute 0.2%/0.02%/0.002% to those examples. Larger graphs, heavy observation registration and repeated very short algorithms may have a material cost; the acceptance does not claim a universal runtime guarantee.
- Resolver profiling/optimization is optional follow-up, not required to finish the migration or proceed after foundation review. C4 performance acceptance is closed; M3 lifecycle/resource ownership and final foundation acceptance remain open. Retention evidence does not settle the dynamic subscription/cache-disposal questions.
- Commit the plan updates and results report. The one-off harness, detailed CSVs, orchestration scripts, raw output and build products are local artifacts, not a permanent benchmark suite. Their temporary location is recorded in the report; they are not required repository assets.

## Provisional migration scope

This source-grounded inventory bounds the authorized migration, carried out in the review packages below. Before the factory cutover, a search for `CreateExecutionInstance` or `WrapExecutionInstance` found 106 main-library source files, 17 Experimental source files, 36 test/spec files and two analyzer files; these are textual matches, not a count of concrete factories. Samples and PythonInterop still require build validation even when they have no direct authoring-method match. Refresh this inventory as packages proceed and reconcile it at C4.

| Area | Required migration |
| --- | --- |
| [Core execution contracts](../src/HeuristicLib/Execution/IConfigurationNode.cs) and [ResolutionScope](../src/HeuristicLib/Execution/ResolutionScope.cs) | Replace creation overloads; introduce the reviewed internal ownership/caching/failure machinery and advanced child-scope/control operations. Keep declaration and resolution phases separate. |
| Operator role interfaces and resolution extensions | Change returned factories across all core roles and Experimental move roles; retain operation contracts, optional/try resolution and type inference. No role enumeration or obsolete entry points. |
| Stateless/stateful/single-item bases | Hide preparation internally; keep leaf override signatures and deterministic batching. |
| Wrapping/multi bases and authored composites | Replace protected preparation hooks; extract persistent cache/counter data, preserve child configuration references and bind children per context. Audit PredefinedCandidatesCreator and CachingEvaluator first. |
| Algorithm bases and ordinary algorithms | Replace public and protected hooks; resolve interceptors on each binding. Hoist GA, NSGA2 and ALPS derived rate wrappers into preparation. |
| Cycle, Pipeline and budget algorithms | Replace endpoint caches with retained domains, maintain fresh activation domains, separate node-owned accumulators from iterator-local duration. |
| Specialized capability producer/consumer | Migrate GaussianMutator and EvolutionStrategy together to the reviewed control contract; test consumer-defined controls and deliberate configured-wrapper forwarding. |
| Analysis observations and counting/duration instrumentation | Prepare stable generated wrapper occurrences, retain source attribution and callback ownership, follow the new original-source callback contract. |
| Experimental dynamic execution | Split DynamicRacing's hall of fame, incumbent configuration and accumulated counters from binding-local scopes/children; leave race-entry enumerators, subscriptions and disposal invocation-local. Preserve inner meta-optimization type adaptations and late contender observations. |
| Analyzer and code fix | Replace the old `CreateExecutionInstance` name/scope-parameter assumptions in HLib0001. Recognize direct child factory creation/invocation in returned lambdas and deferred methods, while allowing typed resolver preparation adapters and legitimate self/base bridges. Do not claim the analyzer proves shared-state ownership. |
| Tests/specs, samples, docs | Migrate test doubles and API contracts, reflection/architecture assertions, examples and documentation references. Run every affected consumer's compilation. |

### Agreed commit boundaries

Keep the two substantive implementation changes in separate commits when implementation and committing are requested:

| Commit | Scope | Required boundary |
| --- | --- | --- |
| 1. Adopt graph and execution type names | Rename `IExecutionConfiguration`/generic form to `IConfigurationNode`, `IExecutionInstance` to `IExecutionNode`, and role-specific runtime contracts/bases from `...Instance` to `...Execution`. Migrate related filenames, generic parameter/local terminology, extensions, tests, reflection assertions, examples, samples and docs across all projects. | Preserve behavior, type arities/constraints, state ownership, caching, decoration ordering and operation signatures. Keep `CreateExecutionInstance(...)` and protected creation hooks returning objects for this intermediate compiling revision. No new factory API, compatibility aliases, forwarding types or state extraction. |
| 2. Implement typed factories and revised resolution | Replace object-creation methods with `CreateExecutionFactory`, add the reviewed preparation/binding contracts and resolver, move persistent data to its intended owners, migrate deferred scopes/observations/capabilities and all consumers, and complete behavior documentation/validation. | This commit contains the functional option-C change and its required authoring changes. Remove the former creation path rather than retaining a second public workflow. |

The temporary old creation-method name in commit 1 is deliberate: changing it to `CreateExecutionFactory` while it still returns an object would be false, and introducing an intermediate `CreateExecution` method would add avoidable churn. Existing behavior descriptions remain accurate after the rename; descriptions of shared persistent state and repeatable factories land with commit 2.

Validate the rename independently with a Release build, core/API usage tests, affected Experimental consumers and final solution validation for the cross-project public rename. Verify that changes to behavioral assertions are limited to renamed symbols, diagnostics and identifiers. Keep unrelated `Instance` names such as singleton properties intact. Review lifecycle policy names such as `NewExecutionInstancesPerCycle` for their actual meaning instead of mechanically replacing a substring; no new policy name is selected here.

The following M0-M3 work packages subdivide preparation and review of the factory/resolution change; they do not require separate commits. The user subsequently requested committing the reviewed M0a preparation separately, recorded below. Direct implementation is authorized after the completed C2/C3 proofs, with C4 before final acceptance. State extraction and resolver work stay out of the naming commit. No commit or index operation is authorized by recording these boundaries.

### Naming migration review record, 2026-09-27

The behavior-preserving naming package was reviewed by staging and committed as `d5ad7fb2` after the user's explicit commit-and-continue instruction. The following records its validation before that review.

- Renamed the common contracts to `IConfigurationNode` and `IExecutionNode`, runtime role contracts and bases to `...Execution`, and nested runtime types/type parameters to `Execution`/`TExecution`. Migrated core and Experimental consumers, test doubles, architecture/reflection checks, analyzer metadata, authoring snippets, documentation filenames and links. Ordinary configuration roles and operators retain their domain names.
- Preserved `CreateExecutionInstance`, `WrapExecutionInstance`, `CombineExecutionInstances`, type arities/constraints, typed role operations, state allocation, resolver caching, decoration order and lifecycle behavior. Kept singleton `Instance` properties and the `NewExecutionInstancesPerCycle` policy name. No typed-factory API, state extraction or resolution rework is included.
- Baseline: `0a48345e4198b53f7be0f8389dc04b591c75f21e` on `container-and-aspect-framing`, with no tracked working-tree changes. The pre-existing untracked `.claude/settings.local.json` was left untouched. SDK 10.0.401, Release. Baseline restore/build passed; core tests passed (2,295) and API usage specs passed (191).
- Final validation passed: `dotnet build --configuration Release --no-restore --no-incremental` (69 existing warnings, zero errors); `dotnet test --configuration Release --no-restore --no-build` (2,686 passed, zero failed or skipped, across core/API/Experimental/scenario suites); repository whitespace, style at warning severity and analyzer verification at error severity; `npm run docs:build`; and `git diff --check`. Format commands reported workspace-loading warnings while exiting successfully.
- A local Roslyn token audit of all 214 changed C# files found no structural or non-string-literal changes. Identifier changes and the 23 string changes were reviewed; strings contain renamed diagnostics, analyzer metadata and compiled analyzer-test snippets. Local validation logs and the audit helper are under ignored `artifacts/execution-naming/`.
- Renamed documentation/source links resolve. The wider link audit found one unrelated pre-existing missing target: `plans/data-analysis-modernization-plan.md` links to `docs/guide/domains/data-analysis.md`, which is also absent at the baseline revision. That historical plan was left unchanged.
- No unresolved naming issue. The current decoration reuse barrier and shared-composite limitation remain current behavior, pending C2 and the later factory migration. The next package is the isolated typed-factory proof, after explicit review continuation. No staging, unstaging, reset or commit was performed.

### C2 completion record, 2026-09-27

The focused typed-factory proof was completed and validated locally: 31 invariant tests and eight executable authoring specs passed. It established the planned core state-preservation, scoped-observation, identity, capability and failure cases. This was local validation before the production migration; C3 followed with explicit continuation.

### C3 completion record, 2026-09-27

The deferred execution and lifecycle proof was completed and validated locally: all 77 checks passed, including the C2 cases. It covered retained/fresh children, Cycle/Pipeline state and RNG behavior, budget and observer ownership, paused iterators, construction/operation failures, cancellation/disposal and collection in both lifetime directions. The Release build passed without warnings or errors; whitespace, style and analyzer verification passed. Independent repository validation passed: core 2,295, API usage 191 and the two selected Cycle analysis scenarios, with no failed or skipped tests. Existing repository analyzer warnings remain.

The constructor audit and the necessary weak ownership links are recorded in the [design](execution-factory-design.md). No additional public factory API was required. Experimental subscription ownership and DynamicRacing's existing exception-path disposal gap remain explicit migration work; C4 must measure the ownership machinery's cost. At this checkpoint, shipping source and normal tests were unchanged, with no dependency on local proof material. The user subsequently authorized the direct implementation workflow above; C4 remains outstanding.

### Factory/resolution work packages

1. **M0a: extract persistent state in the core library.** Separate the predefined-candidate cursor, evaluation cache and limit counter, Gaussian mutation strength and algorithm-observation iteration counter from their execution bindings. Existing creation methods still allocate fresh state per execution; public workflow and behavior remain unchanged. Validate and stop.
2. **M0b: prepare Experimental persistent state.** Separately review DynamicRacing, DynamicCachingEvaluator, DynamicRelativeQualityEvaluator and ReevaluationInterceptor. Separate persistent data from scopes/children and invocation resources without changing resolution semantics. Keep subscription/lifecycle fixes with the later ownership migration where necessary. Validate and stop.
3. **M1: replace the actual resolver and contracts.** Introduce typed factories, preparation/binding ownership, occurrence lifetime, construction failure and child-domain behavior directly in production files. Migrate the authoring bases and representative call paths with normal tests. Review resolver/contracts and authoring-base changes in bounded subpackages, stopping after each. Temporary compilation failures in unmigrated consumers are permitted and must be reported; do not maintain a parallel candidate engine or temporary public API.
4. **M2: migrate dependent implementations and consumers.** Review bounded groups of role extensions/wrappers, ordinary algorithms/composites, deferred algorithms/budgets, observations/instrumentation, capabilities, Experimental code, consumers/tests/analyzers and documentation. Identify the next group's concrete files before editing and stop after each group. Preserve typed operations and repeatable binding; wrapping the old state-resetting factory in a lambda is insufficient. Add integrated regression coverage for the C2/C3 requirements through the public route.
5. **M3 and C4: integration, performance and final review.** Reconcile all R1-R16 cases, finish consumer compilation and documentation, run the affected suites and final solution checks, and compare the actual implementation against the old-version worktree. Review measured costs and any design adjustments before accepting the functional change. Resume container/aspect package 4c.2 selector registration only after this foundation is reviewed.

Review checkpoints may be smaller than a compiling migration because the user explicitly accepts temporary breaking code. Each handoff must identify changed files, available validation, unresolved dependencies and the proposed next package. The final commit must be self-contained, compiling and fully validated without local proof files. No merge/commit or index operation is part of these packages unless explicitly requested.

### M0a review record, 2026-09-27

The first direct implementation package changes five existing core files: PredefinedCandidatesCreator, CachingEvaluator, LimitEvaluator, GaussianMutator and AlgorithmObservation. Private `ExecutionState` holders now carry the cursor, adaptable strength and observation counter; the cache and limit accumulator are passed into execution constructors directly. Existing object-returning creation methods still allocate fresh state per execution. Child resolution order, typed operations, capability access, cache/limit behavior and invocation-local iterator state are preserved. The later factory cutover will control state reuse.

Naming agreed during review: use `ExecutionState` for these private state holders and the short name `state` for their parameters, matching the glossary's execution state term. Use specific names such as `searchState` for other values where needed to avoid ambiguity. Apply this naming in subsequent migration packages too. The rename passed a Release core-test-project build, all 19 tests in PredefinedCandidatesCreatorTests, AdaptableMutationStrengthTests and AlgorithmObservationTests, and `git diff --check`.

Validation passed: `dotnet build --configuration Release --no-restore` (zero errors, 65 existing warnings); `dotnet test --project test/HeuristicLib.Tests/HeuristicLib.Tests.csproj --configuration Release --no-restore --no-build` (2,295 passed, zero failed or skipped); the repository whitespace, style and analyzer verification commands; plan link checks and `git diff --check`. Format commands reported workspace-loading warnings and exited successfully. No new warning diagnostic appeared compared with the recorded baseline. Existing behavioral tests were used without changes; broader runtime suites remain for the later affected packages and final integration.

The user reviewed and staged M0a, including its naming adjustments and plan updates, then requested committing and continuing. The staged package was committed as `5762e477` (`Extract persistent state from core executions`). M0b followed that explicit continuation. The user subsequently reviewed and staged M0b and explicitly requested amending it into the same commit; the combined commit is now `23d65748` (`Extract persistent state from core and experimental executions`). No staging or unstaging was performed.

### M0b review record, 2026-09-27

Four existing Experimental files now receive persistent `ExecutionState state` through their execution constructors:

- DynamicRacingAlgorithm: hall of fame, incumbent candidate/configuration and completed race/epoch counters. Scopes, resolved children and settings stay on the execution; race-entry observers, enumerators and subscriptions remain invocation-local.
- DynamicCachingEvaluator: cache and consecutive-hit count, using the agreed state naming.
- DynamicRelativeQualityEvaluator: cached best-known objective vector and its epoch.
- ReevaluationInterceptor: pending reevaluation count, preserving atomic request/consume operations.

Existing object-returning creation methods still allocate fresh state. The cache and reevaluation epoch subscriptions are installed alongside that state creation, after constructing the execution, and capture only state. This removes retention of unused execution nodes and child evaluators while preserving subscription count, timing and epoch behavior. Two regression cases in the normal Experimental suite check collection while the source problem remains alive. Existing epoch/cache/normalization tests cover operation behavior. The problem still owns the subscription lifetime; general subscription cleanup and DynamicRacing's existing entry-disposal gap remain deferred to the lifecycle migration.

The production Release solution build passed with zero errors and 69 existing warnings. After adding the collection tests, the focused dynamic tests passed (32), the full Experimental suite passed (178), and the self-contained MovingPeaks DynamicRacing scenario passed (1), with no failures or skips. The test helper's initial file-local-type signature error was corrected by making it a static local function; the final test project compiled successfully. Repository whitespace, style and analyzer verification, plan link checks and `git diff --check` passed. Format commands reported workspace-loading warnings and exited successfully; the build introduced no new warning diagnostics compared with the recorded baseline.

Validation commands for this package:

```powershell
dotnet build --configuration Release --no-restore
dotnet test --project test/HeuristicLib.Tests.Experimental/HeuristicLib.Tests.Experimental.csproj --configuration Release --no-restore --filter-class '*DynamicEvaluationCacheTests' --filter-class '*DynamicAnalysisTests' --filter-class '*TravelingSalesmanProblemTests'
dotnet test --project test/HeuristicLib.Tests.Experimental/HeuristicLib.Tests.Experimental.csproj --configuration Release --no-restore --no-build
dotnet test --project test/HeuristicLib.Tests.Scenarios/HeuristicLib.Tests.Scenarios.csproj --configuration Release --no-restore --no-build --filter-method '*DynamicRacingGa_OnMovingPeaks_ProducesPaperExperimentSignals'
```

The user reviewed and staged M0b, then requested amending it into M0a and continuing. The amendment is `23d65748`; M1a follows below. Authoring bases and dependent roles remain separate review packages.

### M1a review record, 2026-09-27

The real common contract and resolver now use typed factories:

- Added `ExecutionFactory<TExecution>` and changed `IConfigurationNode<TExecution>` to scope-free `CreateExecutionFactory()`. The generic resolver accepts a typed preparation adapter; the old object-returning adapter is removed.
- Replaced the old execution cache/reuse barrier with reference-identity logical selections, once-only preparation, pinned dependencies and context-owned raw/completed binding caches. A changed observation context rebinds composites and their children without resetting persistent state. Empty child contexts can reuse completed ancestor bindings.
- Added fresh/retained child-domain behavior, immutable deferred construction frames, original-source decoration occurrences with per-binding predecessors, private extra-child ownership, and raw-binding control projection after successful completion.
- Implemented sticky preparation, occurrence and retained-declaration faults; context-local binding faults; checked contract diagnostics; and recursion detection across child contexts. Weak ownership links and declaration-owned weak source keys follow the reviewed lifetime design. The occurrence's strong private-domain field has a narrow S1450 suppression because making it a local would discard its ownership.
- Migrated `ResolutionScopeTests` to the common factory contract and added 35 cases in `ExecutionFactoryResolutionTests` and its lifetimes partial file. They cover selection, composite/advice state, child-first control collisions, ordering, failure ownership, retained/deferred children, contextual depth and collection in both directions. These are normal core test files and depend only on production code.
- Updated the resolver and execution-node guides, glossary, contributor ownership rules and design examples. Documentation explicitly identifies the incomplete role/base cutover. Existing leaf overrides and typed operation contracts are unchanged in this package.

Validation at this checkpoint:

- The Release solution build stops at ten CS0411 errors in the unchanged resolution adapters for Algorithm, Creator, Crossover, Evaluator, Interceptor, Mutator, Refiner, Replacer, Selector and Terminator. They still supply the former object-creation callback. No compiler error is reported in the new factory contract or resolver.
- The focused normal test command below also stops at that production compilation barrier. **No test compiled or ran against this new implementation.** Full core/API/Experimental/scenario validation remains pending until the migration compiles; previously built binaries were not used to claim success.
- Repository whitespace, style (warning severity) and analyzer (error severity) verification exited successfully with workspace-loading warnings. Those checks are limited by the incomplete compilation and do not establish semantic correctness.
- `npm run docs:build` passed after removing two links from the published docs to repository-only plans. `git diff --check` passed. Local validation logs are in ignored `artifacts/execution-factory/`.

```powershell
dotnet build --configuration Release --no-restore
dotnet test --project test/HeuristicLib.Tests/HeuristicLib.Tests.csproj --configuration Release --no-restore --filter-class '*ResolutionScopeTests' --filter-class '*ExecutionFactoryResolutionTests'
```

Stop for M1a review with its changes unstaged and uncommitted. No compatibility path, duplicate implementation project or hidden runtime dependency was added. The accepted temporary compilation break remains the unresolved integration issue; runtime correctness and lifetime tests are not yet validated.

Proposed M1b: migrate the ten core role interfaces/resolution adapters and their role configuration bases, including stateless/stateful/single-item leaf bases, to typed preparation while preserving leaf override ergonomics and typed operations. Review that package separately. Wrapping/multi and iterative algorithm preparation hooks, concrete implementations, Experimental roles and consumer migrations follow in further bounded packages; do not repair the build by wrapping old state-resetting creation calls in lambdas.

### M1a maintainability and diagram revision, 2026-09-27

This record describes the first extraction. The subsequent wrapper vocabulary revision below replaces its decoration names and nests the small support types.

During review, the user requested clearer internal names, an actual decomposition of `ResolutionScope`, and restoration of the resolution diagrams. This revision stays within M1a and preserves the current public contracts and resolution rules.

The decomposition follows responsibilities, rather than moving the same dictionaries behind new names:

| Previous component | Replacement | Responsibility moved out of the public scope |
| --- | --- | --- |
| `LogicalDomain` | `ExecutionSharingScope` | Direct/ancestor state selection, ancestry, retained standalone children and the active construction guard. |
| `ExecutionRecord` | `ExecutionPreparation` | Once-only factory preparation/faults, pinned dependencies and retained children. This name includes pending and failed preparation and does not imply a callable node. |
| `BindingView` plus the scope's `Bind` algorithm | `ExecutionBindings` | Contextual construction, node caching/publication/faults, decoration ordering and retained contextual scopes. |
| `Predecessor` | `DecorationTarget` | The original source and immutable inner chain supplied only to its generated wrapper. |
| `Decoration` / `DecorationOccurrence` | `DecorationDeclaration` / private `PreparedDecoration` | Stable declaration identity and weak-key per-source wrapper preparation, including private dependency ownership. |
| `ChildSlot` | `RetainedChildScope` | Reserved child sharing scope and sticky once-only declaration snapshot. |

The six collaborators live under `Execution/Resolution` in the existing namespace. Their dictionaries and state machines are private. The public scope now routes selection and binding in 167 lines; builder, typed scope conveniences and origin enum have their own files. There is no additional public resolution workflow. The architecture check now protects the extracted components, with six focused boundary cases.

Astra provided a read-only design critique and inspected the extraction for semantic/lifetime regressions. The review found no actionable difference in reference selection, dependency pinning, preparation/guard/cache ordering, retained scopes, decoration targets, weak ownership or fault boundaries. Runtime equivalence remains unverified while the migration does not compile.

The execution-resolution guide restores three diagrams: the selection/binding flow, parent/child nodes sharing persistent state, and inner-to-outer decoration construction. The existing budget-order diagram remains. The diagrams were inspected in the built documentation preview, including dark-theme rendering. The guide records that future design changes must update the diagrams; visualization remains part of the documentation.

Validation: baseline and refactored Release builds report the same ten CS0411 role-adapter errors, with identical error text and locations and 28 warnings. No new compiler error appears. The normal focused resolver/architecture test command remains blocked at the production build, so no runtime or architecture test result is claimed. The documentation build, `git diff --check`, whitespace, style (warning severity) and analyzer (error severity) verification pass. All three format commands report workspace-loading warnings, which limit their assurance while compilation is incomplete.

The user's staged M1a changes were preserved; these revisions remain unstaged. No Git index or commit operation was performed. Stop again for review before M1b.

### M1a wrapper vocabulary and encapsulation revision, 2026-09-27

The user approved `Wrap`, `WrapperRegistration` and `WrappedNodes`, while asking whether the remaining resolver components were necessary. Applied the names throughout production callers, normal tests, XML documentation, the glossary and current architecture guides. Historical comparison sections retain the former names explicitly as history.

- Renamed `ResolutionScopeBuilder.Decorate` to `Wrap` without a compatibility alias. Removed the public `DecorationOrigin` enum; the builder's nested-installation flag stamps internal registration metadata. Configuration/module, contextual depth and registration sequence ordering are preserved.
- Replaced the abstract/generic declaration hierarchy with one sealed `WrapperRegistration` and a typed creation helper. Its source matching, weak-key preparation ownership, sticky faults and private wrapper dependency scope retain the same rules.
- Nested immutable `WrappedNodes` under `ExecutionBindings` and retained-child bookkeeping under `ExecutionSharingScope`. Updated the architecture probes for those nested types. These support records do not constitute independent resolver components.
- Retained sharing, preparation and contextual binding as separate internal storage owners after assessing each possible merger. One sharing scope selects many preparations, one preparation supplies nodes in multiple contexts, and a retained sharing scope can appear at different effective depths. Merging the owners would still require equivalent records/caches to express those identities and lifetimes. This is an implementation judgment, not a requirement for these exact class names or file boundaries. The [design plan](execution-factory-design.md#wrapper-registration-and-internal-ownership) records the reasoning and the AOP integration boundary.
- Updated and visually checked the wrapper-chain and budget-order diagrams in the built documentation. Removed the resolved public-origin question from the backlog and corrected budget-factory XML to describe its original-source argument.

Validation: baseline and renamed Release builds report the same ten CS0411 role-adapter errors and 28 warnings. The focused normal resolver/factory/architecture/observation test command is blocked at production compilation; its compiler errors match the baseline exactly. No test result is claimed. Whitespace, style (warning severity), analyzer (error severity), documentation build and `git diff --check` pass. Format commands report workspace-loading warnings. A read-only Astra review found no actionable semantic or lifetime regression; it does not replace runtime validation. Logs are in ignored `artifacts/execution-factory/m1a-wrap-*` files.

The Git index remains unchanged. Stop at M1a for review; M1b role/base migration remains the proposed next implementation package.

### M1b role and leaf-base review record, 2026-09-29

Reviewed and committed as `01126da1` on 2026-09-30, including the reviewed one-line direct factory casts in bound role and algorithm bridges. The validation and working-tree notes below describe the original review handoff.

The user authorized this package after reviewing the proposed next step. M1b changes 38 production files: the nine operator role contracts/resolution adapters, `IAlgorithm.cs`, `Algorithm.cs`, and the unprefixed, stateless and stateful base files for all nine operator roles. The five single-item base files inherit the migrated stateless factories and require no changes to their sealed batching methods or random forks.

- Replaced the migrated object-returning creation methods with scope-free `CreateExecutionFactory` preparation returning the exact typed `ExecutionFactory<TExecution>`. The canonical role resolution adapters now pass static preparation callbacks to the real resolver. `Resolve`, `ResolveOptional`, `TryResolve`, typed scope conveniences and role operation signatures retain their call shapes.
- Kept each bound role's checked bridge and both algorithm bridges. They check run search-space/problem compatibility and the role's existing search-state variance rule before calling preparation, then adapt the returned factory's node for each binding. The bound algorithm's public factory retains its concrete `AlgorithmExecution` return family. No state allocation is moved into a bridge's binding lambda.
- Stateless bases prepare a factory returning `this`. Stateful bases create `TState` and their raw execution during preparation, then return that execution for every binding. `CreateInitialState()` and all ordinary operation overrides remain unchanged. Updated their XML ownership wording to describe one fresh state per preparation rather than per contextual node.
- Added 20 normal core regression cases in `RoleExecutionFactoryTests`: all three mutator leaf arities, parent/child observations and state continuity, child-first ownership, independent preparations, compatible problem contracts, preparation-fault identity, input-free terminator variance, interceptor invariance, and bound/agnostic algorithm bridges. Added nine discovered-role architecture cases for scope-free typed preparation and one ordinary stateful authoring spec. These are pending tests, not completed proof results.
- Updated the glossary, contributor rules, execution guides and operator authoring examples together. The pages explicitly identify wrapping/multi bases, iterative hooks, concrete implementations and Experimental roles as the remaining cutover. Existing topology-hook examples remain labelled as that migration baseline.

Validation:

- `dotnet build --configuration Release --no-restore` stops at 68 errors in unchanged production consumers: missing new interface/abstract members, obsolete overrides, and lost inherited generic constraints on those obsolete overrides (`CS0535`, `CS0534`, `CS0115`, `CS0452`). No diagnostic names an edited production file. The failure precedes complete semantic/XML validation, so this is not proof that the changed files compile independently.
- Focused core resolver/factory/role-binding tests and `OperatorAuthoringSpecs` both stop at the same 68-error production compilation barrier. No normal test compiled or ran. Full core, API usage, Experimental and scenario results remain pending; previously built binaries were not used.
- Repository whitespace, style at warning severity and analyzer verification at error severity exit successfully. All three report workspace-loading warnings; their assurance remains limited while compilation is incomplete. `npm run docs:build` and `git diff --check` pass.
- Logs are under ignored `artifacts/execution-factory/m1b-*`. The Git index's SHA256 is unchanged from package start. Changes remain unstaged and uncommitted; the unrelated `.claude/settings.local.json` is untouched.

Commands attempted for focused tests:

```powershell
dotnet test --project test/HeuristicLib.Tests/HeuristicLib.Tests.csproj --configuration Release --no-restore --filter-class '*RoleExecutionFactoryTests' --filter-class '*RoleBindingTests' --filter-class '*ResolutionScopeTests' --filter-class '*ExecutionFactoryResolutionTests'
dotnet test --project test/HeuristicLib.Tests.ApiUsageSpecs/HeuristicLib.Tests.ApiUsageSpecs.csproj --configuration Release --no-restore --filter-class '*OperatorAuthoringSpecs'
```

Stop for M1b review. Proposed M1c is the 18 core wrapping/multi configuration-base files, introducing once-only preparation hooks and contextual child resolution. Iterative algorithm hooks follow in a separate review package, then concrete implementations and consumers. These later packages must resolve the temporary compilation barrier through the reviewed preparation model; no compatibility method or old state-resetting factory replay is introduced here.

### M1c wrapping and multi-base review record, 2026-09-30

The user authorized this package after reviewing and committing M1b. This package changes the 18 `Wrapping*` and `Multi*` configuration-base files under the nine core operator roles' `Composition` directories. Matching execution bases, child properties, configuration arities, role operations and contract composition are retained.

- Each public, scope-free `CreateExecutionFactory` calls one protected preparation hook before returning its binding factory. `CreateWrapperFactory` returns a constructor receiving one typed child; `CreateCompositeFactory` returns a constructor receiving the existing ordered `ImmutableArray` of typed children. This replaces `WrapExecutionInstance` and `CombineExecutionInstances`, with no compatibility hook.
- Persistent state is allocated in the preparation hook. Child resolution and constructor invocation occur inside the returned factory using its construction scope. Multi bases preserve child order, repeated references and empty arrays. Interceptor and terminator factories forward the run's search-state type through child resolution and constructor signatures.
- The hooks return the exact role execution interface, as in the reviewed wrapper design. Derived authors can retain a more concrete matching execution base through `Func` result covariance; the authoring specs exercise both shapes. `CreateCompositeFactory` is the matching multi-base name for the reviewed constructor-preparation pattern.
- Added 15 core topology cases covering preparation before child resolution, descendant child observations and both state counters, pinned children despite a prior child-local selection, independent roots, preparation and binding fault ownership, child order, repeated references and empty children. Added 18 discovered-topology architecture cases protecting scope-free public and protected preparation. These tests use the real public resolver and remain pending until normal production compilation succeeds.
- Migrated the 14 existing topology authoring overrides in `OperatorAuthoringSpecs` and added one stateful wrapper usage spec. Updated the operator authoring guide, contributor rules, glossary and migration status together. Concrete library operators, iterative hooks, Experimental implementations and other consumer migrations remain outside M1c.

Validation:

- `dotnet build --configuration Release --no-restore` stops at 274 errors in the unmigrated consumers: 44 obsolete-override errors (`CS0115`), 172 inherited-constraint errors on those obsolete overrides (`CS0452`), 47 missing abstract hook implementations (`CS0534`) and 11 missing role factory implementations (`CS0535`). No diagnostic names an edited production base. The four warnings are analyzer release-tracking warnings (`RS2008`) in unchanged analyzer files. The increased declaration errors expose consumers of the replaced hooks; this is not complete semantic validation of the changed bases.
- Both focused test commands below stop at exactly the same 274 production errors. No normal test compiled or ran. The 34 added core/architecture/usage cases remain pending. Full core, API usage, Experimental and scenario execution is deferred until production compilation succeeds; old binaries were not used.
- Parsed all 21 changed C# files, including the new topology test file, with no syntax error. A small compiler check confirmed that a matching execution base is a valid covariant result for a `Func` override. This checks authoring syntax and the return-type mechanism, not integrated runtime behavior.
- Repository whitespace, style at warning severity and analyzer verification at error severity passed. All three report workspace-loading warnings, limiting assurance while compilation is incomplete. `npm run docs:build` and `git diff --check` passed. Logs are under ignored `artifacts/execution-factory/m1c-*`.
- The Git index's SHA256 remains unchanged from package start. No staging, unstaging, reset or commit was performed. Changes remain in the working tree; the unrelated `.claude/settings.local.json` is untouched.

Commands attempted for focused tests:

```powershell
dotnet test --project test/HeuristicLib.Tests/HeuristicLib.Tests.csproj --configuration Release --no-restore --filter-class '*TopologyExecutionFactoryTests' --filter-class '*RoleExecutionFactoryTests' --filter-class '*RoleContractArityTests' --filter-class '*ResolutionScopeTests'
dotnet test --project test/HeuristicLib.Tests.ApiUsageSpecs/HeuristicLib.Tests.ApiUsageSpecs.csproj --configuration Release --no-restore --filter-class '*OperatorAuthoringSpecs'
```

Stop for M1c review. Proposed M1d migrates the two iterative algorithm authoring forms in `Algorithms/BaseClasses/IterativeAlgorithm.cs`, using once-only iteration preparation and contextual interceptor binding. Concrete algorithms, operators, observations and consumers follow in separately reviewed groups. No later package is started here.

### M2a core concrete composition operators review record, 2026-09-30

Reviewed and committed with the named factory contracts as `3da86299` on 2026-09-30. The validation and working-tree notes below describe the original review handoff.

The user reviewed M1c and authorized committing or continuing on the same commit. M1c, including the documentation corrections, was committed as `3ba6a197`. The continuation addresses concrete operators before M1d because these operators depend only on the reviewed wrapping/multi bases. The iterative algorithm bases remain a separate package.

Production scope is the following 17 files under `src/HeuristicLib/Operators`:

- `Creators/ChooseOneCreator.cs`, `Crossovers/ChooseOneCrossover.cs`, `Mutators/ChooseOneMutator.cs`, `Refiners/ChooseOneRefiner.cs`, `Selectors/ChooseOneSelector.cs` and `Replacers/ChooseOneReplacer.cs`.
- `Mutators/PipelineMutator.cs`, `Refiners/PipelineRefiner.cs` and `Interceptors/PipelineInterceptor.cs`.
- `Terminators/AllTerminator.cs` and `Terminators/AnyTerminator.cs`.
- `Evaluators/CachingEvaluator.cs`, `Evaluators/LimitEvaluator.cs`, `Evaluators/RepeatingEvaluator.cs` and `Evaluators/RelativeQualityEvaluator.cs`.
- `Refiners/IteratedRefiner.cs` and `Selectors/NoSameMatesSelector.cs`.

Each implementation adopts `CreateWrapperFactory` or `CreateCompositeFactory`. Cache storage and the evaluation-limit counter are allocated once in preparation and captured by the returned constructor. Each binding receives newly resolved children. Cache lifetime follows the selected logical execution; bindings do not dispose shared cache storage and no resolver-wide disposal protocol is introduced.

Batch choose-one dispatchers contain only compiled index-sampling data and are prepared once. Selector/replacer dispatchers retain resolved children and therefore remain binding-local. Weight shape, nonempty choice sets, repetitions and iteration counts are validated in preparation. Child arrays, operator dispatch, RNG use, batching, aggregation, fallback behavior and existing constructor/configuration APIs are retained.

Added five regression cases through the real public factories/resolver: concrete choose-one/pipeline child rebinding, configuration validation before child resolution, shared cache hits across observation contexts, and shared evaluation limits across contexts. The cache and limit cases also check independent roots and preserve the child's advancing state. Migrated the two direct-role fixtures in `ChooseOneOperatorTests` to prepare their raw nodes once and the six direct creation calls in `RepeatingEvaluatorTests` to resolution or preparation.

Validation:

- `dotnet build --configuration Release --no-restore` stops at 166 errors in the remaining consumers, down from M1c's 274: 27 obsolete-override errors (`CS0115`), 98 inherited-constraint errors on those obsolete overrides (`CS0452`), 30 missing abstract hook implementations (`CS0534`) and 11 missing role factory implementations (`CS0535`). No diagnostic names one of the 17 edited production files. Four unchanged analyzer release-tracking warnings (`RS2008`) remain. This is limited compiler evidence while the migration is incomplete.
- Focused core tests for `EvaluatorExecutionFactoryTests`, `TopologyExecutionFactoryTests`, `ChooseOneOperatorTests` and `RepeatingEvaluatorTests`, and focused API usage specs for `OperatorAuthoringSpecs`, stop at the same 166 production errors. No normal test compiled or ran; the five added cases remain pending. Full core, API usage, Experimental and scenario execution remains deferred until production compilation succeeds. No stale binaries or isolated replacement engine were used as runtime evidence.
- Parsed all 21 changed C# files without syntax errors. A Roslyn token comparison against `3ba6a197` confirmed that all non-factory methods and constructors in the 17 production files are unchanged. This verifies the unchanged operation source, not integrated behavior or the new tests' semantic compilation.
- Repository whitespace, style at warning severity and analyzer verification at error severity passed. All three reported workspace-loading warnings, limiting assurance while compilation is incomplete. `npm run docs:build` and `git diff --check` passed. Logs and the read-only syntax/token audit are under ignored `artifacts/execution-factory/m2a-*` and `artifacts/execution-factory/migration-audit/`.
- The Git index's SHA256 remains unchanged from immediately after the authorized M1c commit. M2a is left unstaged and uncommitted for review. The unrelated `.claude/settings.local.json` is untouched.

Focused checks:

```text
dotnet test --project test/HeuristicLib.Tests/HeuristicLib.Tests.csproj --configuration Release --no-restore --filter-class '*EvaluatorExecutionFactoryTests' --filter-class '*TopologyExecutionFactoryTests' --filter-class '*ChooseOneOperatorTests' --filter-class '*RepeatingEvaluatorTests'
dotnet test --project test/HeuristicLib.Tests.ApiUsageSpecs/HeuristicLib.Tests.ApiUsageSpecs.csproj --configuration Release --no-restore --filter-class '*OperatorAuthoringSpecs'
```

Instrumentation wrappers, iterative bases, other explicit composite/leaf implementations, algorithms, observations, capabilities, Experimental roles and remaining consumers are outside this package.

Stop for M2a review. Proposed M2b migrates the 18 core counting and duration-measuring wrappers in the nine role `Instrumentation` folders, preserving their external result sinks and invocation-local timing. M1d's iterative authoring bases remain outstanding and follow separately. No later package is started here.

### Named execution factories review record, 2026-09-30

Reviewed and committed with M2a as `3da86299` on 2026-09-30. The validation and working-tree notes below describe the original review handoff.

The user approved replacing the raw `Func` signatures with the proposed named factory family. `ExecutionFactory<TExecution>` retains its scope input and covariance. New `WrapperExecutionFactory<TExecution>` and `CompositeExecutionFactory<TExecution>` accept one execution or an ordered immutable execution array and return the same execution role. Their type parameter is invariant. This supersedes M1c's optional covariant hook result; the three authoring examples using a more concrete result now declare the role interface while constructing the same execution classes.

Migrated 54 protected hook declarations in 38 existing C# files: the 18 core topology bases, M2a's 17 concrete composition operators, the topology/evaluator fixtures and the 15 topology authoring examples. Added the two public delegate files under `Execution`. Updated the 18 discovered-topology architecture cases to check the named delegate, its input topology and its matching result role. Contributor rules, operator guides, glossary and the selected design describe the named contracts. Preparation, child resolution, constructor calls and operation bodies are unchanged.

Validation:

- Before and after Release builds report exactly the same 166 errors, with identical error text and locations. Neither build reports a warning at this incremental checkpoint. No additional compiler diagnostic appears after replacing the return types.
- Focused core factory/architecture tests and `OperatorAuthoringSpecs` both stop at those same 166 production errors. No normal test compiled or ran. Integrated core, API usage, Experimental and scenario validation remains pending until the remaining consumers compile.
- A Roslyn audit verified all 54 return-type replacements against snapshots taken immediately before this change. All other declaration/body tokens in those 38 files are identical except for required namespace imports. All 43 changed C# files, including the new delegates and the existing M2a changes, parse without syntax errors. This does not establish semantic compilation of the tests or integrated runtime behavior.
- Documentation build, `git diff --check`, whitespace verification, style verification and analyzer verification passed. The formatter commands reported workspace-loading warnings; their success does not establish compilation or runtime behavior. Verification logs and the AST migration/audit helpers are under ignored `artifacts/execution-factory/named-factory-*` paths.
- The Git index remains unchanged from this package's start; the user's four staged documentation/plan files and the unrelated `.claude/settings.local.json` are untouched by index operations. New code and documentation changes remain unstaged. No commit was made.

This is an API-shape revision to M1c/M2a, not the start of M2b or M1d. Stop for review of the named factory contracts. Instrumentation wrappers and iterative bases remain separate packages.

### M2b core instrumentation wrappers review record, 2026-10-01

Reviewed and committed as `705cb4a3`. The validation and working-tree notes below describe the original review handoff.

The user reviewed and committed M2a and the named factories, then authorized this package. Migrated the 18 `Counting*` and `DurationMeasuring*` configuration classes in the nine core operator roles' `Instrumentation` folders. Each replaces `WrapExecutionInstance` with scope-free `CreateWrapperFactory`, returning the role's `WrapperExecutionFactory` and constructing the existing execution with its contextual child. Interceptor and terminator factories retain the run's search-state type.

The supplied count/duration accumulators, count metric and time provider are retained. These accumulators are external result sinks, not fresh private state allocated during preparation. Every binding and independent root configured with the same sink contributes to it. Operation bodies, constructor arguments, fluent/configuration APIs, count placement and timing are unchanged: failed child calls do not increment counters, and elapsed time is recorded in `finally` using an invocation-local timestamp.

Added three core regression cases in `OperatorInstrumentationTests`: call and candidate counts across contextual bindings, and duration accumulation across those bindings. All three check child state continuity, child-only counting in the observed scope, one child preparation across related scopes, fresh child state in an independent root and continued accumulation into the supplied result sink. Existing nested-duration and exception cases are retained. Migrated the ten obsolete direct creation calls in the two instrumentation test files to resolution, and the callback mutator fixture to once-only factory preparation. The operator composition guide describes accumulator ownership and invocation-local timing without migration history.

Validation:

- Release compilation falls from 166 to 50 errors: nine `CS0115`, 18 `CS0452`, 12 `CS0534` and 11 `CS0535`. Comparing the error text and locations shows exactly 116 removed diagnostics and no added diagnostic. No remaining error names an edited production file. The baseline reports four unchanged analyzer release-tracking warnings; the post-edit incremental build reports no warnings. This is limited compiler evidence while migration remains incomplete.
- Focused core instrumentation/topology tests and API authoring/practitioner usage specs both stop at the same 50 production errors, with identical text and locations to the post-edit Release build. No normal test compiled or ran. The three new cases remain pending, and full core, API usage, Experimental and scenario execution remains deferred until production compilation succeeds.
- The Roslyn source audit parses all 20 changed C# files without syntax errors and confirms that all non-factory methods and constructors in the 18 production files match `3da86299`. This verifies unchanged operation source, not integrated runtime behavior or semantic compilation of the tests.
- Documentation build, `git diff --check`, whitespace verification, style verification and analyzer verification pass. All three formatter commands report workspace-loading warnings, limiting assurance while compilation remains incomplete. Verification logs are under ignored `artifacts/execution-factory/m2b-*`; the existing read-only Roslyn audit is under `artifacts/execution-factory/migration-audit/`.
- The Git index remains unchanged from package start. Changes are unstaged and uncommitted; the unrelated `.claude/settings.local.json` is untouched.

Focused checks:

```text
dotnet test --project test/HeuristicLib.Tests/HeuristicLib.Tests.csproj --configuration Release --no-restore --filter-class '*OperatorInstrumentationTests' --filter-class '*EvaluatorInstrumentationTests' --filter-class '*TopologyExecutionFactoryTests'

dotnet test --project test/HeuristicLib.Tests.ApiUsageSpecs/HeuristicLib.Tests.ApiUsageSpecs.csproj --configuration Release --no-restore --filter-class '*OperatorAuthoringSpecs' --filter-class '*PractitionerUsageSpecs'
```

Stop for M2b review. M1d follows separately: migrate the two iterative algorithm authoring forms in `Algorithms/BaseClasses/IterativeAlgorithm.cs` to once-only iteration preparation and contextual interceptor binding. No iterative bases, concrete algorithms, other operator implementations, observations, Experimental roles, selector/advice work or C4 were started here.

### M1d iterative algorithm bases review record, 2026-10-01

The user reviewed and committed M2b, then authorized both iterative algorithm bases, a named iteration factory and focused preparation/interceptor/iterator coverage. The initial package added `IterationExecutionFactory.cs` beside `Algorithms/BaseClasses/IterativeAlgorithm.cs`. The delegate received a construction scope and an optional typed interceptor and returned the matching iterative execution; its four run type parameters were invariant. The common-factory revision below replaces this initial authoring shape within the same uncommitted package.

Both bases expose scope-free `CreateExecutionFactory` preparation. The agnostic base calls generic `CreateIterationFactory`; the bound base calls its nongeneric hook and exposes the matching public factory. Each returned execution factory resolves the optional interceptor inside its supplied construction scope, then invokes the prepared iteration factory. The bound public bridge preserves the existing search-space/problem compatibility guard and checks it before preparation; its direct one-line factory cast mirrors the common algorithm bridge. The sealed generic hook on the bound base retains the existing unsupported-route guard, while the sealed public bridge uses its nongeneric preparation path.

Persistent state is prepared before any binding. No child execution is captured from the first scope and no shared execution node's interceptor is changed. The complete `IterativeAlgorithmExecution` class is unchanged, including cancellation, termination, previous-state handling, RNG forks, yielding and interceptor invocation. Both bound authoring examples in `AlgorithmAuthoringSpecs` adopt the named hook; the stateful example prepares its step counter once and shares it across bindings. Three direct-role fixtures prepare their raw nodes once. Contributor rules, the algorithm authoring guide, glossary and selected design describe the implemented contracts without migration history in user-facing pages.

Added 14 core regression cases in `IterativeAlgorithmFactoryTests`: both base variants cover preparation before interceptor resolution, repeat binding versus independent factories, descendant interceptor observations with shared algorithm/interceptor state, paused iterators retaining their original observation context and previous state/RNG fork sequence, absent interceptors and preparation failure before child preparation/binding. Further cases check rejected search-space/problem/search-state contracts before preparation and a compatible concrete problem through the bound factory cast. Added two architecture cases checking the public/protected factory contracts for both bases. These 16 cases remain pending until normal compilation succeeds.

Validation:

- The Release baseline reports 50 production errors. The post-edit build reports 65: 12 `CS0115`, 30 `CS0452`, 12 `CS0534` and 11 `CS0535`. Nine prior diagnostics disappear and 24 are exposed in unmigrated concrete iterative algorithms that override the retired hooks or lack `CreateIterationFactory`. No error names either edited production file. The baseline has four unchanged analyzer release-tracking warnings; the post-edit incremental build has none. This is limited compiler evidence, not successful compilation of the migrated API or tests.
- Focused iterative factory/loop/architecture tests and `AlgorithmAuthoringSpecs` stop at the same 65 production errors, with identical text and locations to the post-edit Release build. No normal test compiled or ran. Full core, API usage, Experimental and scenario execution remains pending until the remaining consumers compile; old binaries were not used.
- A Roslyn audit parses all five changed C# files without syntax errors. It verifies that non-factory methods/constructors in the edited base match `705cb4a3`, and separately compares the complete iterative execution class and bound compatibility guard token-for-token. This checks preserved source, not the new tests' semantic compilation or integrated behavior.
- Documentation build, `git diff --check`, whitespace verification, style verification and analyzer verification pass. All three formatter commands report workspace-loading warnings, limiting assurance while compilation remains incomplete. Logs are under ignored `artifacts/execution-factory/m1d-*`; the existing read-only source audit is under `artifacts/execution-factory/migration-audit/`.
- The Git index remains unchanged from package start. Changes are unstaged and uncommitted; the unrelated `.claude/settings.local.json` is untouched.

Focused checks:

```text
dotnet test --project test/HeuristicLib.Tests/HeuristicLib.Tests.csproj --configuration Release --no-restore --filter-class '*IterativeAlgorithmFactoryTests' --filter-class '*IterativeAlgorithmExecutionTests' --filter-class '*RoleContractArityTests'

dotnet test --project test/HeuristicLib.Tests.ApiUsageSpecs/HeuristicLib.Tests.ApiUsageSpecs.csproj --configuration Release --no-restore --filter-class '*AlgorithmAuthoringSpecs'
```

Stop for M1d review. Proposed M2c migrates the four core concrete iterative algorithms: `HillClimber`, `GeneticAlgorithm`, `EvolutionStrategy` and `NSGA2`, including once-only preparation of their persistent state and stable derived configurations. Meta-algorithms, remaining operators, observations, Experimental consumers, selector/advice work and C4 remain separately reviewed packages. No later package is started here.

### M1d common execution factory revision, 2026-10-01

The user rejected the algorithm-specific delegate accepting a resolved interceptor and approved the common `ExecutionFactory<IterativeAlgorithmExecution<...>>` instead. Both protected `CreateIterationFactory` forms return this factory, receiving only the construction scope at binding time. The public methods forward directly to the preparation hook without another binding lambda. Derived algorithm authors resolve the optional interceptor alongside their other configured children and pass it to the execution constructor. Keeping the protected hook requires the matching iterative execution result. The separate delegate file is removed; no raw `Func` or compatibility route is added.

Updated all five authoring overrides in this package: three focused core fixtures and two API usage examples. State is still allocated before the returned factory; interceptor resolution remains inside it and precedes execution construction. The two architecture cases require the common factory, its single scope parameter and the matching iterative result. Contributor rules, the glossary, the algorithm authoring guide and the selected design describe this wiring responsibility. The complete iteration loop and bound compatibility guard remain unchanged. Concrete production algorithms and later migration packages are outside this revision.

The bound public-factory usage spec calls the nongeneric public method directly. It avoids reflective overload lookup, which is ambiguous when both generic and nongeneric public factories take no arguments. The focused core cases already exercise the generic bridge.

Revision validation:

- Release build and both focused test commands above stop at the same 65 production errors as the initial M1d package, with identical diagnostic text and locations. No diagnostic names the edited production base. No normal test compiled or ran; integrated runtime validation remains pending.
- The Roslyn source audit parses all four changed C# files without syntax errors and confirms the complete iterative execution class and bound compatibility guard still match `705cb4a3`. This is source preservation evidence, not semantic compilation or runtime proof.
- Documentation build, whitespace verification, style verification, analyzer verification and `git diff --check` pass. Each formatter reports workspace-loading warnings, limiting assurance while compilation remains incomplete. Revision logs are under ignored `artifacts/execution-factory/m1d-common-factory-*`.
- The Git index remains unchanged. Changes are unstaged and uncommitted, and `.claude/settings.local.json` is untouched. Stop for review of this revision; M2c has not started.

### M2c core concrete iterative algorithms review record, 2026-10-02

The user reviewed and committed M1d and its common-factory revision as `38550590`, then authorized the four core concrete algorithms: `HillClimber`, `GeneticAlgorithm`, `EvolutionStrategy` and `NSGA2`. Each protected override returns `ExecutionFactory<IterativeAlgorithmExecution<...>>` through `CreateIterationFactory`. The returned factory resolves the optional interceptor first, followed by the algorithm's other children, and constructs the matching typed execution in the supplied context.

GeneticAlgorithm and NSGA2 prepare their effective mutation-rate wrapper before returning the factory, preserving its reference identity across bindings. The configured rate comparison and wrapper weights are unchanged, including unusual-rate semantics. HillClimber and EvolutionStrategy have no additional persistent mutable state to extract; all four nested executions retain their existing constructors, state transitions, selection, replacement, termination, random-source use and invocation-local data. No execution-state class or additional factory contract is introduced.

Two private direct-role fixtures in `HillClimberTests` prepare their stateless execution references once and return them from factories. Added eight normal core regression cases in `ConcreteAlgorithmFactoryTests`: each algorithm exercises contextual creator/interceptor observations with shared creator state and independent-root preparation, plus a paused iterator retaining its original interceptor context and previous state when another context binds. These cases remain pending until normal compilation succeeds.

Validation:

- Release build errors fall from 65 to 41. The 24 removed diagnostics all belong to the four migrated algorithms; no new diagnostic appears and none names those files. The remaining errors belong to other unmigrated core consumers. This is limited compiler evidence while the core assembly still cannot build.
- Focused core algorithm tests and the algorithm authoring/practitioner API specs stop at the same 41 production errors, with identical diagnostic text and locations to the Release build. No normal test compiled or ran. Full core, Experimental and scenario execution remains pending until compilation succeeds.
- The Roslyn source audit parses all six changed C# files without syntax errors and compares every non-factory method and constructor in the four production files against `38550590`, with no differences. This confirms preserved operation source, not semantic compilation of the new tests or integrated runtime behavior.
- Documentation build, whitespace verification, style verification, analyzer verification and `git diff --check` pass. All three formatter commands report workspace-loading warnings, limiting assurance while compilation is incomplete. Logs are under ignored `artifacts/execution-factory/m2c-*`.
- The Git index hash matches package start, and the unrelated `.claude/settings.local.json` is untouched.

Focused commands:

```text
dotnet test --project test/HeuristicLib.Tests/HeuristicLib.Tests.csproj --configuration Release --no-restore --filter-class '*ConcreteAlgorithmFactoryTests' --filter-class '*HillClimberTests' --filter-class '*GeneticAlgorithmSolvingTests' --filter-class '*GeneticAlgorithmTests' --filter-class '*NSGA2Tests' --filter-class '*IterativeAlgorithmFactoryTests'

dotnet test --project test/HeuristicLib.Tests.ApiUsageSpecs/HeuristicLib.Tests.ApiUsageSpecs.csproj --configuration Release --no-restore --filter-class '*AlgorithmAuthoringSpecs' --filter-class '*PractitionerUsageSpecs'
```

Production scope is the four algorithm files. Meta-algorithms, remaining operator implementations, shared test-support migrations, Experimental consumers, selector/advice work and C4 are separate packages. Stop for M2c review; changes remain unstaged and uncommitted.

### M2d remaining core operators review record, 2026-10-02

The user reviewed and committed M2c as `cc69510c`, then authorized the eight remaining concrete core operators: `GaussianMutator`, `PredefinedCandidatesCreator`, `TransformedCreator`, `TransformedCrossover`, `RefinementEvaluator`, `ImprovementCheckingRefiner`, `EliteSelector` and `GenderSpecificSelector`. Each creation hook returns the ordinary named `ExecutionFactory` for its execution role. No additional authoring contract is introduced.

GaussianMutator prepares its raw execution and existing strength state before returning the factory, retaining the adaptable-strength capability. PredefinedCandidatesCreator allocates its existing cursor state during preparation and resolves its fallback inside the returned factory. The six other composites resolve their configured children at binding time, preserving resolution order and immutable child references. All eight retain their existing operation methods and execution constructors. The separate mutation-strength control redesign remains pending.

Existing refinement-evaluator, improvement-checking-refiner and elite-selector callers use `ResolutionScope.Resolve`. Eight added regression cases cover contextual observations on every child of the six composites, predefined-candidate cursor continuity with a contextual fallback, and Gaussian strength continuity through observed bindings. Each also checks independent-root behavior. Evaluator XML describes configuration sharing and shared state without promising identical bound nodes. These tests remain pending until normal compilation succeeds.

Validation:

- `dotnet build --configuration Release --no-restore`: errors fall from 41 to 32. All nine removed diagnostics belong to the eight migrated operators; the remaining diagnostics have identical text and locations, with none added. This is limited compiler evidence while the core assembly still cannot build.
- Both focused commands below stop at the same remaining production errors. No normal tests compile or run. Complete core, Experimental and scenario runs would hit the same barrier and are deferred.
- The Roslyn source audit parses all 14 changed C# files without syntax errors and verifies that all non-factory methods and constructors in the eight production files match `cc69510c`. This establishes source preservation, not semantic compilation or runtime behavior.
- Whitespace, style at warning severity, analyzer verification at error severity and `git diff --check` pass. The format tools report workspace-loading warnings, limiting assurance while compilation remains incomplete. Logs are under ignored `artifacts/execution-factory/m2d-*`.
- The Git index is unchanged; changes remain unstaged and uncommitted. The unrelated `.claude/settings.local.json` is untouched.

Focused commands:

```text
dotnet test --project test/HeuristicLib.Tests/HeuristicLib.Tests.csproj --configuration Release --no-restore --filter-class '*ConcreteOperatorFactoryTests' --filter-class '*PredefinedCandidatesCreatorTests' --filter-class '*AdaptableMutationStrengthTests' --filter-class '*RefinementEvaluatorTests' --filter-class '*ImprovementCheckingRefinerTests' --filter-class '*EliteSelectorTests' --filter-class '*SelectorCompositionTests'

dotnet test --project test/HeuristicLib.Tests.ApiUsageSpecs/HeuristicLib.Tests.ApiUsageSpecs.csproj --configuration Release --no-restore --filter-class '*InferenceConstructionSpecs'
```

Stop for M2d review. Deferred algorithms, control wrappers, observations, remaining test-support migrations, Experimental consumers, specialized control capabilities, selector/advice work and C4 remain separate packages.

### M2e deferred algorithms and control wrappers review record, 2026-10-02

The user reviewed M2d and authorized its commit and continuation. It is committed as `c82c6d69`. This package migrates the six core algorithms named before editing: `CycleAlgorithm`, `PipelineAlgorithm`, `StateTerminatedAlgorithm`, `AlgorithmDurationBudgetAlgorithm`, `OperatorBudgetAlgorithm` and `OperatorDurationBudgetAlgorithm`. All public preparation methods return the ordinary typed `ExecutionFactory`.

Cycle removes its per-binding cache of wrapped child executions. Its deferred resolver uses a fresh child domain for each reset-mode activation, or a retained domain keyed by the child algorithm reference for reuse mode. Repeated references share a reuse slot; equal but distinct references and different Cycle preparations have independent slots. A fresh activation still inherits state already owned by an ancestor. Pipeline retains its existing fresh child activation scopes and delayed child resolution. Neither streaming loop changes its state handoff, random forks, cycle/stage order, cancellation checks or yielding behavior.

Count and operator-duration budgets allocate their accumulators and private child keys during preparation, then obtain their retained child domains through each binding's construction scope. Rebinding neither reinstalls the measurement declaration nor resets usage. Algorithm-duration budgets preserve invocation-local duration tracking. StateTerminatedAlgorithm resolves its terminator before the wrapped algorithm at binding time. All four control-wrapper streaming loops and constructors are unchanged. No iterator resources or resolved children are added to persistent state.

Migrated the seven authoring hooks in AdditiveStepAlgorithm, the shared meta-algorithm test helpers, and the private fixtures in CycleAlgorithmTests, CycleAlgorithmAnalysisTests and StateTerminatedAlgorithmTests. The terminator-order test uses public scope resolution; its private recording terminator constructs the requested typed execution directly. The meta-algorithm guide describes factory preparation/binding and fresh/retained domains, with no migration-history wording. It states the current analyzer limitation without promising factory-call enforcement.

Added 14 normal regression cases in DeferredAlgorithmFactoryTests: both Cycle modes across repeated references and invocations, reference identity and owner isolation, ancestor reuse, fresh Pipeline stages with no upward state publication, paused Cycle/Pipeline observation paths, count/duration budget rebinding with single measurement declarations and independent roots, nested count/duration budgets, shared state termination, and invocation-local duration under a paused stream. These cases remain pending until normal compilation succeeds.

Validation:

- `dotnet build --configuration Release --no-restore`: errors fall from 32 to eight. All 24 removed diagnostics belong to the six migrated production files. The remaining diagnostics have identical text and locations; none is added. The remaining failures belong to AlgorithmObservation and the crossover, evaluator, interceptor and mutator observation adapters. This remains limited compiler evidence while the core assembly cannot build.
- Both focused commands below stop at those same eight production errors. No normal tests compile or run. Complete core, Experimental and scenario validation is deferred while the production barrier remains.
- The Roslyn source audit parses all 12 changed C# files and the guide's five C# code blocks without syntax errors. Non-factory methods and constructors in the six production files match `c82c6d69`, except for Cycle's deliberately replaced private resolver/cache helpers and its execution constructor's removed cache allocation. All six complete streaming loops remain compared and unchanged. This is source preservation evidence, not semantic compilation or runtime proof.
- `npm run docs:build`, whitespace verification, style verification at warning severity, analyzer verification at error severity and `git diff --check` pass. Format tools report workspace-loading warnings, limiting assurance while compilation remains incomplete. Logs are under ignored `artifacts/execution-factory/m2e-*`.
- The Git index hash matches the start of M2e. M2e changes are unstaged and uncommitted; the unrelated `.claude/settings.local.json` is untouched.

Focused commands:

```text
dotnet test --project test/HeuristicLib.Tests/HeuristicLib.Tests.csproj --configuration Release --no-restore --filter-class '*DeferredAlgorithmFactoryTests' --filter-class '*CycleAlgorithmTests' --filter-class '*PipelineAlgorithmTests' --filter-class '*OperatorBudgetAlgorithmTests' --filter-class '*DurationBudgetTests' --filter-class '*StateTerminatedAlgorithmTests'

dotnet test --project test/HeuristicLib.Tests.ApiUsageSpecs/HeuristicLib.Tests.ApiUsageSpecs.csproj --configuration Release --no-restore --filter-class '*CompositionSpecs' --filter-class '*BudgetSpecs' --filter-class '*AlgorithmAuthoringSpecs'
```

Stop for M2e review. The next proposed package is the five core observation adapter files: AlgorithmObservation, CrossoverObservation, EvaluatorObservation, InterceptorObservation and MutatorObservation. Remaining test-support/consumer migrations, Experimental code, specialized control capabilities, selector/advice work and C4 remain separate packages. No observation adapter or later package is changed here.

### M2f core observation adapters review record, 2026-10-02

The user approved M2e as `90c1b961` and authorized the proposed observation package. The current `GetOrCreateChildScope` design remains unchanged. Migrated these five files:

- `src/HeuristicLib/Analysis/Tracing/Observations/AlgorithmObservation.cs`
- `src/HeuristicLib/Analysis/Tracing/Observations/CrossoverObservation.cs`
- `src/HeuristicLib/Analysis/Tracing/Observations/EvaluatorObservation.cs`
- `src/HeuristicLib/Analysis/Tracing/Observations/InterceptorObservation.cs`
- `src/HeuristicLib/Analysis/Tracing/Observations/MutatorObservation.cs`

Each adapter returns the common typed execution factory. The existing signature check runs during preparation, and the returned factory resolves the predecessor through the current binding scope. Algorithm observation prepares its `ExecutionState` once, retaining iteration numbering across bindings. Its previous search state remains invocation-local. All five typed operation bodies, callbacks, original-source references and `Fits` checks are unchanged. Public observation registration overloads are unchanged.

Added five cases in `ObservationFactoryTests`: algorithm iteration continuity across rebinding, a paused parent stream retaining its own child observations and previous state, independent-root counters, and predecessor binding/source attribution/result identity/callback order for all four operator observation roles. The algorithm assertions cover those lifetime cases together. Also corrected the missing `HEAL.HeuristicLib.Operators.Terminators` import in M2e's `DeferredAlgorithmFactoryTests`, exposed when those tests first compiled.

Validation:

- The baseline Release solution build stopped at eight errors in the five observation adapters. After migration, `dotnet build --configuration Release --no-restore` compiles the core library and reports 106 errors in remaining consumers: 33 in Experimental and 73 in the core test project. These are newly reachable migration diagnostics, not an increase in core errors. Later compilation phases may expose further issues.
- `dotnet build src/HeuristicLib/HeuristicLib.csproj --configuration Release --no-restore` passes. The normal focused core test command below fails during compilation of old test authoring fixtures. The normal API usage command is blocked by Experimental compilation. Neither normal suite runs; complete core, Experimental and scenario validation remains pending.
- An ignored MSBuild source-selection file compiles the actual core test project against the actual core library, including `ObservationFactoryTests`, `ObservationModuleTests`, `AlgorithmObservationTests`, `DeferredAlgorithmFactoryTests`, normal global usings and their three existing mock/helper files. All 41 selected cases pass, including all five new cases and all 14 M2e deferred-algorithm cases. The selected compilation excludes the other core tests; it is limited runtime evidence, not a successful normal-project or full-suite run. No alternate production implementation, tracked project edit or expansion of the earlier C2/C3 proofs is involved.
- Whole-solution whitespace verification, style verification at warning severity, analyzer verification at error severity and `git diff --check` pass. Format tools report workspace-loading warnings, limiting assurance for unbuildable consumers. No diagnostics refer to the migrated production files or the new test file. Existing floating-point comparison warnings occur in unchanged AlgorithmObservationTests.
- Logs and the temporary source-selection file are under ignored `artifacts/execution-factory/m2f-*`. The Git index hash remains `1A976BF27D7119E43BB502C9AD7FA8E452BB01E0ABF82F83F91E52C1BB23CBE9`. Changes remain unstaged and uncommitted; `.claude/settings.local.json` is untouched.

Commands:

```text
dotnet test --project test/HeuristicLib.Tests/HeuristicLib.Tests.csproj --configuration Release --no-restore --filter-class '*ObservationFactoryTests' --filter-class '*ObservationModuleTests' --filter-class '*AlgorithmObservationTests' --filter-class '*DeferredAlgorithmFactoryTests'
dotnet test --project test/HeuristicLib.Tests.ApiUsageSpecs/HeuristicLib.Tests.ApiUsageSpecs.csproj --configuration Release --no-restore
dotnet test --project test/HeuristicLib.Tests/HeuristicLib.Tests.csproj --configuration Release --no-restore -p:CustomAfterMicrosoftCommonTargets=C:/Users/phili/Repositories/HeuristicLib/artifacts/execution-factory/m2f-selected-tests.targets
```

Stop for M2f review. The next proposed package migrates remaining core test authoring fixtures and their direct-creation consumers, then retries the normal core suite. The first exposed fixture files are AnalysisUsabilityTests, TraceCompositionTests, AlgorithmRunTests, ExperimentTestSupport, CompositeBindingTests, OperatorCompatibilityTests, RefinerFailureTests and the seven creator/crossover/evaluator/mutator/refiner/replacer/selector configuration-equality test files. Identify the final bounded scope before editing; later compiler phases may expose additional callers. Experimental code, specialized control capabilities, selector/advice work and C4 remain separate packages.

### M2g core test fixtures and callers review record, 2026-10-03

The user approved M2f as `c4d91e2f` and authorized migrating the remaining core test authoring fixtures and direct callers, then retrying the normal core suite. This package changes 26 C# files under `test/HeuristicLib.Tests` and these two plans. No production, analyzer implementation, Experimental source or project file changes are included.

Scope:

- Fourteen fixture files: AnalysisUsabilityTests, TraceCompositionTests, AlgorithmRunTests, ExperimentTestSupport, CompositeBindingTests, OperatorCompatibilityTests, RefinerFailureTests and the seven creator/crossover/evaluator/mutator/refiner/replacer/selector configuration-equality test files. Creation hooks return `ExecutionFactory`, `WrapperExecutionFactory` or `CompositeExecutionFactory` as appropriate. CompositeBindingTests retain both the deliberately invalid leaf-ladder fixture and the correctly typed generic fixture. Children resolve inside the returned binding lambda. Probe setup recording and setup exceptions remain in preparation. RefinerFailureTests prepares its private counter once per logical execution rather than retaining mutable runtime state on the reusable configuration.
- Six existing regression files: IterativeAlgorithmFactoryTests, TopologyExecutionFactoryTests, EvaluatorExecutionFactoryTests and OperatorInstrumentationTests specify the role interface in `Wrap<TSource>`, allowing the callback to return another implementation of that role. ConcreteAlgorithmFactoryTests and AdaptableMutationStrengthTests import the extension namespaces for their existing counting calls.
- Five refiner caller files: ImprovementCheckingCompositionTests (one call), RefinerBatchSemanticsTests (five), RefinerCompositionTests (21), RefinerEvaluatorAccountingTests (one) and NumericParameterFittingRefinerTests (three). All 31 direct creation calls use the public `ResolutionScope.Create().Resolve<TCandidate, TSearchSpace, TProblem>(source)` route, preserving their concrete run types and fresh root scope per construction.
- ExecutionFactoryResolutionTests.Lifetimes changes only the collection helper. The initial full run and a focused rerun both failed the two `LongLivedSource_DoesNotRetainDiscardedDeclarationDomains` cases. Three bounded collection/finalization cycles followed by a final collection make both pass. Weak-reference assertions are unchanged, and no resolver behavior or retained-object condition is relaxed. This establishes that additional cleanup cycles suffice in this runtime; it does not establish a particular internal finalization cause.

Existing typed operations and assertions are preserved. The old-name references remaining in the core test sources are analyzer/code-fix example strings, analyzer test names and intentional architecture assertions rejecting retired members. Migrating analyzer semantics and those examples is a separate package; removing their compilation assertions would conceal the outstanding work.

Validation:

- The baseline normal core test build reported 73 errors and 33 warnings. Migrating fixture hooks exposed 54 errors in later compilation phases. After correcting the direct callers, role inference and missing imports, the final normal Release core test project build passes with zero errors and 25 warnings. Existing warning categories include Sonar and xUnit diagnostics; explicit collection in the lifetime helper retains its GC warning. No warning suppression is added.
- The focused normal core test run with the single class filter `*ExecutionFactoryTests` passes all 40 cases. The focused lifetime rerun passes both cases after the cleanup-helper correction.
- The initial full normal core run executed 2,457 cases, with 2,444 passing and 13 failing. The final full run executes the same 2,457 cases, with 2,446 passing, 11 failing and none skipped. The failures are eight CreateExecutionInstanceAnalyzerTests and two CreateExecutionInstanceCodeFixTests whose embedded source uses retired hooks, plus LayerDependencyTests.SourceDependencies_HaveOnlyTheDocumentedException. The architecture test fails while compiling Experimental sources (60 semantic diagnostics), before it can validate dependencies. No layer violation is established by that failure.
- `dotnet build --configuration Release --no-restore` reports 33 errors, all in unmigrated Experimental declarations. The normal API usage test command encounters the same 33 errors and does not run tests. Later consumer compilation phases remain unverified. Experimental, scenario and final solution runtime validation remain pending.
- Whole-solution whitespace verification, style verification at warning severity, analyzer verification at error severity and `git diff --check` pass. Format tools report workspace-loading warnings, limiting assurance for unbuildable consumers.
- Every runtime result above uses the normal core test project and actual core library, without source selection, skipped failures or an alternate production implementation. An ignored Roslyn tool under `artifacts/execution-factory/m2g-call-migration/` rewrote only the 31 inspected invocation nodes; it is an editing aid, not a runtime proof. Validation logs are under ignored `artifacts/execution-factory/m2g-*`. The Git index SHA256 remains `6B326D26E3AA6751F60B870FDEEA9E08F379658E24AE01CE0AB716C2CD4743D4`. Changes remain unstaged and uncommitted; `.claude/settings.local.json` is untouched.

Commands:

```text
dotnet build test/HeuristicLib.Tests/HeuristicLib.Tests.csproj --configuration Release --no-restore
dotnet test --project test/HeuristicLib.Tests/HeuristicLib.Tests.csproj --configuration Release --no-restore --no-build --filter-class '*ExecutionFactoryTests'
dotnet test --project test/HeuristicLib.Tests/HeuristicLib.Tests.csproj --configuration Release --no-restore --filter-method '*LongLivedSource_DoesNotRetainDiscardedDeclarationDomains'
dotnet test --project test/HeuristicLib.Tests/HeuristicLib.Tests.csproj --configuration Release --no-restore
dotnet test --project test/HeuristicLib.Tests.ApiUsageSpecs/HeuristicLib.Tests.ApiUsageSpecs.csproj --configuration Release --no-restore
dotnet build --configuration Release --no-restore
```

Stop for M2g review. The next proposed package migrates HLib0001, its code fix and their executable examples to the factory contract: recognize direct child factory creation/invocation, including returned lambdas and deferred methods, preserve legitimate self/base bridges and resolver adapters, and compile the fixed source against the actual library. Experimental migration remains separate and is required to unblock the remaining architecture case and API usage specs. Specialized control capabilities, selector/advice work and C4 remain later packages. None is started here.

### M2h execution-factory analyzer and code fix review record, 2026-10-03

The user reviewed M2g, authorized a commit and authorized continuing with the next error-fixing package. Committed the already-staged M2g changes as `3b95f8d5`, then migrated the analyzer package without further index changes. Experimental implementation remains outside this package.

Scope:

- Replace CreateExecutionInstanceAnalyzer with `ExecutionFactoryAnalyzer`, retaining the diagnostic ID `HLib0001`. The rule binds the actual invocation: an instance, parameterless `CreateExecutionFactory` returning the common library factory on a configuration contract. Construction context includes factory-returning methods and their lambdas/local functions, methods with a library resolution-scope parameter, and execution-node methods, including deferred activation. Consumer-defined configuration and execution contracts participate through the common contracts, without a built-in role list. Unrelated methods with matching names and deliberate calls outside construction are not diagnosed.
- Preserve implicit, explicit `this` and `base` preparation bridges. A child sharing its holder's type is still diagnosed. Typed `ResolutionScope.Resolve`/`TryResolve` preparation adapters can call the hook on their own callback source parameter; calling a different child inside that adapter remains a bypass.
- Replace CreateExecutionInstanceCodeFixProvider with `ExecutionFactoryCodeFixProvider`. Immediate factory invocation, including parenthesized delegates and explicit `Invoke`, becomes typed resolution on the supplied scope expression. A hook directly returning a child's factory becomes `scope => scope.Resolve<...>(child)`. The common generic configuration contract supports inferred resolution; role fixes use the execution type arguments and names valid at the call site. Generated fixes are semantically checked before being offered. Stored factories receive a diagnostic without a partial rewrite; custom roles without a usable resolver receive no uncompilable fix.
- Replace both old analyzer/code-fix fixture files with ExecutionFactoryAnalyzerTests and ExecutionFactoryCodeFixTests. The 18 analyzer cases cover generic/bound hooks, direct and stored preparation, local functions, scope resolution, outside callers, self/base delegation, explicit bridges, same-type children, deferred methods, preparation adapters, custom contracts and unrelated methods. The 11 code-fix cases compile the input and fixed output against the actual library, including deferred scope expressions, four-argument roles, consumer-defined contracts/resolvers and withheld fixes. Assertions that detect compilation errors and analyzer exceptions remain active. Two source examples in OperatorAuthoringAnalyzerTests use factory hooks while preserving their original HLib0002/HLib0003 assertions.
- Update writing-meta-algorithms.md to describe the current HLib0001 coverage and its limits. The rule recognizes direct calls; it is not a provenance analysis of arbitrary helper/delegate flows and does not prove correct shared-state ownership. No runtime resolver or algorithm operation changes are included.

Validation:

- The normal Release core test project build passes with zero errors. Its final build reports 54 existing warnings across rebuilt projects. An initially introduced loop-style warning in the fixer was corrected; no new warning remains in the changed implementation. HLib0001 retains the analyzer project's existing release-tracking warning when that project rebuilds.
- Focused normal-project runs pass all 18 analyzer cases and all 11 code-fix cases. The final full core run, after simplifying the generated binding lambda, executes 2,476 cases: 2,475 pass, one fails, none are skipped. All ten M2g analyzer/code-fix failures are resolved. The remaining failure is LayerDependencyTests.SourceDependencies_HaveOnlyTheDocumentedException, which stops while compiling unchanged Experimental sources before it can validate dependency rules.
- `npm run docs:build`, whole-solution whitespace verification, style verification at warning severity, analyzer verification at error severity and `git diff --check` pass. Format tools report workspace-loading warnings, limiting assurance for unbuildable consumers.
- The final Release solution build reports 33 errors, all in unmigrated Experimental declarations. The normal API usage test command encounters the same 33 errors and does not run tests. These are the same remaining declaration failures as M2g; later consumer compilation and Experimental/scenario runtime validation remain pending.
- Runtime results use the normal project and actual library; no source selection, alternate implementation or skipped failure is used. Logs are under ignored `artifacts/execution-factory/m2h-*`. The post-commit Git index SHA256 remains `0BCAF6A133CE57514E2B1C1BB2EA5F34F4D9947816D738DE93FCEB735C91D833`. M2h changes remain unstaged and uncommitted; `.claude/settings.local.json` is untouched.

Commands:

```text
dotnet build test/HeuristicLib.Tests/HeuristicLib.Tests.csproj --configuration Release --no-restore
dotnet test --project test/HeuristicLib.Tests/HeuristicLib.Tests.csproj --configuration Release --no-restore --no-build --filter-class '*ExecutionFactoryAnalyzerTests'
dotnet test --project test/HeuristicLib.Tests/HeuristicLib.Tests.csproj --configuration Release --no-restore --filter-class '*ExecutionFactoryCodeFixTests'
dotnet test --project test/HeuristicLib.Tests/HeuristicLib.Tests.csproj --configuration Release --no-restore --no-build
dotnet build --configuration Release --no-restore
dotnet test --project test/HeuristicLib.Tests.ApiUsageSpecs/HeuristicLib.Tests.ApiUsageSpecs.csproj --configuration Release --no-restore
npm run docs:build
```

Stop for M2h review. The next proposed package migrates 14 Experimental role/operator files: the three move-role interfaces, their six ordinary/stateless bases, Swap2Neighborhood, CompositeSearchSpace, DynamicCachingEvaluator, DynamicRelativeQualityEvaluator and ReevaluationInterceptor. Preserve typed operations, prepare persistent data once and bind children through the current scope. The three Experimental algorithms (ALPSGeneticAlgorithm, OpenEndedRelevantAllelesPreservingGeneticAlgorithm and DynamicRacingAlgorithm) remain separate packages; normal Experimental/API/scenario compilation may expose further consumers after their declarations migrate. Specialized control capabilities, selector/advice work and C4 remain later work. None of those implementations is changed here.

### M2i Experimental role and operator consumers review record, 2026-10-03

The user reviewed M2h, authorized committing it and continuing with the proposed Experimental role/operator package. Committed the already-staged M2h changes as `1998a0a2`, then left the index untouched. The proposed 14-file inventory contains 13 active implementations: Swap2Neighborhood is entirely commented-out legacy code and is not changed or revived.

Migrated:

- IMoveCreator, IMoveEvaluator and IMoveApplier return the common `ExecutionFactory` and their typed resolution adapters prepare it through the normal resolver overload. Optional/try resolution, variance, type arguments and typed operations are unchanged.
- MoveCreator, MoveEvaluator and MoveApplier prepare their existing execution node and initial state once, then return that node for every binding. StatelessMoveCreator, StatelessMoveEvaluator and StatelessMoveApplier return factories binding to `this`. All six retain their existing virtual public hook, protected state/operation hooks, signature checks and explicit generic bridges. The bridges cast the typed factory, matching core leaf bases; no replacement creation API or compatibility shim is added.
- CompositeSearchSpace's creator, crossover and mutator return factories that resolve both adapted child signatures per binding. Child resolution order, the outer execution-type check and its exception, no-problem adaptation, result construction and the mutator's `All` option are unchanged. These configurations have no separate persistent mutable data to extract.
- DynamicCachingEvaluator and DynamicRelativeQualityEvaluator use named wrapper factories, preparing the cache/hit count and the best-known reference/epoch once respectively. Their typed evaluation bodies, problem-identity checks, normalization, grace-count and cache-invalidation logic are unchanged. ReevaluationInterceptor prepares its pending request counter once and resolves its evaluator per binding; transformation and atomic request consumption are unchanged.

DynamicCachingEvaluator and ReevaluationInterceptor subscribe once during preparation. Their handlers are methods on the small state objects, rather than lambdas sharing the binding factory's closure. This avoids introducing a path from the problem event to a captured configuration or child. Existing weak-reference assertions for discarded execution nodes and children remain unchanged. Subscription cleanup and state/cache disposal retain the existing lifecycle limitation and remain follow-up work; this package does not claim to solve that ownership problem.

Added eight cases in the normal Experimental test project: three stateful move-role cases for preparation count, ancestor/child continuity and independent roots; two composite creator cases for adapted child rebinding/cursor continuity/observation context and incompatible outer search-space rejection; and three dynamic cases for cache/epoch sharing, best-known reference sharing and shared reevaluation requests with independent-root isolation. A small unrestricted search-space fixture supports the move/composite cases. These are authored regression cases, not executed evidence: the normal project remains blocked before test compilation by the remaining algorithm declarations. No existing test assertions are weakened or skipped.

Validation:

- The baseline normal Release Experimental build reports 33 errors. After migration it reports 16 errors, all in ALPSGeneticAlgorithm, OpenEndedRelevantAllelesPreservingGeneticAlgorithm and DynamicRacingAlgorithm (including its nested performance evaluator). No error points to a migrated declaration. Remaining declaration errors prevent normal compiler validation of later consumer phases.
- The focused normal core LayerDependencyTests run executes 14 cases: 13 pass and SourceDependencies_HaveOnlyTheDocumentedException fails while compiling Experimental source. Its complete semantic diagnostic list contains 36 errors, all in those same three algorithm files, and none in the 13 migrated production files. This supplies source-level semantic evidence for the migrated files, not a green Experimental project, successful layer validation or runtime proof of the new behavior.
- The full normal core run executes 2,476 cases: 2,475 pass, one fails, none are skipped. The same Experimental-dependent architecture failure remains. The normal Experimental test command fails during dependency compilation and runs no tests; the eight new cases and existing dynamic lifetime cases remain unexecuted.
- Whole-solution whitespace verification, style verification at warning severity, analyzer verification at error severity and `git diff --check` pass. Format tools report workspace-loading warnings, limiting assurance for unbuildable consumers.
- The final normal Release solution build reports the same 16 Experimental declaration errors. The normal API usage test command encounters those 16 errors and does not run tests. Experimental tests, API usage specs, later consumer compilation and final scenario/integration validation remain pending; the absence of diagnostics in the migrated production files is not complete cross-project validation.
- All runtime results use normal projects and the actual library. No source selection, alternate production implementation or temporary runtime proof is used. Logs are under ignored `artifacts/execution-factory/m2i-*`. The post-commit Git index SHA256 remains `E66FB825D74FA6511F24BEF8A73AA02157A5D4F86ACD832945A9F1C0C84D4DB4`. M2i changes remain unstaged and uncommitted; `.claude/settings.local.json` is untouched.

Commands:

```text
dotnet build src/HeuristicLib.Experimental/HeuristicLib.Experimental.csproj --configuration Release --no-restore
dotnet test --project test/HeuristicLib.Tests/HeuristicLib.Tests.csproj --configuration Release --no-restore --no-build --filter-class '*LayerDependencyTests'
dotnet test --project test/HeuristicLib.Tests/HeuristicLib.Tests.csproj --configuration Release --no-restore
dotnet test --project test/HeuristicLib.Tests.Experimental/HeuristicLib.Tests.Experimental.csproj --configuration Release --no-restore
```

The final broader checks also use `dotnet build --configuration Release --no-restore` and `dotnet test --project test/HeuristicLib.Tests.ApiUsageSpecs/HeuristicLib.Tests.ApiUsageSpecs.csproj --configuration Release --no-restore`.

Stop for M2i review. The next proposed package migrates the two Experimental genetic algorithms: ALPSGeneticAlgorithm and OpenEndedRelevantAllelesPreservingGeneticAlgorithm. Prepare stable generated operators and persistent algorithm data once, resolve the interceptor and other children inside the returned common execution factory, and preserve invocation-local iteration data. DynamicRacingAlgorithm, its nested performance evaluator and its deferred contenders remain a separate package. Remaining test/API usage/scenario consumers, subscription lifecycle work, selector/advice work and C4 remain later work. No algorithm or later consumer migration is started here.

### M2j Experimental genetic algorithms review record, 2026-10-03

M2i was reviewed and committed as `3af2e5cb`. The user authorized the next bounded package: ALPS and OpenEndedRelevantAllelesPreservingGeneticAlgorithm. Dynamic Racing remains a separate migration and review package.

Scope and ownership:

- Both genetic algorithms implement the scope-free `CreateIterationFactory` hook returning the common `ExecutionFactory<IterativeAlgorithmExecution<...>>`. Each returned factory resolves the optional interceptor and all other declared children in its requesting context.
- ALPS prepares its effective mutation-rate configuration once, outside the returned factory, following the core GeneticAlgorithm pattern. Rebinding resolves the same prepared wrapper reference, preserving child selection and observation context.
- Neither algorithm has mutable node-owned fields to extract. Generation populations, ages, selection buffers and offspring remain operation-local values or public search states. Generation limits, refinement, elitism, strictness, random draws and population algorithms are unchanged; paused iterators retain their original execution nodes and children.
- Four new Experimental cases cover root/child/root creator state continuity, contextual creator/interceptor counts, independent roots and paused iterator progress for both algorithms. They are authored but have not compiled or run. Existing generation-budget, refinement and usage cases remain unchanged and pending normal Experimental compilation.

Validation:

- The baseline normal Release Experimental build reports 16 declaration errors. The migrated project reports four errors, all in DynamicRacingAlgorithm and its nested PerformanceTrackingEvaluator. No declaration error points to either migrated algorithm. Remaining declaration failures still prevent later-phase normal compiler validation.
- The full normal core suite executes 2,476 cases: 2,475 pass, one fails, none are skipped. SourceDependencies_HaveOnlyTheDocumentedException fails while compiling Experimental source; its complete semantic diagnostic list contains the same four errors, all in DynamicRacingAlgorithm. There are no semantic diagnostics in the two migrated algorithms. This is source-level evidence, not a green Experimental build, successful layer validation or runtime proof of the new cases.
- The focused Experimental test command stops during dependency compilation at those four errors and runs no tests. M2i's eight new cases, M2j's four new cases and existing Experimental algorithm regressions await normal-project compilation.
- The normal Release solution build and API usage test command stop at the same four Experimental errors. API usage specs run no tests. Later consumer compilation and final scenario/integration validation remain pending.
- Whole-solution whitespace verification, style verification at warning severity, analyzer verification at error severity and `git diff --check` pass. Format tools report workspace-loading warnings, limiting assurance for unbuildable consumers.
- Logs are under ignored `artifacts/execution-factory/m2j-*`. All checks use normal projects and actual library sources; no temporary source selection or alternate runtime implementation is used. M2j stays unstaged and uncommitted. The post-M2i-commit index SHA256 is `3EB9B0A01FC9C437D382111A87AB55705B04E163FDBB51B412E60395526CCDC8`; `.claude/settings.local.json` remains untouched.

Commands:

```text
dotnet build src/HeuristicLib.Experimental/HeuristicLib.Experimental.csproj --configuration Release --no-restore
dotnet test --project test/HeuristicLib.Tests/HeuristicLib.Tests.csproj --configuration Release --no-restore
dotnet test --project test/HeuristicLib.Tests.Experimental/HeuristicLib.Tests.Experimental.csproj --configuration Release --no-restore --filter-class '*GeneticAlgorithmFactoryTests'
```

Stop for M2j review. The next proposed package migrates DynamicRacingAlgorithm, its nested performance evaluator and its deferred contenders. Prepare persistent performance statistics and stable generated configurations once, bind interceptors and direct children per context, and preserve deferred contender ownership and invocation-local progress. Remaining test/API usage/scenario consumers, subscription lifecycle work, selector/advice work and C4 remain later work. Dynamic Racing and later consumers are not changed here.

### M2k Dynamic Racing review record, 2026-10-03

M2j was reviewed and committed as `e692802c`. The user authorized the Dynamic Racing package and requested squashing the small migration checkpoints into one `migrate` commit after the migration part is complete; see the pending history-cleanup note above. No history rewrite is performed here.

Scope and ownership:

- DynamicRacingAlgorithm implements the common scope-free `CreateIterationFactory`. Its existing ExecutionState is prepared once, preserving the incumbent, incumbent algorithm configuration, hall of fame and completed race/epoch counters across contextual bindings and separating independent roots.
- The returned factory binds the optional interceptor, meta creator and meta mutator and passes its construction frame to the execution for deferred contender construction. The frame is binding-local; shared state retains no resolved child or scope.
- PerformanceTrackingEvaluator returns the named common execution factory and resolves its child evaluator within the requesting context. Its performance observer remains explicitly owned by one contender Entry, including when a completed contender iterator is restarted.
- Contender creation and iterator restart retain fresh child scopes with per-contender wrapper registrations. No scope is added to shared state and no global performance observer is introduced. Race entries, performance models, event handlers, active contender iterators, random inputs and progress remain activation/invocation-local; winner selection, burn-in, early termination, hall-of-fame logic and merger behavior are unchanged.
- Two new cases cover one/two contenders, one-generation contender restarts, a prebound shared contender, preserved incumbent/burn-in state across rebinding, independent roots, contextual evaluator/interceptor counts and resuming an outer iterator after a child binding is used. These cases are authored but have not compiled or run; normal Experimental test compilation stops in older fixture declarations.

Validation:

- The preceding checkpoint has four Experimental production errors, all in Dynamic Racing. The normal Release Experimental library build succeeds with zero errors and 41 warnings, including existing Experimental warnings and unused event-handler-parameter warnings in M2i. No production compatibility shim is added. A source scan finds no active retired execution-creation hooks in `src`; the only match is the entirely commented-out Swap2Neighborhood file.
- The full normal core suite passes all 2,476 cases, with no failures or skips, including the previously blocked source-level layer check. This proves the migrated production sources compile semantically and the existing core suite passes; it does not supply runtime proof for the pending Experimental cases.
- The normal Experimental test project build reports six errors in AccumulatingAnalyzerTests.SketchEvaluator, AlgorithmRefinementTests.CountingRefiner and DynamicAnalysisTests.BatchEvaluationAlgorithm. The focused DynamicRacingFactoryTests command stops at the same declaration failures and runs no tests. The fourteen new Experimental cases from M2i/M2j/M2k and existing Experimental algorithm/dynamic lifetime cases still await normal-project test compilation.
- The normal solution build reports 28 declaration errors: six in Experimental tests, eighteen in API usage specs and four in the scenario CycleAlgorithmAnalysisScenarios fixture. These consumers remain outside this package; later compiler phases may expose additional callers once those declarations are migrated. Integrated Experimental, API usage, scenario and C4 validation remain pending.
- Whole-solution whitespace verification, style verification at warning severity, analyzer verification at error severity and `git diff --check` pass. Format tools report workspace-loading warnings, limiting assurance for unbuildable test consumers.
- Logs are under ignored `artifacts/execution-factory/m2k-*`. All checks use normal projects and actual library sources. No temporary source selection or alternate runtime implementation is used. M2k remains unstaged and uncommitted; the post-M2j-commit index SHA256 is `B7C3166E741EDE0A085F9CD26D2DF81AD5A18555C629F12609D1E416862C528C`. `.claude/settings.local.json` remains untouched.

Commands:

```text
dotnet build src/HeuristicLib.Experimental/HeuristicLib.Experimental.csproj --configuration Release --no-restore
dotnet build test/HeuristicLib.Tests.Experimental/HeuristicLib.Tests.Experimental.csproj --configuration Release --no-restore
dotnet test --project test/HeuristicLib.Tests/HeuristicLib.Tests.csproj --configuration Release --no-restore
dotnet build --configuration Release --no-restore
dotnet test --project test/HeuristicLib.Tests.Experimental/HeuristicLib.Tests.Experimental.csproj --configuration Release --no-restore --filter-class '*DynamicRacingFactoryTests'
```

Stop for M2k review. The next proposed package migrates the remaining Experimental test fixtures and callers, then runs the normal Experimental suite including all new factory and dynamic lifetime cases. API usage specs and the scenario fixture follow as separate consumer packages. Complete migration validation precedes the requested history squash. Subscription lifecycle work, selector/advice work and C4 remain later work.

### M2l Experimental test consumers review record, 2026-10-03

M2k was reviewed and committed as `b2d9a87f`. The user authorized the next bounded package: remaining Experimental test fixtures and callers, followed by normal-project validation of the pending factory and lifetime cases. API usage specs and the scenario fixture remain separate consumer packages.

Scope:

- AccumulatingAnalyzerTests.SketchEvaluator and AlgorithmRefinementTests.CountingRefiner expose the typed execution factory. Their scoring/refinement operations and existing counter probe are unchanged.
- DynamicAnalysisTests.BatchEvaluationAlgorithm returns a factory that resolves its evaluator inside the requesting context; batching, iteration and progress assertions are unchanged.
- The Experimental test project imports Instrumentation for CountAccumulator. GeneticAlgorithmFactoryTests imports interceptor instrumentation extensions, and DynamicRacingFactoryTests imports evaluator/interceptor instrumentation extensions. Normal compilation exposed these missing imports once the retired fixture declarations were migrated.
- No assertions are weakened, no tests are skipped and no new tests are added in this package. Production code, API usage specs and scenario fixtures are unchanged. A source scan finds no retired creation hooks in Experimental test sources.

Validation:

- The initial normal Experimental test build advances past the six old declaration errors and exposes eleven missing CountAccumulator import errors, followed by four missing role-instrumentation extension import errors. After the import fixes, the normal Release test project builds with zero errors and 21 warnings.
- The focused factory-class run passes all eleven cases from MoveExecutionFactoryTests, CompositeExecutionFactoryTests, GeneticAlgorithmFactoryTests and DynamicRacingFactoryTests.
- The focused DynamicEvaluationCacheTest run passes all thirteen cases, including the three new dynamic rebinding cases and both existing weak-reference lifetime cases. Lifetime assertions and collection helpers are unchanged.
- The full normal Experimental suite passes all 192 cases, with no failures or skips. All fourteen previously pending factory regressions from M2i/M2j/M2k have compiled and run against the actual library. Existing generation-budget, refinement, dynamic analysis and Experimental usage cases pass as part of that suite.
- The normal solution build reports 22 remaining declaration errors: eighteen in API usage specs and four in CycleAlgorithmAnalysisScenarios. Experimental compilation is no longer a blocker. Later compiler phases may expose additional API usage/scenario callers after those declarations migrate; final solution, API usage and scenario runtime validation remain pending.
- M2k's full normal core run passed all 2,476 cases; core production and tests are unchanged in this package, so that suite is not repeated here.
- Whole-solution whitespace verification, style verification at warning severity, analyzer verification at error severity and `git diff --check` pass. Format tools report workspace-loading warnings, limiting assurance for the remaining unbuildable consumer projects.
- Logs are under ignored `artifacts/execution-factory/m2l-*`. All runtime results use normal projects and actual production sources, without source selection or temporary alternative implementations. M2l remains unstaged and uncommitted. The post-M2k-commit index SHA256 is `2CB51CEBC328899FDC529650E9CE25C3B1873F225546A93FCD1506F14A9BF39C`; `.claude/settings.local.json` is untouched.

Commands:

```text
dotnet build test/HeuristicLib.Tests.Experimental/HeuristicLib.Tests.Experimental.csproj --configuration Release --no-restore
dotnet test --project test/HeuristicLib.Tests.Experimental/HeuristicLib.Tests.Experimental.csproj --configuration Release --no-restore --no-build --filter-class '*FactoryTests'
dotnet test --project test/HeuristicLib.Tests.Experimental/HeuristicLib.Tests.Experimental.csproj --configuration Release --no-restore --no-build --filter-class '*DynamicEvaluationCacheTest*'
dotnet test --project test/HeuristicLib.Tests.Experimental/HeuristicLib.Tests.Experimental.csproj --configuration Release --no-restore
dotnet build --configuration Release --no-restore
```

Stop for M2l review. The next proposed package migrates API usage authoring fixtures and direct creation calls, then runs the normal API usage suite. The scenario fixture follows as the last identified consumer package. Complete migration validation precedes the requested history squash; the reminder remains active. Subscription lifecycle work, selector/advice work and C4 remain later work.

### M2m API usage consumers review record, 2026-10-03

M2l was reviewed and committed as `21eaff60`. The user authorized the API usage migration package. The remaining scenario fixture and final migration validation follow separately; the history-squash reminder remains active.

Scope:

- NodeSelectionSpecs.NamedMutator implements `CreateWrapperFactory` returning the named `WrapperExecutionFactory`. Its factory returns the supplied child execution unchanged, preserving the naming example's shared-child identity assertions. Selection semantics and selector/resolver integration are unchanged.
- Six forwarding configurations in OperatorAuthoringSpecs return typed execution factories: evaluator, selector, replacer, interceptor, terminator and crossover. Child resolution happens inside each returned factory. Their operations and execution bases are unchanged.
- Eight direct retired-creation calls in evaluator, replacer, interceptor and terminator topology specs use canonical `ResolutionScope.Resolve` instead. Each retains its independently created scope and original operation inputs and assertions.
- Existing named topology factory examples and ordinary stateful leaf hooks were already migrated and remain unchanged. No API usage setup or assertions are removed, weakened or skipped. No production or scenario source is changed; a source scan finds no retired execution-creation hooks in the API usage project.

Validation:

- The normal Release API usage project builds with zero errors and 43 warnings across dependencies and specs. All 193 normal API usage specs pass, with no failures or skips, including authoring, topology, node selection and usage flows.
- The normal solution build reports four remaining declaration errors, all in CycleAlgorithmAnalysisScenarios.SingleStepAlgorithm. API usage and Experimental consumers compile. Later scenario compiler phases may expose additional callers after that declaration migrates; final solution and scenario runtime validation remain pending.
- M2k's full normal core run passed all 2,476 cases and M2l's full Experimental run passed all 192 cases. Those projects and production sources are unchanged here, so the suites are not repeated in this package.
- Whole-solution whitespace verification, style verification at warning severity, analyzer verification at error severity and `git diff --check` pass. Format tools report workspace-loading warnings, limiting assurance for the remaining unbuildable scenario project.
- Logs are under ignored `artifacts/execution-factory/m2m-*`. All results use normal projects and actual library sources, without source selection or alternative implementations. M2m remains unstaged and uncommitted. The post-M2l-commit index SHA256 is `B163EDAFF72394013CE42E40022C786DF718049EF84D94045B7BB5E6699186EC`; `.claude/settings.local.json` remains untouched.

Commands:

```text
dotnet build test/HeuristicLib.Tests.ApiUsageSpecs/HeuristicLib.Tests.ApiUsageSpecs.csproj --configuration Release --no-restore
dotnet test --project test/HeuristicLib.Tests.ApiUsageSpecs/HeuristicLib.Tests.ApiUsageSpecs.csproj --configuration Release --no-restore
dotnet build --configuration Release --no-restore
```

Stop for M2m review. The next proposed package migrates CycleAlgorithmAnalysisScenarios.SingleStepAlgorithm and any scenario callers exposed by compilation, then performs final migration validation through the normal solution and scenario suite. Complete migration validation precedes the requested squash of the small migration commits into one `migrate` commit. Subscription lifecycle work, selector/advice work and C4 remain later work.

### M2n Scenario consumer and final migration validation review record, 2026-10-03

M2m was reviewed and committed as `64fb4ed3`. The user authorized the remaining scenario fixture and final migration checks. The migration is complete at this review checkpoint; history cleanup remains pending review/commit of M2n.

Scope:

- CycleAlgorithmAnalysisScenarios.SingleStepAlgorithm returns a common typed execution factory and resolves its evaluator and interceptor in the requesting context. Its operation, streaming loop, inputs and assertions are unchanged; it owns no mutable persistent data to extract.
- No additional scenario consumers are exposed by normal compilation. The source scan over `src`, `test`, `samples` and `docs` finds no active retired execution-creation hooks. Remaining textual matches are the entirely commented-out Swap2Neighborhood file and architecture assertions forbidding the retired API.
- No production source, test assertions, skips or external-prerequisite checks change. The full solution validation runs against actual projects and library sources.

Validation:

- Normal solution restore succeeds with all projects up to date. The normal Release solution build succeeds with zero errors and 93 warnings. All samples and consumers included in the solution compile.
- The focused CycleAlgorithmAnalysisScenarios run passes both cases, preserving analyzer observations with fresh and retained child scopes.
- The complete normal Release solution test run passes all 2,885 cases across core, Experimental, API usage and scenarios, with zero failures and zero reported skips. This includes the previously pending factory, dynamic lifetime, authoring and scenario checks. The four projects each report success; the complete run takes about 30 seconds.
- AutoEcPaperScenarioTests.DynamicRacingGa_OnActivatedTsp_UsesPaperLikeScenario retains its pre-existing early return when local `eil51.tsp`/Concorde files are absent. Those prerequisites are absent along its lookup path, so the passing suite does not prove that external-tool workflow ran. The in-memory Dynamic Racing cases and Moving Peaks paper scenario execute in the normal suite. C4 performance/final acceptance evidence remains separate.
- Whole-solution whitespace verification, style verification at warning severity, analyzer verification at error severity and `git diff --check` pass. Format tools still report workspace-loading warnings; the normal solution build and complete runtime suite supply the compilation/runtime evidence independently.
- Logs are under ignored `artifacts/execution-factory/m2n-*`. No source selection or alternate runtime implementation is used. M2n remains unstaged and uncommitted. The post-M2m-commit index SHA256 is `01C94C3CB2EFAFED41F4C5768EB8590749BB054B063C345FCB0B6D11F908623B`; `.claude/settings.local.json` remains untouched.

Commands:

```text
dotnet restore
dotnet build --configuration Release --no-restore
dotnet test --project test/HeuristicLib.Tests.Scenarios/HeuristicLib.Tests.Scenarios.csproj --configuration Release --no-restore --no-build --filter-class '*CycleAlgorithmAnalysisScenarios'
dotnet test --configuration Release --no-restore
```

Stop for M2n review. After review and commit, perform the requested squash of the migration range described above into one coherent `migrate` commit, with a recoverable backup and verified tree equality. No squash or later design work starts here. Subscription lifecycle work, selector/advice work and C4 remain outstanding after migration cleanup.

### Documentation and enforcement changes with implementation

Update developer guidelines § 4.1, § 4.3, § 4.4, § 4.7, § 4.12, § 4.14 and § 4.17 together: reusable configuration, once-only preparation, repeatable typed binding, persistent versus binding-local data, protected preparation hooks and validation phases. Keep the ordinary stateful-leaf restriction on graph dependencies. Explain why execution factories are runtime machinery rather than persisted value strategies.

The glossary records Configuration node and Execution node as the agreed terms now, while explicitly retaining current API/state facts. Migrate existing API references and role terminology in the naming commit. Update Execution state, Execution graph, Resolution scope and Wrapper chain behavior descriptions with the factory/resolution commit. Review wording on [execution nodes](../docs/contributing/architecture/execution-nodes.md), [execution resolution](../docs/contributing/architecture/execution-resolution.md), [operator implementation](../docs/contributing/architecture/operator-implementation.md), [writing algorithms](../docs/guide/extending/writing-algorithms.md) and [writing meta-algorithms](../docs/guide/extending/writing-meta-algorithms.md). Budget factory XML documentation must no longer promise the previous generated configuration as its callback input after that behavioral change.

Revise HLib0001/code-fix tests and [role contract architecture tests](../test/HeuristicLib.Tests/Architecture/RoleContractArityTests.cs). Recognizable misuse should be diagnosed without treating every mutable field as an error or pretending that nullable annotations need defensive runtime guards. No analyzer or generator is required to make a valid factory work.

### Validation commands at rollout

Use the repository's commands and suite order; builds/tests run sequentially to avoid output-file locks. The following is the final integration scope, not a requirement to repeat every check after each review package:

```powershell
dotnet restore
dotnet build --configuration Release --no-restore
dotnet test --project test/HeuristicLib.Tests/HeuristicLib.Tests.csproj --configuration Release --no-restore
dotnet test --project test/HeuristicLib.Tests.ApiUsageSpecs/HeuristicLib.Tests.ApiUsageSpecs.csproj --configuration Release --no-restore
dotnet test --project test/HeuristicLib.Tests.Experimental/HeuristicLib.Tests.Experimental.csproj --configuration Release --no-restore
dotnet test --configuration Release --no-restore
dotnet format whitespace ./HEAL.HeuristicLib.slnx --verify-no-changes --no-restore
dotnet format style ./HEAL.HeuristicLib.slnx --verify-no-changes --no-restore --severity warn
dotnet format analyzers ./HEAL.HeuristicLib.slnx --verify-no-changes --no-restore --severity error
git diff --check
```

The individual suites run as their packages become ready; the full solution test command is the single final broad pass, not an extra loop after every change. Include the current documentation build command when public docs change, and inspect its local package scripts before running it. Preserve XML documentation verification with a clean final build where incremental compilation could suppress diagnostics. Record existing unrelated failures separately from regressions; a green job does not prove formatting clean.

## Outcome criteria

The chosen design must avoid pervasive author ceremony, hidden state reset, per-call service lookup, unclear iterator ownership and unjustified costs. Report any conflict with these requirements before weakening custom-role support or narrowing the selected scope. Returning to option A would be a new decision, not the default outcome of an incomplete proof.

A complete result supplies explicit ownership, understandable author examples, a typed end-to-end proof through a reused composite, lifecycle evidence, measured costs and a reviewed migration plan. The graph/role naming family is approved. That does not approve the remaining proposed helper APIs, universal state objects, mandatory state interfaces or removal of authoring capabilities before their replacement is proved.
