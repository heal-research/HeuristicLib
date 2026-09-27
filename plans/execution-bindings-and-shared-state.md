# Design execution factories and shared state

Status: implementation authorized on 2026-09-27. The user reviewed and staged the naming migration, then explicitly authorized committing it and continuing; it is committed as `d5ad7fb2`. C2 was completed and validated locally. The selected direction is option C using typed execution factories (candidate 1 in the [design comparison](execution-bindings-design-investigation.md)), superseding option A in the [container and aspect rework](container-and-aspect-framing.md#case-5-a-shared-composite-retains-its-dependency-bindings). End the turn for explicit review after C2, C3 and C4, then after each reviewed migration package. C4 must finalize the library-wide migration scope before rollout. Leave further index and commit operations to the user unless explicitly authorized.

C1 has a [concrete design](execution-factory-design.md), including authoring examples, resolver ownership, retained child scopes, decoration construction, capabilities and failure behavior. C2 validated its core authoring examples locally. C3 proof and C4 cost evidence remain outstanding; the migration sequence must be finalized from their results before rollout.

Naming decision, 2026-09-27: adopt the [graph naming family](execution-factory-design.md#agreed-naming-family): `IConfigurationNode`, `IExecutionNode` and role-specific `...Execution` types, retaining ordinary algorithm/operator configuration names. The [commit boundaries](#agreed-commit-boundaries) separate a behavior-preserving rename from the factory/resolution rework. The naming migration is the first implementation review package; it retains the existing object-returning creation methods and behavior.

## Problem and current decision

An outer scope P constructs algorithm G, which retains its resolved mutator M. A child scope C adds an observer of M and reuses G. G continues to call its existing M binding, so C's additional observer does not receive those calls. Modifying G would affect its other callers; recreating G through today's factory may reset execution state.

The 2026-09-26 decision chose option A: retain a reused composite's existing bindings and reject reuse that would bypass a required child observation. On 2026-09-27 the user selected option C instead: preserve logical execution state while constructing scope-specific typed bindings, including a reused composite's child bindings. Do not implement option A's composite-reuse guard as an intermediate prerequisite. Silent observer omission remains unacceptable; any unsupported case during a future migration must have an explicit reviewed policy.

The design must remove that composite limitation while preserving parent-first state reuse, independent siblings without ancestor state, child-first ownership and no hoisting. Keep ordinary configuration users and stateless/stateful leaf authors insulated from factory/state machinery. Show the necessary cooperation for non-terminal operator and algorithm authors concretely, and keep it as small as possible.

## Selected direction and open design

A configuration prepares a typed factory once per logical execution. The factory binds typed execution nodes using persistent execution data and the requesting binding's child/observer context. Resolution still returns a fully constructed execution node. A child must be able to obtain another G binding that reaches both outer and inner observations while retaining the same logical G and M state. Parent callers keep their original bindings. The common/role naming family, `ExecutionFactory` and `CreateExecutionFactory` are agreed; detailed delegate shapes, protected authoring hooks and cache structures still require review.

The [design comparison](execution-bindings-design-investigation.md) supplies candidate sketches and source evidence. Its explicit-rebind and factory-replay alternatives remain comparison material, not parallel APIs to implement. The lambda return form was compiled successfully in a small standalone compiler check; that confirms C# target typing, not the proposed execution model.

Plan the work in two steps: first produce and review the concrete design in C1; then use the reviewed design and focused proof results to write a detailed implementation/migration plan. Do not invent a file-by-file migration before the contracts and ownership rules are settled.

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

Implementation follows the explicit review stops authorized on 2026-09-27: naming first, then C2, C3, C4 and the reviewed migration packages. C0's decision alignment and C1's concrete design are recorded. Do not begin the next package until the user explicitly continues after review. If a requirement proves infeasible, present the conflict and revisit the decision explicitly rather than silently falling back to option A.

| Package | Deliverable | Validation and review stop |
| --- | --- | --- |
| C0. Plan and decision alignment | Select typed-factory option C; supersede option A in the earlier plan and backlog; update package 4c dependencies and mark the old review prompt historical. | Documentation consistency and link/diff checks. No runtime changes. |
| C1. Concrete factory design | Specify the typed factory and authoring-base contracts, ownership/cache rules, failure publication, observation ordering and capability access using the agreed graph/role names. Show before/after stateless/stateful leaves, a stateful non-terminal operator, a two-role consumer composite, HillClimber/GeneticAlgorithm, and Cycle/Pipeline. Separate persistent, binding-local and invocation-local fields. Resolve or explicitly bound every question above. | Review complete typed call paths and all acceptance cases, author ceremony and required guideline changes. The naming family is settled; remaining signatures and semantics require design review. |
| C2. Small typed proof | Prove the reviewed contract with a stateful leaf, a consumer-defined two-child composite and a representative algorithm. Include richer capability preservation and the child-first dependency collision. Avoid a full role-family migration. | Focused sharing/observation/failure tests and executable API usage specs, Release build and appropriate core tests. Review before expansion; a typed syntax check alone is insufficient. |
| C3. Deferred/lifecycle proof | Prove both Cycle modes, Pipeline, retained iterators, budget/advice ownership and late child binding. Define migration handling for unsupported existing factories. | Focused lifecycle/budget tests; core/API suites, experimental consumers when affected and a selected workflow scenario. Preserve RNG behavior and paused invocation identity. |
| C4. Cost evidence and migration plan | Compare with the recorded pre-change baseline. Use the results and reviewed design to write bounded migration packages for resolver/contracts, authoring bases, algorithms/compositions, observations/instrumentation, consumers and documentation. Distinguish structural edits from behavior changes. | Review measured costs and the detailed implementation plan. Do not automatically turn a successful proof into a library-wide rollout. Final migration validation includes affected suites, formatting/analyzers, docs and one full solution run. |

Follow [AGENTS.md](../AGENTS.md) and [test/README.md](../test/README.md): focused tests first, full core after meaningful core changes, API usage specs for authoring changes, experimental tests for affected consumers and scenarios when validating broad workflows. Use repository Release build, formatting and analyzer commands. A substantial public execution-model change requires a full solution test run near completion, not repeated scenario runs during sketches.

## Detailed execution plan

### C1 deliverable and review

Review [the concrete design](execution-factory-design.md) as one coherent contract. Its D1-D7 table identifies the consequential proposals: pinned dependency continuity, preparation hooks, retained child scopes, original-source decoration recipes, explicit controls, sticky construction failures and iterator ownership. Review the complete examples before approving executable work. The factory direction itself is already selected.

In particular, changing `Decorate` to receive the original source and introducing a construction-time control projection are supporting API changes, not consequences that should be hidden inside a resolver implementation. There is no proposal to make handwritten wrappers transparently implement arbitrary interfaces. Binding-time persistent initialization that requires a child's live value is outside the initial contract; no current production need has been established for its additional helper.

### C2: typed core proof

Build the smallest isolated proof of the reviewed contract before changing every public role. Use a test-only namespace/project or internal candidate engine exercised by tests, keeping it out of the public API. Reuse the eventual resolver implementation where practical; do not mistake a disconnected hand-simulated state dictionary for a proof of the actual binding design. Do not create a permanent second resolution workflow.

1. Implement typed preparation, reference-identity logical selection, explicit construction frames, dependency pinning and view-owned binding caches. Include failed preparation and re-entrant construction states from the beginning.
2. Implement stable generated-decoration occurrences and predecessor binding with the reviewed `Decorate` semantics. Cover ordering, original-source attribution and exclusion of generated wrappers before adding selector integration.
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

### C4: cost evidence and rollout decision

Record the exact pre-change revision plus any relevant working-tree state, SDK/runtime, build configuration, machine and benchmark source. Do not stage, commit or reset to obtain a baseline. Run the baseline in a separate suitable checkout or exported source snapshot after checking existing worktree attachments. Keep all measurements reproducible; do not add a permanent benchmark framework without a concrete need.

Measure these cases separately:

- First preparation/resolution and warm repeated resolution for stateless leaves, stateful leaves and composites.
- Additional binding with no new declarations, a leaf observer, and an observer only on a descendant.
- Cheap operation throughput with zero, one and several wrappers; no factory invocation or scope lookup may enter the steady-state operation path.
- Allocations per node, binding and declaration; retained bytes after many short Cycle/Pipeline activations, both reset and reuse modes.
- Real HillClimber/GA execution and short repeated algorithm invocations; include the one-time construction delegates and typed-scope views in total costs.

Use warmups, repeated samples and allocation measurements, report variance and distinguish equivalent baseline workloads from option C's newly supported composite case. Do not assign an arbitrary acceptable percentage without seeing the cheap-operation and memory results. If costs conflict with the design goals, present the measured tradeoff and an explicit design revision before rollout. C4 completes the migration package boundaries below using the actual proof diff and compile dependencies.

## Provisional migration scope

This is a source-grounded migration inventory and sequence, not an approved library-wide edit. A current search for `CreateExecutionInstance` or `WrapExecutionInstance` finds 106 main-library source files, 17 Experimental source files, 36 test/spec files and two analyzer files; these counts include textual matches and are not a count of concrete factories. Samples and PythonInterop still require build validation even when they have no direct authoring-method match. Refresh this inventory at C4.

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

The C2/C3 proofs and C4 review still precede the final rollout decision. The following M0-M3 work packages subdivide preparation and review of commit 2; they are not instructions to create four additional commits. State extraction and internal resolver work stay out of the naming commit. No commit or index operation is authorized by recording these boundaries.

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

The focused typed-factory proof was completed and validated locally: 31 invariant tests and eight executable authoring specs passed. It established the planned core state-preservation, scoped-observation, identity, capability and failure cases. The production factory/resolution migration has not started. C3 remains the next review-gated step and requires explicit continuation.

### Factory/resolution work packages to finalize at C4

1. **M0: prepare existing authored state without changing the public workflow.** Where independently useful, extract counters/caches from explicit composite instances into private data holders and pass them to existing constructors. Existing factories still create one fresh holder per existing instance. Split core composites and Experimental algorithms into separate review packages. Test behavior preservation; avoid unrelated moves/renames.
2. **M1: land the proved internal resolver machinery.** Keep it internal and exercise it directly through the candidate tests until the public cutover. This is a temporary implementation seam, not a supported alternative execution API. Include explicit ownership, occurrence lifetime, construction failure and child-domain behavior. Review its diff independently of routine authoring conversions.
3. **M2: perform the public contract cutover and all dependent migrations.** Changing interface return contracts necessarily affects implementations, typed bridges, role extensions, wrappers, algorithms, test doubles and analyzer expectations together. Prepare/review those facets as separate coherent diff groups, but publish a complete compiling package. Do not promise an independently building per-role sequence that the interfaces do not permit. The code must be repeatably bindable at this point; wrapping the old state-resetting factory in a lambda is insufficient. Wire the proved engine, update current docs and remove the temporary proof seam.
4. **M3: final integration and validation.** Reconcile all R1-R16 cases against the public route, run the full affected suites and final solution checks, repeat only benchmarks materially affected by the final adapter/code layout, remove prototype-only duplication and complete the documentation audit. Resume container/aspect package 4c.2 selector registration only after this foundation is reviewed.

M2 is unavoidably wider than the individual proof packages because it is a breaking contract replacement. C4 must use the compiled proof to minimize and accurately bound that package. If further subdivision would require parallel public APIs or unsupported non-rebindable factories, bring that tradeoff to review instead of adding compatibility scaffolding or an option-A fallback silently. No merge/commit or index operation is part of these packages unless explicitly requested.

### Documentation and enforcement changes with implementation

Update developer guidelines § 4.1, § 4.3, § 4.4, § 4.7, § 4.12, § 4.14 and § 4.17 together: reusable configuration, once-only preparation, repeatable typed binding, persistent versus binding-local data, protected preparation hooks and validation phases. Keep the ordinary stateful-leaf restriction on graph dependencies. Explain why execution factories are runtime machinery rather than persisted value strategies.

The glossary records Configuration node and Execution node as the agreed terms now, while explicitly retaining current API/state facts. Migrate existing API references and role terminology in the naming commit. Update Execution state, Execution graph, Resolution scope and Decoration chain behavior descriptions with the factory/resolution commit. Review wording on [execution nodes](../docs/contributing/architecture/execution-nodes.md), [instance resolution](../docs/contributing/architecture/execution-resolution.md), [operator implementation](../docs/contributing/architecture/operator-implementation.md), [writing algorithms](../docs/guide/extending/writing-algorithms.md) and [writing meta-algorithms](../docs/guide/extending/writing-meta-algorithms.md). Budget factory XML documentation must no longer promise the previous generated configuration as its callback input after that behavioral change.

Revise HLib0001/code-fix tests and [role contract architecture tests](../test/HeuristicLib.Tests/Architecture/RoleContractArityTests.cs). Recognizable misuse should be diagnosed without treating every mutable field as an error or pretending that nullable annotations need defensive runtime guards. No analyzer or generator is required to make a valid factory work.

### Validation commands at rollout

Use the repository's commands and suite order; builds/tests run sequentially to avoid output-file locks. The following is the final rollout scope, not a request to execute it for this documentation draft:

```powershell
dotnet restore
dotnet build --configuration Release --no-restore
dotnet test test/HeuristicLib.Tests --configuration Release --no-restore
dotnet test test/HeuristicLib.Tests.ApiUsageSpecs --configuration Release --no-restore
dotnet test test/HeuristicLib.Tests.Experimental --configuration Release --no-restore
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
